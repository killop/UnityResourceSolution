using System.Collections.Generic;
using System.IO;
using Daihenka.AssetPipeline.Filters;
using UnityEngine;

namespace Daihenka.AssetPipeline.Import
{
    public class AssetFilter : ScriptableObject
    {
        public bool enabled;
        public bool showOptions;
        public NamingConventionRule file;
        public ImportAssetType assetType;
        public List<string> otherAssetExtensions;
        public List<string> subPaths = new List<string>();
        public List<PathFilter> fileExclusions = new List<PathFilter>();
        public List<AssetProcessor> assetProcessors;
        public AssetImportProfile parent;

        public bool IsMatch(string assetPath, bool mustBeEnabled = true)
        {
            var dirname = Path.GetDirectoryName(assetPath).FixPathSeparators();
            var filename = Path.GetFileNameWithoutExtension(assetPath);

            var isEnabled = !mustBeEnabled || enabled;
            var isValidFileExtension = assetType.IsValidFileExtension(assetPath, otherAssetExtensions);
            var isNotExcluded = IsValidSubPath(dirname) && !IsExcludedFile(filename);
            var isFileValid = file.IsValid && file.IsMatch(filename);
           // Debug.LogError("assetPath    "+ assetPath+"   isEnabled " + isEnabled+ " isValidFileExtension "+ isValidFileExtension+ " isNotExcluded "+ isNotExcluded+ " isFileValid "+ isFileValid);
            return isEnabled && isValidFileExtension && isNotExcluded && isFileValid;
        }

        bool IsValidSubPath(string assetPath)
        {
            if (subPaths.Count == 0) return true;
            foreach (var subPath in subPaths)
            {
                if (assetPath.Contains(subPath))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsExcludedFile(string assetPath)
        {
            foreach (var exclusion in fileExclusions)
            {
                if (exclusion.IsMatch(assetPath))
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveNullProcessors()
        {
            assetProcessors.RemoveAll(x => x == null);
        }
    }
}