using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    public class LibraryData : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        internal struct LibraryEntry
        {
            /// <summary>
            /// Ids of all of the collections that reference the asset.
            /// </summary>
            public List<int> collectionIds;
            /// <summary>
            /// The GUID of the asset.
            /// </summary>
            public LibraryItem item;

            public LibraryEntry(LibraryItem item, List<int> collectionIds)
            {
                this.item = item;
                this.collectionIds = collectionIds;
            }
        }

        private static LibraryData _instance;

        [SerializeField] private RootLibraryCollection _rootCollection;
        [HideInInspector]
        [SerializeField] private List<LibraryEntry> _serializedEntries = new List<LibraryEntry>();
        [SerializeField] private List<LibraryCollection> _collectionsToDestory = new List<LibraryCollection>();

        /// <summary>
        /// Dictionary of all items in the library with a a list of the IDs of every collection the item is in.
        /// </summary>
        public Dictionary<LibraryItem, List<int>> ItemCollectionIdsDictionary { get; } = new Dictionary<LibraryItem, List<int>>();

        public static Dictionary<string, LibraryItem> Items { get; set; } = new Dictionary<string, LibraryItem>();

        public static LibraryData Instance
        {
            get 
            {
                if (_instance == null)
                    SetInstance();
                return _instance; 
            }
        }

        public static IReadOnlyCollection<LibraryItem> AllItems
        {
            get { return Instance.ItemCollectionIdsDictionary.Keys; }
        }

        internal static void RegisterWithCollection(LibraryItem item, int collectionId)
        {
            if (Instance.ItemCollectionIdsDictionary.TryGetValue(item, out List<int> ids))
                ids.Add(collectionId);
            else
                Instance.ItemCollectionIdsDictionary.Add(item, new List<int>() { collectionId });
        }

        internal static void UnregisterWithCollection(LibraryItem item, int collectionId)
        {
            if (Instance.ItemCollectionIdsDictionary.TryGetValue(item, out List<int> ids))
            {
                ids.Remove(collectionId);
                if (ids.Count == 0)
                {
                    Instance.ItemCollectionIdsDictionary.Remove(item);
                }
            }
        }

        private static void SetInstance()
        {
            if (_instance != null)
                return;

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LibraryData)}");

            if (guids.Length > 0)
            {
                _instance = AssetDatabase.LoadAssetAtPath<LibraryData>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        /*
        internal static void ConvertToNewSave()
        {
            // If the folder already exists that means that we must have already converted.
            if (Directory.Exists(LibraryConstants.CollectionsPath))
                return;
            
            if (Instance == null)
                return;

            // We need to set the root collection to be the LibraryData root here
            // because EnumerateCollections uses the collections from the session data.

            RootLibraryCollection rootCollection = Instance._rootCollection;
            LibraryDatabase.RootCollection = rootCollection;
            rootCollection.SubcollectionIDs = rootCollection.SubcollectionsInternal.Select(c => c.ID).ToList();
            rootCollection.hideFlags = HideFlags.DontSave;
            rootCollection.CollectionName = "ROOT";

            AssetDatabase.RemoveObjectFromAsset(rootCollection);
            LibraryUtility.SaveCollection(rootCollection);

            foreach (var collection in rootCollection.LegacyIDToCollectionMap.Values)
            {
                SessionData.instance.IDToCollectionMap[collection.ID] = collection;
                
                collection.CollectionName = collection.name;
                collection.ParentID = collection.Parent.ID;
                collection.SubcollectionIDs = collection.SubcollectionsInternal.Select(c => c.ID).ToList();
                collection.hideFlags = HideFlags.DontSave;
                
                AssetDatabase.RemoveObjectFromAsset(collection);
                LibraryUtility.SaveCollection(collection);
            }

            foreach (LibraryEntry entry in Instance._serializedEntries)
            {
                // Convert from the legacy id system. to the new UniqueID system.
                List<UniqueID> collectionIDs = new List<UniqueID>(entry.collectionIds.Count);
                foreach (int legacyId in entry.collectionIds)
                {
                    collectionIDs.Add(LibraryDatabase.RootCollection.LegacyIDToCollectionMap[legacyId].ID);
                }
                
                LibraryDatabase.RootCollection.CollectionsContainingItems.Add(entry.item, collectionIDs);
                LibraryDatabase.RootCollection.AssetGuidToItem.Add(entry.item.GUID, entry.item);
            }

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Instance));
        }
        */
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _serializedEntries.Clear();
            if (ItemCollectionIdsDictionary == null)
                return;
            foreach (var pair in ItemCollectionIdsDictionary)
            {
                _serializedEntries.Add(new LibraryEntry(pair.Key, pair.Value));
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            ItemCollectionIdsDictionary.Clear();
            Items.Clear();
            foreach (var entry in _serializedEntries)
            {
                ItemCollectionIdsDictionary.Add(entry.item, entry.collectionIds);
                if (!Items.ContainsKey(entry.item.GUID))
                    Items.Add(entry.item.GUID, entry.item);
            }
        }
    }

}