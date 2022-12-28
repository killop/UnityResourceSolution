using System;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    internal static class AssetPreviewRef
    {
        private static Func<string, int, Texture2D> _getAssetPreviewFromGUID;
        private static Action<int, int> _setPreviewTextureCacheSize;
        private static Action<int> _deletePreviewTextureManagerByID;
        private static Func<int, Texture2D> _getMiniTypeThumbnailFromClassID;

        public static Texture2D GetAssetPreviewFromGUID(string guid, int clientID)
        {
            if (_getAssetPreviewFromGUID == null)
                _getAssetPreviewFromGUID = TypeAccessor.GetMethod<AssetPreview>("GetAssetPreviewFromGUID", typeof(string), typeof(int))
                    .CreateDelegate<Func<string, int, Texture2D>>();

            return _getAssetPreviewFromGUID(guid, clientID);
        }

        public static void SetPreviewTextureCacheSize(int size, int clientID)
        {
            if (_setPreviewTextureCacheSize == null)
                _setPreviewTextureCacheSize = TypeAccessor.GetMethod<AssetPreview>("SetPreviewTextureCacheSize", typeof(int), typeof(int))
                    .CreateDelegate<Action<int, int>>();

            _setPreviewTextureCacheSize(size, clientID);
        }

        public static void DeletePreviewTextureManagerByID(int clientID)
        {
            if (_deletePreviewTextureManagerByID == null)
                _deletePreviewTextureManagerByID = TypeAccessor.GetMethod<AssetPreview>("DeletePreviewTextureManagerByID", typeof(int))
                    .CreateDelegate<Action<int>>();

            _deletePreviewTextureManagerByID(clientID);
        }

        public static Texture2D GetMiniTypeThumbnailFromClassID(int classID)
        {
            if (_getMiniTypeThumbnailFromClassID == null)
                _getMiniTypeThumbnailFromClassID = TypeAccessor.GetMethod<AssetPreview>("GetMiniTypeThumbnailFromClassID", typeof(int))
                    .CreateDelegate<Func<int, Texture2D>>();

            return _getMiniTypeThumbnailFromClassID(classID);
        }
    }
}