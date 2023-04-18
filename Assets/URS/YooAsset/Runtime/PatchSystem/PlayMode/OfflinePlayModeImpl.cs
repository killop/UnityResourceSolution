using System;
using System.Collections;
using System.Collections.Generic;
using URS;
namespace YooAsset
{
	internal class OfflinePlayModeImpl : IFileSystemServices
	{
		//internal FileManifest AppFileManifest;
        //internal BundleManifest AppBundleManifest;
        internal bool ClearCacheWhenDirty { private set; get; }
     
        /// <summary>
        /// 异步初始化
        /// </summary>
        public InitializationOperation InitializeAsync()
		{
            ClearCacheWhenDirty = true;
            var operation = new OfflinePlayModeInitializationOperation(this);
			OperationSystem.ProcessOperaiton(operation);
			return operation;
		}

		

		#region IBundleServices接口
		public HardiskFileSearchResult SearchHardiskFile(FileMeta fileMeta)
		{
            return URSFileSystem.SearchHardiskFile(fileMeta);
        }

        public HardiskFileSearchResult SearchHardiskFileByPath(string relativePath)
        {
            return URSFileSystem.SearchHardiskFileByPath(relativePath);
        }
        public FileMeta GetBundleRelativePath(string assetPath)
		{
            return URSFileSystem.GetBundleRelativePath(assetPath);
        }
       public  List<FileMeta> GetAllDependencieBundleRelativePaths(string assetPath)
		{
            return URSFileSystem.GetAllDependencieBundleRelativePaths(assetPath);
        }
        public void SearchLocalSecurityBundleHardDiskFileByPath(
            string relativePath,
            out HardiskFileSearchResult mainResult,
            out List<HardiskFileSearchResult> dependency,
            bool skipDownloadFolder = true)
        {
            URSFileSystem.SearchLocalSecurityBundleHardDiskFileByPath(relativePath, out mainResult, out dependency, skipDownloadFolder);
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