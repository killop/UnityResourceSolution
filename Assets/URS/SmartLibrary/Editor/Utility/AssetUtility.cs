using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    internal static class AssetUtility
    {
        // Source: AssetDatabase.bindings.cs.
        private static Lazy<Func<string, int>> _getMainAssetInstanceID = new Lazy<Func<string, int>>(() => 
        (Func<string, int>)TypeAccessor.GetMethod<AssetDatabase>("GetMainAssetInstanceID")
        .CreateDelegate(typeof(Func<string, int>)));

        // Source: AssetPreview.bindings.cs
        private static MethodInfo _getAssetPreviewFromGUIDInfo;
        // Source: EditorGUIUtility.cs
        private static MethodInfo _loadIconInfo;

        public static Texture2D LoadBuiltinTypeIcon(Type type)
        {
            if (_loadIconInfo == null)
                _loadIconInfo = TypeAccessor.GetMethod<EditorGUIUtility>("LoadIcon");

            return (Texture2D)_loadIconInfo.Invoke(null, new object[] { type.FullName.Replace('.', '/') + " Icon" });
        }

        /// <summary>
        /// Returns the InstanceID of an asset object from the provided GUID.
        /// </summary>
        /// <param name="guid">The GUID of the asset to get the InstacneID of.</param>
        public static int GetMainAssetInstanceIDFromGUID(string guid)
        {
            return GetMainAssetInstanceID(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static int GetMainAssetInstanceID(string assetPath)
        {
            return _getMainAssetInstanceID.Value(assetPath);
        }


        public static Texture2D GetAssetPreviewFromGUID(string guid)
        {
            if (_getAssetPreviewFromGUIDInfo == null)
            {
                _getAssetPreviewFromGUIDInfo = TypeAccessor.GetMethod<AssetPreview>("GetAssetPreviewFromGUID", typeof(string));
            }

            return (Texture2D)_getAssetPreviewFromGUIDInfo.Invoke(null, new object[] { guid });
        }

        public static Texture2D GetAssetPreviewFromGUID(string guid, int ownerID)
        {
            if (_getAssetPreviewFromGUIDInfo == null)
            {
                _getAssetPreviewFromGUIDInfo = TypeAccessor.GetMethod<AssetPreview>("GetAssetPreviewFromGUID", typeof(string), typeof(int));
            }

            return (Texture2D)_getAssetPreviewFromGUIDInfo.Invoke(null, new object[] { guid, ownerID });
        }

        /// <summary>
        /// Returns the main asset object
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static Object LoadMainAssetFromGUID(string guid)
        {
            return AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static IEnumerable<Object> LoadAllAtPath(string path)
        {
            string dataPath = Application.dataPath;
            dataPath = dataPath.Remove(dataPath.Length - "assets".Length);

            string[] paths = Directory.GetFiles(Path.Combine(dataPath, path));

            List<Object> assets = new List<Object>();

            foreach (var filePath in paths)
            {
                if (Path.GetExtension(filePath) == ".meta")
                    continue;

                string temp = filePath.Replace("\\", "/");
                int index = temp.LastIndexOf("/");
                string localPath = path;

                if (index > 0)
                    localPath += temp.Substring(index);

                assets.Add(AssetDatabase.LoadMainAssetAtPath(localPath));
            }

            return assets;
        }

        public class LoadAssetScope : IDisposable
        {
            private bool _disposed;
            private bool _wasLoaded = false;
            private string _path;
            private Object _asset;

            public Object Asset
            {
                get { return _asset; }
            }

            public string Path
            {
                get { return _path; }
            }

            public LoadAssetScope(string guid)
            {
                _path = AssetDatabase.GUIDToAssetPath(guid);
                _wasLoaded = AssetDatabase.IsMainAssetAtPathLoaded(_path);
                _asset = AssetDatabase.LoadMainAssetAtPath(_path);
            }

            ~LoadAssetScope()
            {
                if (!_disposed)
                    CloseScope();
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                if (disposing)
                    CloseScope();

                _disposed = true;
            }

            private void CloseScope()
            {
                if (!_wasLoaded)
                {
                    if (Asset != null && !Asset.hideFlags.HasFlag(HideFlags.DontUnloadUnusedAsset) && !(Asset is GameObject) && !(Asset is Component) && !(Asset is AssetBundle))
                    {
                        Resources.UnloadAsset(Asset);
                    }
                }
            }
        }
    } 
}
