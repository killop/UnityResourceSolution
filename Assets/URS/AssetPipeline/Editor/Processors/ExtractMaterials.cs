using System.Collections.Generic;
using System.IO;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription(typeof(Material), ImportAssetTypeFlag.Models)]
    public class ExtractMaterials : AssetProcessor
    {
        [SerializeField] MaterialPathType pathType;
        [SerializeField] string destination;
        [SerializeField] DefaultAsset targetFolder;
        [SerializeField] FileExistsAction fileExistsAction;

        public override void OnPostprocess(Object asset, string assetPath)
        {
            var materialPaths = new HashSet<string>();
            var materials = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .Where(x => x.GetType() == typeof(Material));

            var destinationPath = GetDestinationPath(assetPath);
            foreach (var material in materials)
            {
                var materialPath = Path.Combine(destinationPath, $"{material.name}.mat");
                if (File.Exists(materialPath))
                {
                    if (fileExistsAction == FileExistsAction.Skip) { continue; }
                    if (fileExistsAction == FileExistsAction.UniquePath)
                    {
                        materialPath = AssetDatabase.GenerateUniqueAssetPath(materialPath);
                    }
                    else
                    {
                        File.Delete(materialPath);
                    }
                }

                var error = AssetDatabase.ExtractAsset(material, materialPath);
                if (string.IsNullOrEmpty(error))
                {
                    materialPaths.Add(assetPath);
                }
                else
                {
                    Debug.LogError($"[{GetName()}] Failed to extract material \"{material.name}\" from \"{assetPath}\": <b>{error}</b>");
                }
            }

            foreach (var path in materialPaths)
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            if (materialPaths.Count > 0)
            {
                Debug.Log($"[{GetName()}] Materials extracted from <b>{assetPath}</b> to <b>{destinationPath}</b>");
            }
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
        }

        string GetDestinationPath(string assetPath)
        {
            var destinationPath = destination;
            if (pathType == MaterialPathType.Absolute || pathType == MaterialPathType.Relative)
            {
                destinationPath = ReplaceVariables(destinationPath, assetPath);
            }

            destinationPath = pathType.GetFolderPath(assetPath, destinationPath, targetFolder);
            if (pathType == MaterialPathType.TargetFolder && !targetFolder)
            {
                Debug.LogWarning($"[{GetName()}] Target Folder was not set.  Extracting materials to <b>{destinationPath}</b>");
            }

            destinationPath = destinationPath.FixPathSeparators();
            PathUtility.CreateDirectoryIfNeeded(destinationPath);

            return destinationPath;
        }
    }
}