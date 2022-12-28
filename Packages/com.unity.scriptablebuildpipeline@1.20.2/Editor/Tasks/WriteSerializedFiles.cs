using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using static UnityEditor.Build.Pipeline.Utilities.TaskCachingUtility;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Serializes all cache data.
    /// </summary>
    public class WriteSerializedFiles : IBuildTask, IRunCachedCallbacks<WriteSerializedFiles.Item>
    {
        /// <inheritdoc />
        public int Version { get { return 4; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.In)]
        IWriteData m_WriteData;

        [InjectContext]
        IBuildResults m_Results;

        [InjectContext(ContextUsage.In, true)]
        IProgressTracker m_Tracker;

        [InjectContext(ContextUsage.In, true)]
        IBuildCache m_Cache;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;
#pragma warning restore 649

        static Hash128 GetPlayerSettingsHash128(BuildTarget target)
        {
            return HashingMethods.Calculate(
                PlayerSettings.stripUnusedMeshComponents,
                PlayerSettings.bakeCollisionMeshes
#if UNITY_2020_1_OR_NEWER
                , PlayerSettings.mipStripping ? PlayerSettingsApi.GetNumberOfMipsStripped() : 0
#endif
                , PlayerSettings.GetGraphicsAPIs(target)
                ).ToHash128();
        }

        CacheEntry GetCacheEntry(IWriteOperation operation, BuildSettings settings, BuildUsageTagGlobal globalUsage, bool onlySaveFirstSerializedObject)
        {
            using (m_Log.ScopedStep(LogLevel.Verbose, "GetCacheEntry", operation.Command.fileName))
            {
                var entry = new CacheEntry();
                entry.Type = CacheEntry.EntryType.Data;
                entry.Guid = HashingMethods.Calculate("WriteSerializedFiles", operation.Command.fileName).ToGUID();
                entry.Hash = HashingMethods.Calculate(Version, operation.GetHash128(m_Log), settings.GetHash128(), globalUsage, onlySaveFirstSerializedObject, GetPlayerSettingsHash128(settings.target)).ToHash128();
                entry.Version = Version;
                return entry;
            }
        }

        static void SlimifySerializedObjects(ref WriteResult result)
        {
            var fileOffsets = new List<ObjectSerializedInfo>();
            foreach (ResourceFile serializedFile in result.resourceFiles)
            {
                if (!serializedFile.serializedFile)
                    continue;
                fileOffsets.Add(result.serializedObjects.First(x => x.header.fileName == serializedFile.fileAlias));
            }

            result.SetSerializedObjects(fileOffsets.ToArray());
        }

        CachedInfo GetCachedInfo(CacheEntry entry, WriteResult result, SerializedFileMetaData metaData)
        {
            var info = new CachedInfo();
            info.Asset = entry;
            info.Data = new object[] { result, metaData };
            info.Dependencies = new CacheEntry[0];
            return info;
        }

        class Item
        {
            public WriteResult Result;
            public SerializedFileMetaData MetaData;
        }

        BuildSettings m_BuildSettings;
        BuildUsageTagGlobal m_GlobalUsage;
        IBuildCache m_UseCache;

        internal void SetupTaskContext()
        {
            m_GlobalUsage = m_DependencyData.GlobalUsage;
            foreach (var sceneInfo in m_DependencyData.SceneInfo)
                m_GlobalUsage |= sceneInfo.Value.globalUsage;

            m_BuildSettings = m_Parameters.GetContentBuildSettings();
            m_UseCache = m_Parameters.UseCache ? m_Cache : null;
        }

        /// <inheritdoc />
        public ReturnCode Run()
        {
            SetupTaskContext();

            List<WorkItem<Item>> workItems = m_WriteData.WriteOperations.Select(
                i => new WorkItem<Item>(new Item(), i.Command.internalName)
                ).ToList();

            return TaskCachingUtility.RunCachedOperation<Item>(
                m_UseCache,
                m_Log,
                m_Tracker,
                workItems,
                this);
        }

        internal static SerializedFileMetaData CalculateFileMetadata(ref WriteResult result)
        {
            List<RawHash> contentHashObjects = new List<RawHash>();
            List<RawHash> fullHashObjects = new List<RawHash>();
            foreach (ResourceFile file in result.resourceFiles)
            {
                RawHash fileHash = HashingMethods.CalculateFile(file.fileName);
                RawHash contentHash = fileHash;
                fullHashObjects.Add(fileHash);
                if (file.serializedFile && result.serializedObjects.Count > 0)
                {
                    ObjectSerializedInfo firstObj = result.serializedObjects.First(x => x.header.fileName == file.fileAlias);
                    using (var stream = new FileStream(file.fileName, FileMode.Open, FileAccess.Read))
                    {
                        stream.Position = (long)firstObj.header.offset;
                        contentHash = HashingMethods.CalculateStream(stream);
                    }
                }
                contentHashObjects.Add(contentHash);
            }
            SerializedFileMetaData data = new SerializedFileMetaData();
            data.RawFileHash = HashingMethods.Calculate(fullHashObjects).ToHash128();
            data.ContentHash = HashingMethods.Calculate(contentHashObjects).ToHash128();
            return data;
        }

        /// <inheritdoc/>
        CacheEntry IRunCachedCallbacks<Item>.CreateCacheEntry(WorkItem<Item> item)
        {
            return GetCacheEntry(m_WriteData.WriteOperations[item.Index], m_BuildSettings, m_GlobalUsage, ScriptableBuildPipeline.slimWriteResults);
        }

        /// <inheritdoc/>
        void IRunCachedCallbacks<Item>.ProcessUncached(WorkItem<Item> item)
        {
            IWriteOperation op = m_WriteData.WriteOperations[item.Index];

            string targetDir = m_UseCache != null ? m_UseCache.GetCachedArtifactsDirectory(item.entry) : m_Parameters.TempOutputFolder;
            Directory.CreateDirectory(targetDir);

            using (m_Log.ScopedStep(LogLevel.Info, $"Writing {op.GetType().Name}", op.Command.fileName))
            {
#if UNITY_2020_2_OR_NEWER || ENABLE_DETAILED_PROFILE_CAPTURING
                using (new ProfileCaptureScope(m_Log, ProfileCaptureOptions.None))
                    item.Context.Result = op.Write(targetDir, m_BuildSettings, m_GlobalUsage);
#else
                item.Context.Result = op.Write(targetDir, m_BuildSettings, m_GlobalUsage);
#endif
            }

            item.Context.MetaData = CalculateFileMetadata(ref item.Context.Result);

            if (ScriptableBuildPipeline.slimWriteResults)
                SlimifySerializedObjects(ref item.Context.Result);
        }

        /// <inheritdoc/>
        void IRunCachedCallbacks<Item>.ProcessCached(WorkItem<Item> item, CachedInfo info)
        {
            item.Context.Result = (WriteResult)info.Data[0];
            item.Context.MetaData = (SerializedFileMetaData)info.Data[1];
        }

        /// <inheritdoc/>
        void IRunCachedCallbacks<Item>.PostProcess(WorkItem<Item> item)
        {
            IWriteOperation op = m_WriteData.WriteOperations[item.Index];
            m_Results.WriteResults.Add(op.Command.internalName, item.Context.Result);
            m_Results.WriteResultsMetaData.Add(op.Command.internalName, item.Context.MetaData);
        }

        /// <inheritdoc/>
        CachedInfo IRunCachedCallbacks<Item>.CreateCachedInfo(WorkItem<Item> item)
        {
            return GetCachedInfo(item.entry, item.Context.Result, item.Context.MetaData);
        }
    }
}
