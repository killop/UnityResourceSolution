using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Bewildered.SmartLibrary
{
    [InitializeOnLoad]
    public static class AssetPreviewManager
    {
        /// <summary>
        /// The longest a batch operation can take before holding for the next update.
        /// </summary>
        private const long maxBatchMilliseconds = 50;

        private static string _savePath;

        private static readonly HashSet<string> _renderlessAssets = new HashSet<string>();
        private static readonly HashSet<string> _regenerateRequests = new HashSet<string>();
        private static readonly HashSet<(string guid, int clientID)> _previewRequests = new HashSet<(string guid, int clientID)>();
        private static readonly Queue<(string guid, Texture2D preview)> _unsavedPreviews = new Queue<(string guid, Texture2D preview)>();
        private static bool _isLoadingPreviews = false;

        /// <summary>
        /// <c>true</c> if previews have been requested and is loading them; otherwise, <c>fasle</c>.
        /// </summary>
        public static bool IsLoadingPreviews
        {
            get { return _isLoadingPreviews; }
        }
        
        static AssetPreviewManager()
        {
            GetAndSetupSavePath();

            EditorApplication.update += PreviewTask;
        }

        public static Texture2D GetAssetPreview(string guid)
        {
            // Check if the preview generator supports the asset type. If it does not then we just return a preview from a default Unity method.
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (!Previewer.SupportedTypes.Contains(assetType))
            {
                return AssetUtility.GetAssetPreviewFromGUID(guid);
            }
            else
            {
                return Previewer.GenerateFromGuid(guid);
            }
        }

        public static Texture2D GetAssetPreview(string guid, int clientID)
        {
            return GetAssetPreview(guid, clientID, out _);
        }

        public static Texture2D GetAssetPreview(string guid, int clientID, out bool generated)
        {
            generated = false;
            if (clientID == 0)
                return GetAssetPreview(guid);

            Texture2D preview = PreviewCacheManager.GetCachedPreview(guid, clientID);
            if (preview != null)
            {
                if (!IsValidPreview(preview))
                    ForceRegenerate(guid);

                generated = true;
                return preview;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // Check if the preview generator supports the asset type. If it does not then we just return a preview from a default Unity method.
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (Previewer.SupportedTypes.Contains(assetType) && !_renderlessAssets.Contains(guid))
            {
                _previewRequests.Add((guid, clientID));
            }

            // AssetDatabase.GetCachedIcon returns blury for textures so we need to use the asset preview to get the preview for them.
            // However the AssetPreview has worse performance so we don't use it for all of them, it also doesn't return the right icon for things like AnimationController.
            if (assetType != null && assetType.IsSubclassOf(typeof(Texture)))
                return AssetPreviewRef.GetAssetPreviewFromGUID(guid, clientID);
            else
                return (Texture2D)AssetDatabase.GetCachedIcon(assetPath);
        }

        public static void ClearTemporaryAssetPreviews(int clientID)
        {
            PreviewCacheManager.ClearPreviewCache(clientID);
            AssetPreviewRef.DeletePreviewTextureManagerByID(clientID);
        }

        public static void ForceRegenerate(string guid)
        {
            _regenerateRequests.Add(guid);
        }

        public static void DeletePreviewTexture(string guid)
        {
            foreach (PreviewCache cache in PreviewCacheManager.Caches.Values)
            {
                Texture2D preview = cache.GetCachedPreview(guid);
                if (preview)
                {
                    Object.DestroyImmediate(preview);
                    string path = GetPreviewFilePath(guid);
                    if (File.Exists(path))
                        File.Delete(path);
                    return;
                }
            }
        }

        public static void DeleteAllPreviewTextures()
        {
            PreviewCacheManager.ClearAllPreviewCaches();
            
            string[] allPreviewFilePaths = Directory.GetFiles(_savePath);
            foreach (var previewPath in allPreviewFilePaths)
            {
                File.Delete(previewPath);
            }
        }

        public static void SetPreviewCacheSize(int size, int clientID)
        {
            PreviewCacheManager.SetPreviewCacheSize(size, clientID);
            AssetPreviewRef.SetPreviewTextureCacheSize(size, clientID);
        }
        
        private static void PreviewTask()
        {
            _isLoadingPreviews = _previewRequests.Count > 0 || _regenerateRequests.Count > 0;

            if (_previewRequests.Count > 0 || _unsavedPreviews.Count > 0 || _regenerateRequests.Count > 0)
            {
                // Ensure that the path is still valid. It can become invalid if the user manually deletes the cache folder.
                GetAndSetupSavePath();

                // The timer is used to keep track of how much time has been taken by the operations this frame.
                // If it reaches the max time while any of the tasks are being performed, they break and wait for the next update to resume.
                // This is to prevent the editor for lagging when doing either a high number of operations, and or expensive operations.
                var timer = new Stopwatch();
                timer.Start();

                LoadGeneratePreviewsTask(timer);
                SavePreviewsTask(timer);
                RegeneratePreviewsTask(timer);

                timer.Stop();
            }
        }

        private static void LoadGeneratePreviewsTask(Stopwatch timer)
        {
            foreach ((string guid, int clientID) in _previewRequests)
            {
                if (timer.ElapsedMilliseconds > maxBatchMilliseconds)
                    break;

                if (TryLoadTexture(guid, out Texture2D preview))
                {
                    PreviewCacheManager.CachePreview(guid, preview, clientID);
                    continue;
                }

                // Generate the preview for the asset.
                preview = Previewer.GenerateFromGuid(guid);
                // The preview will be null if the asset associated with the guid is not supported or does not have any render components.
                // We keep track of which guids don't have renderers so we will use unity's default thumbnails instead.
                if (preview == null)
                {
                    _renderlessAssets.Add(guid);
                    continue;
                }

                _unsavedPreviews.Enqueue((guid, preview));
                PreviewCacheManager.CachePreview(guid, preview, clientID);
            }

            // The previewRequests are simply cleared at the end so that it does not try to generate ones next update that are nolonger needed. Like when scrolling very fast.
            // The previews are requested in IMGUI so are requested each update, so only the ones that are actually needed should be in the requests set at the start of the task.
            _previewRequests.Clear();
        }

        private static void SavePreviewsTask(Stopwatch timer)
        {
            while (_unsavedPreviews.Count > 0)
            {
                if (timer.ElapsedMilliseconds > maxBatchMilliseconds)
                    break;

                (string guid, Texture2D preview) = _unsavedPreviews.Dequeue();

                if (!preview)
                    continue;

                File.WriteAllBytes(GetPreviewFilePath(guid), preview.EncodeToPNG());
            }
        }

        /// <summary>
        /// Process the requests for regenerating preview textures.
        /// </summary>
        private static void RegeneratePreviewsTask(Stopwatch timer)
        {
            var regeneratedGuids = new List<string>();
            foreach (var guid in _regenerateRequests)
            {
                if (timer.ElapsedMilliseconds > maxBatchMilliseconds)
                    break;
                
                // Get all nodes that node that have a preview for the asset.
                var cachedNodes = PreviewCacheManager.GetAllCachedNodesFor(guid);
                foreach (var node in cachedNodes)
                {
                    // Cleanup the previous preview texture before generate a new one.
                    Object.DestroyImmediate(node.Preview);
                    // Generate the new texture for the node and enqueue it to be saved.
                    node.Preview = Previewer.GenerateFromGuid(guid);
                    _unsavedPreviews.Enqueue((guid, node.Preview));
                }
                regeneratedGuids.Add(guid);
            }

            // Remove all requests that have been fullfilled this frame.
            _regenerateRequests.ExceptWith(regeneratedGuids);
        }

        /// <summary>
        /// Loads a <see cref="Texture2D"/> for the specified asset guid from a file if it exsits.
        /// </summary>
        /// <param name="guid">The asset guid to try to load the texture for.</param>
        /// <param name="preview">The loaded <see cref="Texture2D"/>, <c>null</c> if a texture file does not exist for the specified guid.</param>
        /// <returns><c>true</c> if a texture file exists for <paramref name="guid"/>; otherwise, <c>false</c>.</returns>
        private static bool TryLoadTexture(string guid, out Texture2D preview)
        {
            string path = GetPreviewFilePath(guid);
            
            if (File.Exists(path))
            {
                // Create an empty texture.
                preview = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
                // Load the preview texture file saved on disk, to the newly created empty texture.
                preview.LoadImage(File.ReadAllBytes(path));

                return true;
            }

            preview = null;
            return false;
        }

        private static void GetAndSetupSavePath()
        {
            if (!string.IsNullOrEmpty(_savePath))
            {
                if (!Directory.Exists(_savePath))
                    Directory.CreateDirectory(_savePath);
            }
            else
            {
                string path = Application.dataPath;
                path = path.Remove(path.Length - "assets".Length);
                _savePath = path + "Library/SmartLibraryCache";
                
                if (!Directory.Exists(_savePath))
                    Directory.CreateDirectory(_savePath);
            }
        }

        private static string GetPreviewFilePath(string guid)
        {
            return $"{_savePath}/{guid}.png";
        }

        private static bool IsValidPreview(Texture2D preview)
        {
            if (preview == null)
                return false;

            if (preview.width != (int)LibraryPreferences.PreviewResolution || preview.height != (int)LibraryPreferences.PreviewResolution)
                return false;

            return true;
        }
    } 
}
