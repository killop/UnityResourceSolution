using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    internal class CollectionUndoManager : ScriptableSingleton<CollectionUndoManager>
    {
        [SerializeField] private int _undoState;
        [SerializeField] private UStack<CollectionUndoRecord> _collectionUndoRecords = new UStack<CollectionUndoRecord>();
        [SerializeField] private UStack<CollectionUndoRecord> _collectionRedoRecords = new UStack<CollectionUndoRecord>();

        public int UndoState
        {
            get { return _undoState; }
        }

        public UStack<CollectionUndoRecord> CollectionUndoRecords
        {
            get { return _collectionUndoRecords; }
        }

        public UStack<CollectionUndoRecord> CollectionRedoRecords
        {
            get { return _collectionRedoRecords; }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Undo.undoRedoPerformed += instance.OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            if (_undoState == CollectionsUndoState.instance.State)
                return;
            
            // Undo action.
            if (_undoState > CollectionsUndoState.instance.State)
            {
                // Multiple collection changes can be combined in to a single undo operation
                // like when removing a collection, so we need to support undoing all of them as well.
                while (_collectionUndoRecords.Count > 0 && _collectionUndoRecords.Peek().undoState !=
                    CollectionsUndoState.instance.State)
                {
                    UndoCollectionChange();
                }
            }

            // Redo action.
            if (_undoState < CollectionsUndoState.instance.State)
            {
                // Multiple collection changes can be combined in to a single undo operation
                // like when removing a collection, so we need to support undoing all of them as well.
                while (_collectionRedoRecords.Count > 0 && _collectionRedoRecords.Peek().undoState !=
                    CollectionsUndoState.instance.State)
                {
                    RedoCollectionChange();
                }
            }
                
            _undoState = CollectionsUndoState.instance.State;
        }

        /// <summary>
        /// Registers that a collection is about to be modified.
        /// </summary>
        internal static void RegisterUndo()
        {
            Undo.RegisterCompleteObjectUndo(CollectionsUndoState.instance, "Collection undo state modified");
            CollectionsUndoState.instance.State++;
            instance._undoState = CollectionsUndoState.instance.State;
            instance._collectionRedoRecords.Clear();
        }
        
        internal static void RecordItemChange(LibraryItemsChangedEventArgs args)
        {
            instance._collectionUndoRecords.Push(CollectionUndoRecord.CreateFromItemsChanged(args));
        }
        
        internal static void RecordHierarchyChange(LibraryHierarchyChangedEventArgs args)
        {
            instance._collectionUndoRecords.Push(CollectionUndoRecord.CreateFromHierarchyChanged(args));
        }

        internal static void RecordDestroyCollection(LibraryCollection collection)
        {
            instance._collectionUndoRecords.Push(CollectionUndoRecord.CreateForCollectionDestroy(LibraryUtility.GetCollectionPath(collection)));
        }
        
        private void UndoCollectionChange()
        {
            if (!ValidCollectionUndo())
                return;

            var collectionRecord = _collectionUndoRecords.Pop();
            
            if (collectionRecord.recordType == CollectionUndoRecord.RecordType.ItemsChange)
            {
                LibraryDatabase.HandleLibraryItemsChanged(collectionRecord.ToItemsChangedArgs(), false);
            }
            else if (collectionRecord.recordType == CollectionUndoRecord.RecordType.HierarchyChange)
            {
                LibraryDatabase.HandleLibraryHierarchyChanged(collectionRecord.ToHierarchyChangedArgs(), false);
            }
            else if (collectionRecord.recordType == CollectionUndoRecord.RecordType.DestroyCollection)
            {
                collectionRecord.undoState = CollectionsUndoState.instance.State;
                UniqueID id = LibraryUtility.GetCollectionIDFromPath(collectionRecord.collectionPath);
                LibraryUtility.SaveCollection(LibraryDatabase.FindCollectionByID(id));
            }
            _collectionRedoRecords.Push(collectionRecord);
        }

        private void RedoCollectionChange()
        {
            if (!ValidCollectionRedo())
                return;
            
            var collectionRecord = _collectionRedoRecords.Pop();
            
            if (collectionRecord.recordType == CollectionUndoRecord.RecordType.ItemsChange)
            {
                LibraryDatabase.HandleLibraryItemsChanged(collectionRecord.ToItemsChangedArgs(), false);
            }
            else if (collectionRecord.recordType == CollectionUndoRecord.RecordType.HierarchyChange)
            {
                LibraryDatabase.HandleLibraryHierarchyChanged(collectionRecord.ToHierarchyChangedArgs(), false);
            }
            else if (collectionRecord.recordType == CollectionUndoRecord.RecordType.DestroyCollection)
            {
                collectionRecord.undoState = CollectionsUndoState.instance.State;
                File.Delete(collectionRecord.collectionPath);
            }
            _collectionUndoRecords.Push(collectionRecord);
        }

        private bool ValidCollectionUndo()
        {
            return _collectionUndoRecords.Count > 0;
        }

        private bool ValidCollectionRedo()
        {
            return _collectionRedoRecords.Count > 0;
        }
    }
}
