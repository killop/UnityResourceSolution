using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Player;

namespace UnityEditor.Build.Pipeline.Tasks
{
#if !UNITY_2020_2_OR_NEWER
    internal class CalculateAssetDependencyHooks
    {
        public virtual UnityEngine.Object[] LoadAllAssetRepresentationsAtPath(string assetPath)
        {
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
        }
    }
#endif

    /// <summary>
    /// Calculates the dependency data for all assets.
    /// </summary>
    public class CalculateAssetDependencyData : IBuildTask
    {
        internal const int kVersion = 5;
        /// <inheritdoc />
        public int Version { get { return kVersion; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBundleBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IBuildContent m_Content;

        [InjectContext]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)]
        IBuildSpriteData m_SpriteData;

        [InjectContext(ContextUsage.InOut, true)]
        IBuildExtendedAssetData m_ExtendedAssetData;

        [InjectContext(ContextUsage.In, true)]
        IProgressTracker m_Tracker;

        [InjectContext(ContextUsage.In, true)]
        IBuildCache m_Cache;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;
#pragma warning restore 649

        internal struct TaskInput
        {
            public IBuildCache BuildCache;
            public BuildTarget Target;
            public TypeDB TypeDB;
            public List<GUID> Assets;
            public IProgressTracker ProgressTracker;
            public BuildUsageTagGlobal GlobalUsage;
            public BuildUsageCache DependencyUsageCache;
#if !UNITY_2020_2_OR_NEWER
            public CalculateAssetDependencyHooks EngineHooks;
#endif
            public bool NonRecursiveDependencies;
            public IBuildLogger Logger;
        }

        internal struct AssetOutput
        {
            public GUID asset;
            public AssetLoadInfo assetInfo;
            public BuildUsageTagSet usageTags;
            public SpriteImporterData spriteData;
            public ExtendedAssetData extendedData;
            public List<ObjectTypes> objectTypes;
        }

        internal struct TaskOutput
        {
            public AssetOutput[] AssetResults;
            public int CachedAssetCount;
        }

        static CacheEntry GetAssetCacheEntry(IBuildCache cache, GUID asset, bool NonRecursiveDependencies)
        {
            CacheEntry entry;
            entry = cache.GetCacheEntry(asset, NonRecursiveDependencies ? -kVersion : kVersion);
            return entry;
        }

        static CachedInfo GetCachedInfo(IBuildCache cache, GUID asset, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags, SpriteImporterData importerData, ExtendedAssetData assetData, bool NonRecursiveDependencies)
        {
            var info = new CachedInfo();
            info.Asset = GetAssetCacheEntry(cache, asset, NonRecursiveDependencies);

            var uniqueTypes = new HashSet<System.Type>();
            var objectTypes = new List<ObjectTypes>();
            var dependencies = new HashSet<CacheEntry>();
            ExtensionMethods.ExtractCommonCacheData(cache, assetInfo.includedObjects, assetInfo.referencedObjects, uniqueTypes, objectTypes, dependencies);
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { assetInfo, usageTags, importerData, assetData, objectTypes };
            return info;
        }

        /// <inheritdoc />
        public ReturnCode Run()
        {
            TaskInput input = new TaskInput();
            input.Target = m_Parameters.Target;
            input.TypeDB = m_Parameters.ScriptInfo;
            input.BuildCache = m_Parameters.UseCache ? m_Cache : null;
#if NONRECURSIVE_DEPENDENCY_DATA
            input.NonRecursiveDependencies = m_Parameters.NonRecursiveDependencies;
#else
            input.NonRecursiveDependencies = false;
#endif
            input.Assets = m_Content.Assets;
            input.ProgressTracker = m_Tracker;
            input.DependencyUsageCache = m_DependencyData.DependencyUsageCache;
            input.GlobalUsage = m_DependencyData.GlobalUsage;
            input.Logger = m_Log;
            foreach (SceneDependencyInfo sceneInfo in m_DependencyData.SceneInfo.Values)
                input.GlobalUsage |= sceneInfo.globalUsage;

            ReturnCode code = RunInternal(input, out TaskOutput output);
            if (code == ReturnCode.Success)
            {
                foreach (AssetOutput o in output.AssetResults)
                {
                    m_DependencyData.AssetInfo.Add(o.asset, o.assetInfo);
                    m_DependencyData.AssetUsage.Add(o.asset, o.usageTags);

                    if (o.spriteData != null)
                    {
                        if (m_SpriteData == null)
                            m_SpriteData = new BuildSpriteData();
                        m_SpriteData.ImporterData.Add(o.asset, o.spriteData);
                    }

                    if (!m_Parameters.DisableVisibleSubAssetRepresentations && o.extendedData != null)
                    {
                        if (m_ExtendedAssetData == null)
                            m_ExtendedAssetData = new BuildExtendedAssetData();
                        m_ExtendedAssetData.ExtendedData.Add(o.asset, o.extendedData);
                    }

                    if (o.objectTypes != null)
                        BuildCacheUtility.SetTypeForObjects(o.objectTypes);
                }
            }

            return code;
        }

#if !UNITY_2020_2_OR_NEWER
        static internal void GatherAssetRepresentations(string assetPath, System.Func<string, UnityEngine.Object[]> loadAllAssetRepresentations, ObjectIdentifier[] includedObjects, out ExtendedAssetData extendedData)
        {
            extendedData = null;
            var representations = loadAllAssetRepresentations(assetPath);
            if (representations.IsNullOrEmpty())
                return;

            var resultData = new ExtendedAssetData();
            for (int j = 0; j < representations.Length; j++)
            {
                if (representations[j] == null)
                {
                    BuildLogger.LogWarning($"SubAsset {j} inside {assetPath} is null. It will not be included in the build.");
                    continue;
                }

                if (AssetDatabase.IsMainAsset(representations[j]))
                    continue;

                string guid;
                long localId;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(representations[j], out guid, out localId))
                    continue;

                resultData.Representations.AddRange(includedObjects.Where(x => x.localIdentifierInFile == localId));
            }

