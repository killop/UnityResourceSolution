using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Player;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.U2D;

namespace UnityEditor.Build.Pipeline.Tests
{
    public class CalculateAssetDependencyTests
    {
        const string kTestAssetFolder = "Assets/TestAssets";
        const string kTestAsset = "Assets/TestAssets/SpriteTexture32x32.png";
        const string kSpriteTexture1Asset = "Assets/TestAssets/SpriteTexture1_32x32.png";
        const string kSpriteTexture2Asset = "Assets/TestAssets/SpriteTexture2_32x32.png";
        const string kSpriteAtlasAsset = "Assets/TestAssets/sadependencies.spriteAtlas";
        

        SpritePackerMode m_PrevMode;

        class TestParams : TestBundleBuildParameters
        {
            bool m_DisableVisibleSubAssetRepresentations = false;
            public TestParams(bool disableVisibleSubAssetRepresentations)
            {
                m_DisableVisibleSubAssetRepresentations = disableVisibleSubAssetRepresentations;
            }

            public override bool DisableVisibleSubAssetRepresentations { get => m_DisableVisibleSubAssetRepresentations; }

            // Inputs
            public override bool UseCache { get; set; }
            public override BuildTarget Target { get => EditorUserBuildSettings.activeBuildTarget; }
            public override BuildTargetGroup Group { get => BuildTargetGroup.Unknown; }
            public override TypeDB ScriptInfo { get => null; }
            public override ContentBuildFlags ContentBuildFlags { get => ContentBuildFlags.None; }
            public override bool NonRecursiveDependencies { get => false; }

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
            public List<GUID> TestAssets = new List<GUID>();

            // Inputs
            public override List<GUID> Assets => TestAssets;
        }

        class TestDependencyData : TestDependencyDataBase
        {
            public Dictionary<GUID, AssetLoadInfo> TestAssetInfo = new Dictionary<GUID, AssetLoadInfo>();
            public Dictionary<GUID, BuildUsageTagSet> TestAssetUsage = new Dictionary<GUID, BuildUsageTagSet>();
            public Dictionary<GUID, SceneDependencyInfo> TestSceneInfo = new Dictionary<GUID, SceneDependencyInfo>();
            public Dictionary<GUID, BuildUsageTagSet> TestSceneUsage = new Dictionary<GUID, BuildUsageTagSet>();
            public Dictionary<GUID, Hash128> TestDependencyHash = new Dictionary<GUID, Hash128>();

            // Inputs
            public override BuildUsageCache DependencyUsageCache => null;
            public override BuildUsageTagGlobal GlobalUsage => new BuildUsageTagGlobal();

            // Outputs
            public override Dictionary<GUID, AssetLoadInfo> AssetInfo => TestAssetInfo;
            public override Dictionary<GUID, BuildUsageTagSet> AssetUsage => TestAssetUsage;
            public override Dictionary<GUID, SceneDependencyInfo> SceneInfo => TestSceneInfo;
            public override Dictionary<GUID, BuildUsageTagSet> SceneUsage => TestSceneUsage;
            public override Dictionary<GUID, Hash128> DependencyHash => TestDependencyHash;
        }

        class TestExtendedAssetData : TestBundleExtendedAssetData
        {
            public Dictionary<GUID, ExtendedAssetData> TestExtendedData = new Dictionary<GUID, ExtendedAssetData>();
            public override Dictionary<GUID, ExtendedAssetData> ExtendedData => TestExtendedData;
        }

        static CalculateAssetDependencyData CreateDefaultBuildTask(List<GUID> assets, bool disableVisibleSubassetRepresentations = false)
        {
            var task = new CalculateAssetDependencyData();
            var testParams = new TestParams(disableVisibleSubassetRepresentations);
            var testContent = new TestContent { TestAssets = assets };
            var testDepData = new TestDependencyData();
            var testExtendedData = new TestExtendedAssetData();

            IBuildContext context = new BuildContext(testParams, testContent, testDepData, testExtendedData);
            ContextInjector.Inject(context, task);
            return task;
        }

