#if UNITY_2019_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Build Task that calculates teh included objects and references objects for custom assets not tracked by the AssetDatabase.
    /// <seealso cref="IBuildTask"/>
    /// </summary>
    public class CalculateCustomDependencyData : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 2; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.InOut)]
        IBundleBuildContent m_Content;

        [InjectContext(ContextUsage.InOut)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.Out, true)]
        ICustomAssets m_CustomAssets;

        [InjectContext(ContextUsage.In, true)]
        IProgressTracker m_Tracker;

        [InjectContext(ContextUsage.In, true)]
        IBuildCache m_Cache;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;
#pragma warning restore 649

        BuildUsageTagGlobal m_GlobalUsage;
        BuildUsageTagGlobal m_CustomUsage;

        Dictionary<string, AssetLoadInfo> m_AssetInfo = new Dictionary<string, AssetLoadInfo>();
        Dictionary<string, BuildUsageTagSet> m_BuildUsage = new Dictionary<string, BuildUsageTagSet>();

        /// <inheritdoc />
        public ReturnCode Run()
        {
            m_CustomAssets = new CustomAssets();
            m_GlobalUsage = m_DependencyData.GlobalUsage;
            foreach (SceneDependencyInfo sceneInfo in m_DependencyData.SceneInfo.Values)
                m_GlobalUsage |= sceneInfo.globalUsage;

            foreach (CustomContent info in m_Content.CustomAssets)
            {
                if (!m_Tracker.UpdateInfoUnchecked(info.Asset.ToString()))
                    return ReturnCode.Canceled;

                using (m_Log.ScopedStep(LogLevel.Verbose, "CustomAssetDependency", info.Asset.ToString()))
                    info.Processor(info.Asset, this);
            }

            // Add all the additional global usage for custom assets back into the dependency data result
            // for use in the write serialized file build task
            m_DependencyData.GlobalUsage |= m_CustomUsage;
            return ReturnCode.Success;
        }

        CacheEntry GetCacheEntry(string path, BuildUsageTagGlobal additionalGlobalUsage)
        {
            var entry = new CacheEntry();
            entry.Type = CacheEntry.EntryType.Data;
            entry.Guid = HashingMethods.Calculate("CalculateCustomDependencyData", path).ToGUID();
            entry.Hash = HashingMethods.Calculate(HashingMethods.CalculateFile(path), additionalGlobalUsage).ToHash128();
            entry.Version = Version;
            return entry;
        }

        CachedInfo GetCachedInfo(CacheEntry entry, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags)
        {
            var info = new CachedInfo();
            info.Asset = entry;

            var uniqueTypes = new HashSet<Type>();
            var objectTypes = new List<ObjectTypes>();
            var dependencies = new HashSet<CacheEntry>();
            ExtensionMethods.ExtractCommonCacheData(m_Cache, assetInfo.includedObjects, assetInfo.referencedObjects, uniqueTypes, objectTypes, dependencies);
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { assetInfo, usageTags, objectTypes };
            return info;
        }

        bool LoadCachedData(string path, out AssetLoadInfo assetInfo, out BuildUsageTagSet buildUsage, BuildUsageTagGlobal globalUsage)
        {
            assetInfo = default;
            buildUsage = default;

            if (!m_Parameters.UseCache || m_Cache == null)
                return false;

            CacheEntry entry = GetCacheEntry(path, globalUsage);
            m_Cache.LoadCachedData(new List<CacheEntry> { entry }, out IList<CachedInfo> cachedInfos);
            var cachedInfo = cachedInfos[0];
            if (cachedInfo != null)
            {
                assetInfo = (AssetLoadInfo)cachedInfo.Data[0];
                buildUsage = (BuildUsageTagSet)cachedInfo.Data[1];
                var objectTypes = (List<ObjectTypes>)cachedInfo.Data[2];
                BuildCacheUtility.SetTypeForObjects(objectTypes);
            }
            else
            {
                GatherAssetData(path, out assetInfo, out buildUsage, globalUsage);
                cachedInfo = GetCachedInfo(entry, assetInfo, buildUsage);
                m_Cache.SaveCachedData(new List<CachedInfo> { cachedInfo });
            }
            return true;
        }

        void GatherAssetData(string path, out AssetLoadInfo assetInfo, out BuildUsageTagSet buildUsage, BuildUsageTagGlobal globalUsage)
        {
            assetInfo = new AssetLoadInfo();
            buildUsage = new BuildUsageTagSet();

            var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInSerializedFile(path, m_Parameters.Target);
            var referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, m_Parameters.Target, m_Parameters.ScriptInfo);

            assetInfo.includedObjects = new List<ObjectIdentifier>(includedObjects);
            assetInfo.referencedObjects = new List<ObjectIdentifier>(referencedObjects);

            ContentBuildInterface.CalculateBuildUsageTags(referencedObjects, includedObjects, globalUsage, buildUsage, m_DependencyData.DependencyUsageCache);
        }

        /// <summary>
        /// Returns the Object Identifiers and Types in a raw Unity Serialized File. The resulting arrays will be empty if a non-serialized file path was used.
        /// </summary>
        /// <param name="path">Path to the Unity Serialized File</param>
        /// <param name="objectIdentifiers">Object Identifiers for all the objects in the serialized file</param>
        /// <param name="types">Types for all the objects in the serialized file</param>
        public void GetObjectIdentifiersAndTypesForSerializedFile(string path, out ObjectIdentifier[] objectIdentifiers, out Type[] types)
        {
            GetObjectIdentifiersAndTypesForSerializedFile(path, out objectIdentifiers, out types, default);
        }

        /// <summary>
        /// Returns the Object Identifiers and Types in a raw Unity Serialized File. The resulting arrays will be empty if a non-serialized file path was used.
        /// </summary>
        /// <param name="path">Path to the Unity Serialized File</param>
        /// <param name="objectIdentifiers">Object Identifiers for all the objects in the serialized file</param>
        /// <param name="types">Types for all the objects in the serialized file</param>
        /// <param name="additionalGlobalUsage">Additional global lighting usage information to include with this custom asset</param>
        public void GetObjectIdentifiersAndTypesForSerializedFile(string path, out ObjectIdentifier[] objectIdentifiers, out Type[] types, BuildUsageTagGlobal additionalGlobalUsage)
        {
            // Additional global usage is local to the custom asset, so we are using a local copy of this additional data to avoid influencing the calcualtion
            // of other custom assets. Additionally we store all the addtional global usage for later copying back into the dependency data result for the final write build task.
            var globalUsage = m_GlobalUsage | additionalGlobalUsage;
            m_CustomUsage = m_CustomUsage | additionalGlobalUsage;
            if (!LoadCachedData(path, out var assetInfo, out var buildUsage, globalUsage))
                GatherAssetData(path, out assetInfo, out buildUsage, globalUsage);

            // Local cache to reuse data from this function in the next function
            m_AssetInfo[path] = assetInfo;
            m_BuildUsage[path] = buildUsage;

            objectIdentifiers = assetInfo.includedObjects.ToArray();
            types = BuildCacheUtility.GetSortedUniqueTypesForObjects(objectIdentifiers);
        }

        /// <summary>
        /// Adds mapping and bundle information for a custom asset that contains a set of unity objects.
        /// </summary>
        /// <param name="includedObjects">Object Identifiers that belong to this custom asset</param>
        /// <param name="path">Path on disk for this custom asset</param>
        /// <param name="bundleName">Asset Bundle name where to add this custom asset</param>
        /// <param name="address">Load address to used to load this asset from the Asset Bundle</param>
        /// <param name="mainAssetType">Type of the main object for this custom asset</param>
        public void CreateAssetEntryForObjectIdentifiers(ObjectIdentifier[] includedObjects, string path, string bundleName, string address, Type mainAssetType)
        {
            AssetLoadInfo assetInfo = m_AssetInfo[path];
            BuildUsageTagSet buildUsage = m_BuildUsage[path];

            assetInfo.asset = HashingMethods.Calculate(address).ToGUID();
            assetInfo.address = address;
            if (m_DependencyData.AssetInfo.ContainsKey(assetInfo.asset))
                throw new ArgumentException(string.Format("Custom Asset '{0}' already exists. Building duplicate asset entries is not supported.", address));
            SetOutputInformation(bundleName, assetInfo, buildUsage);
        }

        void SetOutputInformation(string bundleName, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags)
        {
            List<GUID> assets;
            m_Content.BundleLayout.GetOrAdd(bundleName, out assets);
            assets.Add(assetInfo.asset);

            m_Content.Addresses.Add(assetInfo.asset, assetInfo.address);
            m_DependencyData.AssetInfo.Add(assetInfo.asset, assetInfo);
            m_DependencyData.AssetUsage.Add(assetInfo.asset, usageTags);
            m_CustomAssets.Assets.Add(assetInfo.asset);
        }
    }
}
#endif
