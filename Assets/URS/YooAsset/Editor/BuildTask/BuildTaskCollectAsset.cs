
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

public class BuildTaskCollectAsset : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        var shadervaraintCollectionPath = "Assets/GameResources/ShaderVarians/ShaderVariantCollection.shadervariants";
        CollectAssets(true, shadervaraintCollectionPath);
        this.FinishTask();
    }

    public void CollectAssets(bool combineShareShader = true, string shadervaraintCollectionPath = "")
    {
#if SPRITE_STRIP
        BuildTaskStripSpriteInAtlas.Checker checker = GetOrAddData<BuildTaskStripSpriteInAtlas.Checker>(CONTEXT_SPRITE_CHECHER);
#endif
        EditorUtility.DisplayProgressBar("CollectAssets", "整理资源", 0);
        var dependencyCache = new Dictionary<string, List<string>>();
        System.Func<string, List<string>> getDependencys = (string assetPath) =>
        {
            if (dependencyCache.ContainsKey(assetPath))
            {
                return dependencyCache[assetPath];
            }
            else
            {
                var dps = AssetDatabase.GetDependencies(assetPath);
                var list = new List<string>();
                if (dps != null)
                {
                    foreach (var dp in dps)
                    {
#if SPRITE_STRIP
                        if (!checker.IsSpriteInAtlas(dp))
                            list.Add(dp);
#else 
                        list.Add(dp);
#endif
                    }
                }
                dependencyCache[assetPath] = list;
                return list;
            }
        };
        var assets = new Dictionary<string, AssetInfo>();

        var root = SessionData.instance;
        if (root != null)
        {
            foreach (var kv in root.IDToCollectionMap)
            {
                var collection = kv.Value;
                var en = collection.GetEnumerator();
                GetCollectionTagInfo(collection, out var tagInfo);
                while (en.MoveNext())
                {
                    var item = en.Current;
                    if (Directory.Exists(item.AssetPath)) continue;
                    if (assets.ContainsKey(item.AssetPath))
                    {
                        var first = assets[item.AssetPath];
                        var second = collection;
                        Debug.LogErrorFormat("存在2个组重复收集的资源 资源路径 {0}，这种情况一下只会应用第一组的配置", item.AssetPath);
                    }
                    else
                    {
                        string assetBunleName = ExtractAssetBundleName(item, collection, tagInfo);
                        assetBunleName= AssetInfo.FormatBundleName(assetBunleName);
                        string resourceGroup = ExtractResourceGroup(item, collection, tagInfo);
                        List<string> customTag = ExtractCustomTag(item, collection, tagInfo);
                        var assetInfo = new AssetInfo()
                        {
                            assetPath = item.AssetPath,
                            assetBundleNames = new HashSet<string>(new string[] { assetBunleName }),
                            customTag = new HashSet<string>(customTag.ToArray()),
                            isMain = true,
                            refrenceCount = 1,
                            mainAssetDependencys = getDependencys(item.AssetPath)
                        };
                        assets[item.AssetPath] = assetInfo;

                    }
                }
            }
            Debug.Log("find self define asset count:" + assets.Count);
            if (combineShareShader)
            {
                var shareShaderAssets = new Dictionary<string, AssetInfo>();
                var shaderVaraintCollectionTag = new HashSet<string>();

                foreach (var item in assets)
                {
                    var referenceAssets = getDependencys(item.Key);
                    var mainAssetTag = item.Value.customTag.ToArray();
                    if (referenceAssets != null && referenceAssets.Count > 0)
                    {
                        for (int i = 0; i < referenceAssets.Count; i++)
                        {
                            var referenceAsset = referenceAssets[i];

                            if (!assets.ContainsKey(referenceAsset))
                            {
                                if (shareShaderAssets.ContainsKey(referenceAsset))
                                {
                                    var shaderAsset = shareShaderAssets[referenceAsset];
                                    shaderAsset.customTag.UnionWith(mainAssetTag);
                                }
                                else
                                {
                                    if (AssetDatabase.GetMainAssetTypeAtPath(referenceAsset) == typeof(Shader))
                                    {
                                        var assetInfo = new AssetInfo()
                                        {
                                            assetPath = referenceAsset,
                                            assetBundleNames = new HashSet<string>(new string[] { "share_shader.bundle" }),
                                            customTag = new HashSet<string>(mainAssetTag),
                                            isMain = true,
                                            refrenceCount = 1,
                                            mainAssetDependencys = getDependencys(referenceAsset)
                                        };
                                        shareShaderAssets[referenceAsset] = assetInfo;
                                        shaderVaraintCollectionTag.UnionWith(mainAssetTag);
                                    }
                                }

                            }
                        }
                    }
                }
                var shaderVaraintCollectionAssetInfo = new AssetInfo()
                {
                    assetPath = shadervaraintCollectionPath,
                    assetBundleNames = new HashSet<string>(new string[] { "share_shader.bundle" }),
                    customTag = shaderVaraintCollectionTag,
                    isMain = true,
                    refrenceCount = 1,
                    mainAssetDependencys = getDependencys(shadervaraintCollectionPath)
                };
                shareShaderAssets[shadervaraintCollectionPath]= shaderVaraintCollectionAssetInfo;

                foreach (var item in shareShaderAssets)
                {
                    assets[item.Key] = item.Value;
                }
            }
            var dependencyAssets = new Dictionary<string, AssetInfo>();
            foreach (var item in assets)
            {
                var assetInfo = item.Value;
                var dependencys = assetInfo.mainAssetDependencys;
                foreach (var dp in dependencys)
                {
                    if (!assets.ContainsKey(dp))
                    {
                        if (!dependencyAssets.ContainsKey(dp))
                        {
                            var dpInfo = new AssetInfo()
                            {
                                assetPath = dp,
                                assetBundleNames = new HashSet<string>(assetInfo.assetBundleNames),
                                customTag = new HashSet<string>(assetInfo.customTag),
                                isMain = false,
                                refrenceCount = 1,
                            };
                            dependencyAssets[dp] = dpInfo;
                        }
                        else
                        {
                            var dpInfo = dependencyAssets[dp];
                            dpInfo.assetBundleNames.UnionWith(assetInfo.assetBundleNames);
                            dpInfo.customTag.UnionWith(assetInfo.customTag);
                            dpInfo.refrenceCount = dpInfo.refrenceCount + 1;
                        }
                    }
                }
            }
            foreach (var item in dependencyAssets)
            {
                assets[item.Key] = item.Value;
            }
            var paths= new List<string>(assets.Keys);
            foreach (var path in paths)
            {
                bool remove = false;
                var extension= Path.GetExtension(path);
                if (extension==".cs")
                {
                    remove = true;
                }
                if (path.Contains("/Editor/")) 
                {
                    remove = true;
                }
                if (path.Contains("/Editor Resources/"))
                {
                    remove = true;
                }
                if (remove)
                {
                    assets.Remove(path);    
                }
            }   
            SetData(CONTEXT_ASSET_INFO, assets);
            EditorUtility.ClearProgressBar();
        }
    }

    public static void GetCollectionTagInfo(
        LibraryCollection collection,
        out TagInfo tagsInfo
        )
    {
        tagsInfo = new TagInfo();
        foreach (var rule in collection.Rules)
        {
            var tagRule = rule as TagRule;
            if (tagRule != null)
            {
                tagsInfo[tagRule.TagType] = tagRule.Text;
            }
        }
    }
    public static string ExtractAssetBundleName(LibraryItem item, LibraryCollection collection, TagInfo tagInfo)
    {
        string assetBundleName = collection.CollectionName;
        if (tagInfo.ContainsKey(TagRule.TagRuleType.BundleName))
        {

            string text = tagInfo[TagRule.TagRuleType.BundleName];

            if (!string.IsNullOrEmpty(text))
            {
                switch (text)
                {
                    case "collection_name":
                        {
                            assetBundleName = $"{collection.CollectionName}.bundle";
                            break;
                        }
                    case "directory_name":
                        {
                            assetBundleName = $"{GetParentDiretoryName(item)}.bundle";
                            break;
                        }
                    case "collection_name_and_self_name":
                        {
                            string selfName = Path.GetFileName(item.AssetPath);
                            assetBundleName = $"{collection.CollectionName}_{selfName}.bundle";
                            break;
                        }
                    case "directory_name_and_self_name":
                        {
                            string selfName = Path.GetFileName(item.AssetPath);
                            assetBundleName = $"{GetParentDiretoryName(item)}_{selfName}.bundle";
                            break;
                        }
                        /// 如果对分包有特殊的要求，可以在这里扩展
                }
            }
        }
        //Debug.LogError("ab name "+ assetBundleName);
        return assetBundleName;
    }
   
    public string ExtractResourceGroup(LibraryItem item, LibraryCollection collection, TagInfo tagInfo)
    {
        string resourceGroup = "";
        if (tagInfo.ContainsKey(TagRule.TagRuleType.ResourceGroup))
        {
            resourceGroup = tagInfo[TagRule.TagRuleType.ResourceGroup];
        }
        return resourceGroup;
    }

    public List<string> ExtractCustomTag(LibraryItem item, LibraryCollection collection, TagInfo tagInfo)
    {
        List<string> result = new List<string>();
        result.Add(URSRuntimeSetting.instance.DefaultTag);
        if (tagInfo.ContainsKey(TagRule.TagRuleType.CustomTag))
        {
            string text = tagInfo[TagRule.TagRuleType.CustomTag];
            // result = new List<string>();
            result.AddRange(text.Split('|'));
        }
        return result;
    }
    public static string GetParentDiretoryName(LibraryItem item)
    {
        string path = item.AssetPath;
        string parentName = Directory.GetParent(path).Name;
        return parentName;
    }
}