        static void ExtractTestData(IBuildTask task, out TestExtendedAssetData extendedAssetData)
        {
            IBuildContext context = new BuildContext();
            ContextInjector.Extract(context, task);
            extendedAssetData = (TestExtendedAssetData)context.GetContextObject<IBuildExtendedAssetData>();
        }

        [SetUp]
        public void Setup()
        {
            m_PrevMode = EditorSettings.spritePackerMode;
            Directory.CreateDirectory(kTestAssetFolder);
            CreateTestSpriteTexture(kTestAsset, false);
            CreateTestSpriteTexture(kSpriteTexture1Asset, true);
            CreateTestSpriteTexture(kSpriteTexture2Asset, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            CreateSpriteAtlas();

            
        }

        [TearDown]
        public void OneTimeTeardown()
        {
            AssetDatabase.DeleteAsset(kTestAssetFolder);
            File.Delete(kTestAssetFolder + ".meta");
            EditorSettings.spritePackerMode = m_PrevMode;
            AssetDatabase.Refresh();
        }

        static void CreateTestSpriteTexture(string texturePath, bool single)
        {
            var data = ImageConversion.EncodeToPNG(new Texture2D(32, 32));
            File.WriteAllBytes(texturePath, data);
            AssetDatabase.ImportAsset(texturePath);//, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            if (single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }
            else
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritesheet = new[]
                {
                    new SpriteMetaData
                    {
                        name = "WhiteTexture32x32_0",
                        rect = new Rect(0, 19, 32, 13),
                        alignment = 0,
                        pivot = new Vector2(0.5f, 0.5f),
                        border = new Vector4(0, 0, 0, 0)
                    },
                    new SpriteMetaData
                    {
                        name = "WhiteTexture32x32_1",
                        rect = new Rect(4, 19, 24, 11),
                        alignment = 0,
                        pivot = new Vector2(0.5f, 0.5f),
                        border = new Vector4(0, 0, 0, 0)
                    },
                    new SpriteMetaData
                    {
                        name = "WhiteTexture32x32_2",
                        rect = new Rect(9, 5, 12, 7),
                        alignment = 0,
                        pivot = new Vector2(0.5f, 0.5f),
                        border = new Vector4(0, 0, 0, 0)
                    }
                };
            }
            
            importer.SaveAndReimport();
        }

        static void CreateSpriteAtlas()
        {
            var sa = new SpriteAtlas();
            var targetObjects = new UnityEngine.Object[] { AssetDatabase.LoadAssetAtPath<Texture>(kSpriteTexture1Asset), AssetDatabase.LoadAssetAtPath<Texture>(kSpriteTexture2Asset) };
            sa.Add(targetObjects);
            AssetDatabase.CreateAsset(sa, kSpriteAtlasAsset);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        CalculateAssetDependencyData.TaskInput CreateDefaultInput()
        {
            CalculateAssetDependencyData.TaskInput input = new CalculateAssetDependencyData.TaskInput();
            input.Target = EditorUserBuildSettings.activeBuildTarget;
            return input;
        }

        // Create a prefab and writes it to the specified path. The target file will have 2 objects in it: the GameObject and the Transform
        GUID CreateGameObject(string assetPath, string name = "go")
        {
            GameObject go = new GameObject(name);
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            UnityEngine.Object.DestroyImmediate(go, false);
            string guidString = AssetDatabase.AssetPathToGUID(assetPath);
            GUID.TryParse(guidString, out GUID guid);
            return guid;
        }

        [Test]
        public void WhenAssetHasNoDependencies()
        {
            // Create an asset
            string assetPath = Path.Combine(kTestAssetFolder, "myPrefab.prefab");
            GUID guid = CreateGameObject(assetPath);

            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            input.Assets = new List<GUID>() { guid };

            CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);

            Assert.AreEqual(1, output.AssetResults.Length);
            Assert.AreEqual(guid, output.AssetResults[0].asset);
            Assert.AreEqual(2, output.AssetResults[0].assetInfo.includedObjects.Count); // GameObject and Transform
            Assert.AreEqual(0, output.AssetResults[0].assetInfo.referencedObjects.Count);
            Assert.IsNull(output.AssetResults[0].spriteData);
            Assert.IsNull(output.AssetResults[0].extendedData);
        }

