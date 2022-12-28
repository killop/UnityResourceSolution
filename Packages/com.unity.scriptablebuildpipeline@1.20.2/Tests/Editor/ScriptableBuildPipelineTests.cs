using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.WriteTypes;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Build.Pipeline;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEditor.Build.Pipeline.Tests
{
    [TestFixture]

    class ScriptableBuildPipelineTests
    {
        const string k_FolderPath = "Test";
        const string k_TmpPath = "tmp";

        const string k_ScenePath = "Assets/testScene.unity";
        const string k_TestAssetsPath = "Assets/TestAssetsOnlyWillBeDeleted";
        const string k_CubePath = k_TestAssetsPath + "/Cube.prefab";
        const string k_CubePath2 = k_TestAssetsPath + "/Cube2.prefab";

        ScriptableBuildPipeline.Settings m_Settings;

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

            m_Settings = LoadSettingsFromFile();
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

            ScriptableBuildPipeline.s_Settings = m_Settings;
            ScriptableBuildPipeline.SaveSettings();
        }

        static ReturnCode RunTask<T>(params IContextObject[] args) where T : IBuildTask
        {
            IBuildContext context = new BuildContext(args);
            IBuildTask instance = Activator.CreateInstance<T>();
            ContextInjector.Inject(context, instance);
            var result = instance.Run();
            ContextInjector.Extract(context, instance);
            return result;
        }

        static IBundleBuildParameters GetBuildParameters()
        {
            if (Directory.Exists(k_FolderPath))
                Directory.Delete(k_FolderPath, true);
            if (Directory.Exists(k_TmpPath))
                Directory.Delete(k_TmpPath, true);

            Directory.CreateDirectory(k_FolderPath);
            Directory.CreateDirectory(k_TmpPath);

            IBundleBuildParameters buildParams = new BundleBuildParameters(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.selectedBuildTargetGroup, k_FolderPath);
            buildParams.TempOutputFolder = k_TmpPath;
            return buildParams;
        }

        static AssetBundleBuild CreateBundleBuild(string name, string path)
        {
            return new AssetBundleBuild()
            {
                addressableNames = new[] { path },
                assetBundleName = name,
                assetBundleVariant = "",
                assetNames = new[] { path }
            };
        }

        static IBundleBuildContent GetBundleContent(bool createAllBundles = false)
        {
            List<AssetBundleBuild> buildData = new List<AssetBundleBuild>();
            buildData.Add(CreateBundleBuild("bundle", k_CubePath));
            if (createAllBundles)
                buildData.Add(CreateBundleBuild("bundle2", k_CubePath2));

            IBundleBuildContent buildContent = new BundleBuildContent(buildData);
            return buildContent;
        }

        static IDependencyData GetDependencyData()
        {
            GUID guid;
            GUID.TryParse(AssetDatabase.AssetPathToGUID(k_CubePath), out guid);
            ObjectIdentifier[] oId = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(guid, EditorUserBuildSettings.activeBuildTarget);
            AssetLoadInfo loadInfo = new AssetLoadInfo()
            {
                asset = guid,
                address = k_CubePath,
                includedObjects = oId.ToList(),
                referencedObjects = oId.ToList()
            };

            IDependencyData dep = new BuildDependencyData();
            dep.AssetInfo.Add(guid, loadInfo);
            dep.AssetUsage.Add(guid, new BuildUsageTagSet());

            return dep;
        }

        [UnityTest]
        public IEnumerator BuildPipeline_AssetBundleBuild_DoesNotResetUnsavedScene()
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            yield return null;
            EditorSceneManager.SaveScene(s, k_ScenePath);
            GameObject.CreatePrimitive(PrimitiveType.Cube);
            EditorSceneManager.MarkSceneDirty(s);

            GameObject objectWeAdded = GameObject.Find("Cube");
            Assert.IsNotNull(objectWeAdded, "No object before entering playmode");
            Assert.AreEqual("testScene", SceneManager.GetActiveScene().name);

            IBundleBuildParameters buildParameters = GetBuildParameters();
            IBundleBuildContent buildContent = GetBundleContent();
            IBundleBuildResults results;

            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out results);
            Assert.AreEqual(ReturnCode.UnsavedChanges, exitCode);

            Assert.AreEqual("testScene", SceneManager.GetActiveScene().name);
            objectWeAdded = GameObject.Find("Cube");
            Assert.IsNotNull(objectWeAdded, "No object after entering playmode");

            EditorSceneManager.SaveScene(s, k_ScenePath);
        }

        [Test]
        public void BuildPipeline_AssetBundleBuild_WritesLinkXMLFile()
        {
            IBundleBuildParameters buildParameters = GetBuildParameters();
            buildParameters.WriteLinkXML = true;
            IBundleBuildContent buildContent = GetBundleContent();
            IBundleBuildResults results;

            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out results);
            Assert.AreEqual(ReturnCode.Success, exitCode);


            var assemblies = new HashSet<Assembly>();
            var types = new HashSet<Type>();
            foreach (var writeResult in results.WriteResults)
            {
                foreach (var type in writeResult.Value.includedTypes)
                {
                    assemblies.Add(type.Assembly);
                    types.Add(type);
                }
            }

            var xml = LinkXMLGeneratorTests.ReadLinkXML(buildParameters.GetOutputFilePathForIdentifier("link.xml"), out int assemblyCount, out int typeCount);
            Assert.AreEqual(assemblyCount, assemblies.Count);
            Assert.AreEqual(typeCount, types.Count);
            foreach (var t in types)
                LinkXMLGeneratorTests.AssertTypePreserved(xml, t);
        }

        [UnityTest]
        public IEnumerator ValidationMethods_HasDirtyScenes()
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            yield return null;

            bool dirty = ValidationMethods.HasDirtyScenes();
            Assert.IsFalse(dirty);

            EditorSceneManager.MarkSceneDirty(s);

            dirty = ValidationMethods.HasDirtyScenes();
            Assert.IsTrue(dirty);
        }

        [Test]
        public void DefaultBuildTasks_WriteSerializedFiles()
        {
            IBuildParameters buildParams = GetBuildParameters();
            IDependencyData dependencyData = new BuildDependencyData();
            IWriteData writeData = new BuildWriteData();
            IBuildResults results = new BuildResults();

            ReturnCode exitCode = RunTask<WriteSerializedFiles>(buildParams, dependencyData, writeData, results);
            Assert.AreEqual(ReturnCode.Success, exitCode);
        }

        [Test]
        public void DefaultBuildTasks_GenerateBundlePacking()
        {
            IBundleBuildContent buildContent = GetBundleContent();
            IDependencyData dep = GetDependencyData();
            IBundleWriteData writeData = new BundleWriteData();
            IDeterministicIdentifiers deterministicId = new PrefabPackedIdentifiers();

            ReturnCode exitCode = RunTask<GenerateBundlePacking>(buildContent, dep, writeData, deterministicId);
            Assert.AreEqual(ReturnCode.Success, exitCode);
        }

        [Test]
        public void DefaultBuildTasks_GenerateBundleCommands()
        {
            IBundleBuildContent buildContent = GetBundleContent();
            IDependencyData dep = GetDependencyData();
            IBundleWriteData writeData = new BundleWriteData();
            IDeterministicIdentifiers deterministicId = new PrefabPackedIdentifiers();

            RunTask<GenerateBundlePacking>(buildContent, dep, writeData, deterministicId);

            IBundleBuildParameters buildParams = GetBuildParameters();

            ReturnCode exitCode = RunTask<GenerateBundleCommands>(buildParams, buildContent, dep, writeData, deterministicId);
            Assert.AreEqual(ReturnCode.Success, exitCode);
        }

        [Test]
        public void DefaultBuildTasks_GenerateBundleMaps()
        {
            IDependencyData dep = GetDependencyData();
            IBundleWriteData writeData = new BundleWriteData();

            ReturnCode exitCode = RunTask<GenerateBundleMaps>(dep, writeData);
            Assert.AreEqual(ReturnCode.Success, exitCode);
        }

        [Test]
        public void DefaultBuildTasks_PostPackingCallback()
        {
            bool packingCallbackCalled = false;

            IBuildParameters buildParams = GetBuildParameters();
            IDependencyData dep = GetDependencyData();
            IBundleWriteData writeData = new BundleWriteData();
            BuildCallbacks callback = new BuildCallbacks();
            callback.PostPackingCallback = (parameters, data, arg3) =>
            {
                packingCallbackCalled = true;
                return ReturnCode.Success;
            };

            ReturnCode exitCode = RunTask<PostPackingCallback>(buildParams, dep, writeData, callback);
            Assert.AreEqual(ReturnCode.Success, exitCode);
            Assert.IsTrue(packingCallbackCalled);
        }

        [Test]
        public void DefaultBuildTasks_PostWritingCallback()
        {
            bool writingCallbackCalled = false;

            IBuildParameters buildParams = GetBuildParameters();
            IDependencyData dep = GetDependencyData();
            IWriteData writeData = new BuildWriteData();
            IBuildResults results = new BuildResults();
            BuildCallbacks callback = new BuildCallbacks();
            callback.PostWritingCallback = (parameters, data, arg3, arg4) =>
            {
                writingCallbackCalled = true;
                return ReturnCode.Success;
            };

            ReturnCode exitCode = RunTask<PostWritingCallback>(buildParams, dep, writeData, results, callback);
            Assert.AreEqual(ReturnCode.Success, exitCode);
            Assert.IsTrue(writingCallbackCalled);
        }

        [Test]
        public void DefaultBuildTasks_PostDependencyCallback()
        {
            bool dependencyCallbackCalled = false;

            IBuildParameters buildParameters = GetBuildParameters();
            IDependencyData dep = GetDependencyData();
            BuildCallbacks callback = new BuildCallbacks();
            callback.PostDependencyCallback = (parameters, data) =>
            {
                dependencyCallbackCalled = true;
                return ReturnCode.Success;
            };

            ReturnCode exitCode = RunTask<PostDependencyCallback>(buildParameters, dep, callback);
            Assert.AreEqual(ReturnCode.Success, exitCode);
            Assert.IsTrue(dependencyCallbackCalled);
        }

        [Test]
        public void DefaultBuildTasks_PostScriptsCallbacks()
        {
            bool scriptsCallbackCalled = false;

            IBuildParameters buildParameters = GetBuildParameters();
            IBuildResults results = new BuildResults();
            BuildCallbacks callback = new BuildCallbacks();
            callback.PostScriptsCallbacks = (parameters, buildResults) =>
            {
                scriptsCallbackCalled = true;
                return ReturnCode.Success;
            };

            ReturnCode exitCode = RunTask<PostScriptsCallback>(buildParameters, results, callback);
            Assert.AreEqual(ReturnCode.Success, exitCode);
            Assert.IsTrue(scriptsCallbackCalled);
        }

        [Test]
        public void DefaultBuildTasks_AppendBundleHash()
        {
            IBundleBuildParameters buildParameters = GetBuildParameters();
            buildParameters.AppendHash = true;
            var fileName = k_FolderPath + "/TestBundle";
            var fileHash = HashingMethods.Calculate(fileName).ToHash128();
            File.WriteAllText(fileName, fileName);
            IBundleBuildResults results = new BundleBuildResults();
            results.BundleInfos["TestBundle"] = new BundleDetails
            {
                Crc = 0,
                FileName = fileName,
                Hash = fileHash
            };

            ReturnCode exitCode = RunTask<AppendBundleHash>(buildParameters, results);
            Assert.AreEqual(ReturnCode.Success, exitCode);
            FileAssert.Exists(fileName + "_" + fileHash);
        }

        [UnityTest]
        public IEnumerator SceneDataWriteOperation_HashChanges_WhenPrefabDepenencyChanges()
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            yield return null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_CubePath);
            prefab.transform.position = new Vector3(0, 0, 0);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            PrefabUtility.InstantiatePrefab(prefab);

            EditorSceneManager.SaveScene(s, k_ScenePath);

            var op = new SceneDataWriteOperation
            {
                Command = new WriteCommand(),
                PreloadInfo = new PreloadInfo(),
#if !UNITY_2019_3_OR_NEWER
                ProcessedScene = k_ScenePath,
#endif
                ReferenceMap = new BuildReferenceMap(),
                UsageSet = new BuildUsageTagSet(),
                Scene = k_ScenePath,
                DependencyHash = AssetDatabase.GetAssetDependencyHash(k_CubePath)
            };
            var cacheVersion1 = op.GetHash128();

            prefab.transform.position = new Vector3(1, 1, 1);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            op.DependencyHash = AssetDatabase.GetAssetDependencyHash(k_CubePath);
            var cacheVersion2 = op.GetHash128();

            Assert.AreNotEqual(cacheVersion1, cacheVersion2);
        }

        [UnityTest]
        public IEnumerator SceneBundleWriteOperation_HashChanges_WhenPrefabDepenencyChanges()
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            yield return null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_CubePath);
            prefab.transform.position = new Vector3(0, 0, 0);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            PrefabUtility.InstantiatePrefab(prefab);

            EditorSceneManager.SaveScene(s, k_ScenePath);

            var op = new SceneBundleWriteOperation
            {
                Command = new WriteCommand(),
                PreloadInfo = new PreloadInfo(),
#if !UNITY_2019_3_OR_NEWER
                ProcessedScene = k_ScenePath,
#endif
                ReferenceMap = new BuildReferenceMap(),
                UsageSet = new BuildUsageTagSet(),
                Info = new SceneBundleInfo(),
                Scene = k_ScenePath,
                DependencyHash = AssetDatabase.GetAssetDependencyHash(k_CubePath)
            };
            var cacheVersion1 = op.GetHash128();

            prefab.transform.position = new Vector3(1, 1, 1);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            op.DependencyHash = AssetDatabase.GetAssetDependencyHash(k_CubePath);
            var cacheVersion2 = op.GetHash128();

            Assert.AreNotEqual(cacheVersion1, cacheVersion2);
        }

        [Test]
        public void BuildAssetBundles_WhenNoBuildInContextLog_CreatesPerformanceLogReport()
        {
            IBundleBuildParameters buildParameters = GetBuildParameters();
            IBundleBuildContent buildContent = GetBundleContent();
            IBundleBuildResults results;

            buildParameters.Group = EditorUserBuildSettings.selectedBuildTargetGroup;

            ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out results);

            string tepBuildLog = buildParameters.GetOutputFilePathForIdentifier("buildlogtep.json");
            FileAssert.Exists(tepBuildLog);
        }

        [Test]
        public void BuildAssetBundles_WhenBuildLogProvided_DoesNotCreatePerformanceLogReport()
        {
            IBundleBuildParameters buildParameters = GetBuildParameters();
            IBundleBuildContent buildContent = GetBundleContent();
            IBundleBuildResults results;

            var taskList = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible);
            ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out results, taskList, new BuildLog());

            string tepBuildLog = buildParameters.GetOutputFilePathForIdentifier("buildlogtep.json");
            FileAssert.DoesNotExist(tepBuildLog);
        }

        [Test]
        public void BuildParameters_SetsBuildCacheServerParameters_WhenUseBuildCacheServerEnabled()
        {
            int port = 9999;
            string host = "fake host";

            using (new StoreCacheServerConfig(true, host, port))
            {
                IBundleBuildParameters buildParameters = GetBuildParameters();

                Assert.AreEqual(port, buildParameters.CacheServerPort);
                Assert.AreEqual(host, buildParameters.CacheServerHost);
            }
        }

        [Test]
        public void BuildParameters_DoesNotSetBuildCacheServerParameters_WhenUseBuildCacheServerDisabled()
        {
            int port = 9999;
            string host = "testHost";

            using (new StoreCacheServerConfig(false, host, port))
            {
                IBundleBuildParameters buildParameters = GetBuildParameters();

                Assert.AreEqual(8126, buildParameters.CacheServerPort);
                Assert.AreEqual(null, buildParameters.CacheServerHost);
            }
        }

        [Test]
        public void BuildAssetBundles_WithCache_Succeeds()
        {
            IBundleBuildParameters buildParameters = GetBuildParameters();
            IBundleBuildContent buildContent = GetBundleContent(true);

            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out IBundleBuildResults results);
            Assert.AreEqual(ReturnCode.Success, exitCode);
        }

        [Test]
        public void BuildAssetBundles_WithoutCache_Succeeds()
        {
            IBundleBuildParameters buildParameters = GetBuildParameters();
            buildParameters.UseCache = false;
            IBundleBuildContent buildContent = GetBundleContent(true);

            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out IBundleBuildResults results);
            Assert.AreEqual(ReturnCode.Success, exitCode);
        }

        ScriptableBuildPipeline.Settings LoadSettingsFromFile()
        {
            var settings = new ScriptableBuildPipeline.Settings();
            if (File.Exists(ScriptableBuildPipeline.kSettingPath))
            {
                var json = File.ReadAllText(ScriptableBuildPipeline.kSettingPath);
                EditorJsonUtility.FromJsonOverwrite(json, settings);
            }
            return settings;
        }

        void AssertSettingsChanged(ScriptableBuildPipeline.Settings preSettings, ScriptableBuildPipeline.Settings postSettings, FieldInfo[] fields, int fieldChanged)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (i == fieldChanged)
                    Assert.AreNotEqual(fields[i].GetValue(preSettings), fields[i].GetValue(postSettings), $"Unexpected field '{fields[i].Name}' value is unchanged.");
                else
                    Assert.AreEqual(fields[i].GetValue(preSettings), fields[i].GetValue(postSettings), $"Unexpected field '{fields[i].Name}' value is changed.");
            }
        }

        [Test]
        public void PreferencesProperties_ChangesSerializeToDisk()
        {
            PropertyInfo[] properties = typeof(ScriptableBuildPipeline).GetProperties(BindingFlags.Public | BindingFlags.Static);
            FieldInfo[] fields = typeof(ScriptableBuildPipeline.Settings).GetFields(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < properties.Length; i++)
            {
                var preSettings = LoadSettingsFromFile();

                if (properties[i].PropertyType == typeof(bool))
                {
                    bool previousValue = (bool)fields[i].GetValue(preSettings);
                    properties[i].SetValue(null, !previousValue);
                }
                else if (properties[i].PropertyType == typeof(int))
                {
                    int previousValue = (int)fields[i].GetValue(preSettings);
                    properties[i].SetValue(null, previousValue + 5);
                }
                else if (properties[i].PropertyType == typeof(string))
                {
                    string previousValue = (string)fields[i].GetValue(preSettings);
                    properties[i].SetValue(null, previousValue + "Test");
                }
                else
                {
                    Assert.Fail($"Unhandled property type '{properties[i].PropertyType.Name}' in test '{nameof(PreferencesProperties_ChangesSerializeToDisk)}'");
                }

                AssertSettingsChanged(preSettings, LoadSettingsFromFile(), fields, i);
            }
        }

        internal class StoreCacheServerConfig : IDisposable
        {
            private bool m_StoredUseServerFlag;
            private string m_StoredServerHost;
            private int m_UseServerPort;

            public StoreCacheServerConfig(bool useCacheServer, string host, int port)
            {
                m_StoredUseServerFlag = ScriptableBuildPipeline.UseBuildCacheServer;
                ScriptableBuildPipeline.UseBuildCacheServer = useCacheServer;

                m_StoredServerHost = ScriptableBuildPipeline.CacheServerHost;
                ScriptableBuildPipeline.CacheServerHost = host;

                m_UseServerPort = ScriptableBuildPipeline.CacheServerPort;
                ScriptableBuildPipeline.CacheServerPort = port;
            }

            public void Dispose()
            {
                ScriptableBuildPipeline.UseBuildCacheServer = m_StoredUseServerFlag;
                ScriptableBuildPipeline.CacheServerHost = m_StoredServerHost;
                ScriptableBuildPipeline.CacheServerPort = m_UseServerPort;
            }
        }
    }
}