            if (resultData.Representations.Count > 0)
                extendedData = resultData;
        }

#else
        static internal void GatherAssetRepresentations(GUID asset, BuildTarget target, ObjectIdentifier[] includedObjects, out ExtendedAssetData extendedData)
        {
            extendedData = null;
            var includeSet = new HashSet<ObjectIdentifier>(includedObjects);
            // GetPlayerAssetRepresentations can return editor only objects, filter out those to only include what is in includedObjects
            ObjectIdentifier[] representations = ContentBuildInterface.GetPlayerAssetRepresentations(asset, target);
            var filteredRepresentations = representations.Where(includeSet.Contains);
            // Main Asset always returns at index 0, we only want representations, so check for greater than 1 length
            if (representations.IsNullOrEmpty() || filteredRepresentations.Count() < 2)
                return;

            extendedData = new ExtendedAssetData();
            extendedData.Representations.AddRange(filteredRepresentations.Skip(1));
        }

#endif

        static internal ReturnCode RunInternal(TaskInput input, out TaskOutput output)
        {
#if !UNITY_2020_2_OR_NEWER
            input.EngineHooks = input.EngineHooks != null ? input.EngineHooks : new CalculateAssetDependencyHooks();
#endif
            output = new TaskOutput();
            output.AssetResults = new AssetOutput[input.Assets.Count];

            IList<CachedInfo> cachedInfo = null;
            using (input.Logger.ScopedStep(LogLevel.Info, "Gathering Cache Entries to Load"))
            {
                if (input.BuildCache != null)
                {
                    IList<CacheEntry> entries = input.Assets.Select(x => GetAssetCacheEntry(input.BuildCache, x, input.NonRecursiveDependencies)).ToList();
                    input.BuildCache.LoadCachedData(entries, out cachedInfo);
                }
            }

            for (int i = 0; i < input.Assets.Count; i++)
            {
                using (input.Logger.ScopedStep(LogLevel.Info, "Calculate Asset Dependencies"))
                {
                    AssetOutput assetResult = new AssetOutput();
                    assetResult.asset = input.Assets[i];

                   
                    if (cachedInfo != null && cachedInfo[i] != null)
                    {
                        var objectTypes = cachedInfo[i].Data[4] as List<ObjectTypes>;
                        var assetInfos = cachedInfo[i].Data[0] as AssetLoadInfo;

                        bool useCachedData = true;
                        foreach (var objectType in objectTypes)
                        {
                            //Sprite association to SpriteAtlas might have changed since last time data was cached, this might 
                            //imply that we have stale data in our cache, if so ensure we regenerate the data.
                            if (objectType.Types[0] == typeof(UnityEngine.Sprite))
                            {
                                var referencedObjectOld = assetInfos.referencedObjects.ToArray();
                                ObjectIdentifier[] referencedObjectsNew = null;
#if NONRECURSIVE_DEPENDENCY_DATA
                                if (input.NonRecursiveDependencies)
                                {
                                    referencedObjectsNew = ContentBuildInterface.GetPlayerDependenciesForObjects(assetInfos.includedObjects.ToArray(), input.Target, input.TypeDB, DependencyType.ValidReferences);
                                    referencedObjectsNew = ExtensionMethods.FilterReferencedObjectIDs(input.Assets[i], referencedObjectsNew, input.Target, input.TypeDB, new HashSet<GUID>(input.Assets));
                                }
                                else
#endif
                                {
                                    referencedObjectsNew = ContentBuildInterface.GetPlayerDependenciesForObjects(assetInfos.includedObjects.ToArray(), input.Target, input.TypeDB);
                                }

                                if (Enumerable.SequenceEqual(referencedObjectOld, referencedObjectsNew) == false)
                                {
                                    useCachedData = false;
                                }
                                break;
                            }
                        }
                        if (useCachedData)
                        { 
                            assetResult.assetInfo = assetInfos;
                            assetResult.usageTags = cachedInfo[i].Data[1] as BuildUsageTagSet;
                            assetResult.spriteData = cachedInfo[i].Data[2] as SpriteImporterData;
                            assetResult.extendedData = cachedInfo[i].Data[3] as ExtendedAssetData;
                            assetResult.objectTypes = objectTypes;
                            output.AssetResults[i] = assetResult;
                            output.CachedAssetCount++;
                            input.Logger.AddEntrySafe(LogLevel.Info, $"{assetResult.asset} (cached)");
                            continue;
                        }
                    }

                    GUID asset = input.Assets[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(asset.ToString());

                    if (!input.ProgressTracker.UpdateInfoUnchecked(assetPath))
                        return ReturnCode.Canceled;

                    input.Logger.AddEntrySafe(LogLevel.Info, $"{assetResult.asset}");

                    assetResult.assetInfo = new AssetLoadInfo();
                    assetResult.usageTags = new BuildUsageTagSet();

                    assetResult.assetInfo.asset = asset;
                    var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(asset, input.Target);
                    assetResult.assetInfo.includedObjects = new List<ObjectIdentifier>(includedObjects);
                    ObjectIdentifier[] referencedObjects;
#if NONRECURSIVE_DEPENDENCY_DATA
                    if (input.NonRecursiveDependencies)
                    {
                        referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, input.Target, input.TypeDB, DependencyType.ValidReferences);
                        referencedObjects = ExtensionMethods.FilterReferencedObjectIDs(asset, referencedObjects, input.Target, input.TypeDB, new HashSet<GUID>(input.Assets));
                    }
                    else
#endif
                    {
                        referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, input.Target, input.TypeDB);
                    }

                    assetResult.assetInfo.referencedObjects = new List<ObjectIdentifier>(referencedObjects);
                    var allObjects = new List<ObjectIdentifier>(includedObjects);
                    allObjects.AddRange(referencedObjects);
                    ContentBuildInterface.CalculateBuildUsageTags(allObjects.ToArray(), includedObjects, input.GlobalUsage, assetResult.usageTags, input.DependencyUsageCache);

                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && importer.textureType == TextureImporterType.Sprite)
                    {
                        assetResult.spriteData = new SpriteImporterData();
                        assetResult.spriteData.PackedSprite = false;
                        assetResult.spriteData.SourceTexture = includedObjects.FirstOrDefault();

                        if (EditorSettings.spritePackerMode != SpritePackerMode.Disabled)
                            assetResult.spriteData.PackedSprite = referencedObjects.Length > 0;
#if !UNITY_2020_1_OR_NEWER
                        if (EditorSettings.spritePackerMode == SpritePackerMode.AlwaysOn || EditorSettings.spritePackerMode == SpritePackerMode.BuildTimeOnly)
                            assetResult.spriteData.PackedSprite = !string.IsNullOrEmpty(importer.spritePackingTag);
#endif
                    }

#if !UNITY_2020_2_OR_NEWER
                    GatherAssetRepresentations(assetPath, input.EngineHooks.LoadAllAssetRepresentationsAtPath, includedObjects, out assetResult.extendedData);
#else
                    GatherAssetRepresentations(asset, input.Target, includedObjects, out assetResult.extendedData);
#endif
                    output.AssetResults[i] = assetResult;
                }
            }

            using (input.Logger.ScopedStep(LogLevel.Info, "Gathering Cache Entries to Save"))
            {
                if (input.BuildCache != null)
                {
                    List<CachedInfo> toCache = new List<CachedInfo>();
                    for (int i = 0; i < input.Assets.Count; i++)
                    {
                        AssetOutput r = output.AssetResults[i];
                        if (cachedInfo[i] == null)
                        {
                            toCache.Add(GetCachedInfo(input.BuildCache, input.Assets[i], r.assetInfo, r.usageTags, r.spriteData, r.extendedData, input.NonRecursiveDependencies));
                        }
                    }
                    input.BuildCache.SaveCachedData(toCache);
                }
            }

            return ReturnCode.Success;
        }
    }
}
