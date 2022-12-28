using UnityEditor;

namespace Bewildered.SmartLibrary
{
    internal class LibraryAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Will be null when the LibraryData is first created.
            if (SessionData.instance == null)
                return;

            foreach (string path in importedAssets)
            {
                // Try adding all of the newly importated assets to all of the Smart Collections so that they don't need to do another search.
                var item = LibraryItem.GetItemInstance(AssetDatabase.AssetPathToGUID(path));
                foreach (SmartCollection collection in LibraryDatabase.GetAllCollectionsOfType<SmartCollection>())
                {
                    collection.IncrementalUpdateItem(item);
                }

                // Clears the type field since ScriptableObjects can have their type name changed.
                item.ClearNonSerializedFields();

                // The asset may have changed in a way that affects how it looks in a preview, so we need to force regenerate it to make sure it is up to date.
                AssetPreviewManager.ForceRegenerate(item.GUID);
            }

            foreach (string path in movedAssets)
            {
                var item = LibraryItem.GetItemInstance(AssetDatabase.AssetPathToGUID(path));
                item.ClearNonSerializedFields();
                foreach (SmartCollection collection in LibraryDatabase.GetAllCollectionsOfType<SmartCollection>())
                {
                    collection.IncrementalUpdateItem(item);
                }
            }

            foreach (var path in deletedAssets)
            {
#if UNITY_2021_1_OR_NEWER
                var guid = AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.IncludeRecentlyDeletedAssets);
#else
                var guid = AssetDatabase.AssetPathToGUID(path);
#endif
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    continue;
                }
                LibraryDatabase.RemoveItemFromLibrary(LibraryItem.GetItemInstance(guid));
                AssetPreviewManager.DeletePreviewTexture(guid);
            }
        }
    } 
}
