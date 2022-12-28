#if UNITY_2019_3_OR_NEWER
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Player;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    public class CalculateCustomDependencyTests
    {
        class TestBuildParameters : TestBuildParametersBase
        {
            // Optional Inputs
            public override BuildTarget Target => BuildTarget.NoTarget;
            public override TypeDB ScriptInfo => null;
            public override bool UseCache { get => false; set => base.UseCache = value; }
        }

        class TestContent : TestBundleBuildContent
        {
            // Inputs
            List<CustomContent> m_CustomAssets;
            public override List<CustomContent> CustomAssets => m_CustomAssets;

            // Outputs
            Dictionary<string, List<GUID>> m_BundleLayout;
            Dictionary<GUID, string> m_Addresses;
            public override Dictionary<string, List<GUID>> BundleLayout => m_BundleLayout;
            public override Dictionary<GUID, string> Addresses => m_Addresses;

            public TestContent(List<CustomContent> customAssets)
            {
                m_CustomAssets = customAssets;
                m_BundleLayout = new Dictionary<string, List<GUID>>();
                m_Addresses = new Dictionary<GUID, string>();
            }
        }

        class TestDependencyData : TestDependencyDataBase
        {
            // Input / Output
            Dictionary<GUID, AssetLoadInfo> m_AssetInfo;
            public override Dictionary<GUID, AssetLoadInfo> AssetInfo => m_AssetInfo;

            // Optional Inputs
            BuildUsageTagGlobal m_GlobalUsage;
            Dictionary<GUID, SceneDependencyInfo> m_SceneInfo;
            public override BuildUsageTagGlobal GlobalUsage { get => m_GlobalUsage; set => m_GlobalUsage = value; }
            public override Dictionary<GUID, SceneDependencyInfo> SceneInfo => m_SceneInfo;
            public override BuildUsageCache DependencyUsageCache => null;

            // Outputs
            Dictionary<GUID, BuildUsageTagSet> m_AssetUsage;
            public override Dictionary<GUID, BuildUsageTagSet> AssetUsage => m_AssetUsage;

            public TestDependencyData(Dictionary<GUID, AssetLoadInfo> assetInfo)
            {
                m_AssetInfo = assetInfo;
                m_GlobalUsage = new BuildUsageTagGlobal();
                m_SceneInfo = new Dictionary<GUID, SceneDependencyInfo>();
                m_AssetUsage = new Dictionary<GUID, BuildUsageTagSet>();
            }
        }

        static CalculateCustomDependencyData CreateDefaultBuildTask(List<CustomContent> customAssets, Dictionary<GUID, AssetLoadInfo> assetInfo = null)
        {
            var task = new CalculateCustomDependencyData();
            var testParams = new TestBuildParameters();
            var testContent = new TestContent(customAssets);
            if (assetInfo == null)
                assetInfo = new Dictionary<GUID, AssetLoadInfo>();
            var testData = new TestDependencyData(assetInfo);
            IBuildContext context = new BuildContext(testParams, testContent, testData);
            ContextInjector.Inject(context, task);
            return task;
        }

        static void ExtractTestData(IBuildTask task, out CustomAssets customAssets, out TestContent content, out TestDependencyData dependencyData)
        {
            IBuildContext context = new BuildContext();
            ContextInjector.Extract(context, task);
            customAssets = (CustomAssets)context.GetContextObject<ICustomAssets>();
            content = (TestContent)context.GetContextObject<IBundleBuildContent>();
            dependencyData = (TestDependencyData)context.GetContextObject<IDependencyData>();
        }

        static ObjectIdentifier MakeObjectId(string guid, long localIdentifierInFile, FileType fileType, string filePath)
        {
            var objectId = new ObjectIdentifier();
            var boxed = (object)objectId;
            var type = typeof(ObjectIdentifier);
            type.GetField("m_GUID", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, new GUID(guid));
            type.GetField("m_LocalIdentifierInFile", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, localIdentifierInFile);
            type.GetField("m_FileType", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, fileType);
            type.GetField("m_FilePath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, filePath);
            return (ObjectIdentifier)boxed;
        }

        [Test]
        public void CreateAssetEntryForObjectIdentifiers_ThrowsExceptionOnAssetGUIDCollision()
        {
            var assetPath = "temp/test_serialized_file.asset";
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[] { Texture2D.whiteTexture, Texture2D.redTexture }, assetPath, false);

            var address = "CustomAssetAddress";
            var assetInfo = new Dictionary<GUID, AssetLoadInfo>();
            assetInfo.Add(HashingMethods.Calculate(address).ToGUID(), new AssetLoadInfo());

            var customContent = new List<CustomContent>
            {
                new CustomContent
                {
                    Asset = new GUID(),
                    Processor = (guid, task) =>
                    {
                        task.GetObjectIdentifiersAndTypesForSerializedFile(assetPath, out var objectIdentifiers, out var types);
                        var ex = Assert.Throws<System.ArgumentException>(() => task.CreateAssetEntryForObjectIdentifiers(objectIdentifiers, assetPath, "CustomAssetBundle", address, types[0]));
                        var expected = string.Format("Custom Asset '{0}' already exists. Building duplicate asset entries is not supported.", address);
                        Assert.That(ex.Message, Is.EqualTo(expected));
                    }
                }
            };

            var buildTask = CreateDefaultBuildTask(customContent, assetInfo);
            buildTask.Run();
        }

        [Test]
        public void GetObjectIdentifiersAndTypesForSerializedFile_ReturnsAllObjectIdentifiersAndTypes()
        {
            var assetPath = "temp/test_serialized_file.asset";
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[] { Texture2D.whiteTexture, Texture2D.redTexture }, assetPath, false);

            var customContent = new List<CustomContent>
            {
                new CustomContent
                {
                    Asset = new GUID(),
                    Processor = (guid, task) =>
                    {
                        task.GetObjectIdentifiersAndTypesForSerializedFile(assetPath, out var objectIdentifiers, out var types);
                        Assert.AreEqual(2, objectIdentifiers.Length);
                        Assert.AreEqual(MakeObjectId("00000000000000000000000000000000", 1, FileType.NonAssetType, assetPath), objectIdentifiers[0]);
                        Assert.AreEqual(MakeObjectId("00000000000000000000000000000000", 2, FileType.NonAssetType, assetPath), objectIdentifiers[1]);

                        Assert.AreEqual(1, types.Length);
                        Assert.AreEqual(typeof(Texture2D), types[0]);
                    }
                }
            };

            var buildTask = CreateDefaultBuildTask(customContent);
            buildTask.Run();
        }

        [Test]
        public void CreateAssetEntryForObjectIdentifiers_AddsNewBundleAndAssetDataForCustomAsset()
        {
            var assetPath = "temp/test_serialized_file.asset";
            var bundleName = "CustomAssetBundle";
            var address = "CustomAssetAddress";
            var assetGuid = HashingMethods.Calculate(address).ToGUID();
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[] { Texture2D.whiteTexture, Texture2D.redTexture }, assetPath, false);

            var customContent = new List<CustomContent>
            {
                new CustomContent
                {
                    Asset = new GUID(),
                    Processor = (guid, task) =>
                    {
                        task.GetObjectIdentifiersAndTypesForSerializedFile(assetPath, out var includedObjects, out var types);
                        task.CreateAssetEntryForObjectIdentifiers(includedObjects, assetPath, bundleName, address, types[0]);
                    }
                }
            };

            var buildTask = CreateDefaultBuildTask(customContent);
            buildTask.Run();

            ExtractTestData(buildTask, out var customAssets, out var content, out var dependencyData);

            // Ensure the bundle name was added, and the custom asset guid was added to that bundle
            Assert.IsTrue(content.BundleLayout.ContainsKey(bundleName));
            CollectionAssert.Contains(content.BundleLayout[bundleName], assetGuid);

            // Ensure the custom address was added
            Assert.IsTrue(content.Addresses.ContainsKey(assetGuid));
            Assert.AreEqual(address, content.Addresses[assetGuid]);

            // Ensure AssetInfo contains the calculated includes and references for the custom asset
            Assert.IsTrue(dependencyData.AssetInfo.ContainsKey(assetGuid));
            var loadInfo = dependencyData.AssetInfo[assetGuid];
            Assert.AreEqual(address, loadInfo.address);
            Assert.AreEqual(assetGuid, loadInfo.asset);
            Assert.AreEqual(2, loadInfo.includedObjects.Count);
            Assert.AreEqual(0, loadInfo.referencedObjects.Count);

            // Ensure the usage tags were added
            Assert.IsTrue(dependencyData.AssetUsage.ContainsKey(assetGuid));
            Assert.IsNotNull(dependencyData.AssetUsage[assetGuid]);

            // Ensure the custom asset was registered in the customAssets list
            CollectionAssert.Contains(customAssets.Assets, assetGuid);
        }
    }
}
#endif
