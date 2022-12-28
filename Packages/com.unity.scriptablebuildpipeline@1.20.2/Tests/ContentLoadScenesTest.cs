#if UNITY_2022_2_OR_NEWER
using System;
using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Content;
using Unity.IO.Archive;
using Unity.Loading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor.TestTools;
#endif

namespace UnityEditor.Build.Pipeline.Tests.ContentLoad
{
    abstract public class SceneTests : ContentFileFixture
    {
        public static IEnumerator UnloadAllScenesExceptInitTestSceneAsync()
        {
#pragma warning disable 0618
            var allScenes = SceneManager.GetAllScenes();
            var allScenesNoInit = allScenes.Where(x => !x.name.Contains("InitTestScene")).ToList();
            if (allScenes.Length == allScenesNoInit.Count)
                SceneManager.CreateScene("InitTestScene");
            foreach (var allScene in allScenesNoInit)
            {
                yield return SceneManager.UnloadSceneAsync(allScene);
            }
#pragma warning restore 0618
        }

        [UnitySetUp]
        public IEnumerator UnloadAllScenesExceptInitTestScene()
        {
            Assert.AreEqual(1, SceneManager.sceneCount);
            yield return null;
            yield return UnloadAllScenesExceptInitTestSceneAsync();
        }

        [TearDown]
        public void Teardown()
        {

        }

        public ContentSceneFile LoadSceneHelper(string path, string sceneName, LoadSceneMode mode, ContentFile[] deps,
            bool integrate = true, bool autoIntegrate = false)
        {
            var sceneParams = new ContentSceneParameters();
            sceneParams.loadSceneMode = mode;
            sceneParams.localPhysicsMode = LocalPhysicsMode.None;
            sceneParams.autoIntegrate = autoIntegrate;

            NativeArray<ContentFile> files =
                new NativeArray<ContentFile>(deps.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < deps.Length; i++)
            {
                files[i] = deps[i];
            }

            ContentSceneFile op = ContentLoadInterface.LoadSceneAsync(m_NS, path, sceneName, sceneParams, files);
            files.Dispose();

            if (integrate)
            {
                op.WaitForLoadCompletion(0);
                if (op.Status == SceneLoadingStatus.WaitingForIntegrate)
                    op.IntegrateAtEndOfFrame();
            }

            return op;
        }

        private void AssertNoDepSceneLoaded(ContentSceneFile sceneFile)
        {
            LoadCatalog("nodepscene");
            Assert.AreEqual(SceneLoadingStatus.Complete, sceneFile.Status);

            Scene scene = sceneFile.Scene;
            GameObject[] objs = scene.GetRootGameObjects();
            GameObject test = objs.First(x => x.name == "testobject");
            Assert.IsTrue(SceneManager.GetSceneByName("testscene").IsValid());

            Assert.AreEqual(sceneFile, ContentLoadInterface.GetSceneFiles(m_NS)[0]);
        }

        [UnityTest]
        public IEnumerator CanLoadSceneWithNoDependencies()
        {
            LoadCatalog("nodepscene");
            Catalog.AddressableLocation p1Loc = m_Catalog.GetLocation("nodepscene");

            ArchiveHandle aHandle = ArchiveFileInterface.MountAsync(ContentNamespace.Default, GetVFSFilename(p1Loc.Filename), "a:");
            aHandle.JobHandle.Complete();
            Assert.True(aHandle.JobHandle.IsCompleted);
            Assert.True(aHandle.Status == ArchiveStatus.Complete);
            try
            {
                var mountPath = aHandle.GetMountPath();
                var vfsPath = Path.Combine(mountPath, p1Loc.Filename);
                var sceneFile = LoadSceneHelper(vfsPath, "testscene", LoadSceneMode.Additive,
                    new ContentFile[] {ContentFile.GlobalTableDependency});
            
                while (sceneFile.Status == SceneLoadingStatus.InProgress)
                    yield return null;

                Assert.AreEqual(SceneLoadingStatus.WillIntegrateNextFrame, sceneFile.Status);
                yield return null;

                AssertNoDepSceneLoaded(sceneFile);
                sceneFile.UnloadAtEndOfFrame();
                yield return null;
            }
            finally
            {
                aHandle.Unmount();
            }
        }

#if UNITY_EDITOR
        protected override void PrepareBuildLayout()
        {
            Directory.CreateDirectory("Assets/Temp");

            // Create a scene with no dependencies
            using (var c = CreateCatalog("nodepscene"))
            {
                Scene scene1 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                RenderSettings.skybox = null;
                SceneManager.SetActiveScene(scene1);
                var go = new GameObject("testobject");
                EditorSceneManager.SaveScene(scene1, "Assets/Temp/nodepscene.unity");
                EditorSceneManager.CloseScene(scene1, true);
                c.Add(
                    new AssetBundleBuild
                    {
                        assetNames = new string[] {"Assets/Temp/nodepscene.unity"},
                        addressableNames = new string[] {"nodepscene"}
                    });
            }
        }
#endif
    }

    [UnityPlatform(exclude = new RuntimePlatform[]
        {RuntimePlatform.LinuxEditor, RuntimePlatform.OSXEditor, RuntimePlatform.WindowsEditor})]
    class SceneTests_Standalone : SceneTests
    {
    }

#if UNITY_EDITOR
    [UnityPlatform(RuntimePlatform.WindowsEditor)]
    [RequirePlatformSupport(BuildTarget.StandaloneWindows64)]
    class SceneTests_WindowsEditor : SceneTests
    {
    }

    [UnityPlatform(RuntimePlatform.OSXEditor)]
    [RequirePlatformSupport(BuildTarget.StandaloneOSX)]
    class SceneTests_OSXEditor : SceneTests
    {
    }

    [UnityPlatform(RuntimePlatform.LinuxEditor)]
    [RequirePlatformSupport(BuildTarget.StandaloneLinux64)]
    class SceneTests_LinuxEditor : SceneTests
    {
    }
#endif

}
#endif