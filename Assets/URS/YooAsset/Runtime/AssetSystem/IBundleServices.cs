using URS;
using System.Collections.Generic;
namespace YooAsset
{
	public interface IFileSystemServices
	{	
		
		HardiskFileSearchResult SearchHardiskFile(FileMeta fileMeta);

        HardiskFileSearchResult SearchHardiskFileByPath(string relativePath);
        /// <summary>
        /// 获取资源所属的资源包名称
        /// </summary>
        FileMeta GetBundleRelativePath(string assetPath);

        /// <summary>
        /// 获取资源依赖的所有AssetBundle列表
        /// </summary>
        List<FileMeta> GetAllDependencieBundleRelativePaths(string assetPath);

        void  SearchLocalSecurityBundleHardDiskFileByPath(string relativePath,out HardiskFileSearchResult mainResult,out List<HardiskFileSearchResult> dependency, bool skipDownloadFolder = true);
    }
}