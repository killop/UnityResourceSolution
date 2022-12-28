using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.Build.Pipeline.Tests
{
    public class ArchiveAndCompressTests : ArchiveAndCompressTestFixture
    {
        [Test]
        public void WhenAssetInBundleHasDependencies_DependenciesAreInDetails()
        {
            ArchiveAndCompressBundles.TaskInput input = GetDefaultInput();
            AddSimpleBundle(input, "mybundle", "internalName");
            AddSimpleBundle(input, "mybundle2", "internalName2");
            AddSimpleBundle(input, "mybundle3", "internalName3");

            input.AssetToFilesDependencies.Add(new GUID(), new List<string>() { "internalName", "internalName2" });
            input.AssetToFilesDependencies.Add(GUID.Generate(), new List<string>() { "internalName", "internalName3" });

            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output);

            Assert.AreEqual(2, output.BundleDetails["mybundle"].Dependencies.Length);
            Assert.AreEqual("mybundle2", output.BundleDetails["mybundle"].Dependencies[0]);
            Assert.AreEqual("mybundle3", output.BundleDetails["mybundle"].Dependencies[1]);
        }

        [Test]
        public void WhenBundleDoesNotHaveDependencies_DependenciesAreNotInDetails()
        {
            ArchiveAndCompressBundles.TaskInput input = GetDefaultInput();
            AddSimpleBundle(input, "mybundle", "internalName");
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output);
            Assert.AreEqual(0, output.BundleDetails["mybundle"].Dependencies.Length);
        }

        [TestCase("01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_d", ".")]
        [TestCase("C:/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars", ".")]
        [TestCase(".", "01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_d")]
        [TestCase(".", "C:/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars/01_long_directory_path_for_35chars")]
        [UnityPlatform(exclude = new[] { RuntimePlatform.LinuxEditor, RuntimePlatform.OSXEditor })]
        public void WhenUsingLongPath_CopyFileWithTimestampIfDifferent_ThrowsPathTooLongException(string path1, string path2)
        {
            Assert.Throws<PathTooLongException>(() => ArchiveAndCompressBundles.CopyFileWithTimestampIfDifferent(path1, path2, null));
        }

        [Test]
        // Windows has Unicode path notation for long paths: \\?\
        // however this does not work on all unity editor versions or windows version.
        // notably this fails on Yamato 2019.4 through 2021.1, but passed on trunk (on Nov 4, 2021)
        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor })]
        public void PlatformCanHandle_LongPathReadingAndWriting()
        {
            string root = Path.GetFullPath("FolderDepthTest");

            string fullPath = root;
            while (fullPath.Length < 300)
                fullPath = Path.Combine(fullPath, $"IncreaseFolderDepth_{fullPath.Length}");
            string file1 = Path.Combine(fullPath, "test1.txt");
            string file2 = Path.Combine(fullPath, "test2.txt");

            Assert.DoesNotThrow(() =>
            {
                // Can create folder > 260 characters deep
                Directory.CreateDirectory(fullPath);

                // Can write file > 260 characters deep
                File.WriteAllText(file1, "Test file contents");

                // Can move file > 260 characters deep
                File.Move(file1, file2);

                // Can read file > 260 characters deep
                var contents = File.ReadAllText(file2);
                Assert.AreEqual("Test file contents", contents);

                // Can delete file > 260 characters deep
                File.Delete(file2);

                // Can delete folder > 260 characters deep
                Directory.Delete(fullPath);
            });

            // Cleanup
            Directory.Delete(root, true);
        }

        public class RebuildTestContext
        {
            internal ArchiveAndCompressBundles.TaskInput input;
            internal ArchiveAndCompressTests _this;
        };

        public static IEnumerable RebuildTestCases
        {
            get
            {
                yield return new TestCaseData(false, new Action<RebuildTestContext>((ctx) => { })).SetName("NoChanges");
                yield return new TestCaseData(true, new Action<RebuildTestContext>((ctx) =>
                {
                    ctx.input.InternalFilenameToWriteMetaData["internalName"] = new SerializedFileMetaData() { ContentHash = new Hash128(0, 1), RawFileHash = new Hash128(1, 2) };
                })).SetName("SourceFileHashChanges");
                yield return new TestCaseData(true, new Action<RebuildTestContext>((ctx) =>
                {
                    ctx.input.GetCompressionForIdentifier = (x) => UnityEngine.BuildCompression.Uncompressed;
                })).SetName("CompressionChanges");
#if UNITY_2019_3_OR_NEWER
                yield return new TestCaseData(true, new Action<RebuildTestContext>((ctx) =>
                {
                    ctx._this.AddRawFileThatTargetsBundle(ctx.input, "mybundle", "rawInternalName");
                })).SetName("AddAdditionalFile");
#endif
            }
        }

        [Test, TestCaseSource(typeof(ArchiveAndCompressTests), "RebuildTestCases")]
        public void WhenInputsChanges_AndRebuilt_CachedDataIsUsedAsExpected(bool shouldRebuild, Action<RebuildTestContext> postFirstBuildAction)
        {
            BuildCache.PurgeCache(false);
            using (BuildCache cache = new BuildCache())
            {
                RebuildTestContext ctx = new RebuildTestContext();
                ctx.input = GetDefaultInput();
                ctx._this = this;
                ctx.input.BuildCache = cache;

                AddSimpleBundle(ctx.input, "mybundle", "internalName");

                ArchiveAndCompressBundles.Run(ctx.input, out ArchiveAndCompressBundles.TaskOutput output);
                cache.SyncPendingSaves();
                Assert.AreEqual(0, ctx.input.OutCachedBundles.Count);

                postFirstBuildAction(ctx);

                ctx.input.OutCachedBundles.Clear();
                ArchiveAndCompressBundles.Run(ctx.input, out ArchiveAndCompressBundles.TaskOutput outputReRun);

                if (shouldRebuild)
                    Assert.AreEqual(0, ctx.input.OutCachedBundles.Count);
                else
                    Assert.AreEqual(1, ctx.input.OutCachedBundles.Count);
            }
        }

        [Test]
        public void WhenArchiveIsAlreadyBuilt_CachedVersionIsUsed()
        {
            string bundleOutDir1 = Path.Combine(m_TestTempDir, "bundleoutdir1");
            string bundleOutDir2 = Path.Combine(m_TestTempDir, "bundleoutdir2");
            Directory.CreateDirectory(bundleOutDir1);
            Directory.CreateDirectory(bundleOutDir2);
            ArchiveAndCompressBundles.TaskInput input = GetDefaultInput();
            BuildCache cache = new BuildCache();
            input.BuildCache = cache;
            AddSimpleBundle(input, "mybundle", "internalName");
            input.GetOutputFilePathForIdentifier = (x) => Path.Combine(bundleOutDir1, x);
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output);
            Assert.AreEqual(0, input.OutCachedBundles.Count);
            cache.SyncPendingSaves();

            input.GetOutputFilePathForIdentifier = (x) => Path.Combine(bundleOutDir2, x);
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output2);
            Assert.AreEqual(1, input.OutCachedBundles.Count);
            Assert.AreEqual("mybundle", input.OutCachedBundles[0]);
            AssertDirectoriesEqual(bundleOutDir1, bundleOutDir2, 1);
        }

        [Test]
        public void WhenArchiveIsCached_AndRebuildingArchive_HashIsAssignedToOutput()
        {
            string bundleName = "mybundle";
            ArchiveAndCompressBundles.TaskInput input = GetDefaultInput();
            BuildCache cache = new BuildCache();
            input.BuildCache = cache;
            AddSimpleBundle(input, bundleName, "internalName");
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output);
            var hash = output.BundleDetails[bundleName].Hash;
            Assert.AreNotEqual(Hash128.Parse(""), output.BundleDetails[bundleName].Hash);
            cache.SyncPendingSaves();

            //Now our bundle is cached so we'll run again and make sure the hashes are non-zero and equal
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output2);
            Assert.AreEqual(hash, output2.BundleDetails[bundleName].Hash);
        }

        public class ContentHashTestContext
        {
            internal ArchiveAndCompressBundles.TaskInput input;
            internal GUID assetGUID;
            internal ArchiveAndCompressTests _this;
        };

        public static IEnumerable ContentHashTestCases
        {
            get
            {
                yield return new TestCaseData(true, new Action<ContentHashTestContext>((ctx) =>
                {
                    ctx.input.AssetToFilesDependencies[ctx.assetGUID] = new List<string>() { "internalName", "internalName3" };
                })).SetName("DependencyChanges");
                yield return new TestCaseData(true, new Action<ContentHashTestContext>((ctx) =>
                {
                    ctx.input.InternalFilenameToWriteMetaData["internalName"].ContentHash = new Hash128(128, 128);
                })).SetName("ContentHashChanges");
                yield return new TestCaseData(false, new Action<ContentHashTestContext>((ctx) =>
                {
                    ctx.input.InternalFilenameToWriteMetaData["internalName"].RawFileHash = new Hash128(128, 128);
                })).SetName("RawHashChanges");
            }
        }

        [Test, TestCaseSource(typeof(ArchiveAndCompressTests), "ContentHashTestCases")]
        public void WhenInputsChange_BundleOutputHashIsAffectedAsExpected(bool hashShouldChange, Action<ContentHashTestContext> postFirstBuildAction)
        {
            ContentHashTestContext ctx = new ContentHashTestContext();
            ctx.input = GetDefaultInput();
            WriteResult result = AddSimpleBundle(ctx.input, "mybundle", "internalName");
            WriteResult result2 = AddSimpleBundle(ctx.input, "mybundle2", "internalName2");
            WriteResult result3 = AddSimpleBundle(ctx.input, "mybundle3", "internalName3");
            ctx.assetGUID = GUID.Generate();
            ctx.input.AssetToFilesDependencies.Add(ctx.assetGUID, new List<string>() { "internalName", "internalName2" });

            ArchiveAndCompressBundles.Run(ctx.input, out ArchiveAndCompressBundles.TaskOutput output);

            postFirstBuildAction(ctx);

            ArchiveAndCompressBundles.Run(ctx.input, out ArchiveAndCompressBundles.TaskOutput output2);

            Hash128 prevHash = output.BundleDetails["mybundle"].Hash;
            Hash128 newHash = output2.BundleDetails["mybundle"].Hash;
            if (hashShouldChange)
                Assert.AreNotEqual(prevHash, newHash);
            else
                Assert.AreEqual(prevHash, newHash);
        }

