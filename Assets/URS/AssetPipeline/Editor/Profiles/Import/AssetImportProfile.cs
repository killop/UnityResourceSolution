using System.Collections.Generic;
using System.IO;
using Daihenka.AssetPipeline.Filters;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Import
{
    public class AssetImportProfile : ScriptableObject
    {
        public bool enabled;
        public NamingConventionRule path = new NamingConventionRule();
        public List<PathFilter> pathExclusions = new List<PathFilter>();
        public List<AssetFilter> assetFilters = new List<AssetFilter>();

        public static AssetImportProfile Create(string filename)
        {
            var path = GenerateUniqueAssetPath(filename);
            var instance = CreateInstance<AssetImportProfile>();
            AssetDatabase.CreateAsset(instance, path);
            instance.path.name = "assetPath";
            return instance;
        }

        static AssetImportProfile[] s_CachedProfiles;
        public static AssetImportProfile[] AllProfiles => s_CachedProfiles ?? (s_CachedProfiles = AssetDatabaseUtility.FindAndLoadAssets<AssetImportProfile>());

        public static void InvalidateCachedProfiles()
        {
            s_CachedProfiles = null;
        }

        public bool IsMatch(string assetPath, bool mustBeEnabled = true)
        {
            return (!mustBeEnabled || enabled) && path.IsMatch(assetPath) && !IsExcludedPath(assetPath);
        }

        bool IsExcludedPath(string assetPath)
        {
            foreach (var exclusion in pathExclusions)
            {
                if (exclusion.IsMatch(assetPath))
                {
                    return true;
                }
            }

            return false;
        }


        static string GenerateUniqueAssetPath(string filename)
        {
            var path = AssetPipelineSettings.Settings.profileStoragePath;
            path = Path.Combine(path, filename).Replace(@"\", "/");
            if (Path.GetExtension(path) != ".asset")
            {
                path += ".asset";
            }

            var folderPath = Path.GetDirectoryName(path);
            PathUtility.CreateDirectoryIfNeeded(folderPath);

            path = AssetDatabase.GenerateUniqueAssetPath(path);
            return path;
        }

        internal static IList<AssetImportProfile> GetProfileMatches(string assetPath)
        {
            var result = new List<AssetImportProfile>();
            foreach (var profile in AllProfiles)
            {
                if (profile.IsMatch(assetPath))
                {
                    result.Add(profile);
                }
            }

            return result;
        }

        public void RemoveNullFilters()
        {
            assetFilters.RemoveAll(x => x == null);
        }
    }
}