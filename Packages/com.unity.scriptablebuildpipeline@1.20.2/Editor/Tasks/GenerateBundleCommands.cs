using System.Collections.Generic;
using System.IO;
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
    /// <summary>
    /// Creates bundle commands for assets and scenes.
    /// </summary>
    public class GenerateBundleCommands : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IBundleBuildContent m_BuildContent;

        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In)]
        IDeterministicIdentifiers m_PackingMethod;

#if UNITY_2019_3_OR_NEWER
        [InjectContext(ContextUsage.In, true)]
        ICustomAssets m_CustomAssets;
#endif
#pragma warning restore 649

        static bool ValidAssetBundle(List<GUID> assets, HashSet<GUID> customAssets)
        {
            // Custom Valid Asset Bundle function that tests if every asset is known by the asset database, is an asset (not a scene), or is a user driven custom asset
            return assets.All(x => ValidationMethods.ValidAsset(x) == ValidationMethods.Status.Asset || customAssets.Contains(x));
        }

        /// <inheritdoc />
        public ReturnCode Run()
        {
            HashSet<GUID> customAssets = new HashSet<GUID>();
#if UNITY_2019_3_OR_NEWER
            if (m_CustomAssets != null)
                customAssets.UnionWith(m_CustomAssets.Assets);
#endif
            Dictionary<GUID, string> assetToMainFile = new Dictionary<GUID, string>();
            foreach (var pair in m_WriteData.AssetToFiles)
                assetToMainFile.Add(pair.Key, pair.Value[0]);

            foreach (var bundlePair in m_BuildContent.BundleLayout)
            {
                if (ValidAssetBundle(bundlePair.Value, customAssets))
                {
                    // Use generated internalName here as we could have an empty asset bundle used for raw object storage (See CreateStandardShadersBundle)
                    var internalName = string.Format(CommonStrings.AssetBundleNameFormat, m_PackingMethod.GenerateInternalFileName(bundlePair.Key));
                    CreateAssetBundleCommand(bundlePair.Key, internalName, bundlePair.Value);
                }
                else if (ValidationMethods.ValidSceneBundle(bundlePair.Value))
                {
                    var firstScene = bundlePair.Value[0];
                    CreateSceneBundleCommand(bundlePair.Key, assetToMainFile[firstScene], firstScene, bundlePair.Value, assetToMainFile);
                    for (int i = 1; i < bundlePair.Value.Count; ++i)
                    {
                        var additionalScene = bundlePair.Value[i];
                        CreateSceneDataCommand(assetToMainFile[additionalScene], additionalScene);
                    }
                }
            }
            return ReturnCode.Success;
        }

        internal static WriteCommand CreateWriteCommand(string internalName, List<ObjectIdentifier> objects, IDeterministicIdentifiers packingMethod)
        {
            var command = new WriteCommand();
            command.internalName = internalName;
            command.fileName = Path.GetFileName(internalName);

            command.serializeObjects = new List<SerializationInfo>();
            var consumedIds = new Dictionary<long, ObjectIdentifier>();
            foreach (var obj in objects)
            {
                var serializationInfo = new SerializationInfo
                {
                    serializationObject = obj,
                    serializationIndex = packingMethod.SerializationIndexFromObjectIdentifier(obj)
                };

                if (consumedIds.TryGetValue(serializationInfo.serializationIndex, out var priorObject))
                {
                    string msg = string.Format(@"File '{0}' contains a file identifier collision between {1} ({2}) and {3} ({4}) at id {5}. Objects will be missing from the bundle!
You can work around this issue by changing the 'FileID Generator Seed' found in the Scriptable Build Pipeline Preferences window.", 
                        internalName, obj, BuildCacheUtility.GetMainTypeForObject(obj), priorObject, BuildCacheUtility.GetMainTypeForObject(priorObject), serializationInfo.serializationIndex);
                    throw new BuildFailedException(msg);
                }
                consumedIds.Add(serializationInfo.serializationIndex, obj);
                command.serializeObjects.Add(serializationInfo);
            }
                
            return command;
        }

        // Sort function to ensure Asset Bundle's path container is ordered deterministically everytime
        internal static int AssetLoadInfoCompare(AssetLoadInfo x, AssetLoadInfo y)
        {
            if (x.asset != y.asset)
                return x.asset.CompareTo(y.asset);
            if (x.includedObjects.IsNullOrEmpty() && !y.includedObjects.IsNullOrEmpty())
                return -1;
            if (!x.includedObjects.IsNullOrEmpty() && y.includedObjects.IsNullOrEmpty())
                return 1;
            if (!x.includedObjects.IsNullOrEmpty() && !y.includedObjects.IsNullOrEmpty())
            {
                if (x.includedObjects[0] < y.includedObjects[0])
                    return -1;
                if (x.includedObjects[0] > y.includedObjects[0])
                    return 1;
            }
            if (string.IsNullOrEmpty(x.address) && !string.IsNullOrEmpty(y.address))
                return -1;
            if (!string.IsNullOrEmpty(x.address) && string.IsNullOrEmpty(y.address))
                return 1;
            if (!string.IsNullOrEmpty(x.address) && !string.IsNullOrEmpty(y.address))
                return x.address.CompareTo(y.address);
            return 0;
        }

        void CreateAssetBundleCommand(string bundleName, string internalName, List<GUID> assets)
        {
            var command = CreateWriteCommand(internalName, m_WriteData.FileToObjects[internalName], m_PackingMethod);
            var usageSet = new BuildUsageTagSet();
            var referenceMap = new BuildReferenceMap();
            var dependencyHashes = new List<Hash128>();
            var bundleAssets = new List<AssetLoadInfo>();


            referenceMap.AddMappings(command.internalName, command.serializeObjects.ToArray());
            foreach (var asset in assets)
            {
                usageSet.UnionWith(m_DependencyData.AssetUsage[asset]);
                if (m_DependencyData.DependencyHash.TryGetValue(asset, out var hash))
                    dependencyHashes.Add(hash);
                AssetLoadInfo assetInfo = m_DependencyData.AssetInfo[asset];
                assetInfo.address = m_BuildContent.Addresses[asset];
                bundleAssets.Add(assetInfo);
            }
            bundleAssets.Sort(AssetLoadInfoCompare);


            var operation = new AssetBundleWriteOperation();
            operation.Command = command;
            operation.UsageSet = usageSet;
            operation.ReferenceMap = referenceMap;
            operation.DependencyHash = !dependencyHashes.IsNullOrEmpty() ? HashingMethods.Calculate(dependencyHashes).ToHash128() : new Hash128();
            operation.Info = new AssetBundleInfo();
            operation.Info.bundleName = bundleName;
            operation.Info.bundleAssets = bundleAssets;


            m_WriteData.WriteOperations.Add(operation);
            m_WriteData.FileToUsageSet.Add(command.internalName, usageSet);
            m_WriteData.FileToReferenceMap.Add(command.internalName, referenceMap);
        }