#if UNITY_2019_3_OR_NEWER
        [Test]
        public void WhenBuildingManyArchives_ThreadedAndNonThreadedResultsAreIdentical()
        {
            const int kBundleCount = 100;
            ArchiveAndCompressBundles.TaskInput input = GetDefaultInput();

            for (int i = 0; i < kBundleCount; i++)
                AddSimpleBundle(input, $"mybundle{i}", $"internalName{i}");

            input.Threaded = false;
            input.GetOutputFilePathForIdentifier = (x) => Path.Combine(m_TestTempDir, "bundleoutdir_nothreading", x);
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output1);

            input.Threaded = true;
            input.GetOutputFilePathForIdentifier = (x) => Path.Combine(m_TestTempDir, "bundleoutdir_threading", x);
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output2);

            AssertDirectoriesEqual(Path.Combine(m_TestTempDir, "bundleoutdir_nothreading"), Path.Combine(m_TestTempDir, "bundleoutdir_threading"), kBundleCount);
        }

#endif

        // Start is called before the first frame update
        [Test]
        public void ResourceFilesAreAddedToBundles()
        {
            ArchiveAndCompressBundles.TaskInput input = GetDefaultInput();
            string bundleOutDir = Path.Combine(m_TestTempDir, "bundleoutdir");

            AddSimpleBundle(input, "mybundle", "internalName");

            string srcFile = input.InternalFilenameToWriteResults["internalName"].resourceFiles[0].fileName;

            ReturnCode code = ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output);
            Assert.AreEqual(ReturnCode.Success, code);

            string[] files = RunWebExtract(Path.Combine(bundleOutDir, "mybundle"));
            Assert.AreEqual(1, files.Length);
            FileAssert.AreEqual(files[0], srcFile);
        }

        [Test]
        public void WhenBuildingArchive_BuildLogIsPopulated()
        {
            ArchiveAndCompressBundles.TaskInput input = GetDefaultInput();
            var log = new BuildLog();
            input.Log = log;
            AddSimpleBundle(input, "mybundle", "internalName");
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output);
            var node = log.Root.Children.Find((x) => x.Name.StartsWith("ArchiveItems"));
            Assert.IsNotNull(node);
        }

        class ScopeCapturer : IBuildLogger
        {
            public List<string> Scopes = new List<string>();
            public void AddEntry(LogLevel level, string msg)
            {
            }

            public void BeginBuildStep(LogLevel level, string stepName, bool subStepsCanBeThreaded)
            {
                lock (Scopes)
                {
                    Scopes.Add(stepName);
                }
            }

            public bool ContainsScopeWithSubstring(string subString)
            {
                return Scopes.Count((x) => x.Contains(subString)) != 0;
            }

            public void EndBuildStep()
            {
            }
        }

        private void AddSimpleBundleAndBuild(out ArchiveAndCompressBundles.TaskInput input, out string bundleBuildDir)
        {
            ScopeCapturer log1 = new ScopeCapturer();
            string bDir = Path.Combine(m_TestTempDir, "bundleoutdir1");
            Directory.CreateDirectory(bDir);
            input = GetDefaultInput();
            BuildCache cache = new BuildCache();
            input.BuildCache = cache;
            input.Log = log1;
            AddSimpleBundle(input, "mybundle", "internalName");
            input.GetOutputFilePathForIdentifier = (x) => Path.Combine(bDir, x);
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output);
            Assert.AreEqual(0, input.OutCachedBundles.Count);
            Assert.IsTrue(log1.ContainsScopeWithSubstring("Copying From Cache"));
            cache.SyncPendingSaves();
            bundleBuildDir = bDir;
        }

        [Test]
        public void WhenArchiveIsAlreadyBuilt_AndArchiveIsInOutputDirectory_ArchiveIsNotCopied()
        {
            AddSimpleBundleAndBuild(out ArchiveAndCompressBundles.TaskInput input, out string bundleOutDir1);
            ScopeCapturer log2 = new ScopeCapturer();

            input.GetOutputFilePathForIdentifier = (x) => Path.Combine(bundleOutDir1, x);
            input.Log = log2;
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output2);
            Assert.AreEqual(1, input.OutCachedBundles.Count);
            Assert.AreEqual("mybundle", input.OutCachedBundles[0]);
            Assert.IsFalse(log2.ContainsScopeWithSubstring("Copying From Cache"));
        }

        [Test]
        public void WhenArchiveIsAlreadyBuilt_AndArchiveIsInOutputDirectoryButTimestampMismatch_ArchiveIsCopied()
        {
            AddSimpleBundleAndBuild(out ArchiveAndCompressBundles.TaskInput input, out string bundleOutDir1);

            // Change the creation timestamp on the bundles
            string bundlePath = Path.Combine(bundleOutDir1, "mybundle");
            File.SetLastWriteTime(bundlePath, new DateTime(2019, 1, 1));

            ScopeCapturer log2 = new ScopeCapturer();

            input.GetOutputFilePathForIdentifier = (x) => Path.Combine(bundleOutDir1, x);
            input.Log = log2;
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output2);

            Assert.AreEqual(1, input.OutCachedBundles.Count);
            Assert.AreEqual("mybundle", input.OutCachedBundles[0]);
            Assert.IsTrue(log2.ContainsScopeWithSubstring("Copying From Cache"));
        }