        [Test]
        public void WhenAssetDoesNotExist_AssetResultIsEmpty()
        {
            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            input.Assets = new List<GUID>() { GUID.Generate() };

            CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);

            Assert.IsNull(output.AssetResults[0].extendedData);
            Assert.AreEqual(0, output.AssetResults[0].assetInfo.includedObjects.Count);
        }

        [Test]
        public void WhenSomeAssetDataIsCached_CachedVersionIsUsed()
        {
            const int kCachedCount = 5;
            // Create 10 assets, import half of them,
            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            string assetPath = Path.Combine(kTestAssetFolder, "myPrefab.prefab");
            List<GUID> allGUIDs = new List<GUID>();
            List<GUID> cachedGUIDs = new List<GUID>();
            for (int i = 0; i < kCachedCount; i++)
            {
                GUID cachedGUID = CreateGameObject(Path.Combine(kTestAssetFolder, $"myPrefab{i * 2}.prefab"), $"go{i * 2}");
                cachedGUIDs.Add(cachedGUID);
                allGUIDs.Add(cachedGUID);
                allGUIDs.Add(CreateGameObject(Path.Combine(kTestAssetFolder, $"myPrefab{i * 2 + 1}.prefab"), $"go{i * 2 + 1}"));
            }

            using (BuildCache cache = new BuildCache())
            {
                input.BuildCache = cache;
                input.Assets = cachedGUIDs;
                CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);
                cache.SyncPendingSaves();
                input.Assets = allGUIDs;
                CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output2);

                Assert.AreEqual(output.AssetResults[0].assetInfo.includedObjects.Count, output2.AssetResults[0].assetInfo.includedObjects.Count); // GameObject and Transform
                Assert.AreEqual(0, output.CachedAssetCount);
                Assert.AreEqual(kCachedCount, output2.CachedAssetCount);

