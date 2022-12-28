using System;
using System.Collections;
using System.Collections.Generic;
using URS;
namespace YooAsset
{
	internal class OfflinePlayModeImpl : IFileSystemServices
	{
		internal FileManifest AppFileManifest;
        internal BundleManifest AppBundleManifest;

        public FileManifest GetJudgedFileManifest()
        {
            var sanboxFileManifest = SandboxFileSystem.GetFileManifest();
            if (sanboxFileManifest != null)
            {
                UnityEngine.Debug.Log("use sanboxFileManifest");
                return sanboxFileManifest;
            }
            UnityEngine.Debug.Log("use AppFileManifest");
            return AppFileManifest;
        }
        /// <summary>
        /// 异步初始化
        /// </summary>
        public InitializationOperation InitializeAsync()
		{
			var operation = new OfflinePlayModeInitializationOperation(this);
			OperationSystem.ProcessOperaiton(operation);
			return operation;
		}

		

		#region IBundleServices接口
		public HardiskFileSearchResult SearchHardiskFile(FileMeta fileMeta)
		{
			if (!fileMeta.IsValid())
                return HardiskFileSearchResult.EMPTY;

            return this.SearchHardiskFileByPath(fileMeta.RelativePath);
        }
        public HardiskFileSearchResult SearchHardiskFileByPath(string relativePath)
        {
            var initedSanboxFileManifest = SandboxFileSystem.GetFileManifest();
            if (initedSanboxFileManifest != null && initedSanboxFileManifest.GetFileMetaMap().TryGetValue(relativePath, out var sandboxFileMeta))
            {
                string hardiskPath = SandboxFileSystem.MakeSandboxFilePath(relativePath);
                return new HardiskFileSearchResult(sandboxFileMeta, hardiskPath,EnumHardiskDirectoryType.Persistent);
            }
            else if (AppFileManifest != null && AppFileManifest.GetFileMetaMap().TryGetValue(relativePath, out var appFileMeta))
            {
                string localPath = AssetPathHelper.MakeStreamingSandboxLoadPath(relativePath);
                HardiskFileSearchResult result = new HardiskFileSearchResult(appFileMeta, localPath,EnumHardiskDirectoryType.StreamAsset);
                return result;
            }
            else
            {
                Logger.Error($"Not found bundle in patch manifest : {relativePath}");
                HardiskFileSearchResult result = new HardiskFileSearchResult(relativePath);
                return result;
            }
        }
        public FileMeta GetBundleRelativePath(string assetPath)
		{
            var sandboxBundleManifest = SandboxFileSystem.GetBundleManifest();
            if (sandboxBundleManifest != null)
            {
                return sandboxBundleManifest.GetBundleFileMeta(assetPath);
            }
            if (AppBundleManifest != null)
            {
                return AppBundleManifest.GetBundleFileMeta(assetPath);
            }
            return FileMeta.ERROR_FILE_META;
		}
       public  List<FileMeta> GetAllDependencieBundleRelativePaths(string assetPath)
		{
            var sandboxBundleManifest = SandboxFileSystem.GetBundleManifest();
            if (sandboxBundleManifest != null)
            {
                return sandboxBundleManifest.GetAllDependenciesRelativePath(assetPath);
            }
            if (AppBundleManifest != null) {
                return AppBundleManifest.GetAllDependenciesRelativePath(assetPath);
            }
            return null;
        }
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
        // 解压相关
        private HardiskFileSearchResult ConvertToUnpackInfo(FileMeta FileMeta)
        {
            string sandboxPath = SandboxFileSystem.MakeSandboxFilePath(FileMeta.RelativePath);
            string streamingLoadPath = AssetPathHelper.MakeStreamingSandboxLoadPath(FileMeta.RelativePath);
            HardiskFileSearchResult bundleInfo = new HardiskFileSearchResult(FileMeta, sandboxPath, streamingLoadPath, streamingLoadPath,EnumHardiskDirectoryType.Persistent);
            return bundleInfo;
        }
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
       */
        #endregion
    }
}