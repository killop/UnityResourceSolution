using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using URS;
namespace YooAsset
{
    internal class HostPlayModeImpl : IFileSystemServices
    {
        // 补丁清单
        //internal PatchManifest AppPatchManifest;
        //internal PatchManifest LocalPatchManifest;

        internal FileManifest WebFileManifest;
        internal BundleManifest WebBundleManifest;

        internal FileManifest AppFileManifest;
        internal BundleManifest AppBundleManifest;
        internal string AppId;

        public BundleManifest GetJudgedBundleManifest()
        {
            if (WebBundleManifest != null)
            {
                return WebBundleManifest;
            }
            var sanboxBundleManifest = SandboxFileSystem.GetBundleManifest();
            if (sanboxBundleManifest != null)
            {
                return sanboxBundleManifest;
            }
            return AppBundleManifest;
        }
        public FileManifest GetJudgedFileManifest()
        {
            if (WebFileManifest != null)
            {
                return WebFileManifest;
            }
            var sanboxFileManifest = SandboxFileSystem.GetFileManifest();
            if (sanboxFileManifest != null)
            {
                return sanboxFileManifest;
            }
            return AppFileManifest;
        }
        // 参数相关
        internal bool ClearCacheWhenDirty { private set; get; }
        internal bool IgnoreResourceVersion { private set; get; }
        internal string RemoteAppToChannelRouterFileUrl { get; set; }
        internal string RemoteVersionsRootUrl { get; set; }
        internal string RemoteVersionRootUrl { get; set; }
        internal string RemoteVersionPatchUrl { get; set; }
        internal AppToChannelItem AppChannel {get;set;}
        internal URSFilesVersionIndex FilesVersionIndex { get; set; }


        /// <summary>
        /// 异步初始化
        /// </summary>
        public InitializationOperation InitializeAsync()
		{
			ClearCacheWhenDirty = true;
			IgnoreResourceVersion = true;
            RemoteAppToChannelRouterFileUrl = $"{URSRuntimeSetting.instance.RemoteChannelRootUrl}/{URSRuntimeSetting.instance.RemoteAppToChannelRouterFileName}"; ;
			var operation = new HostPlayModeInitializationOperation(this);
            OperationSystem.ProcessOperaiton(operation);
			return operation;
		}

		public void InitVersion(AppToChannelItem item)
		{
            AppChannel= item;
            RemoteVersionsRootUrl = $"{URSRuntimeSetting.instance.RemoteChannelRootUrl}/{item.ChannelId}/{URS.PlatformMappingService.GetPlatformPathSubFolder()}";
            RemoteVersionPatchUrl = $"{RemoteVersionsRootUrl}/{URSRuntimeSetting.instance.PatchDirectory}";
        }

		public string GetRemoteFilesVersionIndexUrl() 
		{
			return $"{RemoteVersionsRootUrl}/{URSRuntimeSetting.instance.FilesVersionIndexFileName}";
		}
        public void InitFilesVersion(URSFilesVersionIndex versions)
        {
            FilesVersionIndex = versions;
            var item = FilesVersionIndex.GetVersion(AppChannel.VersionCode);
            RemoteVersionRootUrl = $"{RemoteVersionsRootUrl}/{item.VersionCode}---{item.FilesVersionHash}";
        }

        public PatchItemVersion GetPatch(FileMeta localfileMeta, FileMeta remoteFileMeta) 
        {
            return FilesVersionIndex.GetPatch(localfileMeta, remoteFileMeta);
        }

        /// <summary>
        /// 异步更新补丁清单
        /// </summary>
        public UpdateManifestOperation UpdatePatchManifestAsync(int updateResourceVersion, int timeout)
		{
			var operation = new HostPlayModeUpdateManifestOperation(this, updateResourceVersion, timeout);
            OperationSystem.ProcessOperaiton(operation);
			return operation;
		}

		

		/// <summary>
		/// 创建下载器
		/// </summary>
		public RemoteUpdateOperation CreateDownloaderByTags(string[] tags, int fileLoadingMaxNumber, int failedTryAgain)
		{
			List<UpdateEntry> downloadList = GetDownloadListByTags(tags);
			var operation = new RemoteUpdateOperation(downloadList, fileLoadingMaxNumber, failedTryAgain);
			return operation;
		}
		private List<UpdateEntry> GetDownloadListByTags(string[] tags)
		{
			List<UpdateEntry> downloadList = new List<UpdateEntry>(1000);
            if (WebFileManifest != null) 
            {
                foreach (var kv in WebFileManifest.GetFileMetaMap())
                {
                    var remoteFileMeta = kv.Value;
                    if (!remoteFileMeta.HasAnyTag(tags)) continue;
                    var relativePath = kv.Key;

                    FileMeta sandboxCandicate = null;
                    // 忽略缓存文件
                    if (SandboxFileSystem.TryGetHardiskFilePath(relativePath, out var __, out var sandboxFileMeta))
                    {

                        if (sandboxFileMeta.Hash == remoteFileMeta.Hash)
                        {
                            continue;
                        }
                        else
                        {
                            sandboxCandicate = sandboxFileMeta;
                        }
                    }
                    else
                    {
                        //UnityEngine.Debug.LogError("can not find relativePath" + relativePath);
                    }

                    // 忽略APP资源
                    // 注意：如果是APP资源并且哈希值相同，则不需要下载

                    if (AppFileManifest != null && AppFileManifest.ContainFile(relativePath, remoteFileMeta.Hash))
                    {
                        continue;
                    }
                    UpdateEntry updateEntry = GetUpdateEntry(remoteFileMeta, sandboxCandicate);
                    downloadList.Add(updateEntry);
                }
            }
			
			return downloadList;
		}

       

