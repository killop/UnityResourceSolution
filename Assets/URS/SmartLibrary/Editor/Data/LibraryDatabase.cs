using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// An interface for accessing collections and items in the library and performing opertions on the library.
    /// </summary>
    public static class LibraryDatabase
    {
        /// <summary>
        /// Occurs when <see cref="LibraryItem"/>s are added or removed from a <see cref="LibraryCollection"/>.
        /// </summary>
        public static event Action<LibraryItemsChangedEventArgs> ItemsChanged;

        /// <summary>
        /// Occurs when a <see cref="LibraryCollection"/> is added, removed, or reordered within the <see cref="LibraryDatabase"/>.
        /// </summary>
        public static event Action<LibraryHierarchyChangedEventArgs> HierarchyChanged;

        /// <summary>
        /// All unique items that are in at least one <see cref="LibraryCollection"/> that is in the Library.
        /// </summary>
        public static IReadOnlyCollection<LibraryItem> AllItems
        {
            get { return RootCollection.CollectionsContainingItems.Keys; }
        }

        /// <summary>
        /// All top level <see cref="LibraryCollection"/>s that are the Library.
        /// </summary>
        public static ReadOnlyCollection<LibraryCollection> BaseCollections
        {
            get { return RootCollection.Subcollections; }
        }

        internal static RootLibraryCollection RootCollection
        {
            get { return SessionData.instance.RootCollection; }
            set { SessionData.instance.RootCollection = value; }
        }
        
        /// <summary>
        /// Find a <see cref="LibraryCollection"/> in the Library by it's ID.
        /// </summary>
        /// <param name="id">The ID of the <see cref="LibraryCollection"/> to locate.</param>
        /// <returns>
        /// The <see cref="LibraryCollection"/> with an ID that matches <paramref name="id"/>, <c>null</c>
        /// if a <see cref="LibraryCollection"/> with a matching id could not be found.
        /// </returns>
        public static LibraryCollection FindCollectionByID(UniqueID id)
        {
            if (SessionData.instance.IDToCollectionMap.TryGetValue(id, out LibraryCollection collection))
                return collection;
            else
                return null;
        }

        /// <summary>
        /// Get all the <see cref="LibraryCollection"/>s in the Library of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="LibraryCollection"/>s to get.</typeparam>
        /// <returns>All <see cref="LibraryCollection"/>s in the Library of type <typeparamref name="T"/>.</returns>
        public static IEnumerable<LibraryCollection> GetAllCollectionsOfType<T>() where T : LibraryCollection
        {
            if (SessionData.instance.TypeCollectionPairs.TryGetValue(typeof(T), out List<LibraryCollection> collections))
                return collections;
            else
                return new LibraryCollection[0];
        }

        /// <summary>
        /// Get all the <see cref="LibraryCollection"/>s in the Library of the specified type.
        /// </summary>
        /// <param name="type">The type of <see cref="LibraryCollection"/>s to get.</param>
        /// <returns>All <see cref="LibraryCollection"/>s in the Library of type <paramref name="type"/>.</returns>
        public static IEnumerable<LibraryCollection> GetAllCollectionsOfType(Type type)
        {
            if (!type.IsSubclassOf(typeof(LibraryCollection)) && type != typeof(LibraryCollection))
                return new LibraryCollection[0];

            if (SessionData.instance.TypeCollectionPairs.TryGetValue(type, out List<LibraryCollection> collections))
                return collections;
            else
                return new LibraryCollection[0];
        }
        
        /// <summary>
        /// Enumerates over every <see cref="LibraryCollection"/> in the library.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<LibraryCollection> EnumerateCollections()
        {
            return SessionData.instance.IDToCollectionMap.Values;
        }

        /// <summary>
        /// Add the specified <see cref="LibraryCollection"/> as a top level <see cref="LibraryCollection"/> in the Library.
        /// </summary>
        /// <param name="collection">The <see cref="LibraryCollection"/> to add.</param>
        public static void AddBaseCollection(LibraryCollection collection)
        {
            RootCollection.AddSubcollection(collection);
        }

        /// <summary>
        /// Remove the specified top level <see cref="LibraryCollection"/> from the Library.
        /// </summary>
        /// <param name="collection">The top level <see cref="LibraryCollection"/> to remove.</param>
        public static void RemoveBaseCollection(LibraryCollection collection)
        {
            RootCollection.RemoveSubcollection(collection);
        }

        internal static void RemoveItemFromLibrary(LibraryItem item)
        {
            if (RootCollection.CollectionsContainingItems.TryGetValue(item, out var collectionIds))
            {
                // Iterate in reverse because the RemoveItem method removes the collection id from the list.
                for (int i = collectionIds.Count - 1; i >= 0; i--)
                {
                    FindCollectionByID(collectionIds[i]).RemoveItem(item);
                }
            }
        }

        internal static void HandleLibraryItemsChanged(LibraryItemsChangedEventArgs args, bool recordCollectionChange = true)
        {
            if (recordCollectionChange)
                CollectionUndoManager.RecordItemChange(args);
            
            ItemsChanged?.Invoke(args);
        }

        internal static void HandleLibraryHierarchyChanged(LibraryHierarchyChangedEventArgs args, bool recordCollectionChange = true)
        {
            if (recordCollectionChange)
                CollectionUndoManager.RecordHierarchyChange(args);
            HierarchyChanged?.Invoke(args);
        }
    }

}