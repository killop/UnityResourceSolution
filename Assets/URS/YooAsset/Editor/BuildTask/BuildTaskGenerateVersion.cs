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
using URS.Editor;
using MHLab.Patch.Core.IO;

public class BuildTaskGenerateVersion : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
       // List<FileMeta> rawFileMetas = null;
       // if (this._context.ContainsKey(CONTEXT_RAW_FILES))
       // {
       //     rawFileMetas = (List<FileMeta>)this._context[CONTEXT_RAW_FILES];
       // }
        GenerateVersionFileManifest();
        this.FinishTask();
    }
    public void GenerateVersionFileManifest()
    {
        var versionDirectory = (string)this._context[CONTEXT_VERSION_DIRECTORY];
        var additionFileInfos = GetData<Dictionary<string, AdditionFileInfo>>(CONTEXT_FILE_ADDITION_INFO);
        var buildingVersion = GetData<string>(CONTEXT_BUILDING_VERSION);
        VersionBuilder.BuildVersion(versionDirectory,new FileSystem(), buildingVersion, additionFileInfos,out string newVersionDirctoryName);
        SetData(CONTEXT_VERSION_DIRECTORY, newVersionDirctoryName);
    }
    /*
    public  void GenerateVersionFileManifest()
    {
        List<FileMeta> final = new List<FileMeta>();
        var versionDirectory = (string)this._context[CONTEXT_VERSION_DIRECTORY];
        var tempVersionDirectory = $"{versionDirectory}/{BuildTask.TEMP_VERSION_DIRECTORY}";
        var bundleFilePath = YooAsset.ResourceSettingData.Setting.BundleManifestFileName;
        var bundelManifestPath = $"{tempVersionDirectory}/{bundleFilePath}";
        string jsonData = File.ReadAllText(bundelManifestPath);

        var bundleManifest = BundleManifest.Deserialize(jsonData);
        string fileName = bundleFilePath;
        string hash = HashUtility.FileMD5(bundelManifestPath);
        string crc32 = HashUtility.FileCRC32(bundelManifestPath);
        long size = FileUtility.GetFileSize(bundelManifestPath);
        string[] tags = new string[] { DEFAULT_TAG, BUILDIN_TAG };
        FileMeta bunldeFileMeta = new FileMeta(fileName, hash, crc32, size, tags, false, false);
        bunldeFileMeta.SetRelativePath(fileName);

        final.Add(bunldeFileMeta);
        if (rawFileMetas != null)
        {
            final.AddRange(rawFileMetas);
        }
        final.AddRange(bundleManifest.BundleList);
        FileManifest fm = new FileManifest(final.ToArray());
        var fileManifestSavePath = $"{tempVersionDirectory}/{YooAsset.ResourceSettingData.Setting.FileManifestFileName}";
        FileManifest.Serialize(fileManifestSavePath, fm, true);

        string fileManifestHash = HashUtility.FileSHA1(fileManifestSavePath);
        string hashFileSavePath = $"{versionDirectory}/{YooAsset.ResourceSettingData.Setting.FileManifestHashFileName}";
        File.WriteAllText(hashFileSavePath, fileManifestHash);
        Directory.Move(tempVersionDirectory, $"{versionDirectory}/{fileManifestHash}");
    }
    */
}
