using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hextant;
using YooAsset;
#if UNITY_EDITOR
    using UnityEditor;
#endif
[Settings(SettingsUsage.RuntimeProject)]
public class URSRuntimeSetting : Hextant.Settings<URSRuntimeSetting>
{
    public string FilesVersionIndexFileName = "files_version_index.txt";
    public string PatchDirectory = "patch";
    public string PatchTempDirectory = "patch_temp";
    /// <summary>
    /// AssetBundle文件的后缀名
    /// </summary>
    public string AssetBundleFileVariant = ".bundle";

    /// <summary>
    /// 原生文件的后缀名
    /// </summary>
    public string RawFileVariant = "rawfile";

    /// <summary>
    /// 构建输出的文件清单文件名称
    /// </summary>
    public string FileManifestFileName = "file_manifest.txt";

    /// <summary>
    /// 构建输出的文件清单文件名称
    /// </summary>
    public string BundleManifestFileName = "bundle_manifest.txt";

    /// <summary>
    /// 表示app的id文件名称，放在stream asset的 sandbox 里面
    /// </summary>
    public string AppIdFileName = "app_id.txt";

    /// <summary>
    /// 默认的沙盒目录是 persistent 目录，你可以强制指定这个目录，到本地某个版本的时候
    /// </summary>
    public string ForceSandboxDirectory = string.Empty;

    public YooAssets.EPlayMode PlayMode = YooAssets.EPlayMode.EditorPlayMode;

    public string RemoteChannelRootUrl = "http://127.0.0.1:8000";

    public string RemoteAppToChannelRouterFileName = "channel_router.txt";
   //public string FallbackHostServer = "";

    public string DefaultTag = "default";

    public string BuildinTag = "buildin";
}
#if UNITY_EDITOR
[InitializeOnLoad]
 static class ProjectOpenURSRuntimeSetting
{
    static ProjectOpenURSRuntimeSetting()
    {
        var instance= URSRuntimeSetting.instance;
    }
}
#endif