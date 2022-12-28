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
using MHLab.Patch.Core.Utilities;

public class BuildTaskCopyAsssetBundle : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        var bundleInfo = GetData<Dictionary<string, BundleInfo>>(CONTEXT_BUNDLE_INFO);
        var assets = GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);
        var outFolder =Build.GetTempBundleOutDirectoryPath();
        var results = GetData<IBundleBuildResults>(CONTEXT_BUNDLE_RESULT);
        GenerateBundleManifest(outFolder, results, bundleInfo, assets, out var bundleManifest);
        CopyBundleFilesToVersionFolder(outFolder, bundleManifest);
        this.FinishTask();
    }
    public void CopyBundleFilesToVersionFolder(string srcBundleFolder, BundleManifest bundleManifest)
    {
        EditorUtility.DisplayProgressBar("CopyBundleFilesToVersionFolder", "同步到最新的文件夹", 0);
        var versionDirectory = GetData<string>(CONTEXT_VERSION_DIRECTORY);
        // versionDirectory = $"{versionDirectory}/{BuildTask.TEMP_VERSION_DIRECTORY}";
        Directory.CreateDirectory(versionDirectory);
        for (int i = 0; i < bundleManifest.BundleList.Length; i++)
        {
            var fileName = bundleManifest.BundleList[i].FileName;
            var relativePath = bundleManifest.BundleList[i].RelativePath;
            var dstPath = $"{versionDirectory}/{relativePath}";
            var dstDirectoryPath = Path.GetDirectoryName(dstPath);
            if (!Directory.Exists(dstDirectoryPath))
            {
                Directory.CreateDirectory(dstDirectoryPath);
            }
            File.Copy($"{srcBundleFolder}/{fileName}", dstPath);
        }
        var additionFileInfos = GetData<Dictionary<string, AdditionFileInfo>>(CONTEXT_FILE_ADDITION_INFO);
        if (additionFileInfos == null)
        {
            additionFileInfos = new Dictionary<string, AdditionFileInfo>();
            SetData(CONTEXT_FILE_ADDITION_INFO, additionFileInfos);
        }
        var rPath = URSRuntimeSetting.instance.BundleManifestFileName;
        if (additionFileInfos.ContainsKey(rPath))
        {
            Debug.LogWarning("already exist path " + rPath);
        }
        var tags = new string[] { URSRuntimeSetting.instance.DefaultTag, URSRuntimeSetting.instance.BuildinTag };
        additionFileInfos[rPath] = new AdditionFileInfo()
        {
            Tags = tags,
            IsEncrypted = false,
            IsUnityBundle = false
        };
        BundleManifest.Serialize($"{versionDirectory}/{URSRuntimeSetting.instance.BundleManifestFileName}", bundleManifest, true);
        EditorUtility.ClearProgressBar();
    }

    public void GenerateBundleManifest(
        string tmpBundleOutDirectory,
        IBundleBuildResults result,
        Dictionary<string, BundleInfo> bundleInfos,
        Dictionary<string, AssetInfo> assets,
        out BundleManifest bundleManifest)
    {
        EditorUtility.DisplayProgressBar("GenerateBundleManifest", "生成bundle的版本信息文件", 0);
        bundleManifest = new BundleManifest();
        List<AssetMeta> assetList = new List<AssetMeta>();
        // List<FileMeta> bundleList = new List<FileMeta>();
        Dictionary<string, AssetWithBundle> assetWithBundleInfo = new Dictionary<string, AssetWithBundle>();
        //Dictionary<string, HashSet<string>> bundleTags = new Dictionary<string, HashSet<string>>();
        Dictionary<string, FileMeta> bundleFileMetas = new Dictionary<string, FileMeta>();
        Dictionary<string, HashSet<string>> bundleTags = new Dictionary<string, HashSet<string>>();
        foreach (var kv in assets)
        {
            var assetInfo = kv.Value;
            var assetPath = kv.Key;
            if (assetInfo.isMain)
            {
                AssetMeta am = new AssetMeta();
                am.AssetPath = assetPath;
                assetList.Add(am);

                AssetWithBundle ab = new AssetWithBundle();
                ab.asset = am;
                var assetBundleName = assetInfo.GetAssetBundleName();
                ab.mainBundleName = assetBundleName;
                ab.dependencyBundleNames = new List<string>();
                var dps = result.BundleInfos[assetBundleName].Dependencies;
                ab.dependencyBundleNames.AddRange(dps);
                ab.dependencyBundleNames.Distinct();
                if (ab.dependencyBundleNames.Contains(assetBundleName))
                {
                    //Debug.LogError("internal error :find  main " + assetBundleName + " in dependency");
                    ab.dependencyBundleNames.Remove(assetBundleName);
                }
                if (!bundleTags.ContainsKey(assetBundleName))
                {
                    HashSet<string> hashSet = new HashSet<string>();
                    hashSet.UnionWith(assetInfo.customTag);
                    bundleTags[assetBundleName] = hashSet;
                }
                else
                {
                    HashSet<string> hashSet = bundleTags[assetBundleName];
                    hashSet.UnionWith(assetInfo.customTag);
                }
                if (ab.dependencyBundleNames != null && ab.dependencyBundleNames.Count > 0)
                {
                    for (int i = 0; i < ab.dependencyBundleNames.Count; i++)
                    {
                        var dpName = ab.dependencyBundleNames[i];
                        // Debug.LogError("internal error :find  main " + assetBundleName + " in dependency" + dpName);
                        if (!bundleTags.ContainsKey(dpName))
                        {
                            HashSet<string> hashSet = new HashSet<string>();
                            hashSet.UnionWith(assetInfo.customTag);
                            bundleTags[dpName] = hashSet;
                        }
                        else
                        {
                            HashSet<string> hashSet = bundleTags[dpName];
                            hashSet.UnionWith(assetInfo.customTag);
                        }
                    }
                }
               // assetList.Add(am);
                assetWithBundleInfo.Add(assetPath, ab);
            }
        }

        foreach (var kv in bundleTags)
        {
            var bundleName = kv.Key;
            var tagSet = kv.Value;
            if (!bundleFileMetas.ContainsKey(bundleName))
            {
                string bundleHardiskPath = tmpBundleOutDirectory + "/" + bundleName;
                var hash = Hashing.GetFileXXhash(bundleHardiskPath);
                // string crc = HashUtility.FileCRC32(bundleHardiskPath);
                long bytesize = FileUtility.GetFileSize(bundleHardiskPath);
                FileMeta assetBundleFileMeta = new FileMeta(bundleName, hash, bytesize, tagSet.ToArray(), false, true);
                var targetRelativePath = $"bundles/{bundleName}";
                assetBundleFileMeta.SetRelativePath(targetRelativePath);

                var additionFileInfos = GetData<Dictionary<string, AdditionFileInfo>>(CONTEXT_FILE_ADDITION_INFO);
                if (additionFileInfos == null)
                {
                    additionFileInfos = new Dictionary<string, AdditionFileInfo>();
                    SetData(CONTEXT_FILE_ADDITION_INFO, additionFileInfos);
                }

                if (additionFileInfos.ContainsKey(targetRelativePath))
                {
                    Debug.LogWarning("already exist path " + targetRelativePath);
                }
                additionFileInfos[targetRelativePath] = assetBundleFileMeta.GetAdditionFileInfo();
                bundleFileMetas[bundleName] = assetBundleFileMeta;
            }
        }

        bundleManifest.BundleList = bundleFileMetas.Values.ToArray();

        for (int i = 0; i < assetList.Count; i++)
        {
            var am = assetList[i];
            List<int> dpIndexs = new List<int>();
            var assetWithBundelInfo = assetWithBundleInfo[am.AssetPath];
            for (int j = 0; j < bundleManifest.BundleList.Length; j++)
            {
                var fm = bundleManifest.BundleList[j];
                if (fm.FileName == assetWithBundelInfo.mainBundleName)
                {
                    am.BundleID = j;
                }
                if (assetWithBundelInfo.dependencyBundleNames.Contains(fm.FileName))
                {
                    if (!dpIndexs.Contains(j))
                    {
                        dpIndexs.Add(j);
                    }
                }
            }
            am.DependIDs = dpIndexs.ToArray();
        }
        bundleManifest.AssetList = assetList.ToArray();


        EditorUtility.ClearProgressBar();
    }

}
