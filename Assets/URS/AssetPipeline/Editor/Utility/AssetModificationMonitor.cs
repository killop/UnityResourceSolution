using Daihenka.AssetPipeline.Import;
using UnityEditor;

namespace Daihenka.AssetPipeline
{
    internal class AssetModificationMonitor : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(AssetProcessor))
                {
                    AssetImportProfile.InvalidateCachedProfiles();
                    break;
                }
            }

            return paths;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset is AssetImportProfile)
            {
                ImportProfilesWindow.ReloadTreeViewAfterFileDeletion(assetPath);
            }
            else if (asset is AssetProcessor)
            {
                AssetImportProfile.InvalidateCachedProfiles();
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }
}