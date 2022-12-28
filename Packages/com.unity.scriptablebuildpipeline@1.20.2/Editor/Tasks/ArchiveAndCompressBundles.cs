using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.Build.Pipeline;

namespace UnityEditor.Build.Pipeline.Tasks
{
#if UNITY_2018_3_OR_NEWER
    using BuildCompression = UnityEngine.BuildCompression;
#else
    using BuildCompression = UnityEditor.Build.Content.BuildCompression;
#endif
    /// <summary>
    /// Archives and compresses all asset bundles.
    /// </summary>
    public class ArchiveAndCompressBundles : IBuildTask
    {
        private const int kVersion = 2;
        /// <inheritdoc />
        public int Version { get { return kVersion; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IBundleWriteData m_WriteData;

#if UNITY_2019_3_OR_NEWER
        [InjectContext(ContextUsage.In)]
        IBundleBuildContent m_Content;
#endif

        [InjectContext]
        IBundleBuildResults m_Results;

        [InjectContext(ContextUsage.In, true)]
        IProgressTracker m_Tracker;

        [InjectContext(ContextUsage.In, true)]
        IBuildCache m_Cache;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;
#pragma warning restore 649

        internal static void CopyFileWithTimestampIfDifferent(string srcPath, string destPath, IBuildLogger log)
        {
            if (srcPath == destPath)
                return;

            srcPath = Path.GetFullPath(srcPath);
            destPath = Path.GetFullPath(destPath);

#if UNITY_EDITOR_WIN
            // Max path length per MS Path code.
            const int MaxPath = 260;

            if (srcPath.Length > MaxPath)
                throw new PathTooLongException(srcPath);
            
            if (destPath.Length > MaxPath)
                throw new PathTooLongException(destPath);
#endif

            DateTime time = File.GetLastWriteTime(srcPath);
            DateTime destTime = File.Exists(destPath) ? File.GetLastWriteTime(destPath) : new DateTime();

            if (destTime == time)
                return;

            using (log.ScopedStep(LogLevel.Verbose, "Copying From Cache", $"{srcPath} -> {destPath}"))
            {
                var directory = Path.GetDirectoryName(destPath);
                Directory.CreateDirectory(directory);
                File.Copy(srcPath, destPath, true);
            }
        }

        static CacheEntry GetCacheEntry(IBuildCache cache, string bundleName, IEnumerable<ResourceFile> resources, BuildCompression compression, List<SerializedFileMetaData> hashes)
        {
            var entry = new CacheEntry();
            entry.Type = CacheEntry.EntryType.Data;
            entry.Guid = HashingMethods.Calculate("ArchiveAndCompressBundles", bundleName).ToGUID();
            List<object> toHash = new List<object> { kVersion, compression };
            foreach (var resource in resources)
            {
                toHash.Add(resource.serializedFile);
                toHash.Add(resource.fileAlias);
            }
            toHash.AddRange(hashes.Select(x => (object)x.RawFileHash));
            entry.Hash = HashingMethods.Calculate(toHash).ToHash128();
            entry.Version = kVersion;
            return entry;
        }

        static CachedInfo GetCachedInfo(IBuildCache cache, CacheEntry entry, IEnumerable<ResourceFile> resources, BundleDetails details)
        {
            var info = new CachedInfo();
            info.Asset = entry;
            info.Dependencies = new CacheEntry[0];
            info.Data = new object[] { details };
            return info;
        }

        internal static Hash128 CalculateHashVersion(ArchiveWorkItem item, string[] dependencies)
        {
            List<Hash128> hashes = new List<Hash128>();

            hashes.AddRange(item.SeriliazedFileMetaDatas.Select(x => x.ContentHash));

            return HashingMethods.Calculate(hashes, dependencies).ToHash128();
        }

        internal class ArchiveWorkItem
        {
            public int Index;
            public string BundleName;
            public string OutputFilePath;
            public string CachedArtifactPath;
            public List<ResourceFile> ResourceFiles;
            public BuildCompression Compression;
            public BundleDetails ResultDetails;
            public List<SerializedFileMetaData> SeriliazedFileMetaDatas = new List<SerializedFileMetaData>();
        }

        internal struct TaskInput
        {
            public Dictionary<string, WriteResult> InternalFilenameToWriteResults;
            public Dictionary<string, SerializedFileMetaData> InternalFilenameToWriteMetaData;
#if UNITY_2019_3_OR_NEWER
            public Dictionary<string, List<ResourceFile>> BundleNameToAdditionalFiles;
#endif
            public Dictionary<string, string> InternalFilenameToBundleName;
            public Func<string, BuildCompression> GetCompressionForIdentifier;
            public Func<string, string> GetOutputFilePathForIdentifier;
            public IBuildCache BuildCache;
            public Dictionary<GUID, List<string>> AssetToFilesDependencies;
            public IProgressTracker ProgressTracker;
            public string TempOutputFolder;
            public bool Threaded;
            public List<string> OutCachedBundles;
            public IBuildLogger Log;
        }

        internal struct TaskOutput
        {
            public Dictionary<string, BundleDetails> BundleDetails;
        }

        /// <inheritdoc />
        public ReturnCode Run()
        {
            TaskInput input = new TaskInput();
            input.InternalFilenameToWriteResults = m_Results.WriteResults;
#if UNITY_2019_3_OR_NEWER
            input.BundleNameToAdditionalFiles = m_Content.AdditionalFiles;
#endif
            input.InternalFilenameToBundleName = m_WriteData.FileToBundle;
            input.GetCompressionForIdentifier = (x) => m_Parameters.GetCompressionForIdentifier(x);
            input.GetOutputFilePathForIdentifier = (x) => m_Parameters.GetOutputFilePathForIdentifier(x);
            input.BuildCache = m_Parameters.UseCache ? m_Cache : null;
            input.ProgressTracker = m_Tracker;
            input.TempOutputFolder = m_Parameters.TempOutputFolder;
            input.AssetToFilesDependencies = m_WriteData.AssetToFiles;
            input.InternalFilenameToWriteMetaData = m_Results.WriteResultsMetaData;
            input.Log = m_Log;

            input.Threaded = ReflectionExtensions.SupportsMultiThreadedArchiving && ScriptableBuildPipeline.threadedArchiving;

            TaskOutput output;
            ReturnCode code = Run(input, out output);

            if (code == ReturnCode.Success)
            {
                foreach (var item in output.BundleDetails)
                    m_Results.BundleInfos.Add(item.Key, item.Value);
            }

            return code;
        }

        internal static Dictionary<string, string[]> CalculateBundleDependencies(List<List<string>> assetFileList, Dictionary<string, string> filenameToBundleName)
        {
            var bundleDependencies = new Dictionary<string, string[]>();
            Dictionary<string, HashSet<string>> bundleDependenciesHash = new Dictionary<string, HashSet<string>>();
            foreach (var files in assetFileList)
            {
                if (files.IsNullOrEmpty())
                    continue;

                string bundle = filenameToBundleName[files.First()];
                HashSet<string> dependencies;
                bundleDependenciesHash.GetOrAdd(bundle, out dependencies);
                dependencies.UnionWith(files.Select(x => filenameToBundleName[x]));
                dependencies.Remove(bundle);

                // Ensure we create mappings for all encountered files
                foreach (var file in files)
                    bundleDependenciesHash.GetOrAdd(filenameToBundleName[file], out dependencies);
            }
           //foreach (var dep in bundleDependenciesHash)
           //{
           //    string[] ret = dep.Value.ToArray();
           //    for (int i = 0; i < ret.Length; i++)
           //    {
           //        Debug.LogError("first dep.Key..." + dep.Key + " dps  " + ret[i]);
           //    }
           //}
            // Recursively combine dependencies
            foreach (var dependencyPair in bundleDependenciesHash)
            {
                List<string> dependencies = dependencyPair.Value.ToList();
                for (int i = 0; i < dependencies.Count; i++)
                {
                    if (!bundleDependenciesHash.TryGetValue(dependencies[i], out var recursiveDependencies))
                        continue;
                    foreach (var recursiveDependency in recursiveDependencies)
                    {
                        if (dependencyPair.Value.Add(recursiveDependency))
                            dependencies.Add(recursiveDependency);
                    }
                }
            }

            foreach (var dep in bundleDependenciesHash)
            {
                string[] ret = dep.Value.ToArray();
                Array.Sort(ret);

              //for (int i = 0; i < ret.Length; i++)
              //{
              //    Debug.LogError("dep.Key..."+ dep.Key+" dps  "+ ret[i]);
              //}
                bundleDependencies.Add(dep.Key, ret);
            }
            return bundleDependencies;
        }

        static void PostArchiveProcessing(List<ArchiveWorkItem> items, List<List<string>> assetFileList, Dictionary<string, string> filenameToBundleName, IBuildLogger log)
        {
            using (log.ScopedStep(LogLevel.Info, "PostArchiveProcessing"))
            {
                Dictionary<string, string[]> bundleDependencies = CalculateBundleDependencies(assetFileList, filenameToBundleName);
                foreach (ArchiveWorkItem item in items)
                {
                    // apply bundle dependencies
                    item.ResultDetails.Dependencies = bundleDependencies.ContainsKey(item.BundleName) ? bundleDependencies[item.BundleName] : new string[0];
                    item.ResultDetails.Hash = CalculateHashVersion(item, item.ResultDetails.Dependencies);
                }
            }
        }

        static ArchiveWorkItem GetOrCreateWorkItem(TaskInput input, string bundleName, Dictionary<string, ArchiveWorkItem> bundleToWorkItem)
        {
            if (!bundleToWorkItem.TryGetValue(bundleName, out ArchiveWorkItem item))
            {
                item = new ArchiveWorkItem();
                item.BundleName = bundleName;
                item.Compression = input.GetCompressionForIdentifier(bundleName);
                item.OutputFilePath = input.GetOutputFilePathForIdentifier(bundleName);
                item.ResourceFiles = new List<ResourceFile>();
                bundleToWorkItem[bundleName] = item;
            }
            return item;
        }

        static RawHash HashResourceFiles(List<ResourceFile> files)
        {
            return HashingMethods.Calculate(files.Select((x) => HashingMethods.CalculateFile(x.fileName)));
        }

        static List<ArchiveWorkItem> CreateWorkItems(TaskInput input)
        {
            using (input.Log.ScopedStep(LogLevel.Info, "CreateWorkItems"))
            {
                Dictionary<string, ArchiveWorkItem> bundleNameToWorkItem = new Dictionary<string, ArchiveWorkItem>();

                foreach (var pair in input.InternalFilenameToWriteResults)
                {
                    string internalName = pair.Key;
                    string bundleName = input.InternalFilenameToBundleName[internalName];
                    ArchiveWorkItem item = GetOrCreateWorkItem(input, bundleName, bundleNameToWorkItem);

                    if (input.InternalFilenameToWriteMetaData.TryGetValue(pair.Key, out SerializedFileMetaData md))
                        item.SeriliazedFileMetaDatas.Add(md);
                    else
                        throw new Exception($"Archive {bundleName} with internal name {internalName} does not have associated SerializedFileMetaData");

                    item.ResourceFiles.AddRange(pair.Value.resourceFiles);

#if UNITY_2019_3_OR_NEWER
                    if (input.BundleNameToAdditionalFiles.TryGetValue(bundleName, out List<ResourceFile> additionalFiles))
                    {
                        RawHash hash = HashResourceFiles(additionalFiles);
                        item.SeriliazedFileMetaDatas.Add(new SerializedFileMetaData() { ContentHash = hash.ToHash128(), RawFileHash = hash.ToHash128() });
                        item.ResourceFiles.AddRange(additionalFiles);
                    }
#endif
                }

                List<ArchiveWorkItem> allItems = bundleNameToWorkItem.Select((x, index) => { x.Value.Index = index; return x.Value; }).ToList();
                return allItems;
            }
        }

        static internal ReturnCode Run(TaskInput input, out TaskOutput output)
        {
            output = new TaskOutput();
            output.BundleDetails = new Dictionary<string, BundleDetails>();

            List<ArchiveWorkItem> allItems = CreateWorkItems(input);

            IList<CacheEntry> cacheEntries = null;
            IList<CachedInfo> cachedInfo = null;
            List<ArchiveWorkItem> cachedItems = new List<ArchiveWorkItem>();
            List<ArchiveWorkItem> nonCachedItems = allItems;
            if (input.BuildCache != null)
            {
                using (input.Log.ScopedStep(LogLevel.Info, "Creating Cache Entries"))
                    cacheEntries = allItems.Select(x => GetCacheEntry(input.BuildCache, x.BundleName, x.ResourceFiles, x.Compression, x.SeriliazedFileMetaDatas)).ToList();

                using (input.Log.ScopedStep(LogLevel.Info, "Load Cached Data"))
                    input.BuildCache.LoadCachedData(cacheEntries, out cachedInfo);

                cachedItems = allItems.Where(x => cachedInfo[x.Index] != null).ToList();
                nonCachedItems = allItems.Where(x => cachedInfo[x.Index] == null).ToList();
                foreach (ArchiveWorkItem i in allItems)
                    i.CachedArtifactPath = string.Format("{0}/{1}", input.BuildCache.GetCachedArtifactsDirectory(cacheEntries[i.Index]), HashingMethods.Calculate(i.BundleName));
            }

            using (input.Log.ScopedStep(LogLevel.Info, "CopyingCachedFiles"))
            {
                foreach (ArchiveWorkItem item in cachedItems)
                {
                    if (!input.ProgressTracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", item.BundleName)))
                        return ReturnCode.Canceled;

                    item.ResultDetails = (BundleDetails)cachedInfo[item.Index].Data[0];
                    item.ResultDetails.FileName = item.OutputFilePath;
                    CopyFileWithTimestampIfDifferent(item.CachedArtifactPath, item.ResultDetails.FileName, input.Log);
                }
            }

            // Write all the files that aren't cached
            if (!ArchiveItems(nonCachedItems, input.TempOutputFolder, input.ProgressTracker, input.Threaded, input.Log))
                return ReturnCode.Canceled;

            PostArchiveProcessing(allItems, input.AssetToFilesDependencies.Values.ToList(), input.InternalFilenameToBundleName, input.Log);

            // Put everything into the cache
            if (input.BuildCache != null)
            {
                using (input.Log.ScopedStep(LogLevel.Info, "Copying To Cache"))
                {
                    List<CachedInfo> uncachedInfo = nonCachedItems.Select(x => GetCachedInfo(input.BuildCache, cacheEntries[x.Index], x.ResourceFiles, x.ResultDetails)).ToList();
                    input.BuildCache.SaveCachedData(uncachedInfo);
                }
            }

            output.BundleDetails = allItems.ToDictionary((x) => x.BundleName, (x) => x.ResultDetails);

            if (input.OutCachedBundles != null)
                input.OutCachedBundles.AddRange(cachedItems.Select(x => x.BundleName));

            return ReturnCode.Success;
        }

        static private void ArchiveSingleItem(ArchiveWorkItem item, string tempOutputFolder, IBuildLogger log)
        {
            using (log.ScopedStep(LogLevel.Info, "ArchiveSingleItem", item.BundleName))
            {
                item.ResultDetails = new BundleDetails();
                string writePath = string.Format("{0}/{1}", tempOutputFolder, item.BundleName);
                if (!string.IsNullOrEmpty(item.CachedArtifactPath))
                    writePath = item.CachedArtifactPath;

                Directory.CreateDirectory(Path.GetDirectoryName(writePath));
                item.ResultDetails.FileName = item.OutputFilePath;
                item.ResultDetails.Crc = ContentBuildInterface.ArchiveAndCompress(item.ResourceFiles.ToArray(), writePath, item.Compression);

                CopyFileWithTimestampIfDifferent(writePath, item.ResultDetails.FileName, log);
            }
        }

        static private bool ArchiveItems(List<ArchiveWorkItem> items, string tempOutputFolder, IProgressTracker tracker, bool threaded, IBuildLogger log)
        {
            using (log.ScopedStep(LogLevel.Info, "ArchiveItems", threaded))
            {
                log?.AddEntry(LogLevel.Info, $"Archiving {items.Count} Bundles");
                if (threaded)
                    return ArchiveItemsThreaded(items, tempOutputFolder, tracker, log);

                foreach (ArchiveWorkItem item in items)
                {
                    if (tracker != null && !tracker.UpdateInfoUnchecked(item.BundleName))
                        return false;

                    ArchiveSingleItem(item, tempOutputFolder, log);
                }
                return true;
            }
        }

        static private bool ArchiveItemsThreaded(List<ArchiveWorkItem> items, string tempOutputFolder, IProgressTracker tracker, IBuildLogger log)
        {
            CancellationTokenSource srcToken = new CancellationTokenSource();

            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            List<Task> tasks = new List<Task>(items.Count);
            foreach (ArchiveWorkItem item in items)
            {
                tasks.Add(Task.Run(() =>
                {
                    try { ArchiveSingleItem(item, tempOutputFolder, log); }
                    finally { semaphore.Release(); }
                }, srcToken.Token));
            }

            for (int i = 0; i < items.Count; i++)
            {
                semaphore.Wait(srcToken.Token);
                if (tracker != null && !tracker.UpdateInfoUnchecked($"Archive {i + 1}/{items.Count}"))
                {
                    srcToken.Cancel();
                    break;
                }
            }
            Task.WaitAny(Task.WhenAll(tasks));
            int count = 0;
            foreach (var task in tasks)
            {
                if (task.Exception == null)
                    continue;
                Debug.LogException(task.Exception);
                count++;
            }
            if (count > 0)
                throw new BuildFailedException($"ArchiveAndCompressBundles encountered {count} exception(s). See console for logged exceptions.");

            return !srcToken.Token.IsCancellationRequested;
        }
    }
}
