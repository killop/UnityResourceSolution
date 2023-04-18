using System.IO;
using YooAsset.Utility;

namespace YooAsset
{
	public static class AssetPathHelper
	{
		/// <summary>
		/// 获取规范化的路径
		/// </summary>
		public static string GetRegularPath(string path)
		{
			return path.Replace('\\', '/').Replace("\\", "/"); //替换为Linux路径格式
		}

		/// <summary>
		/// 获取文件所在的目录路径（Linux格式）
		/// </summary>
		public static string GetDirectory(string filePath)
		{
			string directory = Path.GetDirectoryName(filePath);
			return GetRegularPath(directory);
		}

		/// <summary>
		/// 获取基于流文件夹的加载路径
		/// </summary>
		public static string MakeStreamingLoadPath(string path)
		{
			return StringUtility.Format("{0}/{1}", UnityEngine.Application.streamingAssetsPath, path);
		}
        public static string GetBuildInChannelFilePath()
        {
            return MakeStreamingLoadPath(URSRuntimeSetting.instance.ChannelFileName);
        }
        public static string MakeURSBuildInResourcePath(string path)
        {
            return StringUtility.Format("{0}/{1}", GetURSBuildInResourceFolder(), path);
        }
        /// <summary>
        /// 获取基于沙盒文件夹的加载路径
        /// </summary>
        public static string MakePersistentLoadPath(string path)
		{
			string root = GetPersistentRootPath();
			return StringUtility.Format("{0}/{1}", root, path);
		}

        private static string sPersistentRootPath = null;
        /// <summary>
        /// 获取沙盒文件夹路径
        /// </summary>
        public static string GetPersistentRootPath()
		{
            if (sPersistentRootPath == null)
            {
#if UNITY_EDITOR
                // 注意：为了方便调试查看，编辑器下把存储目录放到项目里
                string projectPath = GetDirectory(UnityEngine.Application.dataPath);
                sPersistentRootPath= StringUtility.Format("{0}/app_persistent_root", projectPath);
#else
			    sPersistentRootPath=  StringUtility.Format("{0}/app_persistent_root", UnityEngine.Application.persistentDataPath);
#endif
            }
            return sPersistentRootPath;

        }
        private static string sAppInstallFootprintFilePath = null;

        public static string GetAppInstallFootprintFilePath()
        {
            if (sAppInstallFootprintFilePath == null) {
                sAppInstallFootprintFilePath= $"{GetPersistentRootPath()}/app_install_footprint.text";
            }
            return sAppInstallFootprintFilePath;
        }
        /// <summary>
        /// 删除沙盒内的缓存文件
        /// </summary>
        public static void DeleteAppInstallFootprintFile()
        {
            string filePath = GetAppInstallFootprintFilePath();
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        /// <summary>
        /// 删除沙盒内的缓存文件
        /// </summary>
        public static bool IsAppInstallFootprintFileExist()
        {
            string filePath = GetAppInstallFootprintFilePath();
            return File.Exists(filePath);
        }
        /// <summary>
        /// 获取网络资源加载路径
        /// </summary>
        public static string ConvertToWWWPath(string path)
		{
			// 注意：WWW加载方式，必须要在路径前面加file://
#if UNITY_EDITOR
			return StringUtility.Format("file:///{0}", path);
#elif UNITY_IPHONE
			return StringUtility.Format("file://{0}", path);
#elif UNITY_ANDROID
			return path;
#elif UNITY_STANDALONE
			return StringUtility.Format("file:///{0}", path);
#endif
		}


        public static string sPersistentDownloadFolder = null;
        public static string GetPersistentDownloadFolder()
        {
            if (sPersistentDownloadFolder == null)
            {
                sPersistentDownloadFolder = $"{AssetPathHelper.GetPersistentRootPath()}/download";
            }
            return sPersistentDownloadFolder;
        }

        public static string sPersistentReadOnlyFolder = null;
        public static string GetPersistentReadonlyFolder()
        {
            if (sPersistentReadOnlyFolder == null)
            {
                sPersistentReadOnlyFolder = $"{AssetPathHelper.GetPersistentRootPath()}/readonly";
            }
            return sPersistentReadOnlyFolder;
        }

        public static string sURSBuildInResourceFolder = null;
        public static string GetURSBuildInResourceFolder()
        {
            if (sURSBuildInResourceFolder == null)
            {
                sURSBuildInResourceFolder = $"{UnityEngine.Application.streamingAssetsPath}/{GetURSBuildInResourceFolderName()}";
            }
            return sURSBuildInResourceFolder;
        }
        public static string GetURSBuildInResourceFolderName()
        {
            return "urs_buildin_resources";
        }
        public static string sDownloadTempFolder = null;
        public static string GetDownloadTempFolder()
        {
            if (sDownloadTempFolder == null)
            {
                sDownloadTempFolder = $"{AssetPathHelper.GetPersistentRootPath()}/download_temp";
            }
            return sDownloadTempFolder;
        }
        /// <summary>
        /// 合并资源路径
        /// </summary>
        internal static string CombineAssetPath(string root, string location)
		{
			if (string.IsNullOrEmpty(root))
				return location;
			else
				return $"{root}/{location}";
		}

		/// <summary>
		/// 获取AssetDatabase的加载路径
		/// </summary>
		internal static string FindDatabaseAssetPath(string filePath)
		{
#if UNITY_EDITOR
			if (File.Exists(filePath))
				return filePath;

			// AssetDatabase加载资源需要提供文件后缀格式，然而资源定位地址并没有文件格式信息。
			// 所以我们通过查找该文件所在文件夹内同名的首个文件来确定AssetDatabase的加载路径。
			// 注意：AssetDatabase.FindAssets() 返回文件内包括递归文件夹内所有资源的GUID
			string fileName = Path.GetFileName(filePath);
			string directory = GetDirectory(filePath);
			string[] guids = UnityEditor.AssetDatabase.FindAssets(string.Empty, new[] { directory });
			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);

				if (UnityEditor.AssetDatabase.IsValidFolder(assetPath))
					continue;

				string assetDirectory = GetDirectory(assetPath);
				if (assetDirectory != directory)
					continue;

				string assetName = Path.GetFileNameWithoutExtension(assetPath);

                if (assetName == fileName)
					return assetPath;
			}

			// 没有找到同名的资源文件
			Logger.Warning($"Not found asset : {filePath}");
			return filePath;
#else
			throw new System.NotImplementedException();
#endif
		}

	}
}