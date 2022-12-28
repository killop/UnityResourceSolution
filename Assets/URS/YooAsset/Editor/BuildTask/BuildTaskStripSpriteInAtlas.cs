using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Experimental;
using UnityEngine;

/// <summary>
    /// Removes all sprite source data in atlas.
    /// </summary>
    public class BuildTaskStripSpriteInAtlas : IBuildTask
    {
        public class Checker
        {
            private HashSet<GUID> atlasGUIDs;
            private HashSet<string> atlasAssetPaths;
            private HashSet<GUID> spriteGUIDs;
            private HashSet<string> spriteAssetPaths;

            void Init()
            {
                if (atlasAssetPaths != null)
                    return;

                atlasAssetPaths = new HashSet<string>();
                spriteGUIDs = new HashSet<GUID>();
                spriteAssetPaths = new HashSet<string>();
                atlasGUIDs = new HashSet<GUID>();
                foreach (var atlasGUID in AssetDatabase.FindAssets("t:spriteatlas"))
                {
                    atlasGUIDs.Add(new GUID(atlasGUID));
                    var atlasPath = AssetDatabase.GUIDToAssetPath(atlasGUID);
                    atlasAssetPaths.Add(atlasPath);
                    foreach (var dp in AssetDatabase.GetDependencies(atlasPath))
                    {
                        var guid = AssetDatabase.GUIDFromAssetPath(dp);
                        spriteAssetPaths.Add(dp);
                        spriteGUIDs.Add(guid);
                    }
                }

                foreach (var v in atlasGUIDs)
                    spriteGUIDs.Remove(v);
                
                foreach (var v in atlasAssetPaths)
                    spriteAssetPaths.Remove(v);
            }

            public bool IsSpriteInAtlas(string assetPath)
            {
                Init();
                return spriteAssetPaths.Contains(assetPath);
            }

            public HashSet<GUID> GetAllAtlas()
            {
                Init();
                return atlasGUIDs;
            }
            
            public HashSet<GUID> GetAllSpriteInAtlas()
            {
                Init();
                return spriteGUIDs;
            }
        }
        
        /// <inheritdoc />
        public int Version { get { return 2; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.InOut)]
        IBundleBuildContent m_Content;
        
        [InjectContext]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)]
        IBuildSpriteData m_SpriteData;
        
        [InjectContext(ContextUsage.InOut, true)]
        IBuildExtendedAssetData m_ExtendedAssetData;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            Checker checker = new Checker();
            SetOutputInformation(checker.GetAllAtlas(), checker.GetAllSpriteInAtlas());
            return ReturnCode.Success;
        }

        void SetOutputInformation(HashSet<GUID> atlasGUIDs, HashSet<GUID> spriteGUIDs)
        {
            // 把精灵引用全清掉
            m_Content.Assets.RemoveAll(spriteGUIDs.Contains);
            m_Content.Addresses.RemoveAll(spriteGUIDs.Contains);
            foreach (var bundle in m_Content.BundleLayout)
                bundle.Value.RemoveAll(spriteGUIDs.Contains);
            
            m_DependencyData.AssetInfo.RemoveAll(spriteGUIDs.Contains);
            m_DependencyData.AssetUsage.RemoveAll(spriteGUIDs.Contains);
            m_DependencyData.DependencyHash.RemoveAll(spriteGUIDs.Contains);
            m_SpriteData?.ImporterData.RemoveAll(spriteGUIDs.Contains);
            m_ExtendedAssetData?.ExtendedData.RemoveAll(spriteGUIDs.Contains);
            
            foreach (var asset in m_DependencyData.AssetInfo)
            {
                // atlas资源的精灵引用不能清除，不然精灵信息会丢失
                if (atlasGUIDs.Contains(asset.Key))
                    continue;
                asset.Value.includedObjects.RemoveAll(x => spriteGUIDs.Contains(x.guid));
                asset.Value.referencedObjects.RemoveAll(x => spriteGUIDs.Contains(x.guid));
            }
        }
    }
/// <summary>
/// Extension methods for generic dictionaries.
/// </summary>
public static class DicExtensions
{

    /// <summary>
    /// Works like List.RemoveAll.
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <param name="dictionary">Dictionary to remove entries from</param>
    /// <param name="match">Delegate to match keys</param>
    /// <returns>Number of entries removed</returns>
    public static int RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Predicate<TKey> match)
    {
        if (dictionary == null || match == null) return 0;
        var keysToRemove = dictionary.Keys.Where(k => match(k)).ToList();
        if (keysToRemove.Count > 0)
        {
            foreach (var key in keysToRemove)
            {
                dictionary.Remove(key);
            }
        }
        return keysToRemove.Count;
    }

}