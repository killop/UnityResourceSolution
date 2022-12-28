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
    public const long MAX_COMBINE_SHARE_AB_ITEM_SIZE = 500*1024 * 8; // 500K �ļ����С������������ܺϲ�
    public const long MAX_COMBINE_SHARE_AB_SIZE = 1024 * 1024 * 8; // 1M ���պϲ���Ŀ���С
    public const long MAX_COMBINE_SHARE_NO_NAME = 10 * 1024 * 8; // 10K û�а����������� 
    public const int MAX_COMBINE_SHARE_NO_NAME_REFERENCE_COUNT = 5; // û�а������������ü���
    public const int MIN_COMBINE_SHARE_MIN_REFERENCE_COUNT = 7;//��С�����ü���
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

        int allShareCount = 0; //���е�share ab ������
        int allShareCanCombine = 0; // ����size �ϸ��ab
        int allShareRemoveByNoName = 0; // ��Ϊ size ̫С��ref count ̫�� �����ϲ�����������
        int allShareRmoveByRefrenceCountTooFew = 0;// ��Ϊ ref count ̫�� �����ϲ�
        int allFinalCombine = 0;// ���պϲ�������

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
            if (abInfo.size < MAX_COMBINE_SHARE_NO_NAME && abInfo.refrenceCount < MAX_COMBINE_SHARE_NO_NAME_REFERENCE_COUNT)
            {
                allShareRemoveByNoName++;
                var bundleInfo = bundleInfos[bundleName];
                foreach (var assetPath in bundleInfo.paths)
                {
                    var assetInfo = assets[assetPath];
                    //Debug.LogError(" ȡ������"+assetInfo.assetPath+ " IsShareAsset " + assetInfo.IsShareAsset()+" bundle name"+ bundleName);
                    assetInfo.CancelBundleName(globalBundleExtraAssets);
                }
                allCombines.Remove(bundleName);
                continue;
            }

            if (abInfo.refrenceCount < MIN_COMBINE_SHARE_MIN_REFERENCE_COUNT) 
            {
                allShareRmoveByRefrenceCountTooFew++;
                allCombines.Remove(bundleName);
                continue;
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
                    // Debug.LogError("ǿ�ư��� "+assetInfo.assetPath+ " IsShareAsset " + assetInfo.IsShareAsset()+ "bundleName "+ bundleName);
                   // if (assetInfo.IsShareAsset())
                    {
                        assetInfo.shareCombineAssetBundleName = combineAbName;
                    }
                   
                }
            }
        }
        Debug.Log($"�ܹ���share ab������ {allShareCount}����С�ϸ������ {allShareCanCombine}����Ϊab ̫С�����ü���̫�ٶ���ȡ������������{allShareRemoveByNoName}����Ϊ���ù��ٱ��Ƴ��ϲ������� {allShareRmoveByRefrenceCountTooFew} ,���� {allFinalCombine}��share ab���ϲ���{combineBundles.Count}�� share_combine,��Ϊ��κϲ��������ܹ�������{allShareRemoveByNoName + allFinalCombine - combineBundles.Count}�� share bundle");
    }
}
