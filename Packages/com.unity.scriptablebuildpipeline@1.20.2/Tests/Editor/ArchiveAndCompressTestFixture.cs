using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Tests;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

public class ArchiveAndCompressTestFixture
{
    internal static void AssertDirectoriesEqual(string expectedDirectory, string directory, int expectedCount = -1)
    {
        string[] expectedFiles = Directory.GetFiles(expectedDirectory);
        Array.Sort(expectedFiles);
        string[] files = Directory.GetFiles(directory);
        Array.Sort(files);
        if (expectedCount != -1)
            Assert.AreEqual(expectedCount, expectedFiles.Length);
        Assert.AreEqual(expectedFiles.Length, files.Length);

        for (int i = 0; i < expectedFiles.Length; i++)
            FileAssert.AreEqual(expectedFiles[i], files[i]);
    }

    internal string CreateTempDir(string testDir)
    {
        string tempDirectory = Path.Combine("Temp", testDir);
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    internal static void WriteRandomData(string filename, long size, int seed)
    {
        System.Random r = new System.Random(seed);
        using (var s = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            long written = 0;
            byte[] bytes = new byte[Math.Min(1 * 1024 * 1024, size)];
            while (written < size)
            {
                r.NextBytes(bytes);
                int writeSize = (int)Math.Min(size - written, bytes.Length);
                s.Write(bytes, 0, writeSize);
                written += bytes.Length;
            }
        }
    }

    internal string CreateFileOfSize(string path, long size)
    {
        System.Random r = new System.Random(m_Seed);
        m_Seed++;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        WriteRandomData(path, size, 0);
        return path;
    }

    int m_Seed;
    internal string m_TestTempDir;
    internal string m_FixtureTempDir;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Spooky hash has a static constructor that must be called on the main thread.
        m_FixtureTempDir = CreateTempDir("FixtureTemp");
        HashingMethods.CalculateStream(new MemoryStream(new byte[] { 1 }));
    }

    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        Directory.Delete(m_FixtureTempDir, true);
    }

    [SetUp]
    public void Setup()
    {
        m_Seed = 0;
        BuildCache.PurgeCache(false); // TOOD: If the build cache didn't use global directories, this wouldn't be necessary
        m_TestTempDir = CreateTempDir("TestTemp");
        m_SizeCounts = new Dictionary<int, int>();
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(m_TestTempDir))
            Directory.Delete(m_TestTempDir, true);
    }

    internal string[] RunWebExtract(string filePath)
    {
        var baseDir = Path.GetDirectoryName(EditorApplication.applicationPath);
        var webExtractFiles = Directory.GetFiles(baseDir, "WebExtract*", SearchOption.AllDirectories);
        string webExtractPath = webExtractFiles[0];

        Assert.IsTrue(File.Exists(filePath), "Param filePath does not point to an existing file.");

        var process = new Process
        {
            StartInfo =
            {
                FileName = webExtractPath,
                Arguments = string.Format(@"""{0}""", filePath),
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var exitCode = process.ExitCode;
        process.Close();

        Assert.AreEqual(0, exitCode);
        //UnityEngine.Debug.Log(output);
        return Directory.GetFiles(filePath + "_data");
    }

    internal ArchiveAndCompressBundles.TaskInput GetDefaultInput()
    {
        ArchiveAndCompressBundles.TaskInput input = new ArchiveAndCompressBundles.TaskInput();

        input.InternalFilenameToWriteResults = new Dictionary<string, WriteResult>();
#if UNITY_2019_3_OR_NEWER
        input.BundleNameToAdditionalFiles = new Dictionary<string, List<ResourceFile>>();
#endif
        input.InternalFilenameToBundleName = new Dictionary<string, string>();
        input.AssetToFilesDependencies = new Dictionary<UnityEditor.GUID, List<string>>();
        input.InternalFilenameToWriteMetaData = new Dictionary<string, SerializedFileMetaData>();
        input.BuildCache = null;
        input.Threaded = false;
        input.ProgressTracker = null;
        input.OutCachedBundles = new List<string>();
        input.GetCompressionForIdentifier = (x) => UnityEngine.BuildCompression.LZ4;
        input.GetOutputFilePathForIdentifier = (x) => Path.Combine(m_TestTempDir, "bundleoutdir", x);
        input.TempOutputFolder = Path.Combine(m_TestTempDir, "temptestdir");

        return input;
    }

    internal WriteResult AddSimpleBundle(ArchiveAndCompressBundles.TaskInput input, string bundleName, string internalName, string filePath)
    {
        WriteResult writeResult = new WriteResult();
        ResourceFile file = new ResourceFile();
        file.SetFileName(filePath);
        file.SetFileAlias(internalName);
        file.SetSerializedFile(false);
        writeResult.SetResourceFiles(new ResourceFile[] { file });
        input.InternalFilenameToWriteResults.Add(internalName, writeResult);
        input.InternalFilenameToBundleName.Add(internalName, bundleName);
        SerializedFileMetaData md = new SerializedFileMetaData();
        md.RawFileHash = HashingMethods.CalculateFile(filePath).ToHash128();
        md.ContentHash = HashingMethods.CalculateFile(filePath).ToHash128();
        input.InternalFilenameToWriteMetaData.Add(internalName, md);
        return writeResult;
    }

#if UNITY_2019_3_OR_NEWER
    internal void AddRawFileThatTargetsBundle(ArchiveAndCompressBundles.TaskInput input, string targetBundleName, string rawFileInternalName, string filePath)
    {
        ResourceFile file = new ResourceFile();
        file.SetFileName(filePath);
        file.SetFileAlias(rawFileInternalName);
        file.SetSerializedFile(false);
        List<ResourceFile> files = new List<ResourceFile> { file };
        input.BundleNameToAdditionalFiles.Add(targetBundleName, files);
    }

    internal void AddRawFileThatTargetsBundle(ArchiveAndCompressBundles.TaskInput input, string targetBundleName, string rawFileInternalName)
    {
        string tempFilename = CreateFileOfSize(GetUniqueFilename(Path.Combine(m_FixtureTempDir, "src", "rawfile.bin")), 1024);
        AddRawFileThatTargetsBundle(input, targetBundleName, rawFileInternalName, tempFilename);
    }

#endif

    public static string GetUniqueFilename(string desiredFilename)
    {
        string dir = Path.GetDirectoryName(desiredFilename);
        string noExtention = Path.GetFileNameWithoutExtension(desiredFilename);
        string ext = Path.GetExtension(desiredFilename);
        for (int i = 0; true; i++)
        {
            string testName = Path.Combine(dir, Path.Combine($"{noExtention}{i}{ext}"));
            if (!File.Exists(testName))
            {
                return testName;
            }
        }
    }

    internal Dictionary<int, int> m_SizeCounts = new Dictionary<int, int>();
    internal WriteResult AddSimpleBundle(ArchiveAndCompressBundles.TaskInput input, string bundleName, string internalName, int size = 1024)
    {
        if (!m_SizeCounts.ContainsKey(size))
            m_SizeCounts[size] = 0;
        int count = m_SizeCounts[size]++;
        string tempFilename = Path.Combine(m_FixtureTempDir, "src", $"testfile_{size}_{count}.bin");
        if (!File.Exists(tempFilename))
            CreateFileOfSize(tempFilename, size);
        return AddSimpleBundle(input, bundleName, internalName, tempFilename);
    }
}