#if !UNITY_2019_1_OR_NEWER || NONRECURSIVE_DEPENDENCY_DATA
        static int GetSortIndex(System.Type type)
        {
            // Closely matches CalculateSortIndex in C++, not 100% as the same info is not available to C#, doesn't need to be 100%
            if (type == null)
                return int.MaxValue - 5;
            if (type == typeof(MonoScript))
                return int.MinValue;
            if (typeof(ScriptableObject).IsAssignableFrom(type))
                return int.MaxValue - 4;
            if (typeof(MonoBehaviour).IsAssignableFrom(type))
                return int.MaxValue - 3;
            if (typeof(TerrainData).IsAssignableFrom(type))
                return int.MaxValue - 2;
            return System.BitConverter.ToInt32(HashingMethods.Calculate(type.Name).ToBytes(), 0);
        }

        struct SortObject
        {
            public ObjectIdentifier objectId;
            public int sortIndex;
        }

        internal static List<ObjectIdentifier> GetSortedSceneObjectIdentifiers(List<ObjectIdentifier> objects)
        {
            var types = new List<System.Type>(BuildCacheUtility.GetMainTypeForObjects(objects));
            var sortedObjects = new List<SortObject>();
            for (int i = 0; i < objects.Count; i++)
                sortedObjects.Add(new SortObject { sortIndex = GetSortIndex(types[i]), objectId = objects[i] });
            return sortedObjects.OrderBy(x => x.sortIndex).Select(x => x.objectId).ToList();
        }

