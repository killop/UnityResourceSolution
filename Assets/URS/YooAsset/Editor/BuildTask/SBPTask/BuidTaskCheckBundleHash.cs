using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Pipeline.Utilities;

namespace URS
{
    [Serializable]
    public class BundleHash {

        [SerializeField]
        public BundleHashItem[] bundleHashItems;

        [NonSerialized]
        private Dictionary<string, BundleHashItem> lookUp = null;

        public void Init()
        {
            lookUp = new Dictionary<string, BundleHashItem>();
            if (bundleHashItems != null && bundleHashItems.Length > 0) 
            {
                foreach (var item in bundleHashItems)
                {
                    lookUp[item.BundleName] = item;
                }
            }
        }

        public void Compare(BundleHash another, HashSet<string> differentFileHashBundleName) 
        {
            foreach (var item in lookUp)
            {
                var bundleName = item.Key;
                var fileHashIsDifferent = differentFileHashBundleName.Contains(bundleName);
                if (another.lookUp.ContainsKey(bundleName)) {
                    var myBundleHash = item.Value;
                    var anotherhash = another.lookUp[bundleName];
                    if (fileHashIsDifferent)
                    {
                        Debug.LogError($">>>>>>>>>>>>>>bundleName{bundleName} file hash is different: {fileHashIsDifferent} my :{myBundleHash.ToString()} another:{anotherhash.ToString()}");
                        if (myBundleHash.OrignBundleAssetHash == anotherhash.OrignBundleAssetHash) {
                            Debug.LogError($"<<<<<<<<<<<bundleName{bundleName} file hash is different: {fileHashIsDifferent} my :{myBundleHash.ToString()} another:{anotherhash.ToString()}");
                        }
                    }
                  
                }
            }
        }

    }
    [Serializable]
    public class BundleHashItem {

        [SerializeField]
        public string BundleName;

        [SerializeField]
        public string OrignBundleAssetHash;

        [SerializeField]
        public string ResultBundleHash;

        [SerializeField]
        public string ResultBundleContentHash;

        [SerializeField]
        public uint ResultBundleCrc;

        public override string ToString()
        {
            return $"BundleName:{ BundleName} /OrignBundleAssetHash:{OrignBundleAssetHash}/ResultBundleHash:{ResultBundleHash}/ResultBundleContentHash:{ResultBundleContentHash}/ResultBundleCrc:{ResultBundleCrc}";
        }
    }
    /// <summary>
    /// The BuildTask used to generate the bundle layout.
    /// </summary>
    public class BuidTaskCheckBundleHash : IBuildTask
    {
        const int k_Version = 1;

        /// <summary>
        /// The BuidTaskCheckBundleHash version.
        /// </summary>
        public int Version { get { return k_Version; } }

        public BundleHash BundleHash = new BundleHash();

        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext]
        IBundleWriteData m_BundleWriteData;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;

        [InjectContext]
        IBuildResults m_Results;

        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.In)]
        IBundleBuildResults m_BuildBundleResults;

        [InjectContext(ContextUsage.In)]
        IWriteData m_WriteData;
#pragma warning restore 649


        /// <summary>
        /// Runs the build task with the injected context.
        /// </summary>
        /// <returns>The success or failure ReturnCode</returns>
        public ReturnCode Run()
        {
            Dictionary<string, HashSet<ObjectIdentifier>> bundleToObjectIds = new Dictionary<string, HashSet<ObjectIdentifier>>();

            foreach (var item in m_BundleWriteData.FileToObjects)
            {
                var fileName = item.Key;
                var objectIds = item.Value;
                var bundleName = m_BundleWriteData.FileToBundle[fileName];

                HashSet<ObjectIdentifier> ids = null;
                if (bundleToObjectIds.ContainsKey(bundleName))
                {
                    ids = bundleToObjectIds[bundleName];
                }
                else 
                {
                    ids = new HashSet<ObjectIdentifier>();
                    bundleToObjectIds[bundleName] = ids;
                }
                foreach (var id in objectIds)
                {
                    if (!ids.Contains(id))
                    {
                        ids.Add(id);
                    }
                }
            }
            List<BundleHashItem> bundleItems = new List<BundleHashItem>();
            foreach (var item in bundleToObjectIds)
            {
                List<Hash128> hashs = new List<Hash128>();
                var idList = item.Value.ToList();
                var bundleName = item.Key;
                idList.Sort((a, b) => {
                    if (a > b)
                        return 1;
                    else
                        return -1;
                });
                foreach (var id in item.Value) 
                {
                    if (id.fileType == FileType.MetaAssetType || id.fileType == FileType.SerializedAssetType)
                    {
                        var guid = id.guid;
                        hashs.Add(AssetDatabase.GetAssetDependencyHash(guid));
                    }
                }
                BundleHashItem hashItem = new BundleHashItem();
                hashItem.BundleName = bundleName;
                hashItem.OrignBundleAssetHash =  HashingMethods.Calculate(hashs).ToHash128().ToString();
                hashItem.ResultBundleCrc = m_BuildBundleResults.BundleInfos[bundleName].Crc;
                hashItem.ResultBundleHash = m_BuildBundleResults.BundleInfos[bundleName].Hash.ToString();
                hashItem.ResultBundleContentHash = m_BuildBundleResults.BundleInfos[bundleName].ContentHash.ToString();
                bundleItems.Add(hashItem);
            }
            BundleHash.bundleHashItems = bundleItems.ToArray();
            return ReturnCode.Success;
        }
    }
}
