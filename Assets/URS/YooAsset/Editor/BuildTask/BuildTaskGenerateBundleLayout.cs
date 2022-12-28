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
using UnityEditor.U2D;
using Context = System.Collections.Generic.Dictionary<string, object>;

public class BuildTaskGenerateBundleLayout : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        GenerateBundleLayout();
        this.FinishTask();
    }
    
    public void GenerateBundleLayout()
    {
        EditorUtility.DisplayProgressBar("GenerateBundleLayout", "收集bundle layout", 0);
        var assets = GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);
#if SPRITE_STRIP
        BuildTaskStripSpriteInAtlas.Checker checker = GetOrAddData<BuildTaskStripSpriteInAtlas.Checker>(CONTEXT_SPRITE_CHECHER);
#endif
        var assetBundles = new Dictionary<string, BundleInfo>();
        foreach (var kv in assets)
        {
            var assetInfo = kv.Value;
            if (assetInfo.HasAssetBundleName())
            {
#if SPRITE_STRIP
                if (checker.IsSpriteInAtlas(assetInfo.assetPath))
                    continue;
#endif
                var assetBundleName = assetInfo.GetAssetBundleName();
                if (!assetBundles.ContainsKey(assetBundleName))
                {
                  
                    var bundleName = assetBundleName;
                    var paths = new List<string>();
                    paths.Add(assetInfo.assetPath);
                    var isShare = assetInfo.IsShareAsset();
                    BundleInfo bi = new BundleInfo(bundleName, paths, isShare);
                    //  bi.tags.UnionWith(assetInfo.customTag);
                    assetBundles[bi.bundleName] = bi;
                }
                else
                {
                    BundleInfo bi = assetBundles[assetBundleName];
                    var paths = bi.paths;
                    if (!paths.Contains(assetInfo.assetPath))
                    {
                        paths.Add(assetInfo.assetPath);
                    }
                    //bi.tags.UnionWith(assetInfo.customTag);
                }
            }
        }
        var globalBundleExtraAssets = GetOrAddData<GlobalBundleExtraAsset>(CONTEXT_GLOBAL_BUNDLE_EXTRA_ASSET);
        foreach (var kv in assetBundles) 
        {
           // globalBundleExtraAssets.TryMergeExtraAssets(kv.Value);
        }
        SetData(CONTEXT_BUNDLE_INFO, assetBundles);
        EditorUtility.ClearProgressBar();
    }
    
}
