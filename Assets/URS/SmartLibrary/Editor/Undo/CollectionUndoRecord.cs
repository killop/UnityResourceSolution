using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    [Serializable]
    internal struct CollectionUndoRecord
    {
        internal enum RecordType
        {
            ItemsChange,
            HierarchyChange,
            DestroyCollection
        }
        
        public int undoState;
        public RecordType recordType;
        public string collectionPath;
        public LibraryCollection collection;
        public LibraryCollection subcollection;
        public List<LibraryItem> items;
        public bool isAddOperation;
        public int index;
        public HierarchyChangeType hierarchyChangeType;

        public static CollectionUndoRecord CreateFromItemsChanged(LibraryItemsChangedEventArgs args)
        {
            return new CollectionUndoRecord()
            {
                undoState = CollectionsUndoState.instance.State,
                recordType = RecordType.ItemsChange,
                collection = args.collection,
                items = new List<LibraryItem>(args.items),
                isAddOperation = args.type == LibraryItemsChangeType.Added
            };
        }

        public static CollectionUndoRecord CreateFromHierarchyChanged(LibraryHierarchyChangedEventArgs args)
        {
            return new CollectionUndoRecord()
            {
                undoState = CollectionsUndoState.instance.State,
                recordType = RecordType.HierarchyChange,
                collection = args.collection,
                subcollection = args.subcollection,
                index = args.index,
                hierarchyChangeType = args.type,
            };
        }

        public static CollectionUndoRecord CreateForCollectionDestroy(string path)
        {
            return new CollectionUndoRecord()
            {
                undoState = CollectionsUndoState.instance.State,
                collectionPath = path,
                recordType = RecordType.DestroyCollection
            };
        }

        public LibraryItemsChangedEventArgs ToItemsChangedArgs()
        {
            var args = new LibraryItemsChangedEventArgs(items, collection,
                (LibraryItemsChangeType) (isAddOperation ? 1 : 0));
            isAddOperation = !isAddOperation;
            undoState = CollectionsUndoState.instance.State;
            return args;
        }

        public LibraryHierarchyChangedEventArgs ToHierarchyChangedArgs()
        {
            if (hierarchyChangeType == HierarchyChangeType.Moved)
                hierarchyChangeType = HierarchyChangeType.Moved;
            else
                hierarchyChangeType = hierarchyChangeType == HierarchyChangeType.Added
                    ? HierarchyChangeType.Removed
                    : HierarchyChangeType.Added;

            undoState = CollectionsUndoState.instance.State;
            var args = new LibraryHierarchyChangedEventArgs(subcollection, collection, index, hierarchyChangeType);

            return args;
        }
    }
}
