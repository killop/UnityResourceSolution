using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using YooAsset;

[Serializable]
public class URSRuntimeSetting
{
    public const string SAVE_RESOUCE_PATH = "urs_runtime_setting.txt";

    public const string SAVE_RESOUCE_PATH_NO_EXTENSION = "urs_runtime_setting";

    [SerializeField]
    public string FilesVersionIndexFileName = "files_version_index.txt";

    [SerializeField]
    public string PatchDirectory = "patch";

    [SerializeField]
    public string PatchTempDirectory = "patch_temp";
    /// <summary>
    /// AssetBundle文件的后缀名
    /// </summary>
    [SerializeField]
    public string AssetBundleFileVariant = ".bundle";

    /// <summary>
    /// 原生文件的后缀名
    /// </summary>
    [SerializeField]
    public string RawFileVariant = "rawfile";

    /// <summary>
    /// 构建输出的文件清单文件名称
    /// </summary>
    [SerializeField]
    public string FileManifestFileName = "file_manifest.txt";

    /// <summary>
    /// 构建输出的文件清单文件名称
    /// </summary>
    [SerializeField]
    public string BundleManifestFileRelativePath = "bundles/bundle_manifest.txt";

    /// <summary>
    /// 构建输出的文件清单文件名称
    /// </summary>
    [SerializeField]
    public string BundleManifestFileName = "bundle_manifest.txt";

    /// <summary>
    /// 表示app的id文件名称，放在stream asset的 urs_buildin_resource 里面
    /// </summary>
    [SerializeField]
    public string ChannelFileName = "channel.txt";

    /// <summary>
    /// 默认的沙盒目录是 persistent 目录，你可以强制指定这个目录，到本地某个版本的时候
    /// </summary>
    [SerializeField]
    public string ForceDownloadDirectory = string.Empty;

    [SerializeField]
    public YooAssets.EPlayMode PlayMode = YooAssets.EPlayMode.EditorPlayMode;

    [SerializeField]
    public string RemoteChannelRootUrl = @"https://staticninja.happyelements.cn";

    [SerializeField]
    public string RemoteAppVersionRouterFileName = "app_version_router.txt";
    //public string FallbackHostServer = "";

    [SerializeField]
    public string DefaultTag = "default";

    [SerializeField]
    public string BuildinTag = "buildin";

    public static URSRuntimeSetting instance
    {
        get
        {
            if (_instance == null)
            {
                var textAsset = Resources.Load<TextAsset>(SAVE_RESOUCE_PATH_NO_EXTENSION);
                if (textAsset != null)
                {
                    _instance = UnityEngine.JsonUtility.FromJson<URSRuntimeSetting>(textAsset.text);
                }
                else 
                {
                    _instance = new URSRuntimeSetting();
                }
            }
             return _instance;
        }
    }
    private static URSRuntimeSetting _instance = null;
}