		/// <summary>
		/// 创建下载器
		/// </summary>
		public RemoteUpdateOperation CreateDownloaderByPaths(List<string> assetPaths, int fileLoadingMaxNumber, int failedTryAgain)
		{
			List<UpdateEntry> downloadList = GetDownloadListByPaths(assetPaths);
			var operation = new RemoteUpdateOperation(downloadList, fileLoadingMaxNumber, failedTryAgain);
			return operation;
		}
		private List<UpdateEntry> GetDownloadListByPaths(List<string> assetPaths)
		{
            // 获取资源对象的资源包和所有依赖资源包
            var bundleManifest = GetJudgedBundleManifest();
            Dictionary<string, FileMeta> all= new Dictionary<string, FileMeta>();
            //List<FileMeta> dependency = new List<FileMeta>();
            foreach (var assetPath in assetPaths)
			{
				var mainFileMeta = bundleManifest.GetBundleFileMeta(assetPath);
				if (mainFileMeta!=null&& !all.ContainsKey(mainFileMeta.RelativePath))
				{
                    all.Add(mainFileMeta.RelativePath,mainFileMeta);
                }

				var  dependBundleFileMetas = bundleManifest.GetAllDependenciesRelativePath(assetPath);
                if (dependBundleFileMetas != null && dependBundleFileMetas.Count > 0)
                {
                    for (int i = 0; i < dependBundleFileMetas.Count; i++)
                    {
                        var fm = dependBundleFileMetas[i];
                        if (!all.ContainsKey(fm.RelativePath))
                        {
                            all.Add(fm.RelativePath, fm);
                        }
                    }
                }
			}

            List<UpdateEntry> results = new List<UpdateEntry>();

            foreach (var kv in all)
            {
                var remoteFileMeta = kv.Value;
                FileMeta sanboxCandicate = null;
                if (SandboxFileSystem.TryGetHardiskFilePath(remoteFileMeta.RelativePath, out var hardiskPath, out var sanboxFileMeta))
                {
                    if (sanboxFileMeta.Hash == remoteFileMeta.Hash)
                    {
                        continue;
                    }
                    else
                    {
                        sanboxCandicate = sanboxFileMeta;
                    }
                }
                if (AppFileManifest.ContainFile(remoteFileMeta))
                {
                    continue;
                }
                results.Add(GetUpdateEntry(remoteFileMeta, sanboxCandicate));
            }
            
			return results;
		}

		

		// WEB相关
		internal string GetRemoterPatchDownloadURL(string fileName)
		{
            return $"{RemoteVersionPatchUrl}/{fileName}";
        }
        internal string GetRemoteVersionDownloadURL(string fileName)
        {
            return $"{RemoteVersionRootUrl}/{fileName}";
        }

