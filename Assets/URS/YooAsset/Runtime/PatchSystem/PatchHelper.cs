//using System.IO;
//using System.Text;
//using YooAsset.Utility;

//namespace YooAsset
//{
//	internal static class PatchHelper
//	{
	
//        private static string sSandboxDirectoryName = null;

//        public static string GetSandboxDirectory()
//        {
//            if (sSandboxDirectoryName == null)
//            {
//                sSandboxDirectoryName = StringUtility.Format("{0}/{1}", AssetPathHelper.GetPersistentRootPath(), "sandbox");
//            }
//            return sSandboxDirectoryName;
//        }

//		/// <summary>
//		/// 删除沙盒内补丁清单文件
//		/// </summary>
//		public static void DeleteSandboxPatchManifestFile()
//		{
//			string filePath = AssetPathHelper.MakePersistentLoadPath(ResourceSettingData.Setting.FileManifestFileName);
//			if (File.Exists(filePath))
//				File.Delete(filePath);
//		}


//		/// <summary>
//		/// 删除沙盒内的缓存文件夹
//		/// </summary>
//		public static void DeleteSandboxFolder()
//		{
//			string directoryPath = GetSandboxDirectory();
//			if (Directory.Exists(directoryPath))
//				Directory.Delete(directoryPath, true);
//		}


		

		

//		/// <summary>
//		/// 检测沙盒内补丁清单文件是否存在
//		/// </summary>
//		public static bool CheckSandboxPatchManifestFileExist()
//		{
//			string filePath = AssetPathHelper.MakePersistentLoadPath(ResourceSettingData.Setting.FileManifestFileName);
//			return File.Exists(filePath);
//		}
//        /// <summary>
//		/// 检测沙盒内补丁清单文件是否存在
//		/// </summary>
//		public static bool CheckSandboxBundleManifestFileExist()
//        {
//            string filePath = AssetPathHelper.MakePersistentLoadPath(ResourceSettingData.Setting.BundleManifestFileName);
//            return File.Exists(filePath);
//        }
//        /// <summary>
//        /// 获取沙盒内补丁清单文件的哈希值
//        /// 注意：如果沙盒内补丁清单文件不存在，返回空字符串
//        /// </summary>
//        /// <returns></returns>
//        public static string GetSandboxPatchManifestFileHash()
//		{
//			string filePath = AssetPathHelper.MakePersistentLoadPath(ResourceSettingData.Setting.FileManifestFileName);
//			if (File.Exists(filePath))
//				return HashUtility.FileMD5(filePath);
//			else
//				return string.Empty;
//		}

//		/// <summary>
//		/// 获取缓存文件的存储路径
//		/// </summary>
//		public static string MakeSandboxCacheFilePath(string fileName)
//		{
//			return $"{GetSandboxDirectory()}/{fileName}";
//		}
//	}
//}