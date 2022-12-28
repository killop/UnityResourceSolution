#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.WriteTypes;
using UnityEditor.Build.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public interface IClusterOutput : IContextObject
    {
        Dictionary<ObjectIdentifier, Hash128> ObjectToCluster { get; }
        Dictionary<ObjectIdentifier, long> ObjectToLocalID { get; }
    }

    public class ClusterOutput : IClusterOutput
    {
        private Dictionary<ObjectIdentifier, Hash128> m_ObjectToCluster = new Dictionary<ObjectIdentifier, Hash128>();
        private Dictionary<ObjectIdentifier, long> m_ObjectToLocalID = new Dictionary<ObjectIdentifier, long>();
        public Dictionary<ObjectIdentifier, Hash128> ObjectToCluster { get { return m_ObjectToCluster; } }
        public Dictionary<ObjectIdentifier, long> ObjectToLocalID { get { return m_ObjectToLocalID; } }
    }

    /// <summary>
    /// Build task for creating content archives based asset co-location.
    /// </summary>
    public class ClusterBuildLayout : IBuildTask
    {
        private static void GetOrAdd<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, out TValue value) where TValue : new()
        {
            if (dictionary.TryGetValue(key, out value))
                return;

            value = new TValue();
            dictionary.Add(key, value);
        }

        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In)]
        IDeterministicIdentifiers m_PackingMethod;

        [InjectContext]
        IBuildResults m_Results;

        [InjectContext]
        IClusterOutput m_ClusterResult;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            // Create usage maps
            Dictionary<ObjectIdentifier, HashSet<GUID>> objectToAssets = new Dictionary<ObjectIdentifier, HashSet<GUID>>();
            foreach (var pair in m_DependencyData.AssetInfo)
            {
                ExtractAssets(objectToAssets, pair.Key, pair.Value.includedObjects);
                ExtractAssets(objectToAssets, pair.Key, pair.Value.referencedObjects);
            }
            foreach (var pair in m_DependencyData.SceneInfo)
            {
                ExtractAssets(objectToAssets, pair.Key, pair.Value.referencedObjects);
            }

            // Using usage maps, create clusters
            Dictionary<Hash128, HashSet<ObjectIdentifier>> clusterToObjects = new Dictionary<Hash128, HashSet<ObjectIdentifier>>();
            foreach (var pair in objectToAssets)
            {
                HashSet<GUID> assets = pair.Value;
                Hash128 cluster = HashingMethods.Calculate(assets.OrderBy(x => x)).ToHash128();
                GetOrAdd(clusterToObjects, cluster, out var objectIds);
                objectIds.Add(pair.Key);
                m_ClusterResult.ObjectToCluster.Add(pair.Key, cluster);
            }

            // From clusters, create the final write data
            BuildUsageTagSet usageSet = new BuildUsageTagSet();
            foreach (var pair in m_DependencyData.AssetUsage)
                usageSet.UnionWith(pair.Value);
            foreach (var pair in m_DependencyData.SceneUsage)
                usageSet.UnionWith(pair.Value);

            // Generates Content Archive Files from Clusters
            BuildReferenceMap referenceMap = new BuildReferenceMap();
            foreach (var pair in clusterToObjects)
            {
                var cluster = pair.Key.ToString();
                m_WriteData.FileToObjects.Add(cluster, pair.Value.ToList());
#pragma warning disable CS0618 // Type or member is obsolete
                var op = new RawWriteOperation();
#pragma warning restore CS0618 // Type or member is obsolete
                m_WriteData.WriteOperations.Add(op);

                op.Command = new WriteCommand();
                op.Command.fileName = cluster;
                op.Command.internalName = cluster;
                op.Command.serializeObjects = new List<SerializationInfo>();
                foreach (var objectId in pair.Value)
                {
                    var lfid = m_PackingMethod.SerializationIndexFromObjectIdentifier(objectId);
                    op.Command.serializeObjects.Add(new SerializationInfo { serializationObject = objectId, serializationIndex = lfid });
                    referenceMap.AddMapping(cluster, lfid, objectId);
                    m_ClusterResult.ObjectToLocalID.Add(objectId, lfid);
                }
                op.ReferenceMap = referenceMap;
                op.UsageSet = usageSet;

                m_WriteData.FileToBundle.Add(cluster, cluster);
            }

            // Generates Content Scene Archive Files from Scene Input
            foreach (var pair in m_DependencyData.SceneInfo)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var op = new SceneRawWriteOperation();
#pragma warning restore CS0618 // Type or member is obsolete
                m_WriteData.WriteOperations.Add(op);

                op.Command = new WriteCommand();
                op.Command.fileName = pair.Key.ToString();
                op.Command.internalName = pair.Key.ToString();
                op.Command.serializeObjects = new List<SerializationInfo>();
                op.ReferenceMap = referenceMap;
                op.UsageSet = usageSet;
                op.Scene = pair.Value.scene;

                m_WriteData.FileToBundle.Add(pair.Key.ToString(), pair.Key.ToString());
            }

            return ReturnCode.Success;
        }

        private static void ExtractAssets(Dictionary<ObjectIdentifier, HashSet<GUID>> objectToAssets, GUID asset, IEnumerable<ObjectIdentifier> objectIds)
        {
            foreach (var objectId in objectIds)
            {
                if (objectId.filePath.Equals(CommonStrings.UnityDefaultResourcePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                GetOrAdd(objectToAssets, objectId, out var assets);
                assets.Add(asset);
            }
        }
    }
}
#endif