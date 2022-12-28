using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    class GenerateBundlePackingTests
    {
        IDependencyData GetDependencyData(List<ObjectIdentifier> objects, params GUID[] guids)
        {
            IDependencyData dep = new BuildDependencyData();
            for (int i = 0; i < guids.Length; i++)
            {
                AssetLoadInfo loadInfo = new AssetLoadInfo()
                {
                    asset = guids[i],
                    address = $"path{i}",
                    includedObjects = objects,
                    referencedObjects = objects
                };
                dep.AssetInfo.Add(guids[i], loadInfo);
            }
            return dep;
        }

        List<ObjectIdentifier> CreateObjectIdentifierList(string path, params GUID[] guids)
        {
            var objects = new List<ObjectIdentifier>();
            foreach (GUID guid in guids)
            {
                var obj = new ObjectIdentifier();
                obj.SetObjectIdentifier(guid, 0, FileType.SerializedAssetType, path);
                objects.Add(obj);
            }
            return objects;
        }

        string k_TempAsset = "Assets/temp.prefab";
        GUID k_TempGuid;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.parent = root.transform;
            PrefabUtility.SaveAsPrefabAsset(root, k_TempAsset);
            k_TempGuid = new GUID(AssetDatabase.AssetPathToGUID(k_TempAsset));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(k_TempAsset);
        }

        [Test]
        public void WhenReferencesAreUnique_FilterReferencesForAsset_ReturnsReferences()
        {
            var assetInBundle = new GUID("00000000000000000000000000000000");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList("path", assetInBundle, assetInBundle);
            IDependencyData dep = GetDependencyData(objects, assetInBundle);

            var references = new List<ObjectIdentifier>(objects);
            List<GUID> results = GenerateBundlePacking.FilterReferencesForAsset(dep, assetInBundle, references);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(assetInBundle, results[0]);
        }

        [Test]
        public void WhenReferencesContainsDefaultResources_FilterReferencesForAsset_PrunesDefaultResources()
        {
            var assetInBundle = new GUID("00000000000000000000000000000000");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList(CommonStrings.UnityDefaultResourcePath, assetInBundle);
            IDependencyData dep = GetDependencyData(objects, assetInBundle);

            var references = new List<ObjectIdentifier>(objects);
            GenerateBundlePacking.FilterReferencesForAsset(dep, assetInBundle, references);
            Assert.AreEqual(0, references.Count);
        }

        [Test]
        public void WhenReferencesContainsAssetsInBundles_FilterReferencesForAsset_PrunesAssetsInBundles()
        {
            var assetInBundle = new GUID("00000000000000000000000000000000");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList("path", assetInBundle);
            IDependencyData dep = GetDependencyData(objects, assetInBundle);

            var references = new List<ObjectIdentifier>(objects);
            GenerateBundlePacking.FilterReferencesForAsset(dep, assetInBundle, references);
            Assert.AreEqual(0, references.Count);
        }

        [Test]
        public void WhenReferencesDoesNotContainAssetsInBundles_FilterReferences_PrunesNothingAndReturnsNothing()
        {
            var assetInBundle = new GUID("00000000000000000000000000000000");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList("path", assetInBundle);
            IDependencyData dep = new BuildDependencyData();

            var references = new List<ObjectIdentifier>(objects);
            List<GUID> results = GenerateBundlePacking.FilterReferencesForAsset(dep, assetInBundle, references);
            Assert.AreEqual(1, references.Count);
            Assert.AreEqual(assetInBundle, references[0].guid);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void WhenReferencesContainsRefsIncludedByNonCircularAssets_FilterReferencesForAsset_PrunesRefsIncludedByNonCircularAssets()
        {
            var assetNotInBundle = new GUID("00000000000000000000000000000000");
            var referenceInBundle = new GUID("00000000000000000000000000000001");
            var referenceNotInBundle = new GUID("00000000000000000000000000000002");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList("path", referenceNotInBundle);
            IDependencyData dep = GetDependencyData(objects, referenceInBundle);

            List<ObjectIdentifier> references = CreateObjectIdentifierList("path", referenceInBundle, referenceNotInBundle);
            GenerateBundlePacking.FilterReferencesForAsset(dep, assetNotInBundle, references);
            Assert.AreEqual(0, references.Count);
        }

        [Test]
        public void WhenReferencesContainsRefsIncludedByCircularAssetsWithLowerGuid_FilterReferencesForAsset_PrunesRefsIncludedByCircularAssetsWithLowerGuid()
        {
            var assetNotInBundle = new GUID("00000000000000000000000000000001");
            var referenceInBundle = new GUID("00000000000000000000000000000000");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList("path", assetNotInBundle); // circular reference to asset whose references we want to filter
            IDependencyData dep = GetDependencyData(objects, referenceInBundle);

            List<ObjectIdentifier> references = CreateObjectIdentifierList("path", referenceInBundle, assetNotInBundle);
            GenerateBundlePacking.FilterReferencesForAsset(dep, assetNotInBundle, references);
            Assert.AreEqual(0, references.Count);
        }

        [Test]
        public void WhenReferencesContainsRefsIncludedByCircularAssetsWithHigherGuid_FilterReferencesForAsset_DoesNotPruneRefsIncludedByCircularAssetsWithHigherGuid()
        {
            var assetNotInBundle = new GUID("00000000000000000000000000000000");
            var referenceInBundle = new GUID("00000000000000000000000000000001");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList("path", assetNotInBundle); // circular reference to asset whose references we want to filter
            IDependencyData dep = GetDependencyData(objects, referenceInBundle);

            List<ObjectIdentifier> references = CreateObjectIdentifierList("path", referenceInBundle, assetNotInBundle);
            GenerateBundlePacking.FilterReferencesForAsset(dep, assetNotInBundle, references);
            Assert.AreEqual(1, references.Count);
            Assert.AreEqual(assetNotInBundle, references[0].guid);
        }

        [Test]
        public void WhenReferencesContainsPreviousSceneObjects_FilterReferencesForAsset_PrunesPreviousSceneObjects()
        {
            var assetInBundle = new GUID("00000000000000000000000000000001");
            var referenceNotInBundle = new GUID("00000000000000000000000000000000");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList("path", referenceNotInBundle);
            IDependencyData dep = GetDependencyData(objects, assetInBundle);

            var references = new List<ObjectIdentifier>(objects);
            GenerateBundlePacking.FilterReferencesForAsset(dep, assetInBundle, references, new HashSet<ObjectIdentifier>() { objects[0] });
            Assert.AreEqual(0, references.Count);
        }

        [Test]
        public void WhenReferencesContainPreviousSceneAssetDependencies_FilterReferencesForAsset_PrunesPreviousAssetDependencies([Values] bool containsPreviousSceneAsset)
        {
            var assetInBundle = new GUID("00000000000000000000000000000001");
            var referenceNotInBundle = new GUID("00000000000000000000000000000000");
            List<ObjectIdentifier> objects = CreateObjectIdentifierList("path", referenceNotInBundle);
            IDependencyData dep = GetDependencyData(objects, assetInBundle);

            var references = new List<ObjectIdentifier>(objects);
            var previousSceneReferences = new HashSet<GUID>();
            if (containsPreviousSceneAsset)
                previousSceneReferences.Add(assetInBundle);
            GenerateBundlePacking.FilterReferencesForAsset(dep, assetInBundle, references, new HashSet<ObjectIdentifier>(), previousSceneReferences);

            if (containsPreviousSceneAsset)
                Assert.AreEqual(0, references.Count);
            else
                Assert.AreEqual(1, references.Count);
        }

#if !UNITY_2019_1_OR_NEWER
        [Test]
        public void WhenPrefabContainsDuplicateTypes_GetSortedSceneObjectIdentifiers_DoesNotThorwError()
        {
            var includes = new List<ObjectIdentifier>(ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(k_TempGuid, EditorUserBuildSettings.activeBuildTarget));
            var sorted = GenerateBundleCommands.GetSortedSceneObjectIdentifiers(includes);
            Assert.AreEqual(includes.Count, sorted.Count);
        }
#endif

        [Test]
        public void BuildCacheUtility_GetSortedUniqueTypesForObjects_ReturnsUniqueAndSortedTypeArray()
        {
            var includes = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(k_TempGuid, EditorUserBuildSettings.activeBuildTarget);

            // Test prefab is created using 2 primitive cubes, one parented to the other, so the includes will in turn contain the sequence 2x:
            Type[] expectedTypes = new[] { typeof(GameObject), typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider) };
            Array.Sort(expectedTypes, (x, y) => x.AssemblyQualifiedName.CompareTo(y.AssemblyQualifiedName));

            var actualTypes = BuildCacheUtility.GetSortedUniqueTypesForObjects(includes);
            Assert.AreEqual(expectedTypes.Length * 2, includes.Length);
            Assert.AreEqual(expectedTypes.Length, actualTypes.Length);
            CollectionAssert.AreEqual(expectedTypes, actualTypes);
        }

        [Test]
        public void BuildCacheUtility_GetMainTypeForObjects_ReturnsUniqueAndSortedTypeArray()
        {
            var includes = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(k_TempGuid, EditorUserBuildSettings.activeBuildTarget);

            // Test prefab is created using 2 primitive cubes, one parented to the other, so the includes will in turn contain the sequence:
            Type[] expectedTypes = new[] { typeof(GameObject), typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider),
                                           typeof(GameObject), typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider) };
            // One catch, the ordering of the expected types is based on the order of includes which is in turn ordered by the local identifier in file.
            // Since we are generating the prefab as part of the test, and lfids generation is random, we don't know what order they will be returned in.
            // So sort both expected types lists and compare exact.
            Array.Sort(expectedTypes, (x, y) => x.AssemblyQualifiedName.CompareTo(y.AssemblyQualifiedName));

            var actualTypes = BuildCacheUtility.GetMainTypeForObjects(includes);
            Array.Sort(actualTypes, (x, y) => x.AssemblyQualifiedName.CompareTo(y.AssemblyQualifiedName));

            Assert.AreEqual(expectedTypes.Length, includes.Length);
            Assert.AreEqual(expectedTypes.Length, actualTypes.Length);
            CollectionAssert.AreEqual(expectedTypes, actualTypes);
        }
    }
}
