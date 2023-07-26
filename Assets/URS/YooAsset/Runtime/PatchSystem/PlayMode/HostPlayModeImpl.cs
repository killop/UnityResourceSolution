using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using URS;
namespace YooAsset
{
    public class HostPlayModeImpl : IFileSystemServices
    {
        // 补丁清单
        //internal PatchManifest AppPatchManifest;
        //internal PatchManifest LocalPatchManifest;

       // internal FileManifest WebFileManifest;
      //  internal BundleManifest WebBundleManifest;

       // internal FileManifest AppFileManifest;
       // internal BundleManifest AppBundleManifest;
        internal string channel;

     
        // 参数相关
        internal bool ClearCacheWhenDirty { private set; get; }
        internal string RemoteAppToChannelRouterFileUrl { get; set; }
        internal string RemoteVersionsRootUrl { get; set; }
        internal string RemoteVersionRootUrl { get; set; }
        internal string RemoteVersionPatchUrl { get; set; }
        internal string TargetVersion {get;set;}
        internal URSFilesVersionIndex FilesVersionIndex { get; set; }


        /// <summary>
        /// 异步初始化
        /// </summary>
        public InitializationOperation InitializeAsync()
		{
			ClearCacheWhenDirty = true;
          
			var operation = new HostPlayModeInitializationOperation(this);
            OperationSystem.ProcessOperaiton(operation);
			return operation;
		}
        public void InitChannel(string channel) 
        {
            this.channel= channel;
            RemoteVersionsRootUrl = $"{URSRuntimeSetting.instance.RemoteChannelRootUrl}/{this.channel}/{URS.PlatformMappingService.GetPlatformPathSubFolder()}";
            RemoteVersionPatchUrl = $"{RemoteVersionsRootUrl}/{URSRuntimeSetting.instance.PatchDirectory}";
            RemoteAppToChannelRouterFileUrl = $"{RemoteVersionsRootUrl}/{URSRuntimeSetting.instance.RemoteAppVersionRouterFileName}";
        }
		public void InitVersion(string targetVersion)
		{
            TargetVersion = targetVersion;
        }

		public string GetRemoteFilesVersionIndexUrl() 
		{
			return $"{RemoteVersionsRootUrl}/{URSRuntimeSetting.instance.FilesVersionIndexFileName}";
		}
        public void InitFilesVersion(URSFilesVersionIndex versions)
        {
            FilesVersionIndex = versions;
            var item = FilesVersionIndex.GetVersion(TargetVersion);
            RemoteVersionRootUrl = $"{RemoteVersionsRootUrl}/{item.VersionCode}---{item.FilesVersionHash}";
        }

        public PatchItemVersion GetPatch(FileMeta localfileMeta, FileMeta remoteFileMeta) 
        {
            return FilesVersionIndex.GetPatch(localfileMeta, remoteFileMeta);
        }

        /// <summary>
        /// 异步更新补丁清单
        /// </summary>
        public UpdateManifestOperation UpdatePatchManifestAsync(int timeout)
		{
			var operation = new HostPlayModeUpdateManifestOperation(this, timeout);
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
            Action<AsyncOperationBase> callback = (AsyncOperationBase ab) => {
                Logger.Log($"下载完毕 {operation.Status}");
                if (operation.Status == EOperationStatus.Succeed)
                {
                    URSFileSystem.PersistentDownloadFolder.FlushFileManifestToHardisk();
                }
            };
            operation.Completed += callback;
            return operation;
		}
		private List<UpdateEntry> GetDownloadListByTags(string[] tags)
		{
			List<UpdateEntry> downloadList = new List<UpdateEntry>(1000);
            var patchCount = 0;
            if (URSFileSystem.RemoteFolder.FileManifest != null) 
            {
                foreach (var kv in URSFileSystem.RemoteFolder.FileManifest.GetFileMetaMap())
                {
                    var remoteFileMeta = kv.Value;
                    if (!remoteFileMeta.HasAnyTag(tags)) continue;
                    
                    var searchResult = URSFileSystem.SearchHardiskFile(remoteFileMeta);
                    
                    if (searchResult != null && searchResult.UpdateEntry!=null)
                    {
                        if (searchResult.UpdateEntry.IsPatch()){
                            patchCount++;
                        }
                        downloadList.Add(searchResult.UpdateEntry);
                    }
                }
            }
            Debug.Log($"总共需要下载 {downloadList.Count}个 其中patch的数量 {patchCount}");
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
            var bundleManifest = URSFileSystem.RemoteFolder.BundleManifest;
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
                var searchResult = URSFileSystem.SearchHardiskFile(remoteFileMeta);
                if (searchResult != null&& searchResult.UpdateEntry!=null)
                {
                    results.Add(searchResult.UpdateEntry);
                }
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
            return WWW.UnEscapeURL($"{RemoteVersionRootUrl}/{fileName}");
        }

