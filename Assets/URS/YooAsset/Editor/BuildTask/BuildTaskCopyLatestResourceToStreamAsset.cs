using System.Collections.Generic;
using System.IO;
using URS;
using UnityEditor.Build;


public class StreamingAssetsVersionHook : BuildPlayerProcessor
{
    public static bool Enable = false;
    public static string TargetVersion = null;
    public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
    {
        if (!string.IsNullOrEmpty(TargetVersion)&& Enable)
        {
            var tempFolder = Build.GetBuildInResourceTempFolder();
            buildPlayerContext.AddAdditionalPathToStreamingAssets(tempFolder, YooAsset.AssetPathHelper.GetURSBuildInResourceFolderName());
        }
    }
}
public class BuildTaskCopyLatestResourceToStreamAsset : BuildTask
{

    public bool _useLazyHook = false;

    public BuildTaskCopyLatestResourceToStreamAsset(bool useLazyHook)
    {
        _useLazyHook = useLazyHook;
    }

    public override void BeginTask()
    {
        base.BeginTask();
        var targetVersion = GetData<string>(CONTEXT_COPY_STREAM_TARGET_VERSION);
        StreamingAssetsVersionHook.TargetVersion = targetVersion;
        StreamingAssetsVersionHook.Enable = _useLazyHook;
        var streamTarget = YooAsset.AssetPathHelper.GetURSBuildInResourceFolder();
        if (Directory.Exists(streamTarget))
        {
            Directory.Delete(streamTarget,true);
        }
        var versionRootDirectory = (string)_context[CONTEXT_VERSION_ROOT_DIRECTORY];
        // 如果你的unity版本不支持 buildPlayerContext.AddAdditionalPathToStreamingAssets 切换到 CopyLatestResourceToStreamAsset函数就行了
        if (!string.IsNullOrEmpty(targetVersion) && _useLazyHook)
        {
            var tempFolder = Build.GetBuildInResourceTempFolder();
            BuildTaskCopyLatestResourceToStreamAsset.CopyLatestResourceToStreamAsset(versionRootDirectory,targetVersion, tempFolder, false);
        }
        else 
        {
            CopyLatestResourceToStreamAsset(versionRootDirectory,targetVersion, YooAsset.AssetPathHelper.GetURSBuildInResourceFolder(), true);
        }
        this.FinishTask();
    }
    public static void CopyLatestResourceToStreamAsset(string versionRootDirectory,string targetVersion, string targetFolder,bool freshAssetDataBase)
    {
        var targetVersionCode = targetVersion;
        var di = new DirectoryInfo(versionRootDirectory);
        foreach (DirectoryInfo subDirectory in di.GetDirectories())
        {
            var name = subDirectory.Name;
            var names = name.Split("---");
            var versionCode = names[0];
            if (SemanticVersioning.SemanticVersion.TryParse(versionCode, out var sVersion))
            {
                if (targetVersionCode == versionCode)
                {
                    var versionDirectory = Path.Combine(versionRootDirectory, name);
                    var fileManifestPath = $"{versionDirectory}/{URSRuntimeSetting.instance.FileManifestFileName}";
                    if (File.Exists(fileManifestPath))
                    {
                        string json = File.ReadAllText(fileManifestPath);
                        var fm = FileManifest.Deserialize(json);
                        List<FileMeta> fileMetas = new List<FileMeta>();
                        fm.GetFileMetaByTag(new string[] { URSRuntimeSetting.instance.BuildinTag }, ref fileMetas);
                        if (Directory.Exists((string)targetFolder))
                        {
                            Directory.Delete((string)targetFolder, true);
                        }
                        for (int i = 0; i < fileMetas.Count; i++)
                        {
                            string rlPath = fileMetas[i].RelativePath;
                            string srcPath = $"{versionDirectory}/{rlPath}";
                            string dstPath = $"{targetFolder}/{rlPath}";
                            var dstDirectoryName = Path.GetDirectoryName(dstPath);
                            Directory.CreateDirectory(dstDirectoryName);
                            File.Copy(srcPath, dstPath);
                        }
                        FileManifest fileManifest = new FileManifest(fileMetas.ToArray(), versionCode);
                        string buildinFileManifestPath = $"{targetFolder}/{URSRuntimeSetting.instance.FileManifestFileName}";
                        FileManifest.Serialize(buildinFileManifestPath, fileManifest, true);
                        if (freshAssetDataBase)
                        {
                            UnityEditor.AssetDatabase.Refresh();
                            UnityEditor.AssetDatabase.SaveAssets();
                        }
                    }
                    break;
                }
            }
        }
    }
}
