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
using Context = System.Collections.Generic.Dictionary<string, object>;

[Serializable]
public class ShareAssetBundleName 
{
    [SerializeField]
    public  ShareAssetBundleNameItem[] AssetBundleNameItems;

    [NonSerialized]
    public Dictionary<string, string> AssetPathToBundleName;

    public void Init()
    {
        AssetPathToBundleName = new Dictionary<string, string>();
        foreach (var item in AssetBundleNameItems)
        {
            AssetPathToBundleName[item.AssetPath] = item.AssetBundleName;
        }
    }
}

[Serializable]
public class ShareAssetBundleNameItem
{
    [SerializeField]
    public string AssetPath;

    [SerializeField]
    public string AssetBundleName;

}
public class BuildTaskOptimizeShareAssetBundleName : BuildTask
{
    private Dictionary<string, string> _assetPathToBundleName = null;
    private int _nextBundleNameIndex = 1;
    
    public override void BeginTask()
    {
        base.BeginTask();
        OptimizShareAssetBundleName();
        this.FinishTask();
    }
    public void LoadConfig()
    {
        var filePath = Build.GetShareAssetBundleNameConfigFilePath();

        ShareAssetBundleName sabn = null;
        if (File.Exists(filePath))
        {
            sabn = UnityEngine.JsonUtility.FromJson<ShareAssetBundleName>(File.ReadAllText(filePath));
            sabn.Init();
        }
        if (sabn != null)
        {
            _assetPathToBundleName = sabn.AssetPathToBundleName;
            var currentMaxIndex = 0;
            foreach (var item in _assetPathToBundleName)
            {
                var bundleName = item.Value;
                bundleName = Path.GetFileNameWithoutExtension(bundleName);
                var split = bundleName.Split("-");
                var index = int.Parse(split[2]);
                if (index > currentMaxIndex)
                {
                    currentMaxIndex = index;
                }
            }
            _nextBundleNameIndex = currentMaxIndex + 1;
        }
        else 
        {
            _assetPathToBundleName = new Dictionary<string, string>();
            _nextBundleNameIndex = 1;
        }
    }

    public string GetAssetBundleName(string assetPath, List<string> orignBundleAssetPaths) 
    {
        if (_assetPathToBundleName.ContainsKey(assetPath))
        {
            return _assetPathToBundleName[assetPath];
        }
        if (orignBundleAssetPaths.Count > 0) 
        {
            foreach (var path in orignBundleAssetPaths)
            {
                if (assetPath == path)
                {
                    continue;
                }
                else
                {
                    if (_assetPathToBundleName.ContainsKey(path)) 
                    {
                        var sameBundleName = _assetPathToBundleName[path];
                        _assetPathToBundleName[assetPath] = sameBundleName;
                        return sameBundleName;
                    }
                }
            }
        }
        var newBundleName = $"share-static-{_nextBundleNameIndex++}.bundle";
        _assetPathToBundleName[assetPath] = newBundleName;
        return newBundleName;
    }
    public void SaveConfig(Dictionary<string,string> orignAssets)
    {
        List<string> deletes= new List<string>();
        foreach (var item in _assetPathToBundleName)
        {
            if (!orignAssets.ContainsKey(item.Key)) {
                deletes.Add(item.Key);
            }
        }
        foreach (var item in deletes)
        {
            _assetPathToBundleName.Remove(item);
        }
        List< ShareAssetBundleNameItem > abNameItems= new List<ShareAssetBundleNameItem>();
        foreach (var item in _assetPathToBundleName)
        {
            ShareAssetBundleNameItem abNameItem = new ShareAssetBundleNameItem();
            abNameItem.AssetPath = item.Key;
            abNameItem.AssetBundleName= item.Value;
            abNameItems.Add(abNameItem);
        }
        ShareAssetBundleName config= new ShareAssetBundleName ();
        config.AssetBundleNameItems = abNameItems.ToArray();

        var filePath = Build.GetShareAssetBundleNameConfigFilePath();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        File.WriteAllText(filePath,UnityEngine.JsonUtility.ToJson(config,true));
    }
    public void OptimizShareAssetBundleName()
    {
        EditorUtility.DisplayProgressBar("BuildTaskOptimizeShareAssetBundleName", "优化包名", 0);

        LoadConfig();
        var bundleInfo = GetData<Dictionary<string, BundleInfo>>(CONTEXT_BUNDLE_INFO);
        var newBundleInfos = new Dictionary<string, BundleInfo>();
        var orignShareAssetToBundleName = new Dictionary<string, string>();
        foreach (var item in bundleInfo)
        {
            var bundleAssetPaths = item.Value.paths;
            if (item.Value.isShareBundle)
            {
                foreach (var assetPath in bundleAssetPaths)
                {
                    if (!orignShareAssetToBundleName.ContainsKey(assetPath))
                    {
                        orignShareAssetToBundleName[assetPath] = item.Value.bundleName;
                    }
                    else
                    {
                        throw new Exception($"bundle name conflict ,first {orignShareAssetToBundleName[assetPath]},second {item.Value.bundleName} ");
                    }
                }
            }
            else 
            {
                newBundleInfos.Add(item.Key, item.Value);
            }
        }

        var newShareBundle= new Dictionary<string, string>();
        foreach (var item in orignShareAssetToBundleName)
        {
            var assetPath = item.Key;
            var orignBundleAssetPaths = bundleInfo[item.Value].paths;
            var newBundleName = GetAssetBundleName(assetPath,orignBundleAssetPaths);
            newShareBundle[assetPath] = newBundleName;
        }
        foreach (var item in newShareBundle)
        {
            var assetPath = item.Key;
            var newBundleName = item.Value;
            if (!newBundleInfos.ContainsKey(newBundleName))
            {
                List<string> paths = new List<string>();
                paths.Add(assetPath);
                BundleInfo newBundleInfo = new BundleInfo(newBundleName, paths, true);
                newBundleInfos.Add(newBundleName, newBundleInfo);
            }
            else
            {
                BundleInfo newBundleInfo = newBundleInfos[newBundleName];
                var paths = newBundleInfo.paths;
                if (!paths.Contains(assetPath)) {
                    paths.Add(assetPath);
                }
            }
        }
        SetData(CONTEXT_BUNDLE_INFO, newBundleInfos);
        SaveConfig(orignShareAssetToBundleName);
        EditorUtility.ClearProgressBar();
    }

}
