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

public class BuildTaskRegenerateAssetBundleName : BuildTask
{
    public const long MAX_COMBINE_SHARE_AB_ITEM_SIZE = 500*1024 * 8; // 500K 文件体积小于这个数量才能合并
    public const long MAX_COMBINE_SHARE_AB_SIZE = 1024 * 1024 * 8; // 1M 最终合并的目标大小

    public const long MIN_NO_NAME_COMBINE_SIZE= 32 * 1024 * 8; // 32K 最终合并的目标大小


   // public const long MAX_COMBINE_SHARE_NO_NAME = 60 * 1024 * 8; // 60K 没有包名的最大体积 
   // public const int MAX_COMBINE_SHARE_NO_NAME_REFERENCE_COUNT = 7; // 没有包名的最多的引用计数

  //  public const int MIN_COMBINE_AB_SIZE_2 = 100 * 1024 * 8;//  100K 没有包名的最大体积 
    public const int MAX_COMBINE_SHARE_MIN_REFERENCE_COUNT = 3;//最大的引用计数
    public override void BeginTask()
    {
        base.BeginTask();
        RegenerateAssetBundleName();
        this.FinishTask();
    }

    public class ABInfo {

        public string name;

        public long size;

        public int refrenceCount;
    }
    public void RegenerateAssetBundleName() 
    {
        var tempAbOutDirectory = Build.GetTempBundleOutDirectoryPath();
        var bundleInfos = GetData<Dictionary<string, BundleInfo>>(CONTEXT_BUNDLE_INFO);
        var assets = GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);
        var bundleManifest = GetData<IBundleBuildResults>(CONTEXT_BUNDLE_RESULT);
        Dictionary<string, ABInfo> allCombines = new Dictionary<string, ABInfo>();

        int allShareCount = 0; //所有的share ab 的数量
        int allShareCanCombine = 0; // 所有size 合格的ab
        int allShareRemoveByNoName = 0; // 因为 size 太小，ref count 太少 放弃合并，放弃包名
        int allShareRmoveByRefrenceCountTooFew = 0;// 因为 ref count 太少 放弃合并
        int allFinalCombine = 0;// 最终合并的数量

        foreach (var kv in bundleInfos)
        {
            var assetBundleName = kv.Key;
            var paths = kv.Value.paths;
            var isShare = kv.Value.isShareBundle;
            var abPath = $"{tempAbOutDirectory}/{assetBundleName}";
            if (!isShare) continue;
            allShareCount++;
            if (!File.Exists(abPath))
            {
                Debug.LogError("file do not exist :" + abPath);
                continue;
            }
            long bytesize = FileUtility.GetFileSize(abPath);
            if (bytesize < MAX_COMBINE_SHARE_AB_ITEM_SIZE) 
            {
                allCombines.Add(assetBundleName, new ABInfo()
                {
                    name = assetBundleName,
                    size = bytesize,
                    refrenceCount = 0
                });
                allShareCanCombine++;
            }
        }
        foreach (var kv in bundleManifest.BundleInfos)
        {
            var main = kv.Key;
            var deps = kv.Value.Dependencies;
            if (deps != null) 
            {
                var disDeps= deps.Distinct();
                foreach (var dep in disDeps)
                {
                    if (allCombines.ContainsKey(dep)) 
                    { 
                        var abSize= allCombines[dep];
                        abSize.refrenceCount += 1;
                    }
                }
            }
        }

        var abInfos =new List<ABInfo>(allCombines.Values);
        var globalBundleExtraAssets = GetOrAddData<GlobalBundleExtraAsset>(CONTEXT_GLOBAL_BUNDLE_EXTRA_ASSET);
        foreach (var abInfo in abInfos)
        {
            var bundleName = abInfo.name;
            if (abInfo.size * abInfo.refrenceCount < MIN_NO_NAME_COMBINE_SIZE)
            {
                allShareRemoveByNoName++;
                var bundleInfo = bundleInfos[bundleName];
                foreach (var assetPath in bundleInfo.paths)
                {
                    var assetInfo = assets[assetPath];
                    //Debug.LogError(" 取消包名"+assetInfo.assetPath+ " IsShareAsset " + assetInfo.IsShareAsset()+" bundle name"+ bundleName);
                    assetInfo.CancelBundleName(globalBundleExtraAssets);
                }
                allCombines.Remove(bundleName);
                continue;
            }
            else 
            {
                if ( abInfo.refrenceCount < MAX_COMBINE_SHARE_MIN_REFERENCE_COUNT)
                {
                    allShareRmoveByRefrenceCountTooFew++;
                    allCombines.Remove(bundleName);
                    continue;
                }

            }
        }

        var left =  new List<ABInfo>(allCombines.Values);
        left.Sort(
            (A,B) => {
            return A.size.CompareTo(B.size);
        });
        allFinalCombine = left.Count;
        Dictionary<string,List<string>> combineBundles= new Dictionary<string,List<string>>();
        List<string> currentCombineBundle = null;
        long currentCombineBundleSize = 0;
        for (int i = 0; i < left.Count; i++) 
        { 
            var abName= left[i].name; 
            var size= left[i].size;
            if (currentCombineBundle == null)
            {
                currentCombineBundle = new List<string>();
               
            }
            currentCombineBundle.Add(abName);
            currentCombineBundleSize += size;
            if (currentCombineBundleSize > MAX_COMBINE_SHARE_AB_SIZE)
            {
                var newCombine = string.Join("@@", currentCombineBundle);
                newCombine = $"share_combine_{HashUtility.StringSHA1(newCombine)}.bundle";
               // newCombine = $"share_combine_{(newCombine)}.bundle";
                combineBundles[newCombine] = currentCombineBundle;

                currentCombineBundle = null;
                currentCombineBundleSize = 0;
            }
        }
      
        foreach (var kv in combineBundles)
        {
            var combineAbName = kv.Key;
            var bundles = kv.Value;
            foreach (var bundleName in bundles)
            {
                var bundleInfo = bundleInfos[bundleName];
                foreach (var assetPath in bundleInfo.paths)
                {
                    var assetInfo = assets[assetPath];
                    // Debug.LogError("强制包名 "+assetInfo.assetPath+ " IsShareAsset " + assetInfo.IsShareAsset()+ "bundleName "+ bundleName);
                   // if (assetInfo.IsShareAsset())
                    {
                        assetInfo.shareCombineAssetBundleName = combineAbName;
                    }
                   
                }
            }
        }
        Debug.Log($"总共有share ab的数量 {allShareCount}，大小合格的数量 {allShareCanCombine}，因为ab 太小，引用计数太少而被取消包名的数量{allShareRemoveByNoName}，因为引用过少被移除合并的数量 {allShareRmoveByRefrenceCountTooFew} ,最终 {allFinalCombine}个share ab，合并成{combineBundles.Count}个 share_combine,因为这次合并操作，总共减少了{allShareRemoveByNoName + allFinalCombine - combineBundles.Count}个 share bundle");
    }
}

