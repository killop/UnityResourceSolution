using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Daihenka.AssetPipeline.Import;

namespace Daihenka.AssetPipeline
{
    internal static class ImportAssetTypeUtility
    {
        internal static ImportAssetType GetImportType(this string assetPath)
        {
            var extension = Path.GetExtension(assetPath);
            var assetTypeFileExtensions = AssetPipelineSettings.Settings.assetTypeFileExtensions;
            foreach (var typeExtensions in assetTypeFileExtensions.Where(typeExtensions => typeExtensions.FileExtensions.Contains(extension) || typeExtensions.FileExtensions.Contains(extension.Substring(1))))
            {
                return typeExtensions.AssetType;
            }

            return ImportAssetType.Other;
        }

        internal static bool IsValidFileExtension(this ImportAssetType assetType, string assetPath, IEnumerable<string> otherExtensions = null)
        {
            var assetExtension = Path.GetExtension(assetPath).ToLowerInvariant();
            if (AssetPipelineSettings.Settings.assetTypeFileExtensions.Count > (int) assetType)
            {
                var extensions = AssetPipelineSettings.Settings.assetTypeFileExtensions[(int) assetType].FileExtensions.Where(x => !string.IsNullOrWhiteSpace(x));
                return extensions.Contains(assetExtension) || assetExtension.StartsWith(".") && extensions.Contains(assetExtension.Substring(1));
            }

            return assetType == ImportAssetType.Other && otherExtensions != null && (otherExtensions.Contains(assetExtension) || (assetExtension.StartsWith(".") && otherExtensions.Contains(assetExtension.Substring(1))));
        }

        internal static bool IsValidAssetType(this Type type, ImportAssetType assetType)
        {
            var attr = type.GetCustomAttribute<AssetProcessorDescriptionAttribute>();
            return attr == null || attr.ValidAssetTypes.HasFlag((ImportAssetTypeFlag) (1 << (int) assetType));
        }
    }
}