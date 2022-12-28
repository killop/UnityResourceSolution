#if UNITY_2022_2_OR_NEWER
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Loading;
using Unity.Collections;
using Unity.Content;
using System.IO;
using Unity.IO.Archive;
#if UNITY_EDITOR
using UnityEditor.TestTools;
#endif

namespace UnityEditor.Build.Pipeline.Tests.ContentLoad
{
    abstract public class ContentFileTests : ContentFileFixture
    {
        const string kCatalogTextData = "TextData";

        [UnityTest]
        public IEnumerator LoadFileAsync_CanLoadText()
        { 
            LoadCatalog(kCatalogTextData);
            Catalog.AddressableLocation p1Loc = m_Catalog.GetLocation("Text");
            Catalog.ContentFileInfo fInfo = m_Catalog.GetFileInfo(p1Loc.Filename);
            
            ArchiveHandle aHandle = ArchiveFileInterface.MountAsync(ContentNamespace.Default, GetVFSFilename(fInfo.Filename), "a:");
            aHandle.JobHandle.Complete();
            Assert.True(aHandle.JobHandle.IsCompleted);
            Assert.True(aHandle.Status == ArchiveStatus.Complete);
            try
            {
                var mountPath = aHandle.GetMountPath();
                var vfsPath = Path.Combine(mountPath, fInfo.Filename);
                ContentFile fileHandle = ContentLoadInterface.LoadContentFileAsync(m_NS, vfsPath, new NativeArray<ContentFile>());
        
                while (fileHandle.LoadingStatus == LoadingStatus.InProgress)
                    yield return null;
        
                TextAsset text = (TextAsset)fileHandle.GetObject(p1Loc.LFID);
                Assert.NotZero(text.bytes.Length);

                fileHandle.UnloadAsync().WaitForCompletion(0);
            }
            finally
            {
                aHandle.Unmount();
            }
        }

#if UNITY_EDITOR
        private static System.Int32 randomSeed = 1;

        protected override void PrepareBuildLayout()
        {
            Directory.CreateDirectory("Assets/Temp");
            
            using (var c = CreateCatalog(kCatalogTextData))
            {
                var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Temp/textfile.txt");
                using (var fs =  File.Create(path))
                {
                    var bytes = new byte[64 * 1024];
                    var rand = new System.Random(++randomSeed);
                    rand.NextBytes(bytes);
                    for (System.UInt32 j = 0; j < 10; j++)
                        fs.Write(bytes, 0, bytes.Length);
                }

                AssetDatabase.ImportAsset(path);

                c.Add(new AssetBundleBuild()
                {
                    assetNames = new string[] { path },
                    addressableNames = new string[] { "Text" }
                });
            }
        }
#endif
    }

    [UnityPlatform(exclude = new RuntimePlatform[]
        {RuntimePlatform.LinuxEditor, RuntimePlatform.OSXEditor, RuntimePlatform.WindowsEditor})]
    class ContentFileTests_Standalone : ContentFileTests
    {
    }

#if UNITY_EDITOR
    [UnityPlatform(RuntimePlatform.WindowsEditor)]
    [RequirePlatformSupport(BuildTarget.StandaloneWindows64)]
    class ContentFileTests_WindowsEditor : ContentFileTests
    {
    }

    [UnityPlatform(RuntimePlatform.OSXEditor)]
    [RequirePlatformSupport(BuildTarget.StandaloneOSX)]
    class ContentFileTests_OSXEditor : ContentFileTests
    {
    }

    [UnityPlatform(RuntimePlatform.LinuxEditor)]
    [RequirePlatformSupport(BuildTarget.StandaloneLinux64)]
    class ContentFileTests_LinuxEditor : ContentFileTests
    {
    }
#endif
}
#endif