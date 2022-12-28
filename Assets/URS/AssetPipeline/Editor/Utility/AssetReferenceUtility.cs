using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Daihenka.AssetPipeline.ReflectionMagic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Daihenka.AssetPipeline
{
    internal static class AssetReferenceUtility
    {
        static readonly Dictionary<string, List<string>> s_AssetReferenceCache = new Dictionary<string, List<string>>();
        static bool requiresAssetReferenceCacheRebuild => s_AssetReferenceCache.Keys.Count == 0;
        static readonly List<string> s_UnusedExtensions = new List<string> {".unity", ".prefab"};

        #region Unused Asset Search

        public static Dictionary<string, List<string>> FindUnusedAssets(string searchPath = null, int depth = 0, bool rebuildCache = false)
        {
            if (rebuildCache || requiresAssetReferenceCacheRebuild)
            {
                BuildAssetReferenceCache();
            }

            EditorUtility.DisplayProgressBar("Please wait...", "Finding Unused Assets", 0);
            var keys = s_AssetReferenceCache.Keys.OrderBy(x => x).ToArray();
            var strPathsCount = keys.Length.ToString();
            var progressMultiplier = 1f / keys.Length;

            var result = new Dictionary<string, List<string>>();
            for (var i = 0; i < keys.Length; i++)
            {
                if (i % 30 == 0)
                {
                    EditorUtility.DisplayProgressBar("Please wait...", $"Finding Unused Assets: {i + 1}/{strPathsCount}", (i + 1) * progressMultiplier);
                }

                var key = keys[i];
                var value = s_AssetReferenceCache[key].Where(x => x != key).ToList();

                if (!string.IsNullOrEmpty(searchPath) && !key.Contains(searchPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (value.Count > depth || value.Any(path => s_UnusedExtensions.Contains(Path.GetExtension(path).ToLower())))
                {
                    continue;
                }

                if (AssetDatabase.GetMainAssetTypeAtPath(key) == typeof(DefaultAsset))
                {
                    continue;
                }

                result.Add(key, value);
            }

            EditorUtility.ClearProgressBar();
            return result;
        }

        #endregion

        #region Duplicate Asset Search

        public static List<DuplicateAssets> FindDuplicateAssets(string searchPath = null)
        {
            var hashLookups = new Dictionary<string, List<string>>(2048);
            var duplicates = new List<DuplicateAssets>();

            try
            {
                var paths = AssetDatabase.GetAllAssetPaths();
                if (searchPath != null)
                {
                    paths = paths.Where(x => x.Contains(searchPath, StringComparison.OrdinalIgnoreCase)).ToArray();
                }

                var strPathsCount = paths.Length.ToString();
                var progressMultiplier = 1f / paths.Length;

                for (var i = 0; i < paths.Length; i++)
                {
                    if (i % 30 == 0 && EditorUtility.DisplayCancelableProgressBar("Please wait...", $"Searching: {i + 1}/{strPathsCount}", (i + 1) * progressMultiplier))
                    {
                        EditorUtility.ClearProgressBar();
                        throw new Exception("Search aborted");
                    }

                    if (string.IsNullOrEmpty(paths[i]) || !paths[i].StartsWith("Assets/"))
                    {
                        continue;
                    }

                    if (Directory.Exists(paths[i]))
                    {
                        continue;
                    }

                    var hash = CalculateFileHash(paths[i]);
                    if (!hashLookups.TryGetValue(hash, out var hashMatch))
                    {
                        hashMatch = new List<string>(1);
                        hashLookups[hash] = hashMatch;
                    }

                    hashMatch.Add(paths[i]);
                }
            }
            finally
            {
                FindDuplicatesInLookup(duplicates, hashLookups, true);
            }

            EditorUtility.ClearProgressBar();

            return duplicates;
        }

        static string CalculateFileHash(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream));
                }
            }
        }


        static void FindDuplicatesInLookup(List<DuplicateAssets> duplicates, Dictionary<string, List<string>> lookup, bool sortByExtension)
        {
            var duplicatePrevCount = duplicates.Count;
            foreach (var kvp in lookup)
            {
                var paths = kvp.Value;
                if (paths.Count > 1)
                {
                    paths.Sort();
                    duplicates.Add(new DuplicateAssets(paths));
                }
            }

            var count = duplicates.Count - duplicatePrevCount;
            if (count > 1)
            {
                duplicates.Sort(duplicatePrevCount, count, new DuplicateAssets.Comparer(sortByExtension));
            }
        }

        #endregion

        #region Asset Reference Replacement

        public static void ReplaceAssetReferences(string[] sourceAssetPaths, string replacementAssetPath)
        {
            if (string.IsNullOrEmpty(replacementAssetPath) || sourceAssetPaths == null || sourceAssetPaths.Length == 0 || sourceAssetPaths.All(x => string.IsNullOrEmpty(x)))
            {
                return;
            }

            var sourceAssetsReferences = FindReferencesForAssets(sourceAssetPaths);
            var sourceAssetsReferenceKeys = sourceAssetsReferences.Keys.OrderBy(Path.GetExtension).ToArray();
            var sourceAssets = sourceAssetPaths.ToDictionary(x => x, x => AssetDatabase.LoadAllAssetsAtPath(x).ToList());
            var replacementAssets = AssetDatabase.LoadAllAssetsAtPath(replacementAssetPath);

            foreach (var sourceAssetPath in sourceAssetsReferenceKeys)
            {
                var referencedAssetPaths = sourceAssetsReferences[sourceAssetPath];
                foreach (var referencedAssetPath in referencedAssetPaths)
                {
                    var extension = Path.GetExtension(referencedAssetPath);
                    var isPrefab = extension == ".prefab";
                    var isScene = extension == ".unity";
                    if (isPrefab)
                    {
                        var prefab = PrefabUtility.LoadPrefabContents(referencedAssetPath);
                        var wasModified = ReplaceObjectReferencesInGameObject(prefab, sourceAssets, replacementAssets);
                        if (wasModified)
                        {
                            EditorUtility.SetDirty(prefab);
                            PrefabUtility.SaveAsPrefabAsset(prefab, referencedAssetPath);
                            Debug.Log($"Saved prefab {referencedAssetPath} as {sourceAssetPath} was replaced", AssetDatabase.LoadMainAssetAtPath(referencedAssetPath));
                        }
                    }
                    else if (isScene)
                    {
                        var scene = EditorSceneManager.OpenScene(referencedAssetPath, OpenSceneMode.Single);
                        var objs = GetObjectReferencesInScene(sourceAssetPaths.ToList());
                        var wasModified = ReplaceObjectReferencesInGameObjects(objs, sourceAssets, replacementAssets);
                        if (wasModified)
                        {
                            EditorSceneManager.MarkSceneDirty(scene);
                            Debug.Log($"Saving scene {referencedAssetPath} as {sourceAssetPath} was replaced", AssetDatabase.LoadMainAssetAtPath(referencedAssetPath));
                            EditorSceneManager.SaveScene(scene);
                        }
                    }
                    else
                    {
                        var obj = AssetDatabase.LoadMainAssetAtPath(referencedAssetPath);
                        if (ReplaceObjectReferences(obj, sourceAssets, replacementAssets))
                        {
                            Debug.Log($"Saving prefab {referencedAssetPath} as {sourceAssetPath} was replaced", obj);
                            EditorUtility.SetDirty(obj);
                        }
                    }
                }
            }
        }

        static bool ReplaceObjectReferencesInGameObject(GameObject obj, Dictionary<string, List<Object>> sourceAssets, Object[] replacementAssets)
        {
            return ReplaceObjectReferencesInGameObjects(new[] {obj}, sourceAssets, replacementAssets);
        }

        static bool ReplaceObjectReferencesInGameObjects(IEnumerable<GameObject> objs, Dictionary<string, List<Object>> sourceAssets, Object[] replacementAssets)
        {
            var hasChanged = false;
            foreach (var obj in objs)
            {
                var comps = obj.GetComponentsInChildren<Component>();
                foreach (var comp in comps)
                {
                    if (!comp)
                    {
                        continue;
                    }

                    if (ReplaceObjectReferences(comp, sourceAssets, replacementAssets))
                    {
                        hasChanged = true;
                    }
                }
            }

            return hasChanged;
        }

        static GameObject[] GetObjectReferencesInScene(List<string> items)
        {
            var windows = (SearchableEditorWindow[]) Resources.FindObjectsOfTypeAll(typeof(SearchableEditorWindow));
            var hierarchy = windows.FirstOrDefault(x => x.GetType().ToString() == "UnityEditor.SceneHierarchyWindow");
            if (hierarchy == null)
            {
                return new GameObject[0];
            }

            var h = (hierarchy).AsDynamic();
            var setSearchType = typeof(SearchableEditorWindow).GetMethod("SetSearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = new List<GameObject>();
            foreach (var item in items)
            {
                object[] parameters = {$"ref:\"{item}\"", 0, false, false};
                setSearchType.Invoke(hierarchy, parameters);
                h.m_SceneHierarchy.SelectAll();
                result.AddRange(Selection.gameObjects);
            }

            return result.ToArray();
        }

        static bool ReplaceObjectReferences(Object obj, Dictionary<string, List<Object>> sources, Object[] replacements)
        {
            var hasReplacements = false;
            var so = new SerializedObject(obj);
            var sp = so.GetIterator();
            while (sp.NextVisible(true))
            {
                if (sp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var objRef = sp.objectReferenceValue;
                    if (!objRef)
                    {
                        continue;
                    }

                    foreach (var kvp in sources)
                    {
                        for (var i = 0; i < kvp.Value.Count; i++)
                        {
                            if (kvp.Value[i] == objRef)
                            {
                                var replacementObj = FindReplacementObject(kvp.Value[i], replacements);
                                if (replacementObj == null)
                                {
                                    throw new Exception("Assets do not match - likely import settings or sprite objects do not match");
                                }

                                sp.objectReferenceValue = replacementObj;
                                so.ApplyModifiedProperties();
                                hasReplacements = true;
                            }
                        }
                    }
                }
            }

            return hasReplacements;
        }

        static Object FindReplacementObject(Object source, Object[] replacements)
        {
            if (replacements.Length == 1 && !(replacements[0] is Sprite))
            {
                return replacements[0];
            }

            var sourceSo = new SerializedObject(source);
            var sourceSp = sourceSo.GetIterator();
            foreach (var replacement in replacements)
            {
                var repSo = new SerializedObject(replacement);
                var repSp = repSo.GetIterator();
                var match = true;
                while (repSp.NextVisible(true))
                {
                    var prop = sourceSo.FindProperty(repSp.propertyPath);
                    if (prop == null)
                    {
                        match = false;
                        break;
                    }

                    if (source is Sprite)
                    {
                        if (repSp.propertyPath == "m_RD.texture" || repSp.propertyPath.StartsWith("m_RenderDataKey.first.data"))
                        {
                            continue;
                        }
                    }

                    if (!SerializedPropertyEquals(prop, repSp))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return replacement;
                }

                sourceSp.Reset();
            }

            return null;
        }

        static bool SerializedPropertyEquals(SerializedProperty a, SerializedProperty b)
        {
            if (a.propertyType != b.propertyType)
            {
                return false;
            }

            switch (a.propertyType)
            {
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Gradient:
                    return true;
                case SerializedPropertyType.Integer:
                    return a.intValue == b.intValue;
                case SerializedPropertyType.Boolean:
                    return a.boolValue == b.boolValue;
                case SerializedPropertyType.Float:
                    return a.floatValue.Equals(b.floatValue);
                case SerializedPropertyType.String:
                    return a.stringValue == b.stringValue;
                case SerializedPropertyType.Color:
                    return a.colorValue == b.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return a.objectReferenceValue == b.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return a.intValue == b.intValue;
                case SerializedPropertyType.Enum:
                    return a.enumValueIndex == b.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return a.vector2Value == b.vector2Value;
                case SerializedPropertyType.Vector3:
                    return a.vector3Value == b.vector3Value;
                case SerializedPropertyType.Vector4:
                    return a.vector4Value == b.vector4Value;
                case SerializedPropertyType.Rect:
                    return a.rectValue == b.rectValue;
                case SerializedPropertyType.Character:
                    return a.stringValue == b.stringValue;
                case SerializedPropertyType.AnimationCurve:
                    return a.animationCurveValue.Equals(b.animationCurveValue);
                case SerializedPropertyType.Bounds:
                    return a.boundsValue == b.boundsValue;
                case SerializedPropertyType.Quaternion:
                    return a.quaternionValue == b.quaternionValue;
                case SerializedPropertyType.ExposedReference:
                    return a.exposedReferenceValue == b.exposedReferenceValue;
                case SerializedPropertyType.FixedBufferSize:
                    return a.fixedBufferSize == b.fixedBufferSize;
                case SerializedPropertyType.Vector2Int:
                    return a.vector2IntValue == b.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return a.vector3IntValue == b.vector3IntValue;
                case SerializedPropertyType.RectInt:
                    return a.rectIntValue.Equals(b.rectIntValue);
                case SerializedPropertyType.BoundsInt:
                    return a.boundsIntValue == b.boundsIntValue;
            }

            return false;
        }

        #endregion

        #region Asset References

        public static Dictionary<string, List<string>> FindReferencesForAssets(string[] assetPaths)
        {
            return assetPaths.ToDictionary(assetPath => assetPath, assetPath => FindReferencesForAsset(assetPath));
        }


        public static List<string> FindReferencesForAsset(string assetPath, bool buildCache = false)
        {
            if (buildCache || requiresAssetReferenceCacheRebuild)
            {
                BuildAssetReferenceCache();
            }

            return s_AssetReferenceCache.ContainsKey(assetPath) ? s_AssetReferenceCache[assetPath] : new List<string>();
        }

        public static void BuildAssetReferenceCache()
        {
            s_AssetReferenceCache.Clear();
            EditorUtility.DisplayProgressBar("Build Asset Reference Cache", "Retrieving asset paths", 0);
            var paths = AssetDatabase.GetAllAssetPaths();
            var strPathsCount = paths.Length.ToString();
            var progressMultiplier = 1f / paths.Length;
            for (var i = 0; i < paths.Length; i++)
            {
                if (i % 30 == 0)
                {
                    EditorUtility.DisplayProgressBar("Please wait...", $"Building Asset Reference Cache: {i + 1}/{strPathsCount}", (i + 1) * progressMultiplier);
                }

                var path = paths[i];
                var dependencies = AssetDatabase.GetDependencies(path);
                foreach (var dependency in dependencies)
                {
                    if (s_AssetReferenceCache.ContainsKey(dependency))
                    {
                        if (!s_AssetReferenceCache[dependency].Contains(path))
                        {
                            s_AssetReferenceCache[dependency].Add(path);
                        }
                    }
                    else
                    {
                        s_AssetReferenceCache[dependency] = new List<string> {path};
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        #endregion

        public static void RemoveAssetFromCache(string assetPath)
        {
            if (s_AssetReferenceCache.ContainsKey(assetPath))
            {
                s_AssetReferenceCache.Remove(assetPath);
            }

            foreach (var kvp in s_AssetReferenceCache)
            {
                if (kvp.Value.Contains(assetPath))
                {
                    kvp.Value.Remove(assetPath);
                }
            }
        }
    }

    internal class DuplicateAssets
    {
        public readonly List<string> paths;

        public DuplicateAssets(List<string> paths)
        {
            this.paths = paths;
        }

        public class Comparer : IComparer<DuplicateAssets>
        {
            public bool sortByExtension;

            public Comparer(bool sortByExtension)
            {
                this.sortByExtension = sortByExtension;
            }

            public int Compare(DuplicateAssets a, DuplicateAssets b)
            {
                if (sortByExtension)
                {
                    var ext1 = Path.GetExtension(a.paths[0]) ?? "";
                    var ext2 = Path.GetExtension(b.paths[0]) ?? "";
                    var comp = ext1.CompareTo(ext2);
                    if (comp != 0)
                    {
                        return comp;
                    }
                }

                return a.paths[0].CompareTo(b.paths[0]);
            }
        }
    }
}