using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using YooAsset.Utility;
using System.IO;

namespace URS
{
    [Serializable]
    public class AssetMeta
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// 所属资源包ID
        /// </summary>
        public int BundleID;

        /// <summary>
        /// 依赖的资源包ID列表
        /// </summary>
        public int[] DependIDs;
    }

    /// <summary>
    /// Bundle依赖
    /// </summary>
    [Serializable]
    public class BundleManifest
    {
        /// <summary>
        /// 资源列表（主动收集的资源列表）
        /// </summary>
        public AssetMeta[] AssetList;

        /// <summary>
        /// 资源包列表
        /// </summary>
        public FileMeta[] BundleList;

        /// <summary>
        /// 资源包集合（提供BundleName获取PatchBundle）
        /// </summary>
        [NonSerialized]
        private  Dictionary<string, FileMeta> BundleMap = new Dictionary<string, FileMeta>();

        /// <summary>
        /// 资源映射集合（提供AssetPath获取PatchAsset）
        /// </summary>
        [NonSerialized]
        private  Dictionary<string, AssetMeta> AssetMap = new Dictionary<string, AssetMeta>();


        /// <summary>
        /// 获取资源依赖列表
        /// </summary>
        public List<FileMeta> GetAllDependenciesRelativePath(string assetPath)
        {
            if (AssetMap.TryGetValue(assetPath, out AssetMeta patchAsset))
            {
                List<FileMeta> result = new List<FileMeta>(patchAsset.DependIDs.Length); // TODO:优化gc
                foreach (var dependID in patchAsset.DependIDs)
                {
                    if (dependID >= 0 && dependID < BundleList.Length)
                    {
                        var dependPatchBundle = BundleList[dependID];
                        result.Add(dependPatchBundle);
                    }
                    else
                    {
                        throw new Exception($"Invalid depend id : {dependID} Asset path : {assetPath}");
                    }
                }
                return result;
            }
            else
            {
                Debug.LogError($"Not found asset path in patch manifest : {assetPath}");
                return null;
            }
        }

        /// <summary>
        /// 获取资源包名称
        /// </summary>
        public FileMeta GetBundleFileMeta(string assetPath)
        {
            if (AssetMap.TryGetValue(assetPath, out AssetMeta patchAsset))
            {
                int bundleID = patchAsset.BundleID;
                if (bundleID >= 0 && bundleID < BundleList.Length)
                {
                    var patchBundle = BundleList[bundleID];
                    return patchBundle;
                }
                else
                {
                    throw new Exception($"Invalid depend id : {bundleID} Asset path : {assetPath}");
                }
            }
            else
            {
                Debug.LogWarning($"Not found asset path in patch manifest : {assetPath}");
                return FileMeta.ERROR_FILE_META;
            }
        }

        public void AfterDeserialize()
        {
            if (BundleList != null && BundleList.Length > 0)
            {
                foreach (var bundle in BundleList)
                {
                    if (!BundleMap.ContainsKey(bundle.RelativePath))
                    {
                        BundleMap.Add(bundle.RelativePath, bundle);
                    }
                }
            }

            if (AssetList != null && AssetList.Length > 0)
            {
                foreach (var asset in AssetList)
                {
                    if (!AssetMap.ContainsKey(asset.AssetPath))
                    {
                        AssetMap.Add(asset.AssetPath, asset);
                    }
                }
            }
        }
        /// <summary>
        /// 序列化
        /// </summary>
        public static void Serialize(string savePath, BundleManifest patchManifest,bool pretty=false)
        {
            string json = JsonUtility.ToJson(patchManifest, pretty);
            FileUtility.CreateFile(savePath, json);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        public static BundleManifest Deserialize(string jsonData)
        {
            BundleManifest bundleManifest = JsonUtility.FromJson<BundleManifest>(jsonData);
            bundleManifest.AfterDeserialize();
            return bundleManifest;
        }
    }
}


