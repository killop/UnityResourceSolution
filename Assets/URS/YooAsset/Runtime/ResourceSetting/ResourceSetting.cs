using UnityEngine;

namespace YooAsset
{
	[CreateAssetMenu(fileName = "YooAssetSetting", menuName = "YooAsset/Create Setting")]
	public class ResourceSetting : ScriptableObject
	{
		/// <summary>
		/// AssetBundle文件的后缀名
		/// </summary>
		//public string AssetBundleFileVariant = "bundle";

		/// <summary>
		/// 原生文件的后缀名
		/// </summary>
		//public string RawFileVariant = "rawfile";

		/// <summary>
		/// 构建输出的文件清单文件名称
		/// </summary>
		//public string FileManifestFileName = "FileManifest.txt";

		/// <summary>
		/// 构建输出的补丁清单哈希文件名称
		/// </summary>
		//public string FileManifestHashFileName = "FileManifestHash.txt";


        /// <summary>
		/// 构建输出的文件清单文件名称
		/// </summary>
		//public string BundleManifestFileName = "BundleManifest.txt";

        /// <summary>
		/// 默认的沙盒目录是 persistent 目录，你可以强制指定这个目录，到本地某个版本的时候
		/// </summary>
       // public string ForceSandboxDirectory = string.Empty;  
        /// <summary>
        /// 构建输出的Unity清单文件名称
        /// </summary>
       // public string UnityManifestFileName = "UnityManifest";

		/// <summary>
		/// 构建输出的说明文件
		/// </summary>
		//public string ReadmeFileName = "readme.txt";

       // public YooAssets.EPlayMode PlayMode = YooAssets.EPlayMode.EditorPlayMode;

       // public string DefaultHostServer = "";

       // public string FallbackHostServer = "";
    }
}