using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Packs each asset bundle and calculates the asset load file dependency list.
    /// </summary>
    public class GenerateBundlePacking : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
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
            Dictionary<GUID, List<GUID>> assetToReferences = new Dictionary<GUID, List<GUID>>();
            HashSet<GUID> customAssets = new HashSet<GUID>();
#if UNITY_2019_3_OR_NEWER
            if (m_CustomAssets != null)
                customAssets.UnionWith(m_CustomAssets.Assets);
#endif

            // Pack each asset bundle
            foreach (var bundle in m_BuildContent.BundleLayout)
            {
                if (ValidAssetBundle(bundle.Value, customAssets))
                    PackAssetBundle(bundle.Key, bundle.Value, assetToReferences);
                else if (ValidationMethods.ValidSceneBundle(bundle.Value))
                    PackSceneBundle(bundle.Key, bundle.Value, assetToReferences);
            }

            // Calculate Asset file load dependency list
            foreach (var bundle in m_BuildContent.BundleLayout)
            {
                foreach (var asset in bundle.Value)
                {
                    List<string> files = m_WriteData.AssetToFiles[asset];
                    List<GUID> references = assetToReferences[asset];
                    foreach (var reference in references)
                    {
                        List<string> referenceFiles = m_WriteData.AssetToFiles[reference];
                        if (!files.Contains(referenceFiles[0]))
                            files.Add(referenceFiles[0]);
                    }
                }
            }

            return ReturnCode.Success;
        }

        void PackAssetBundle(string bundleName, List<GUID> includedAssets, Dictionary<GUID, List<GUID>> assetToReferences)
        {
            var internalName = string.Format(CommonStrings.AssetBundleNameFormat, m_PackingMethod.GenerateInternalFileName(bundleName));

            var allObjects = new HashSet<ObjectIdentifier>();
            Dictionary<GUID, HashSet<ObjectIdentifier>> assetObjectIdentifierHashSets = new Dictionary<GUID, HashSet<ObjectIdentifier>>();
            foreach (var asset in includedAssets)
            {
                AssetLoadInfo assetInfo = m_DependencyData.AssetInfo[asset];
                allObjects.UnionWith(assetInfo.includedObjects);

                var references = new List<ObjectIdentifier>();
                references.AddRange(assetInfo.referencedObjects);
                assetToReferences[asset] = FilterReferencesForAsset(m_DependencyData, asset, references, null, null, assetObjectIdentifierHashSets);

                allObjects.UnionWith(references);
                m_WriteData.AssetToFiles[asset] = new List<string> { internalName };
            }

            m_WriteData.FileToBundle.Add(internalName, bundleName);
            m_WriteData.FileToObjects.Add(internalName, allObjects.ToList());
        }

        void PackSceneBundle(string bundleName, List<GUID> includedScenes, Dictionary<GUID, List<GUID>> assetToReferences)
        {
            if (includedScenes.IsNullOrEmpty())
                return;

            string firstFileName = "";
            HashSet<ObjectIdentifier> previousSceneObjects = new HashSet<ObjectIdentifier>();
            HashSet<GUID> previousSceneAssets = new HashSet<GUID>();
            List<string> sceneInternalNames = new List<string>();
            Dictionary<GUID, HashSet<ObjectIdentifier>> assetObjectIdentifierHashSets = new Dictionary<GUID, HashSet<ObjectIdentifier>>();
            foreach (var scene in includedScenes)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(scene.ToString());
                var internalSceneName = m_PackingMethod.GenerateInternalFileName(scenePath);
                if (string.IsNullOrEmpty(firstFileName))
                    firstFileName = internalSceneName;
                var internalName = string.Format(CommonStrings.SceneBundleNameFormat, firstFileName, internalSceneName);

                SceneDependencyInfo sceneInfo = m_DependencyData.SceneInfo[scene];

                var references = new List<ObjectIdentifier>();
                references.AddRange(sceneInfo.referencedObjects);
                assetToReferences[scene] = FilterReferencesForAsset(m_DependencyData, scene, references, previousSceneObjects, previousSceneAssets, assetObjectIdentifierHashSets);
                previousSceneObjects.UnionWith(references);
                previousSceneAssets.UnionWith(assetToReferences[scene]);

                m_WriteData.FileToObjects.Add(internalName, references);
                m_WriteData.FileToBundle.Add(internalName, bundleName);

                var files = new List<string> { internalName };
                files.AddRange(sceneInternalNames);
                m_WriteData.AssetToFiles[scene] = files;

                sceneInternalNames.Add(internalName);
            }
        }

        static HashSet<ObjectIdentifier> GetRefObjectIdLookup(AssetLoadInfo referencedAsset, Dictionary<GUID, HashSet<ObjectIdentifier>> assetObjectIdentifierHashSets)
        {
            HashSet<ObjectIdentifier> refObjectIdLookup;
            if ((assetObjectIdentifierHashSets == null) || (!assetObjectIdentifierHashSets.TryGetValue(referencedAsset.asset, out refObjectIdLookup)))
            {
                refObjectIdLookup = new HashSet<ObjectIdentifier>(referencedAsset.referencedObjects);
                assetObjectIdentifierHashSets?.Add(referencedAsset.asset, refObjectIdLookup);
            }
            return refObjectIdLookup;
        }

        internal static List<GUID> FilterReferencesForAsset(IDependencyData dependencyData, GUID asset, List<ObjectIdentifier> references, HashSet<ObjectIdentifier> previousSceneObjects = null, HashSet<GUID> previousSceneReferences = null, Dictionary<GUID, HashSet<ObjectIdentifier>> assetObjectIdentifierHashSets = null)
        {
            var referencedAssets = new HashSet<AssetLoadInfo>();
            var referencedAssetsGuids = new List<GUID>(referencedAssets.Count);
            var referencesPruned = new List<ObjectIdentifier>(references.Count);
            // Remove Default Resources and Includes for Assets assigned to Bundles
            foreach (ObjectIdentifier reference in references)
            {
                if (reference.filePath.Equals(CommonStrings.UnityDefaultResourcePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (dependencyData.AssetInfo.TryGetValue(reference.guid, out AssetLoadInfo referenceInfo))
                {
                    if (referencedAssets.Add(referenceInfo))
                        referencedAssetsGuids.Add(referenceInfo.asset);
                    continue;
                }
                referencesPruned.Add(reference);
            }
            references.Clear();
            references.AddRange(referencesPruned);

            // Remove References also included by non-circular Referenced Assets
            // Remove References also included by circular Referenced Assets if Asset's GUID is higher than Referenced Asset's GUID
            foreach (AssetLoadInfo referencedAsset in referencedAssets)
            {
                if ((asset > referencedAsset.asset) || (asset == referencedAsset.asset))
                {
                    references.RemoveAll(GetRefObjectIdLookup(referencedAsset, assetObjectIdentifierHashSets).Contains);
                }
                else
                {
                    bool exists = false;
                    foreach (ObjectIdentifier referencedObject in referencedAsset.referencedObjects)
                    {
                        if (referencedObject.guid == asset)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        references.RemoveAll(GetRefObjectIdLookup(referencedAsset, assetObjectIdentifierHashSets).Contains);
                    }
                }
            }

            // Special path for scenes, they can reference the same assets previously references
            if (!previousSceneReferences.IsNullOrEmpty())
            {
                foreach (GUID reference in previousSceneReferences)
                {
                    if (!dependencyData.AssetInfo.TryGetValue(reference, out AssetLoadInfo referencedAsset))
                        continue;

                    var refObjectIdLookup = GetRefObjectIdLookup(referencedAsset, assetObjectIdentifierHashSets);
                    // NOTE: It's impossible for an asset to depend on a scene, thus no need for circular reference checks
                    // So just remove and add a dependency on the asset if there is a need to depend upon it.
                    if (references.RemoveAll(refObjectIdLookup.Contains) > 0)
                        referencedAssetsGuids.Add(referencedAsset.asset);
                }
            }

            // Special path for scenes, they can use data from previous sharedAssets in the same bundle
            if (!previousSceneObjects.IsNullOrEmpty())
                references.RemoveAll(previousSceneObjects.Contains);
            return referencedAssetsGuids;
        }
    }
}