                for (int i = 0; i < kCachedCount; i++)
                {
                    bool seqEqual = Enumerable.SequenceEqual(output.AssetResults[i].assetInfo.includedObjects, output2.AssetResults[i * 2].assetInfo.includedObjects);
                    Assert.IsTrue(seqEqual);
                }
            }
        }

        // Embedding this shader in code and only creating it when the test actually runs so it doesn't exist outside tests.
        string kTestShader = @"Shader ""Custom / NewSurfaceShader""
{
                Properties
    {
                    _Color(""Color"", Color) = (1, 1, 1, 1)
        _MainTex(""Albedo (RGB)"", 2D) = ""white"" {
                    }
                    _Glossiness(""Smoothness"", Range(0, 1)) = 0.5
        _Metallic(""Metallic"", Range(0, 1)) = 0.0
    }
                SubShader
    {
                    Tags { ""RenderType"" = ""Opaque"" }
                    LOD 200

        CGPROGRAM
#pragma surface surf Standard fullforwardshadows
#pragma target 3.0

        #pragma shader_feature _ TEST_DEFINE

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack ""Diffuse""
}
";


        private void CreateTestShader(string path)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, kTestShader);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void WhenObjectInfluencesReferencedObjectBuildTags_BuildUsageTagsAreAdded()
        {
            string testShaderPath = Path.Combine(kTestAssetFolder, "TestShader.shader");
            CreateTestShader(testShaderPath);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(testShaderPath);
            string shaderGUIDString = AssetDatabase.AssetPathToGUID(testShaderPath);
            GUID.TryParse(shaderGUIDString, out GUID shaderGUID);

            // Create a material that points to the test shader asset
            Material mat = new Material(shader);
            string matPath = Path.Combine(kTestAssetFolder, "testmat.mat");
            AssetDatabase.CreateAsset(mat, matPath);
            string guidMatString = AssetDatabase.AssetPathToGUID(matPath);
            GUID.TryParse(guidMatString, out GUID matGUID);

            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            input.Assets = new List<GUID>() { matGUID };
            CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);

            // this define should get added to the shader build usage tags
            mat.shaderKeywords = new string[] { "TEST_DEFINE" };

            CalculateAssetDependencyData.TaskInput input2 = CreateDefaultInput();
            input2.Assets = new List<GUID>() { matGUID };
            CalculateAssetDependencyData.RunInternal(input2, out CalculateAssetDependencyData.TaskOutput output2);

            var ids = output2.AssetResults[0].usageTags.GetObjectIdentifiers();
            Assert.IsTrue(ids.Count((x) => x.guid == shaderGUID) == 1, "Shader is not in build usage tags");
            Assert.AreNotEqual(output.AssetResults[0].usageTags.GetHashCode(), output2.AssetResults[0].usageTags.GetHashCode(), "Build usage tags were not affected by material keywords");
        }

        static object[] SpriteTestCases =
        {
#if UNITY_2020_1_OR_NEWER
            new object[] { SpritePackerMode.Disabled, "", false, false },
            new object[] { SpritePackerMode.BuildTimeOnlyAtlas, "", true, true },
            new object[] { SpritePackerMode.BuildTimeOnlyAtlas, "", false, false },
#else
            new object[] { SpritePackerMode.BuildTimeOnly, "SomeTag", true, true },
            new object[] { SpritePackerMode.BuildTimeOnly, "", true, false },
            new object[] { SpritePackerMode.AlwaysOn, "SomeTag", true, true },
            new object[] { SpritePackerMode.AlwaysOn, "", true, false },
            new object[] { SpritePackerMode.Disabled, "", true, false },
            new object[] { SpritePackerMode.BuildTimeOnlyAtlas, "", true, true },
            new object[] { SpritePackerMode.AlwaysOnAtlas, "", true, true },
            new object[] { SpritePackerMode.AlwaysOnAtlas, "", false, false }
#endif
        };


        [TestCaseSource("SpriteTestCases")]
        [Test]
        public void WhenSpriteWithAtlas_SpriteImportDataCreated(SpritePackerMode spriteMode, string spritePackingTag, bool hasReferencingSpriteAtlas, bool expectedPacked)
        {
            TextureImporter importer = AssetImporter.GetAtPath(kTestAsset) as TextureImporter;
            importer.spritePackingTag = spritePackingTag;
            importer.SaveAndReimport();

            if (hasReferencingSpriteAtlas)
            {
                var sa = new SpriteAtlas();
                var targetObjects = new UnityEngine.Object[] { AssetDatabase.LoadAssetAtPath<Texture>(kTestAsset) };
                sa.Add(targetObjects);
                string saPath = Path.Combine(kTestAssetFolder, "sa.spriteAtlas");
                AssetDatabase.CreateAsset(sa, saPath);
                AssetDatabase.Refresh();
            }

            GUID.TryParse(AssetDatabase.AssetPathToGUID(kTestAsset), out GUID spriteGUID);

            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            EditorSettings.spritePackerMode = spriteMode;
            SpriteAtlasUtility.PackAllAtlases(input.Target);
            input.Assets = new List<GUID>() { spriteGUID };
            CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);

            Assert.AreEqual(expectedPacked, output.AssetResults[0].spriteData.PackedSprite);
        }


        static object[] SpriteUtilityTestCases =
        {
            new object[] { SpritePackerMode.BuildTimeOnlyAtlas }
        };


        [TestCaseSource("SpriteUtilityTestCases")]
        [Test]
        public void WhenSpriteWithAtlasUpdated_SpriteInfoUpdated(SpritePackerMode spriteMode)
        {
            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(kSpriteAtlasAsset);
            var oldSprites = SpriteAtlasExtensions.GetPackables(spriteAtlas);
            spriteAtlas.Remove(oldSprites);

            List<UnityEngine.Object> sprites = new List<UnityEngine.Object>();
            sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(kSpriteTexture1Asset));
            sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(kSpriteTexture2Asset));
            SpriteAtlasExtensions.Add(spriteAtlas, sprites.ToArray());

            GUID.TryParse(AssetDatabase.AssetPathToGUID(kSpriteAtlasAsset), out GUID spriteGUID);

            BuildCacheUtility.ClearCacheHashes();
            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            input.BuildCache = new BuildCache();
            input.NonRecursiveDependencies = true;
            EditorSettings.spritePackerMode = spriteMode;
            SpriteAtlasUtility.PackAllAtlases(input.Target);
            input.Assets = new List<GUID>() { spriteGUID };
            CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);

            Assert.AreEqual(3, output.AssetResults[0].assetInfo.referencedObjects.Count);
            
            spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(kSpriteAtlasAsset);
            oldSprites = SpriteAtlasExtensions.GetPackables(spriteAtlas);
            spriteAtlas.Remove(oldSprites);

            sprites = new List<UnityEngine.Object>();
            sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(kSpriteTexture1Asset));
            SpriteAtlasExtensions.Add(spriteAtlas, sprites.ToArray());
            SpriteAtlasUtility.PackAllAtlases(input.Target);
            CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output2);
            Assert.AreEqual(2, output2.AssetResults[0].assetInfo.referencedObjects.Count);
        }