        // 下载相关
        public HardiskFileSearchResult ConvertToDownloadInfo(FileMeta webFileMeta,FileMeta patchFileMetaCandidate, EnumHardiskDirectoryType patchFileMetaCandidateDirectoryType)
		{
			// 注意：资源版本号只用于确定下载路径
			//string sandboxPath = SandboxFileSystem.MakeSandboxFilePath(fileMeta.RelativePath);
			//string remoteMainURL = GetPatchDownloadMainURL(fileMeta.RelativePath);
		///	string remoteFallbackURL = GetPatchDownloadFallbackURL(fileMeta.RelativePath);
			var result = new HardiskFileSearchResult(webFileMeta, GetUpdateEntry(webFileMeta, patchFileMetaCandidate, patchFileMetaCandidateDirectoryType));
			return result;
		}
		private UpdateEntry GetUpdateEntry(FileMeta webFileMeta, FileMeta patchFileMetaCandidate, EnumHardiskDirectoryType patchFileMetaCandidateDirectoryType)
		{
            UpdateEntry result = null;
            if (patchFileMetaCandidate == null)
            {
                result = new UpdateEntry(webFileMeta, RemoteVersionRootUrl);
                return result;
            }
            else 
            {
                var patch = GetPatch(patchFileMetaCandidate, webFileMeta);
                if (patch != null)
                {
                    result = new UpdateEntry(webFileMeta, RemoteVersionRootUrl,patch,RemoteVersionPatchUrl, patchFileMetaCandidate, patchFileMetaCandidateDirectoryType);
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
                    URSFileSystem.PersistentDownloadFolder.FlushFileManifestToHardisk();
                }
            };
            operation.Completed += callback;
            return operation;
        }
        private List<UnzipEntry> GetUnpackListByTags(string[] tags)
        {
            List<UnzipEntry> unzipEntries = new List<UnzipEntry>();
            if (URSFileSystem.BuildInFolder.FileManifest != null)
            {
                List<FileMeta> downloadList = new List<FileMeta>();
                URSFileSystem.BuildInFolder.FileManifest.GetFileMetaByTag(tags, ref downloadList);

                for (int i = downloadList.Count - 1; i >= 0; i--)
                {
                    var streamFileMeta = downloadList[i];
                    if (!URSFileSystem.PersistentDownloadFolder.ContainsFile(streamFileMeta.RelativePath))
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
            return URSFileSystem.SearchHardiskFile(fileMeta);
        }

        public  HardiskFileSearchResult SearchHardiskFileByPath(string relativePath)
        {
            return URSFileSystem.SearchHardiskFileByPath(relativePath);
        }
        public FileMeta GetBundleRelativePath(string assetPath)
		{
			return URSFileSystem.GetBundleRelativePath(assetPath);
		}
        public List<FileMeta> GetAllDependencieBundleRelativePaths(string assetPath)
		{
            return URSFileSystem.GetAllDependencieBundleRelativePaths(assetPath);
		}
        public void SearchLocalSecurityBundleHardDiskFileByPath(
            string relativePath, 
            out HardiskFileSearchResult mainResult, 
            out List<HardiskFileSearchResult> dependency,
            bool skipDownloadFolder = true) 
        {
            URSFileSystem.SearchLocalSecurityBundleHardDiskFileByPath(relativePath, out  mainResult, out  dependency, skipDownloadFolder);
        }
      
       #endregion
    }
}