using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityObject = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    [Serializable]
    internal class PreviewCache : ISerializationCallbackReceiver
    {
        public const int MinCacheSize = 50;

        private readonly static int defaultCacheSize = 500;

        private PreviewCachePriorityQueue _lastUsedPreviewQueue = new PreviewCachePriorityQueue(defaultCacheSize);

        [SerializeField] private SerializableDictionary<string, PreviewCacheNode> _cachedPreviews = new SerializableDictionary<string, PreviewCacheNode>();
        [SerializeField] private int _maxCacheSize = defaultCacheSize;

        public int Count
        {
            get { return _cachedPreviews.Count; }
        }

        public Texture2D GetCachedPreview(string guid)
        {
            if (_cachedPreviews.TryGetValue(guid, out PreviewCacheNode cacheNode))
            {
                if (cacheNode.Preview == null)
                    return null;

                _lastUsedPreviewQueue.UpdatePriority(cacheNode, EditorApplication.timeSinceStartup);
                return cacheNode.Preview;
            }

            return null;
        }

        public void CachePreview(string guid, Texture2D preview)
        {
            // This is needed because sometimes when exiting playmode the previews seem to not be loaded yet so it will get them again.
            // And when it does it will cache them. This results in the cache filling up and causes flickering.
            if (_cachedPreviews.TryGetValue(guid, out PreviewCacheNode previusNode))
            {
                _lastUsedPreviewQueue.Remove(previusNode);
            }

            // Make room for a new preview if the cache is full.
            while (_lastUsedPreviewQueue.Count >= _maxCacheSize)
            {
                DeleteOldestUsedPreview();
            }
            
            // Cache the preview.
            var cacheNode = new PreviewCacheNode(guid, preview);
            _lastUsedPreviewQueue.Enqueue(cacheNode, EditorApplication.timeSinceStartup);
            _cachedPreviews[guid] = cacheNode;
        }

        public void SetCacheSize(int size)
        {
            _maxCacheSize = Mathf.Max(size, MinCacheSize);
            if (_maxCacheSize != _lastUsedPreviewQueue.MaxSize)
            {
                while (_lastUsedPreviewQueue.Count > _maxCacheSize)
                {
                    DeleteOldestUsedPreview();
                }

                _lastUsedPreviewQueue.Resize(_maxCacheSize);
            }
        }

        public void ClearCache()
        {
            while (_lastUsedPreviewQueue.Count > 0)
            {
                DeleteOldestUsedPreview();
            }
        }

        private void DeleteOldestUsedPreview()
        {
            // Get and remove the node from the queue that the most time has past since it was last used.
            var node = _lastUsedPreviewQueue.Dequeue();
            _cachedPreviews.Remove(node.Guid);

            // Remove the preview texture from memory assuming the node has one.
            if (node.Preview && !EditorUtility.IsPersistent(node.Preview))
                UnityObject.DestroyImmediate(node.Preview);
        }

        internal PreviewCacheNode GetCacheNode(string guid)
        {
            _cachedPreviews.TryGetValue(guid, out PreviewCacheNode node);
            return node;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {

        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            foreach (var node in _cachedPreviews.Values)
            {
                _lastUsedPreviewQueue.Enqueue(node, node.Priority);
            }
        }
    } 
}
