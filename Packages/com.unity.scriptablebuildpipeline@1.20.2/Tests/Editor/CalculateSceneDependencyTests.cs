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
using UnityEditor.Build.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.Build.Pipeline.Tests
{
    public class CalculateSceneDependencyTests
    {
        class TestParams : TestBuildParametersBase
        {
            // Inputs
            public override bool UseCache { get; set; }
            public override BuildTarget Target { get => BuildTarget.NoTarget; }
            public override BuildTargetGroup Group { get => BuildTargetGroup.Unknown; }
            public override TypeDB ScriptInfo { get => null; }
            public override ContentBuildFlags ContentBuildFlags { get => ContentBuildFlags.None; }
            public override bool NonRecursiveDependencies { get; set; }

#if !UNITY_2019_3_OR_NEWER
            public override string TempOutputFolder => ContentPipeline.kTempBuildPath;
#endif

            public override BuildSettings GetContentBuildSettings()
            {
                return new BuildSettings
                {
                    group = Group,
                    target = Target,
                    typeDB = ScriptInfo,
                    buildFlags = ContentBuildFlags
                };
            }
        }

        class TestContent : TestBundleBuildContent
        {
            public List<GUID> TestScenes = new List<GUID>();
            public List<GUID> TestAssets = new List<GUID>();

            // Inputs
            public override List<GUID> Scenes => TestScenes;
            public override List<GUID> Assets => TestAssets;
        }

        class TestDependencyData : TestDependencyDataBase
        {
            public Dictionary<GUID, SceneDependencyInfo> TestSceneInfo = new Dictionary<GUID, SceneDependencyInfo>();
            public Dictionary<GUID, BuildUsageTagSet> TestSceneUsage = new Dictionary<GUID, BuildUsageTagSet>();
            public Dictionary<GUID, Hash128> TestDependencyHash = new Dictionary<GUID, Hash128>();

            // Inputs
            public override BuildUsageCache DependencyUsageCache => null;

            // Outputs
            public override Dictionary<GUID, SceneDependencyInfo> SceneInfo => TestSceneInfo;
            public override Dictionary<GUID, BuildUsageTagSet> SceneUsage => TestSceneUsage;
            public override Dictionary<GUID, Hash128> DependencyHash => TestDependencyHash;
        }

        const string k_FolderPath = "Test";
        const string k_TmpPath = "tmp";

        const string k_ScenePath = "Assets/testScene.unity";
        const string k_TestAssetsPath = "Assets/TestAssetsOnlyWillBeDeleted";
        const string k_CubePath = k_TestAssetsPath + "/Cube.prefab";
        const string k_CubePath2 = k_TestAssetsPath + "/Cube2.prefab";

        static CalculateSceneDependencyData CreateDefaultBuildTask(List<GUID> scenes, BuildCache optionalCache, bool nonRecursive = false)
        {
            var task = new CalculateSceneDependencyData();
            var testParams = new TestParams();
            testParams.UseCache = optionalCache != null;
            testParams.NonRecursiveDependencies = nonRecursive;
            var testContent = new TestContent { TestScenes = scenes };
            var testData = new TestDependencyData();
            IBuildContext context = new BuildContext(testParams, testContent, testData, optionalCache);
            ContextInjector.Inject(context, task);
            return task;
        }

        static void ExtractTestData(IBuildTask task, out TestDependencyData dependencyData)
        {
            IBuildContext context = new BuildContext();
            ContextInjector.Extract(context, task);
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

        [OneTimeSetUp]
        public void Setup()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            Directory.CreateDirectory(k_TestAssetsPath);
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SaveAsPrefabAsset(GameObject.CreatePrimitive(PrimitiveType.Cube), k_CubePath);
            PrefabUtility.SaveAsPrefabAsset(GameObject.CreatePrimitive(PrimitiveType.Cube), k_CubePath2);
#else
            PrefabUtility.CreatePrefab(k_CubePath, GameObject.CreatePrimitive(PrimitiveType.Cube));
            PrefabUtility.CreatePrefab(k_CubePath2, GameObject.CreatePrimitive(PrimitiveType.Cube));
#endif
            AssetDatabase.ImportAsset(k_CubePath);
            AssetDatabase.ImportAsset(k_CubePath2);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            AssetDatabase.DeleteAsset(k_ScenePath);
            AssetDatabase.DeleteAsset(k_CubePath);
            AssetDatabase.DeleteAsset(k_CubePath2);
            AssetDatabase.DeleteAsset(k_TestAssetsPath);

            if (Directory.Exists(k_FolderPath))
                Directory.Delete(k_FolderPath, true);
            if (Directory.Exists(k_TmpPath))
                Directory.Delete(k_TmpPath, true);
        }

        static void SetupSceneForTest(out Scene scene, out GameObject prefab)
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_CubePath);
            prefab.transform.position = new Vector3(0, 0, 0);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            PrefabUtility.InstantiatePrefab(prefab);

            EditorSceneManager.SaveScene(scene, k_ScenePath);
        }

        static object[] DependencyHashTestCases =
        {
            new object[] { true, (Action<object, object>)Assert.AreNotEqual },
            new object[] { false, (Action<object, object>)Assert.AreEqual },
        };

        [TestCaseSource("DependencyHashTestCases")]
        [Test]
        public void CalculateSceneDependencyData_DependencyHashTests(bool modifyPrefab, Action<object, object> assertType)
        {
            SetupSceneForTest(out Scene scene, out GameObject prefab);

            List<GUID> scenes = new List<GUID>();
            GUID sceneGuid = new GUID(AssetDatabase.AssetPathToGUID(scene.path));
            scenes.Add(sceneGuid);

            TestDependencyData dependencyData1;
            using (BuildCache cache = new BuildCache())
            {
                var buildTask = CreateDefaultBuildTask(scenes, cache);
                buildTask.Run();
                ExtractTestData(buildTask, out dependencyData1);
            }
            BuildCache.PurgeCache(false);

            if (modifyPrefab)
                prefab.transform.position = new Vector3(1, 1, 1);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();

            TestDependencyData dependencyData2;
            using (BuildCache cache = new BuildCache())
            {
                var buildTask = CreateDefaultBuildTask(scenes, cache);
                buildTask.Run();
                ExtractTestData(buildTask, out dependencyData2);
            }
            BuildCache.PurgeCache(false);

            assertType(dependencyData1.DependencyHash[sceneGuid], dependencyData2.DependencyHash[sceneGuid]);
        }

        [Test]
        public void CalculateSceneDependencyData_DependencyHash_IsZeroWhenNotUsingCashing()
        {
            SetupSceneForTest(out Scene scene, out var _);

            List<GUID> scenes = new List<GUID>();
            GUID sceneGuid = new GUID(AssetDatabase.AssetPathToGUID(scene.path));
            scenes.Add(sceneGuid);

            TestDependencyData dependencyData;
            var buildTask = CreateDefaultBuildTask(scenes, null);
            buildTask.Run();
            ExtractTestData(buildTask, out dependencyData);

            Assert.AreEqual(new Hash128(), dependencyData.DependencyHash[sceneGuid]);
        }

        [Test]
        public void CalcualteSceneDependencyData_ReturnsNonEmptyUsage_ForNonRecursiveDependencies()
        {
            SetupSceneForTest(out Scene scene, out var _);

            List<GUID> scenes = new List<GUID>();
            GUID sceneGuid = new GUID(AssetDatabase.AssetPathToGUID(scene.path));
            scenes.Add(sceneGuid);

            TestDependencyData dependencyData;
            var buildTask = CreateDefaultBuildTask(scenes, null, true);
            buildTask.Run();
            ExtractTestData(buildTask, out dependencyData);

            BuildUsageTagSet usagetSet = dependencyData.SceneUsage[sceneGuid];
            var method = typeof(BuildUsageTagSet).GetMethod("SerializeToJson", BindingFlags.Instance | BindingFlags.NonPublic);
            var json = method.Invoke(usagetSet, new object[0]) as string;

            Assert.AreNotEqual(json, "{\"m_objToUsage\":[]}");
        }
    }
}