#endif

        List<ObjectIdentifier> GetFileObjectsForScene(string internalName)
        {
            var fileObjects = m_WriteData.FileToObjects[internalName];
#if !UNITY_2019_1_OR_NEWER || NONRECURSIVE_DEPENDENCY_DATA
            // ContentBuildInterface.PrepareScene was not returning stable sorted references, causing a indeterminism and loading errors in some cases
            // Add correct sorting here until patch lands to fix the API.
            // Additionally, if we are using Non-Recursive build mode, we still need to sort by type.
#if NONRECURSIVE_DEPENDENCY_DATA
            if (m_Parameters.NonRecursiveDependencies)
#endif
            {
                fileObjects = GetSortedSceneObjectIdentifiers(fileObjects);
            }
#endif
            return fileObjects;
        }

        void CreateSceneBundleCommand(string bundleName, string internalName, GUID scene, List<GUID> bundledScenes, Dictionary<GUID, string> assetToMainFile)
        {
            var fileObjects = GetFileObjectsForScene(internalName);
            var command = CreateWriteCommand(internalName, fileObjects, new LinearPackedIdentifiers(3)); // Start at 3: PreloadData = 1, AssetBundle = 2
            var usageSet = new BuildUsageTagSet();
            var referenceMap = new BuildReferenceMap();
            var preloadObjects = new List<ObjectIdentifier>();
            var bundleScenes = new List<SceneLoadInfo>();
            var dependencyInfo = m_DependencyData.SceneInfo[scene];
            var fileObjectSet = new HashSet<ObjectIdentifier>(fileObjects);


            usageSet.UnionWith(m_DependencyData.SceneUsage[scene]);
            referenceMap.AddMappings(command.internalName, command.serializeObjects.ToArray());
            foreach (var referencedObject in dependencyInfo.referencedObjects)
            {
                if (fileObjectSet.Contains(referencedObject))
                    continue;
                preloadObjects.Add(referencedObject);
            }
            foreach (var bundledScene in bundledScenes)
            {
                var loadInfo = new SceneLoadInfo();
                loadInfo.asset = bundledScene;
                loadInfo.internalName = Path.GetFileNameWithoutExtension(assetToMainFile[bundledScene]);
                loadInfo.address = m_BuildContent.Addresses[bundledScene];
                bundleScenes.Add(loadInfo);
            }


            var operation = new SceneBundleWriteOperation();
            operation.Command = command;
            operation.UsageSet = usageSet;
            operation.ReferenceMap = referenceMap;
            operation.DependencyHash = m_DependencyData.DependencyHash.TryGetValue(scene, out var hash) ? hash : new Hash128();
            operation.Scene = dependencyInfo.scene;
#if !UNITY_2019_3_OR_NEWER
            operation.ProcessedScene = dependencyInfo.processedScene;
#endif
            operation.PreloadInfo = new PreloadInfo();
            operation.PreloadInfo.preloadObjects = preloadObjects;
            operation.Info = new SceneBundleInfo();
            operation.Info.bundleName = bundleName;
            operation.Info.bundleScenes = bundleScenes;


            m_WriteData.WriteOperations.Add(operation);
            m_WriteData.FileToUsageSet.Add(command.internalName, usageSet);
            m_WriteData.FileToReferenceMap.Add(command.internalName, referenceMap);
        }

        void CreateSceneDataCommand(string internalName, GUID scene)
        {
            var fileObjects = GetFileObjectsForScene(internalName);
            var command = CreateWriteCommand(internalName, fileObjects, new LinearPackedIdentifiers(2)); // Start at 3: PreloadData = 1
            var usageSet = new BuildUsageTagSet();
            var referenceMap = new BuildReferenceMap();
            var preloadObjects = new List<ObjectIdentifier>();
            var dependencyInfo = m_DependencyData.SceneInfo[scene];
            var fileObjectSet = new HashSet<ObjectIdentifier>(fileObjects);


            usageSet.UnionWith(m_DependencyData.SceneUsage[scene]);
            referenceMap.AddMappings(command.internalName, command.serializeObjects.ToArray());
            foreach (var referencedObject in dependencyInfo.referencedObjects)
            {
                if (fileObjectSet.Contains(referencedObject))
                    continue;
                preloadObjects.Add(referencedObject);
            }


            var operation = new SceneDataWriteOperation();
            operation.Command = command;
            operation.UsageSet = usageSet;
            operation.ReferenceMap = referenceMap;
            operation.DependencyHash = m_DependencyData.DependencyHash.TryGetValue(scene, out var hash) ? hash : new Hash128();
            operation.Scene = dependencyInfo.scene;
#if !UNITY_2019_3_OR_NEWER
            operation.ProcessedScene = dependencyInfo.processedScene;
#endif
            operation.PreloadInfo = new PreloadInfo();
            operation.PreloadInfo.preloadObjects = preloadObjects;


            m_WriteData.FileToReferenceMap.Add(command.internalName, referenceMap);
            m_WriteData.FileToUsageSet.Add(command.internalName, usageSet);
            m_WriteData.WriteOperations.Add(operation);
        }
    }
}
