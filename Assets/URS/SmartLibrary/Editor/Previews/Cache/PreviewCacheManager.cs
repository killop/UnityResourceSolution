using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityObject = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    internal class PreviewCacheManager : ScriptableSingleton<PreviewCacheManager>
    {
        private const int sharedClientID = 0;

        [SerializeField] private SerializableDictionary<int, PreviewCache> _caches = new SerializableDictionary<int, PreviewCache>();
        
        public static Dictionary<int, PreviewCache> Caches
        {
            get { return instance._caches; }
        }

        public static Texture2D GetCachedPreview(string guid)
        {
            return GetCachedPreview(guid, sharedClientID);
        }

        public static Texture2D GetCachedPreview(string guid, int clientID)
        {
            if (Caches.TryGetValue(clientID, out PreviewCache cache))
            {
                return cache.GetCachedPreview(guid);
            }

            return null;
        }

        internal static IEnumerable<PreviewCacheNode> GetAllCachedNodesFor(string guid)
        {
            List<PreviewCacheNode> cachedPreviews = new List<PreviewCacheNode>();
            foreach (PreviewCache cache in Caches.Values)
            {
                var node = cache.GetCacheNode(guid);
                if (node != null)
                    cachedPreviews.Add(node);
            }

            return cachedPreviews;
        }

        public static void CachePreview(string guid, Texture2D preview)
        {
            CachePreview(guid, preview, sharedClientID);
        }

        public static void CachePreview(string guid, Texture2D preview, int clientID)
        {
            if (!Caches.TryGetValue(clientID, out PreviewCache cache))
            {
                cache = new PreviewCache();
                Caches.Add(clientID, cache);
            }

            cache.CachePreview(guid, preview);
        }

        public static void SetPreviewCacheSize(int size, int clientID)
        {
            if (!Caches.TryGetValue(clientID, out PreviewCache cache))
            {
                cache = new PreviewCache();
                Caches.Add(clientID, cache);
            }

            cache.SetCacheSize(size);
        }

        public static void ClearPreviewCache(int clientID)
        {
            if (Caches.TryGetValue(clientID, out PreviewCache cache))
            {
                cache.ClearCache();
                Caches.Remove(clientID);
            }
        }

        public static void ClearAllPreviewCaches()
        {
            foreach (PreviewCache cache in Caches.Values)
            {
                cache.ClearCache();
            }
        }
    } 
}
