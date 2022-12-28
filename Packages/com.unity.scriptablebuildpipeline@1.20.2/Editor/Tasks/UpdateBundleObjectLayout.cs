using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Updates the layout for bundle objects.
    /// </summary>
    public class UpdateBundleObjectLayout : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In, true)]
        IBundleExplictObjectLayout m_Layout;

        [InjectContext]
        IBundleBuildContent m_Content;

        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In)]
        IDeterministicIdentifiers m_PackingMethod;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            if (m_Layout == null || m_Layout.ExplicitObjectLocation.IsNullOrEmpty())
                return ReturnCode.SuccessNotRun;

            var ObjectToAssetReferences = new Dictionary<ObjectIdentifier, List<GUID>>();
            var ObjectToFiles = new Dictionary<ObjectIdentifier, List<string>>();

            using (m_Log.ScopedStep(LogLevel.Info, "PopulateReferencesMaps", true))
            {
                var task = Task.Run(() =>
                {
                    using (m_Log.ScopedStep(LogLevel.Info, "Populate Assets Map", $"Count={m_DependencyData.AssetInfo.Count}"))
                    {
                        foreach (KeyValuePair<GUID, AssetLoadInfo> dependencyPair in m_DependencyData.AssetInfo)
                        {
                            PopulateReferencesMap(dependencyPair.Key, dependencyPair.Value.includedObjects, ObjectToAssetReferences);
                            PopulateReferencesMap(dependencyPair.Key, dependencyPair.Value.referencedObjects, ObjectToAssetReferences);
                        }
                    }
                    using (m_Log.ScopedStep(LogLevel.Info, "Populate Scenes Map", $"Count={m_DependencyData.SceneInfo.Count}"))
                    {
                        foreach (KeyValuePair<GUID, SceneDependencyInfo> dependencyPair in m_DependencyData.SceneInfo)
                            PopulateReferencesMap(dependencyPair.Key, dependencyPair.Value.referencedObjects, ObjectToAssetReferences);
                    }
                });
                
                using (m_Log.ScopedStep(LogLevel.Info, "Populate Files Map", $"Count={m_WriteData.FileToObjects.Count}"))
                {
                    foreach (KeyValuePair<string, List<ObjectIdentifier>> filePair in m_WriteData.FileToObjects)
                        PopulateReferencesMap(filePair.Key, filePair.Value, ObjectToFiles);
                }
                task.Wait();
            }

            using (m_Log.ScopedStep(LogLevel.Info, "UpdateWriteData"))
            {
                foreach (var group in m_Layout.ExplicitObjectLocation.GroupBy(s => s.Value))
                {
                    IEnumerable<ObjectIdentifier> objectIDs = group.Select(s => s.Key);
                    string bundleName = group.Key;
                    string internalName = string.Format(CommonStrings.AssetBundleNameFormat, m_PackingMethod.GenerateInternalFileName(bundleName));

                    foreach (var objectID in objectIDs)
                    {
                        UpdateAssetToFilesMap(internalName, ObjectToAssetReferences[objectID], m_WriteData.AssetToFiles);
                        RemoveObjectIDFromFiles(objectID, ObjectToFiles[objectID], m_WriteData.FileToObjects);
                    }

                    // Add new mapping for File to Bundle
                    UpdateFileToBundleMap(bundleName, internalName, m_WriteData.FileToBundle, m_Content.BundleLayout);

                    // Update File to Object map
                    UpdateFileToObjectMap(internalName, objectIDs, m_WriteData.FileToObjects);
                }
            }
            return ReturnCode.Success;
        }

        internal static void PopulateReferencesMap<T>(T key, IList<ObjectIdentifier> objects, Dictionary<ObjectIdentifier, List<T>> map)
        {
            foreach (var obj in objects)
            {
                map.GetOrAdd(obj, out var set);
                set.Add(key);
            }
        }

        internal static void UpdateAssetToFilesMap(string file, List<GUID> assetsToUpdate, Dictionary<GUID, List<string>> AssetToFiles)
        {
            foreach (var asset in assetsToUpdate)
            {
                var assetFiles = AssetToFiles[asset];
                if (!assetFiles.Contains(file))
                    assetFiles.Add(file);
            }
        }

        internal static void RemoveObjectIDFromFiles(ObjectIdentifier objectID, List<string> files, Dictionary<string, List<ObjectIdentifier>> FileToObjects)
        {
            foreach (var file in files)
                FileToObjects[file].Remove(objectID);
        }

        internal static void UpdateFileToBundleMap(string bundleName, string file, Dictionary<string, string> FileToBundle, Dictionary<string, List<GUID>> BundleLayout)
        {
            if (!FileToBundle.ContainsKey(file))
            {
                FileToBundle.Add(file, bundleName);
                // NOTE: We want the output result to know about the new bundle, but since we are only 
                // assigning individual objects to this bundle and not full assets, the asset list will be empty
                BundleLayout.Add(bundleName, new List<GUID>());
            }
        }

        internal static void UpdateFileToObjectMap(string file, IEnumerable<ObjectIdentifier> newObjectIDs, Dictionary<string, List<ObjectIdentifier>> FileToObjects)
        {
            // This is called after remove, thus we can just AddRange as we already know these objects are not in any file
            FileToObjects.GetOrAdd(file, out var objectIDs);
            objectIDs.AddRange(newObjectIDs);
        }
    }
}