#if UNITY_2019_3_OR_NEWER
        [Test]
        public void CanAddRawFilesToBundles()
        {
            ArchiveAndCompressBundles.TaskInput input = GetDefaultInput();
            AddSimpleBundle(input, "mybundle", "internalName");
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output);

            AddRawFileThatTargetsBundle(input, "mybundle", "rawName");
            ArchiveAndCompressBundles.Run(input, out ArchiveAndCompressBundles.TaskOutput output2);
            Assert.IsTrue(output.BundleDetails["mybundle"].Hash.isValid);
            Assert.IsTrue(output2.BundleDetails["mybundle"].Hash.isValid);
            Assert.AreNotEqual(output.BundleDetails["mybundle"].Hash, output2.BundleDetails["mybundle"].Hash);
        }

        [Test]
        public void SupportsMultiThreadedArchiving_WhenEditorIs20193OrLater_IsTrue()
        {
            Assert.IsTrue(ReflectionExtensions.SupportsMultiThreadedArchiving);
        }

#else
        [Test]
        public void SupportsMultiThreadedArchiving_WhenEditorIsBefore20193_IsFalse()
        {
            Assert.IsFalse(ReflectionExtensions.SupportsMultiThreadedArchiving);
        }

#endif

        [Test]
        public void CalculateBundleDependencies_ReturnsRecursiveDependencies_ForNonRecursiveInputs()
        {
            // Dictionary<string, string[]> CalculateBundleDependencies(List<List<string>> assetFileList, Dictionary<string, string> filenameToBundleName)
            // Inputs:  assetFileList = per asset list of unique file dependencies, first entry is the main dependency
            //          filenameToBundleName = mapping of file name to asset bundle name
            // Output:  mapping of bundle name to array of bundle name dependencies

            List<List<string>> assetFileList = new List<List<string>>();
            assetFileList.Add(new List<string> { "file1", "file2" });
            assetFileList.Add(new List<string> { "file2", "file3" });

            Dictionary<string, string> filenameToBundleName = new Dictionary<string, string>();
            filenameToBundleName.Add("file1", "bundle1");
            filenameToBundleName.Add("file2", "bundle2");
            filenameToBundleName.Add("file3", "bundle3");

            Dictionary<string, string[]> results = ArchiveAndCompressBundles.CalculateBundleDependencies(assetFileList, filenameToBundleName);

            CollectionAssert.AreEquivalent(new string[] { "bundle1", "bundle2", "bundle3" }, results.Keys);
            CollectionAssert.AreEquivalent(new string[] { "bundle2", "bundle3" }, results["bundle1"]);
            CollectionAssert.AreEquivalent(new string[] { "bundle3" }, results["bundle2"]);
            CollectionAssert.AreEquivalent(new string[] { }, results["bundle3"]);
        }

        [Test]
        public void CalculateBundleDependencies_ReturnsRecursiveDependencies_ForRecursiveInputs()
        {
            // Dictionary<string, string[]> CalculateBundleDependencies(List<List<string>> assetFileList, Dictionary<string, string> filenameToBundleName)
            // Inputs:  assetFileList = per asset list of unique file dependencies, first entry is the main dependency
            //          filenameToBundleName = mapping of file name to asset bundle name
            // Output:  mapping of bundle name to array of bundle name dependencies

            List<List<string>> assetFileList = new List<List<string>>();
            assetFileList.Add(new List<string> { "file1", "file2", "file3" });
            assetFileList.Add(new List<string> { "file2", "file3" });

            Dictionary<string, string> filenameToBundleName = new Dictionary<string, string>();
            filenameToBundleName.Add("file1", "bundle1");
            filenameToBundleName.Add("file2", "bundle2");
            filenameToBundleName.Add("file3", "bundle3");

            Dictionary<string, string[]> results = ArchiveAndCompressBundles.CalculateBundleDependencies(assetFileList, filenameToBundleName);

            CollectionAssert.AreEquivalent(new string[] { "bundle1", "bundle2", "bundle3" }, results.Keys);
            CollectionAssert.AreEquivalent(new string[] { "bundle2", "bundle3" }, results["bundle1"]);
            CollectionAssert.AreEquivalent(new string[] { "bundle3" }, results["bundle2"]);
            CollectionAssert.AreEquivalent(new string[] { }, results["bundle3"]);
        }
    }
}