#if !UNITY_2020_2_OR_NEWER
        // This test is only important for going through AssetDatabase's LoadAllAssetRepresentationsAtPath
        // in 2020.2 and newer we have a new build api that handles nulls natively and this no longer applies.
        class NullLoadRepresentationFake : CalculateAssetDependencyHooks
        {
            public override UnityEngine.Object[] LoadAllAssetRepresentationsAtPath(string assetPath) { return new UnityEngine.Object[] { null }; }
        }

        [Test]
        public void WhenAssetHasANullRepresentation_LogsWarning()
        {
            // Create an asset
            string assetPath = Path.Combine(kTestAssetFolder, "myPrefab.prefab");
            GUID guid = CreateGameObject(assetPath);

            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            input.Assets = new List<GUID>() { guid };
            input.EngineHooks = new NullLoadRepresentationFake();

            LogAssert.Expect(LogType.Warning, new Regex(".+It will not be included in the build"));

            CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);

            Assert.AreEqual(guid, output.AssetResults[0].asset);
            Assert.AreEqual(2, output.AssetResults[0].assetInfo.includedObjects.Count); // GameObject and Transform
            Assert.AreEqual(0, output.AssetResults[0].assetInfo.referencedObjects.Count);
        }

#endif

        [Test]
        public void WhenAssetHasMultipleRepresentations_ExtendedDataContainsAllButMainAsset()
        {
            const int kExtraRepresentations = 2;
            string assetPath = Path.Combine(kTestAssetFolder, "myPrefab.asset");
            Material mat = new Material(Shader.Find("Transparent/Diffuse"));
            AssetDatabase.CreateAsset(mat, assetPath);

            for (int i = 0; i < kExtraRepresentations; i++)
                AssetDatabase.AddObjectToAsset(new Material(Shader.Find("Transparent/Diffuse")), assetPath);

            AssetDatabase.SaveAssets();

            GUID guid = new GUID(AssetDatabase.AssetPathToGUID(assetPath));
            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            input.Assets = new List<GUID>() { guid };

            CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);

            ObjectIdentifier[] allObjIDs = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(guid, EditorUserBuildSettings.activeBuildTarget);
            HashSet<ObjectIdentifier> expectedReps = new HashSet<ObjectIdentifier>();
            for (int i = 1; i < allObjIDs.Length; i++)
                expectedReps.Add(allObjIDs[i]);

            Assert.AreEqual(kExtraRepresentations, output.AssetResults[0].extendedData.Representations.Count);
            Assert.AreEqual(kExtraRepresentations, expectedReps.Count);
            foreach (var id in output.AssetResults[0].extendedData.Representations)
                Assert.IsTrue(expectedReps.Contains(id));
        }

        class TestProgressTracker : IProgressTracker
        {
            int count = 0;
            public int TaskCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public float Progress => throw new NotImplementedException();

            public bool UpdateInfo(string taskInfo)
            {
                return count++ > 0;
            }

            public bool UpdateTask(string taskTitle) { throw new NotImplementedException(); }
        }

        [Test]
        public void WhenCanceledThroughProgressTracker_ReturnsCanceled()
        {
            string assetPath1 = Path.Combine(kTestAssetFolder, "myPrefab1.prefab");
            string assetPath2 = Path.Combine(kTestAssetFolder, "myPrefab2.prefab");

            CalculateAssetDependencyData.TaskInput input = CreateDefaultInput();
            input.Assets = new List<GUID>() { CreateGameObject(assetPath1), CreateGameObject(assetPath2) };
            input.ProgressTracker = new TestProgressTracker();
            ReturnCode code = CalculateAssetDependencyData.RunInternal(input, out CalculateAssetDependencyData.TaskOutput output);
            Assert.AreEqual(null, output.AssetResults[1].assetInfo);
            Assert.AreEqual(ReturnCode.Canceled, code);
        }

        [Test]
        public void TaskIsRun_WhenAssetHasNoMultipleRepresentations_ExtendedDataIsEmpty()
        {
            string assetPath = Path.Combine(kTestAssetFolder, "myPrefab.prefab");
            GUID guid = CreateGameObject(assetPath);

            bool disableVisibleSubAssetRepresentations = false;
            CalculateAssetDependencyData buildTask = CreateDefaultBuildTask(new List<GUID>() { guid }, disableVisibleSubAssetRepresentations);
            buildTask.Run();
            ExtractTestData(buildTask, out TestExtendedAssetData extendedAssetData);

            Assert.AreEqual(0, extendedAssetData.ExtendedData.Count);
        }

        [Test]
        public void TaskIsRun_WhenAssetHasMultipleRepresentations_ExtendedDataContainsEntryForAsset()
        {
            string assetPath = Path.Combine(kTestAssetFolder, "myPrefab.asset");
            Material mat = new Material(Shader.Find("Transparent/Diffuse"));
            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.AddObjectToAsset(new Material(Shader.Find("Transparent/Diffuse")), assetPath);
            AssetDatabase.SaveAssets();

            string guidString = AssetDatabase.AssetPathToGUID(assetPath);
            GUID.TryParse(guidString, out GUID guid);

            bool disableVisibleSubAssetRepresentations = false;
            CalculateAssetDependencyData buildTask = CreateDefaultBuildTask(new List<GUID>() { guid }, disableVisibleSubAssetRepresentations);
            buildTask.Run();
            ExtractTestData(buildTask, out TestExtendedAssetData extendedAssetData);

            Assert.AreEqual(1, extendedAssetData.ExtendedData.Count);
            Assert.IsTrue(extendedAssetData.ExtendedData.ContainsKey(guid));
        }

        [Test]
        public void TaskIsRun_WhenAssetHasMultipleRepresentations_AndDisableVisibleSubAssetRepresentations_ExtendedDataIsEmpty()
        {
            string assetPath = Path.Combine(kTestAssetFolder, "myPrefab.asset");
            Material mat = new Material(Shader.Find("Transparent/Diffuse"));
            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.AddObjectToAsset(new Material(Shader.Find("Transparent/Diffuse")), assetPath);
            AssetDatabase.SaveAssets();

            string guidString = AssetDatabase.AssetPathToGUID(assetPath);
            GUID.TryParse(guidString, out GUID guid);

            bool disableVisibleSubAssetRepresentations = true;
            CalculateAssetDependencyData buildTask = CreateDefaultBuildTask(new List<GUID>() { guid }, disableVisibleSubAssetRepresentations);
            buildTask.Run();
            ExtractTestData(buildTask, out TestExtendedAssetData extendedAssetData);

            Assert.AreEqual(0, extendedAssetData.ExtendedData.Count);
        }
    }
}
