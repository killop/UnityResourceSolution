using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset.Utility;
using System.IO;

namespace YooAsset
{
	[Serializable]
	public sealed class AppFootPrint
	{
		/// <summary>
		/// 缓存的APP内置版本
		/// </summary>
		public string LastAppFootPrint = string.Empty;

		/// <summary>
		/// 读取缓存文件
		/// 注意：如果文件不存在则创建新的缓存文件
		/// </summary>
		public static AppFootPrint Load()
		{
            string footPrintFilePath= AssetPathHelper.GetAppInstallFootprintFilePath();
			if (AssetPathHelper.IsAppInstallFootprintFileExist())
			{
				Logger.Log("Load patch cache from disk.");
				string jsonData = FileUtility.ReadFile(footPrintFilePath);
				return JsonUtility.FromJson<AppFootPrint>(jsonData);
			}
			else
			{
				return null;
				
			}
		}

		public bool IsDirty() 
		{
			return LastAppFootPrint != Application.buildGUID;
        }
		/// <summary>
		/// 更新缓存文件
		/// </summary>
		public static void Create()
		{
			Logger.Log($"Update patch cache to disk : {Application.version}");
			AppFootPrint cache = new AppFootPrint();
			cache.LastAppFootPrint = Application.buildGUID;
			string filePath = AssetPathHelper.GetAppInstallFootprintFilePath();
			string jsonData = JsonUtility.ToJson(cache);
			
			FileUtility.CreateFile(filePath, jsonData);
		}
	}
}