using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Player;
using UnityEditor.Build.Utilities;
using UnityEngine;

internal class AutoBuildCacheUtility : IDisposable
{
    public AutoBuildCacheUtility()
    {
        BuildCacheUtility.ClearCacheHashes();
    }

    public void Dispose()
    {
        BuildCacheUtility.ClearCacheHashes();
    }
}

internal static class BuildCacheUtility
{
    static Dictionary<KeyValuePair<GUID, int>, CacheEntry> m_GuidToHash = new Dictionary<KeyValuePair<GUID, int>, CacheEntry>();
    static Dictionary<KeyValuePair<string, int>, CacheEntry> m_PathToHash = new Dictionary<KeyValuePair<string, int>, CacheEntry>();
    static Dictionary<KeyValuePair<Type, int>, CacheEntry> m_TypeToHash = new Dictionary<KeyValuePair<Type, int>, CacheEntry>();
    static Dictionary<ObjectIdentifier, Type[]> m_ObjectToType = new Dictionary<ObjectIdentifier, Type[]>();
    static TypeDB m_TypeDB;

#if !ENABLE_TYPE_HASHING
    static Hash128 m_UnityVersion = HashingMethods.Calculate(Application.unityVersion).ToHash128();
#endif

    public static CacheEntry GetCacheEntry(GUID asset, int version = 1)
    {
        CacheEntry entry;
        KeyValuePair<GUID, int> key = new KeyValuePair<GUID, int>(asset, version);
        if (m_GuidToHash.TryGetValue(key, out entry))
            return entry;

        entry = new CacheEntry { Guid = asset, Version = version };
        string path = AssetDatabase.GUIDToAssetPath(asset.ToString());
        entry.Type = CacheEntry.EntryType.Asset;

        if (path.Equals(CommonStrings.UnityBuiltInExtraPath, StringComparison.OrdinalIgnoreCase) || path.Equals(CommonStrings.UnityDefaultResourcePath, StringComparison.OrdinalIgnoreCase))
            entry.Hash = HashingMethods.Calculate(Application.unityVersion, path).ToHash128();
        else
        {
            entry.Hash = AssetDatabase.GetAssetDependencyHash(path);
            if (!entry.Hash.isValid && File.Exists(path))
                entry.Hash = HashingMethods.CalculateFile(path).ToHash128();
            if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                entry.Hash = HashingMethods.Calculate(entry.Hash, BuildInterfacesWrapper.SceneCallbackVersionHash).ToHash128();
        }

        if (entry.Hash.isValid)
            entry.Hash = HashingMethods.Calculate(entry.Hash, entry.Version).ToHash128();

        m_GuidToHash[key] = entry;
        return entry;
    }

    public static CacheEntry GetCacheEntry(string path, int version = 1)
    {
        CacheEntry entry;
        KeyValuePair<string, int> key = new KeyValuePair<string, int>(path, version);
        if (m_PathToHash.TryGetValue(key, out entry))
            return entry;

        var guid = AssetDatabase.AssetPathToGUID(path);
        if (!string.IsNullOrEmpty(guid))
            return GetCacheEntry(new GUID(guid), version);

        entry = new CacheEntry { File = path, Version = version };
        entry.Guid = HashingMethods.Calculate("FileHash", entry.File).ToGUID();
        if (File.Exists(entry.File))
            entry.Hash = HashingMethods.Calculate(HashingMethods.CalculateFile(entry.File), entry.Version).ToHash128();
        entry.Type = CacheEntry.EntryType.File;

        m_PathToHash[key] = entry;
        return entry;
    }

    public static CacheEntry GetCacheEntry(Type type, int version = 1)
    {
        CacheEntry entry;
        KeyValuePair<Type, int> key = new KeyValuePair<Type, int>(type, version);
        if (m_TypeToHash.TryGetValue(key, out entry))
            return entry;

        entry = new CacheEntry { ScriptType = type.AssemblyQualifiedName, Version = version };
        entry.Guid = HashingMethods.Calculate("TypeHash", entry.ScriptType).ToGUID();
#if ENABLE_TYPE_HASHING
        entry.Hash = ContentBuildInterface.CalculatePlayerSerializationHashForType(type, m_TypeDB);
#else
        entry.Hash = m_TypeDB != null ? m_TypeDB.GetHash128() : m_UnityVersion;
#endif
        entry.Type = CacheEntry.EntryType.ScriptType;

        m_TypeToHash[key] = entry;
        return entry;
    }

    static Type[] GetCachedTypesForObject(ObjectIdentifier objectId)
    {
        if (!m_ObjectToType.TryGetValue(objectId, out Type[] types))
        {
#if ENABLE_TYPE_HASHING
            types = ContentBuildInterface.GetTypesForObject(objectId);
#else
            types = ContentBuildInterface.GetTypeForObjects(new[] { objectId });
#endif
            m_ObjectToType[objectId] = types;
        }
        return types;
    }

    public static Type GetMainTypeForObject(ObjectIdentifier objectId)
    {
        Type[] types = GetCachedTypesForObject(objectId);
        return types[0];
    }

    public static Type[] GetMainTypeForObjects(IEnumerable<ObjectIdentifier> objectIds)
    {
        List<Type> results = new List<Type>();
        foreach (var objectId in objectIds)
        {
            Type[] types = GetCachedTypesForObject(objectId);
            results.Add(types[0]);
        }
        return results.ToArray();
    }

    public static Type[] GetSortedUniqueTypesForObject(ObjectIdentifier objectId)
    {
        Type[] types = GetCachedTypesForObject(objectId);
        Array.Sort(types, (x, y) => x.AssemblyQualifiedName.CompareTo(y.AssemblyQualifiedName));
        return types;
    }

    public static Type[] GetSortedUniqueTypesForObjects(IEnumerable<ObjectIdentifier> objectIds)
    {
        Type[] types;
        HashSet<Type> results = new HashSet<Type>();
        foreach (var objectId in objectIds)
        {
            types = GetCachedTypesForObject(objectId);
            results.UnionWith(types);
        }
        types = results.ToArray();
        Array.Sort(types, (x, y) => x.AssemblyQualifiedName.CompareTo(y.AssemblyQualifiedName));
        return types;
    }

    public static void SetTypeForObjects(IEnumerable<ObjectTypes> pairs)
    {
        foreach (var pair in pairs)
            m_ObjectToType[pair.ObjectID] = pair.Types;
    }

    internal static void ClearCacheHashes()
    {
        m_GuidToHash.Clear();
        m_PathToHash.Clear();
        m_TypeToHash.Clear();
        m_ObjectToType.Clear();
        m_TypeDB = null;
    }

    public static void SetTypeDB(TypeDB typeDB)
    {
        if (m_TypeToHash.Count > 0)
            throw new InvalidOperationException("Changing Player TypeDB mid build is not supported at this time.");
        m_TypeDB = typeDB;
    }

    public static CacheEntry GetCacheEntry(ObjectIdentifier objectID, int version = 1)
    {
        if (objectID.guid.Empty())
            return GetCacheEntry(objectID.filePath, version);
        return GetCacheEntry(objectID.guid, version);
    }
}
