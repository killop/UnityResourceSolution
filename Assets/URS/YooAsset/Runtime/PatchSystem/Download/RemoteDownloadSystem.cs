using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using YooAsset.Utility;
using URS;
using MHLab.Patch.Core.Utilities;

namespace YooAsset
{
	/// <summary>
	/// 1. 保证每一时刻资源文件只存在一个下载器
	/// 2. 保证下载器下载完成后立刻验证并缓存
	/// 3. 保证资源文件不会被重复下载
	/// </summary>
	internal static class RemoteDownloadSystem
	{
		private static readonly Dictionary<string, RemoteDownloader> _downloaderDic = new Dictionary<string, RemoteDownloader>();
		private static readonly List<string> _removeList = new List<string>(100);


		/// <summary>
		/// 更新所有下载器
		/// </summary>
		public static void Update()
		{
			// 更新下载器
			_removeList.Clear();
			foreach (var valuePair in _downloaderDic)
			{
				var downloader = valuePair.Value;
				downloader.Update();
				if (downloader.IsDone())
					_removeList.Add(valuePair.Key);
			}

			// 移除下载器
			foreach (var key in _removeList)
			{
				_downloaderDic.Remove(key);
			}
		}

		/// <summary>
		/// 开始下载资源文件
		/// 注意：只有第一次请求的参数才是有效的
		/// </summary>
		public static RemoteDownloader BeginDownload(UpdateEntry updateEntry, int failedTryAgain, int timeout = 700)
		{
			// 查询存在的下载器
			if (_downloaderDic.TryGetValue(updateEntry.GetRemoteDownloadURL(), out var downloader))
			{
				return downloader;
			}

			//// 如果资源已经缓存
			//if(SandboxFileSystem.ContainsFile(hardiskFileSearchResult.OrignRelativePath))
			//{
			//	var newDownloader = new FileDownloader(hardiskFileSearchResult);
			//	newDownloader.SetDone();
			//	return newDownloader;
			//}

			// 创建新的下载器	
			{
				//Logger.Log($"Beginning to download file : {hardiskFileSearchResult.OrignRelativePath} URL : {hardiskFileSearchResult.RemoteDownloadURL} SavePath: {hardiskFileSearchResult.HardiskPath}");
				//FileUtility.CreateFileDirectory(hardiskFileSearchResult.GetHardDiskSavePath());
				var newDownloader = new RemoteDownloader(updateEntry);
				newDownloader.SendRequest(failedTryAgain, timeout);
				_downloaderDic.Add(updateEntry.GetRemoteDownloadURL(), newDownloader);
				return newDownloader;
			}
		}

		/// <summary>
		/// 获取下载器的总数
		/// </summary>
		public static int GetDownloaderTotalCount()
		{
			return _downloaderDic.Count;
		}
	}
  
}