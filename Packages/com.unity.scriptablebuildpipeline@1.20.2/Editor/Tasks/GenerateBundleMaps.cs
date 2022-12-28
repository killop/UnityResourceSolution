using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Generates reference maps and usage sets for asset bundles.
    /// </summary>
    public class GenerateBundleMaps : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            Dictionary<string, WriteCommand> fileToCommand;
            Dictionary<string, HashSet<ObjectIdentifier>> forwardObjectDependencies;
            Dictionary<string, HashSet<string>> forwardFileDependencies;
            Dictionary<string, HashSet<GUID>> reverseAssetDependencies;

            // BuildReferenceMap details what objects exist in other bundles that objects in a source bundle depend upon (forward dependencies)
            // BuildUsageTagSet details the conditional data needed to be written by objects in a source bundle that is in used by objects in other bundles (reverse dependencies)
            using (m_Log.ScopedStep(LogLevel.Info, $"Temporary Map Creations"))
            {
                fileToCommand = m_WriteData.WriteOperations.ToDictionary(x => x.Command.internalName, x => x.Command);
                forwardObjectDependencies = new Dictionary<string, HashSet<ObjectIdentifier>>();
                forwardFileDependencies = new Dictionary<string, HashSet<string>>();
                reverseAssetDependencies = new Dictionary<string, HashSet<GUID>>();
                foreach (var pair in m_WriteData.AssetToFiles)
                {
                    GUID asset = pair.Key;
                    List<string> files = pair.Value;

                    // The includes for an asset live in the first file, references could live in any file
                    forwardObjectDependencies.GetOrAdd(files[0], out HashSet<ObjectIdentifier> objectDependencies);
                    forwardFileDependencies.GetOrAdd(files[0], out HashSet<string> fileDependencies);

                    // Grab the list of object references for the asset or scene and add them to the forward dependencies hash set for this file (write command)
                    if (m_DependencyData.AssetInfo.TryGetValue(asset, out AssetLoadInfo assetInfo))
                        objectDependencies.UnionWith(assetInfo.referencedObjects);
                    if (m_DependencyData.SceneInfo.TryGetValue(asset, out SceneDependencyInfo sceneInfo))
                        objectDependencies.UnionWith(sceneInfo.referencedObjects);

                    // Grab the list of file references for the asset or scene and add them to the forward dependencies hash set for this file (write command)
                    // While doing so, also add the asset to the reverse dependencies hash set for all the other files it depends upon.
                    // We already ensure BuildReferenceMap & BuildUsageTagSet contain the objects in this write command in GenerateBundleCommands. So skip over the first file (self)
                    for (int i = 1; i < files.Count; i++)
                    {
                        fileDependencies.Add(files[i]);
                        reverseAssetDependencies.GetOrAdd(files[i], out HashSet<GUID> reverseDependencies);
                        reverseDependencies.Add(asset);
                    }
                }
            }

            // Using the previously generated forward dependency maps, update the BuildReferenceMap per WriteCommand to contain just the references that we care about
            using (m_Log.ScopedStep(LogLevel.Info, $"Populate BuildReferenceMaps"))
            {
                foreach (var operation in m_WriteData.WriteOperations)
                {
                    var internalName = operation.Command.internalName;

                    BuildReferenceMap referenceMap = m_WriteData.FileToReferenceMap[internalName];
                    if (!forwardObjectDependencies.TryGetValue(internalName, out var objectDependencies))
                        continue; // this bundle has no external dependencies
                    if (!forwardFileDependencies.TryGetValue(internalName, out var fileDependencies))
                        continue; // this bundle has no external dependencies
                    foreach (string file in fileDependencies)
                    {
                        WriteCommand dependentCommand = fileToCommand[file];
                        foreach (var serializedObject in dependentCommand.serializeObjects)
                        {
                            // Only add objects we are referencing. This ensures that new/removed objects to files we depend upon will not cause a rebuild
                            // of this file, unless are referencing the new/removed objects.
                            if (!objectDependencies.Contains(serializedObject.serializationObject))
                                continue;

                            referenceMap.AddMapping(file, serializedObject.serializationIndex, serializedObject.serializationObject);
                        }
                    }
                }
            }

            // Using the previously generate reverse dependency map, create the BuildUsageTagSet per WriteCommand to contain just the data that we care about
            using (m_Log.ScopedStep(LogLevel.Info, $"Populate BuildUsageTagSet"))
            {
                foreach (var operation in m_WriteData.WriteOperations)
                {
                    var internalName = operation.Command.internalName;
                    BuildUsageTagSet fileUsage = m_WriteData.FileToUsageSet[internalName];
                    if (reverseAssetDependencies.TryGetValue(internalName, out var assetDependencies))
                    {
                        foreach (GUID asset in assetDependencies)
                        {
                            if (m_DependencyData.AssetUsage.TryGetValue(asset, out var assetUsage))
                                fileUsage.UnionWith(assetUsage);
                            if (m_DependencyData.SceneUsage.TryGetValue(asset, out var sceneUsage))
                                fileUsage.UnionWith(sceneUsage);
                        }
                    }
                    if (ReflectionExtensions.SupportsFilterToSubset)
                        fileUsage.FilterToSubset(m_WriteData.FileToObjects[internalName].ToArray());
                }
            }
            return ReturnCode.Success;
        }
    }
}
