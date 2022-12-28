using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TagInfo = System.Collections.Generic.Dictionary<Bewildered.SmartLibrary.TagRule.TagRuleType, string>;
using URS;
using YooAsset.Utility;
using System.Linq;
using YooAsset;
using UnityEditor.Build.Content;
using BuildCompression = UnityEngine.BuildCompression;
using UnityEditor.Search;
using Context = System.Collections.Generic.Dictionary<string, object>;
using MHLab.Patch.Core.IO;
using MHLab.Patch.Core.Utilities;

public class BuildTaskCopyLatestResourceToStreamAsset : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        CopyLatestResourceToStreamAsset();
        this.FinishTask();
    }
    public void CopyLatestResourceToStreamAsset()
    {
        var targetVersionCode = GetData<string>(CONTEXT_COPY_STREAM_TARGET_VERSION);
        var versionRootDirectory = Build.GetVersionRoot();
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
                        var streamSandboxFolderName = YooAsset.AssetPathHelper.GetStreamingSandboxDirectory();
                        if (Directory.Exists(streamSandboxFolderName))
                        {
                            Directory.Delete(streamSandboxFolderName, true);
                        }
                        for (int i = 0; i < fileMetas.Count; i++)
                        {
                            string rlPath = fileMetas[i].RelativePath;
                            string srcPath = $"{versionDirectory}/{rlPath}";
                            string dstPath = $"{streamSandboxFolderName}/{rlPath}";
                            var dstDirectoryName = Path.GetDirectoryName(dstPath);
                            Directory.CreateDirectory(dstDirectoryName);
                            File.Copy(srcPath, dstPath);
                        }
                        FileManifest fileManifest = new FileManifest(fileMetas.ToArray());
                        string buildinFileManifestPath = $"{streamSandboxFolderName}/{URSRuntimeSetting.instance.FileManifestFileName}";
                        FileManifest.Serialize(buildinFileManifestPath, fileManifest, true);
                        UnityEditor.AssetDatabase.Refresh();
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
                    break;
                }
            }
        }
    }
}
