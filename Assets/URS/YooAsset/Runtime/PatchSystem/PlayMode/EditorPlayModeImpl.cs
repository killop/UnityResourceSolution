using System;
using System.Collections;
using System.Collections.Generic;
using URS;
namespace YooAsset
{
	internal class EditorPlayModeImpl : IFileSystemServices
	{
		/// <summary>
		/// 异步初始化
		/// </summary>
		public InitializationOperation InitializeAsync()
		{
			var operation = new EditorModeInitializationOperation();
            OperationSystem.ProcessOperaiton(operation);
			return operation;
		}

		/// <summary>
		/// 获取资源版本号
		/// </summary>
		public int GetResourceVersion()
		{
			return 0;
		}

		#region IBundleServices接口
		HardiskFileSearchResult IFileSystemServices.SearchHardiskFile(FileMeta fileMeta)
		{
			Logger.Warning($"Editor play mode can not get bundle info.");
            var result = new HardiskFileSearchResult(fileMeta.RelativePath);
			return result;
		}

        HardiskFileSearchResult IFileSystemServices.SearchHardiskFileByPath(string  relativePath)
        {
            Logger.Warning($"Editor play mode do not support.");
            var result = new HardiskFileSearchResult(relativePath);
            return result;
        }
        FileMeta IFileSystemServices.GetBundleRelativePath(string assetPath)
		{
			return FileMeta.ERROR_FILE_META;
		}
		List<FileMeta> IFileSystemServices.GetAllDependencieBundleRelativePaths(string assetPath)
		{
            return null;
        }
		#endregion
	}
}