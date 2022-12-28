using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    [TestFixture]
    class ObjectExtractionTaskTests
    {
        internal class ExtractionDependencyData : TestDependencyDataBase
        {
            Dictionary<GUID, AssetLoadInfo> m_AssetInfo = new Dictionary<GUID, AssetLoadInfo>();
            public override Dictionary<GUID, AssetLoadInfo> AssetInfo => m_AssetInfo;

            Dictionary<GUID, SceneDependencyInfo> m_SceneInfo = new Dictionary<GUID, SceneDependencyInfo>();
            public override Dictionary<GUID, SceneDependencyInfo> SceneInfo => m_SceneInfo;
        }

        internal class ExtractionObjectLayout : TestBundleExplictObjectLayout
        {
            Dictionary<ObjectIdentifier, string> m_ExplicitObjectLocation = new Dictionary<ObjectIdentifier, string>();
            public override Dictionary<ObjectIdentifier, string> ExplicitObjectLocation { get => m_ExplicitObjectLocation; }
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

        static void TestSetup(out ObjectIdentifier nonMonoScriptId, out ObjectIdentifier monoScriptId, out CreateMonoScriptBundle task, out ExtractionDependencyData dependencyData, out ExtractionObjectLayout layout)
        {
            nonMonoScriptId = MakeObjectId(GUID.Generate().ToString(), 20, FileType.MetaAssetType, "");
            monoScriptId = MakeObjectId(GUID.Generate().ToString(), 50, FileType.MetaAssetType, "");
            task = new CreateMonoScriptBundle("MonoScriptBundle");
            dependencyData = new ExtractionDependencyData();
            layout = new ExtractionObjectLayout();

            BuildCacheUtility.ClearCacheHashes();
            BuildCacheUtility.SetTypeForObjects(new[] {
                new ObjectTypes(nonMonoScriptId, new[] {typeof(GameObject) }),
                new ObjectTypes(monoScriptId, new[] {typeof(MonoScript) })
            });

            IBuildContext context = new BuildContext(dependencyData, layout);
            ContextInjector.Inject(context, task);
        }

        [Test]
        public void CreateMonoScriptBundle_ExtractsMonoScripts_FromAssetInfo()
        {
            TestSetup(out ObjectIdentifier nonMonoScriptId, out ObjectIdentifier monoScriptId, out CreateMonoScriptBundle task, out ExtractionDependencyData dependencyData, out ExtractionObjectLayout layout);

            var assetInfo = new AssetLoadInfo();
            assetInfo.referencedObjects = new List<ObjectIdentifier> { nonMonoScriptId, monoScriptId };
            dependencyData.AssetInfo.Add(GUID.Generate(), assetInfo);

            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(nonMonoScriptId));
            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(monoScriptId));

            task.Run();

            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(nonMonoScriptId));
            Assert.IsTrue(layout.ExplicitObjectLocation.ContainsKey(monoScriptId));
            Assert.AreEqual(task.MonoScriptBundleName, layout.ExplicitObjectLocation[monoScriptId]);
        }

        [Test]
        public void CreateMonoScriptBundle_ExtractsMonoScripts_FromSceneInfo()
        {
            TestSetup(out ObjectIdentifier nonMonoScriptId, out ObjectIdentifier monoScriptId, out CreateMonoScriptBundle task, out ExtractionDependencyData dependencyData, out ExtractionObjectLayout layout);

            var sceneInfo = new SceneDependencyInfo();
            sceneInfo.SetReferencedObjects(new[] { nonMonoScriptId, monoScriptId });
            dependencyData.SceneInfo.Add(GUID.Generate(), sceneInfo);

            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(nonMonoScriptId));
            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(monoScriptId));

            task.Run();

            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(nonMonoScriptId));
            Assert.IsTrue(layout.ExplicitObjectLocation.ContainsKey(monoScriptId));
            Assert.AreEqual(task.MonoScriptBundleName, layout.ExplicitObjectLocation[monoScriptId]);
        }

        [Test]
        public void CreateMonoScriptBundle_DoesNotExtractMonoScripts_FromDefaultResources()
        {
            TestSetup(out ObjectIdentifier nonMonoScriptId, out ObjectIdentifier monoScriptId, out CreateMonoScriptBundle task, out ExtractionDependencyData dependencyData, out ExtractionObjectLayout layout);

            var defaultResourceMonoScriptId = MakeObjectId(CommonStrings.UnityDefaultResourceGuid, 50, FileType.MetaAssetType, CommonStrings.UnityDefaultResourcePath);
            BuildCacheUtility.SetTypeForObjects(new[] {
                new ObjectTypes(defaultResourceMonoScriptId, new[] {typeof(MonoScript) })
            });

            var assetInfo = new AssetLoadInfo();
            assetInfo.referencedObjects = new List<ObjectIdentifier> { nonMonoScriptId, monoScriptId, defaultResourceMonoScriptId };
            dependencyData.AssetInfo.Add(GUID.Generate(), assetInfo);

            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(nonMonoScriptId));
            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(monoScriptId));
            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(defaultResourceMonoScriptId));

            task.Run();

            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(nonMonoScriptId));
            Assert.IsTrue(layout.ExplicitObjectLocation.ContainsKey(monoScriptId));
            Assert.IsFalse(layout.ExplicitObjectLocation.ContainsKey(defaultResourceMonoScriptId));
            Assert.AreEqual(task.MonoScriptBundleName, layout.ExplicitObjectLocation[monoScriptId]);
        }
    }
}