        // 下载相关
        private HardiskFileSearchResult ConvertToDownloadInfo(FileMeta webFileMeta,FileMeta patchSandboxFileMetaCandidate)
		{
			// 注意：资源版本号只用于确定下载路径
			//string sandboxPath = SandboxFileSystem.MakeSandboxFilePath(fileMeta.RelativePath);
			//string remoteMainURL = GetPatchDownloadMainURL(fileMeta.RelativePath);
		///	string remoteFallbackURL = GetPatchDownloadFallbackURL(fileMeta.RelativePath);
			var result = new HardiskFileSearchResult(webFileMeta, GetUpdateEntry(webFileMeta, patchSandboxFileMetaCandidate));
			return result;
		}
		private UpdateEntry GetUpdateEntry(FileMeta webFileMeta, FileMeta patchSandboxFileMetaCandidate)
		{
            UpdateEntry result = null;
            if (patchSandboxFileMetaCandidate == null)
            {
                result = new UpdateEntry(webFileMeta, RemoteVersionRootUrl);
                return result;
            }
            else 
            {
                var patch = GetPatch(patchSandboxFileMetaCandidate, webFileMeta);
                Debug.LogError("patch "+(patch==null)+" relative Path "+ webFileMeta.RelativePath);
                if (patch != null)
                {
                    result = new UpdateEntry(webFileMeta, RemoteVersionRootUrl,patch,RemoteVersionPatchUrl);
                }
                else 
                {
                    result = new UpdateEntry(webFileMeta, RemoteVersionRootUrl);
                }
            }
            return result;

        }
        /*
		private List<HardiskFileSearchResult> ConvertToDownloadList(List<FileMeta> downloadList)
		{
			List<HardiskFileSearchResult> result = new List<HardiskFileSearchResult>(downloadList.Count);
			foreach (var fileMeta in downloadList)
			{
				var bundleInfo = ConvertToDownloadInfo(fileMeta);
				result.Add(bundleInfo);
			}
			return result;
		}
        */
        // 解压相关
        /// <summary>
        /// 创建解压器
        /// </summary>
        public UnzipOperation CreateUnpackerByTags(string[] tags, int fileUpackingMaxNumber, int failedTryAgain)
        {
            List<UnzipEntry> unpcakList = GetUnpackListByTags(tags);
            var operation = new UnzipOperation(unpcakList, fileUpackingMaxNumber, failedTryAgain);
            Action<AsyncOperationBase> callback = (AsyncOperationBase ab) => {
                Logger.Log($"解压完毕 {operation.Status}");
                if (operation.Status == EOperationStatus.Succeed)
                {
                    SandboxFileSystem.FlushSandboxFileManifestToHardisk();
                }
            };
            operation.Completed += callback;
            return operation;
        }
        private List<UnzipEntry> GetUnpackListByTags(string[] tags)
        {
            List<UnzipEntry> unzipEntries = new List<UnzipEntry>();
            if (AppFileManifest != null)
            {
                List<FileMeta> downloadList = new List<FileMeta>();
                AppFileManifest.GetFileMetaByTag(tags, ref downloadList);

                for (int i = downloadList.Count - 1; i >= 0; i--)
                {
                    var streamFileMeta = downloadList[i];
                    if (!SandboxFileSystem.ContainsFile(streamFileMeta.RelativePath))
                    {
                        unzipEntries.Add(new UnzipEntry(streamFileMeta));
                    }
                }
            }
            return unzipEntries;
        }
      
        /*
		private List<HardiskFileSearchResult> ConvertToUnpackList(List<FileMeta> unpackList)
		{
			List<HardiskFileSearchResult> result = new List<HardiskFileSearchResult>(unpackList.Count);
			foreach (var patchBundle in unpackList)
			{
				var bundleInfo = ConvertToUnpackInfo(patchBundle);
				result.Add(bundleInfo);
			}
			return result;
		}
        private HardiskFileSearchResult ConvertToUnpackInfo(FileMeta FileMeta)
        {
            string sandboxPath = SandboxFileSystem.MakeSandboxFilePath(FileMeta.RelativePath);
            string streamingLoadPath = AssetPathHelper.MakeStreamingSandboxLoadPath(FileMeta.RelativePath);
            HardiskFileSearchResult bundleInfo = new HardiskFileSearchResult(FileMeta, sandboxPath, streamingLoadPath, streamingLoadPath, EnumHardiskDirectoryType.Persistent);
            return bundleInfo;
        }
        */
        #region IBundleServices接口
        public HardiskFileSearchResult SearchHardiskFile(FileMeta fileMeta)
		{
			if (!fileMeta.IsValid())
				return HardiskFileSearchResult.EMPTY;
           return this.SearchHardiskFileByPath(fileMeta.RelativePath);
        }

        public  HardiskFileSearchResult SearchHardiskFileByPath(string relativePath)
        {
            if (WebFileManifest.GetFileMetaMap().TryGetValue(relativePath, out var webFileMeta))
            {
                FileMeta patchSandboxFileMetaCandidate = null;
                // 查询沙盒资源				
                if (SandboxFileSystem.TryGetHardiskFilePath(relativePath, out var hardiskPath, out var sandboxFileMeta))
                {
					if (sandboxFileMeta.Hash == webFileMeta.Hash)
					{
						var result = new HardiskFileSearchResult(sandboxFileMeta, hardiskPath, EnumHardiskDirectoryType.Persistent);
						return result;
					}
					else
					{
                        patchSandboxFileMetaCandidate = sandboxFileMeta;
                    }
                  
                }

                // 查询APP资源
                if (AppFileManifest!=null&&AppFileManifest.GetFileMetaMap().TryGetValue(relativePath, out var appFileMeta))
                {
                    if (webFileMeta.Hash == appFileMeta.Hash)
                    {
                        string appLoadPath = AssetPathHelper.MakeStreamingSandboxLoadPath(relativePath);
                        var result = new HardiskFileSearchResult(appFileMeta, appLoadPath, EnumHardiskDirectoryType.StreamAsset);
                        return result;
                    }
                }

                // 从服务端下载
                return ConvertToDownloadInfo(webFileMeta, patchSandboxFileMetaCandidate);
            }
            else
            {
                Logger.Warning($"Not found bundle in patch manifest : {relativePath}");
                HardiskFileSearchResult result = new HardiskFileSearchResult(relativePath);
                return result;
            }
        }
        public FileMeta GetBundleRelativePath(string assetPath)
		{
			return WebBundleManifest.GetBundleFileMeta(assetPath);
		}
        public List<FileMeta> GetAllDependencieBundleRelativePaths(string assetPath)
		{
			return WebBundleManifest.GetAllDependenciesRelativePath(assetPath);
		}
		#endregion
	}
}