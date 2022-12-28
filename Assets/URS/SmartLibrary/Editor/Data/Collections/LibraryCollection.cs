using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bewildered.SmartLibrary.UI;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Inherit from this class to implement a custom <see cref="LibraryItem"/> Collection type for the Smart Library.
    /// </summary>
    [Serializable]
    public abstract class LibraryCollection : ScriptableObject, IEnumerable<LibraryItem>, IReadOnlyCollection<LibraryItem>
    {
        [SerializeField] private RootLibraryCollection _root;
        [SerializeField] private LibraryCollection _parent;
        [SerializeField] private UniqueID _parentID; // Store because references to non-assets cannot be saved to disk.
        [SerializeField] private List<LibraryCollection> _subcollections = new List<LibraryCollection>();
        [SerializeField] private List<UniqueID> _subcollectionIDs = new List<UniqueID>(); // Store because references to non-assets cannot be saved to disk.

        [SerializeField] private string _collectionName;
        [SerializeField] private UniqueID _id = UniqueID.Empty;
        [SerializeField] private Texture2D _icon;
        [SerializeField] private RuleSet _rules = new RuleSet();

        [SerializeField] private AssetGUIDHashSet _items = new AssetGUIDHashSet();
        
        [SerializeField] private float _itemDisplaySize = 100;
        [SerializeField] private ItemsViewStyle _viewStyle;
        [SerializeField] private bool _useCollectionViewSettings = false;
        
        [NonSerialized] private Transform _defaultParentRef;

        internal RootLibraryCollection Root
        {
            get { return _root; }
            set { _root = value; }
        }

        public AssetGUIDHashSet GetGUIDHashSet()
        {
            return _items;
        }

        /// <summary>
        /// The parent of the <see cref="LibraryCollection"/>. <c>null</c> if the <see cref="LibraryCollection"/> is the root collection.
        /// </summary>
        public LibraryCollection Parent
        {
            get { return _parent; }
            internal set
            {
                _parent = value;
                if (_parent != null)
                    _parentID = _parent.ID;
                else
                    _parentID = UniqueID.Empty;
            }
        }

        internal UniqueID ParentID
        {
            get { return _parentID; }
            set { _parentID = value; }
        }

        internal List<LibraryCollection> SubcollectionsInternal
        {
            get { return _subcollections; }
            set { _subcollections = value; }
        }

        internal List<UniqueID> SubcollectionIDs
        {
            get { return _subcollectionIDs; }
            set { _subcollectionIDs = value; }
        }

        /// <summary>
        /// The direct subcollections of the <see cref="LibraryCollection"/>.
        /// </summary>
        public ReadOnlyCollection<LibraryCollection> Subcollections
        {
            get { return _subcollections.AsReadOnly(); }
        }

        /// <summary>
        /// The name of the <see cref="LibraryCollection"/>.
        /// </summary>
        /// <remarks>This should be used in place of <see cref="ScriptableObject.name"/>.</remarks>
        public string CollectionName
        {
            get { return _collectionName; }
            set
            {
                if (value == _collectionName)
                    return;
                
                // We don't want to move the file since it will most likely only be empty when first created.
                if (!string.IsNullOrEmpty(_collectionName))
                    LibraryUtility.RenameCollectionFile(this, value);
                
                _collectionName = value;
                name = value;
            }
        }
        
        /// <summary>
        /// The unique id of the <see cref="LibraryCollection"/>.
        /// </summary>
        public UniqueID ID
        {
            get
            {
                if (_id == UniqueID.Empty)
                    _id = UniqueID.NewUniqueId();
                
                return _id;
            }
        }

        public Texture2D Icon
        {
            get 
            {
                if (_icon)
                    return _icon;
                else
                    return DefaultIcon;
            }
            set { _icon = value; }
        }

        public Texture2D DefaultIcon
        {
            get
            {
                var scriptIcon = AssetPreview.GetMiniThumbnail(this);
                if (scriptIcon != LibraryConstants.ScriptableObjectIcon)
                    return scriptIcon;
                else
                    return (Texture2D)LibraryConstants.DefaultCollectionIcon;
            }
        }

        public RuleSet Rules
        {
            get { return _rules; }
        }
        
        /// <summary>
        /// The number of <see cref="LibraryItem"/>s that the <see cref="LibraryCollection"/> contains.
        /// </summary>
        public int Count
        {
            get { return _items.Count; }
        }

        public float ItemDisplaySize
        {
            get { return _itemDisplaySize; }
            set { _itemDisplaySize = value; }
        }

        public ItemsViewStyle ViewStyle
        {
            get { return _viewStyle; }
            set { _viewStyle = value; }
        }

        /// <summary>
        /// Determines whether the <see cref="LibraryCollection"/> uses the <see cref="ItemsViewStyle"/>
        /// and display size from the <see cref="SmartLibraryWindow"/> or from the <see cref="LibraryCollection"/>.
        /// </summary>
        public bool UseCollectionViewSettings
        {
            get { return _useCollectionViewSettings; }
            set { _useCollectionViewSettings = value; }
        }

        /// <summary>
        /// The <see cref="Transform"/> in the current scene that will be the
        /// parent of objects added from the <see cref="LibraryCollection"/>.
        /// </summary>
        public Transform DefaultSceneParent
        {
            get
            {
                if (_defaultParentRef == null)
                {
                    var defaultParents = GameObject.FindObjectsOfType<CollectionDefaultParent>();
                    foreach (var collectionDefaultParent in defaultParents)
                    {
                        if (collectionDefaultParent.CollectionIds.Contains(ID))
                        {
                            _defaultParentRef = collectionDefaultParent.transform;
                            break;
                        }
                    }
                }

                return _defaultParentRef;
            }
        }
        
        public static LibraryCollection CreateCollection<T>()
        {
            return CreateCollection(typeof(T));
        }

        public static LibraryCollection CreateCollection(Type collectionType)
        {
            if (!collectionType.IsSubclassOf(typeof(LibraryCollection)))
                return null;

            var collection = (LibraryCollection)CreateInstance(collectionType);

            collection._id = UniqueID.NewUniqueId();
            collection.CollectionName = ObjectNames.NicifyVariableName(collectionType.Name);
            collection.hideFlags = HideFlags.DontSave;

            return collection;
        }

        public static void DestroyCollection(LibraryCollection collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            
            int undoGroup = Undo.GetCurrentGroup();
            
            if (collection.Parent != null)
                collection.Parent.RemoveSubcollection(collection);
            
            // TODO: Remove cached info for collection.
            // TODO: Delete file as well.
            CollectionUndoManager.RegisterUndo();
            CollectionUndoManager.RecordDestroyCollection(collection);
            LibraryUtility.DeleteCollectionFile(collection);
            Undo.DestroyObjectImmediate(collection);

            Undo.CollapseUndoOperations(undoGroup);
            Undo.IncrementCurrentGroup();
        }

        /// <summary>
        /// Determines whether the specified <see cref="LibraryItem"/> can be added to the <see cref="LibraryCollection"/>.
        /// </summary>
        /// <param name="item">The <see cref="LibraryItem"/> to check if it can be added to the <see cref="LibraryCollection"/>.</param>
        /// <returns><c>true</c> if <paramref name="item"/> can be added; otherwise, <c>false</c>.</returns>
        public virtual bool IsAddable(LibraryItem item)
        {
            return !Contains(item) && Rules.Evaluate(item);
        }

        /// <summary>
        /// Determins whether the <see cref="LibraryCollection"/> contains the specified <see cref="LibraryItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="LibraryItem"/> to locate in the <see cref="LibraryCollection"/>.</param>
        /// <returns><c>true</c> if the <see cref="LibraryCollection"/> contains <paramref name="item"/>; otherwise, <c>false</c>.</returns>
        public bool Contains(LibraryItem item)
        {
            return _items.Contains(item.GUID);
        }

        public abstract void UpdateItems(bool syn= false);

        /// <summary>
        /// Adds the specified <see cref="LibraryItem"/> to the <see cref="LibraryCollection"/> if it is not already present.
        /// </summary>
        /// <remarks>Does not compare to rules, or record the modification so cannot be undone. Call <see cref="PrepareChange"/> first to support undo/redo and make sure the change is saved.</remarks>
        /// <param name="item">The <see cref="LibraryItem"/> to add to the <see cref="LibraryCollection"/>.</param>
        /// <param name="notifyChange">Whether to raise the <see cref="LibraryDatabase.ItemsChanged"/> event if the item was added.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is added to the <see cref="LibraryCollection"/>; otherwise, <c>false</c> if <paramref name="item"/> is aldreay present.</returns>
        protected bool AddItem(LibraryItem item, bool notifyChange = true)
        {
            bool added = _items.Add(item.GUID);
            if (added)
            {
                LibraryDatabase.RootCollection.RegisterWithCollection(item, ID);
                if (notifyChange)
                    NotifyItemsChanged(new LibraryItem[] { item }, LibraryItemsChangeType.Added);
            }
            return added;
        }

        /// <summary>
        /// Removes the specified <see cref="LibraryItem"/> to the <see cref="LibraryCollection"/>.
        /// </summary>
        /// <remarks>Does not record the modification so cannot be undone. Call <see cref="PrepareChange"/> first to support undo/redo and make sure the change is saved.</remarks>
        /// <param name="item">The <see cref="LibraryItem"/> to remove from the <see cref="LibraryCollection"/>.</param>
        /// <param name="notifyChange">Whether to raise the <see cref="LibraryDatabase.ItemsChanged"/> event if the item was added.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is successfully found and removed; otherwise, <c>false</c>. Also returns <c>false</c> if <paramref name="item"/> is not found.</returns>
        protected internal bool RemoveItem(LibraryItem item, bool notifyChange = true)
        {
            bool removed = _items.Remove(item.GUID);
            if (removed)
            {
                LibraryDatabase.RootCollection.UnregisterWithCollection(item, ID);
                if (notifyChange)
                    NotifyItemsChanged(new LibraryItem[] { item }, LibraryItemsChangeType.Removed);
            }
            return removed;
        }

        /// <summary>
        /// Removes all <see cref="LibraryItem"/>s from the <see cref="LibraryCollection"/>.
        /// </summary>
        /// <remarks>Does not record the modification so cannot be undone. Call <see cref="PrepareChange"/> first to support undo/redo and make sure the change is saved.</remarks>
        /// <param name="notifyChange">Whether to raise the <see cref="LibraryDatabase.ItemsChanged"/> event if the item was added.</param>
        protected void ClearItems(bool notifyChange = true)
        {
            List<LibraryItem> itemsCopy = null;
            if (notifyChange)
                itemsCopy = new List<LibraryItem>(this);

            foreach (var item in _items)
            {
                LibraryDatabase.RootCollection.UnregisterWithCollection(LibraryItem.GetItemInstance(item), ID);
            }
            _items.Clear();

            if (notifyChange)
                NotifyItemsChanged(itemsCopy, LibraryItemsChangeType.Removed);
        }

        /// <summary>
        /// Adds the specified <see cref="LibraryCollection"/> as a subcollection to the <see cref="LibraryCollection"/>. Removes it from it's previus parent <see cref="LibraryCollection"/> if it has one.
        /// </summary>
        /// <param name="collection">The <see cref="LibraryCollection"/> to add as a subcollection.</param>
        public void AddSubcollection(LibraryCollection collection)
        {
            InsertSubcollection(_subcollections.Count, collection);
        }

        public void InsertSubcollection(int index, LibraryCollection collection)
        {
            if (collection == null || !collection.IsValidReparenting(this))
                return;

            int undoGroup = PrepareChange();
            collection.PrepareChange();

            // Remove the subcollection from it's current root if it has one and is different from this root.
            if (collection.Root != Root && collection.Root != null)
            {
                collection.Root.RemoveCollectionFromTree(collection);
            }

            // Remove the subcollection from it's current parent if it has one.
            if (collection.Parent != null)
            {
                collection.Parent.PrepareChange();
                collection.Parent._subcollections.Remove(collection);
                collection.Parent._subcollectionIDs.Remove(collection.ID);
                collection._parent = null;
            }

            if (Root != null && collection.Root != Root)
            {
                Root.AddCollectionToTree(collection);
            }

            AddSubcollectionReference(index, collection);

            //TODO: Replace
            //LibraryData.DequeCollectionDestruction(collection);

            FinishChange(undoGroup);

            if (collection.Count > 0)
                LibraryDatabase.HandleLibraryItemsChanged(new LibraryItemsChangedEventArgs(collection, collection, LibraryItemsChangeType.Added));
            
            NotifySubcollectionsChanged(collection, index, HierarchyChangeType.Added);
        }

        private void AddSubcollectionReference(int index, LibraryCollection collection)
        {
            _subcollections.Insert(index, collection);
            _subcollectionIDs.Insert(index, collection.ID);
            
            collection._parent = this;
            collection._parentID = ID;
        }

        public void RemoveSubcollection(LibraryCollection collection)
        {
            if (!_subcollections.Contains(collection) || collection == this)
                return;

            var undoGroup = PrepareChange();
            collection.PrepareChange();

            // Handle the actual removal from the collection.
            if (collection.Root != null)
                collection.Root.RemoveCollectionFromTree(collection);

            _subcollections.Remove(collection);
            _subcollectionIDs.Remove(collection.ID);
            
            collection._parent = null;
            collection._parentID = UniqueID.Empty;
            
            FinishChange(undoGroup);

            // Notify change events.
            if (collection.Count > 0)
                LibraryDatabase.HandleLibraryItemsChanged(new LibraryItemsChangedEventArgs(collection, collection, LibraryItemsChangeType.Removed));
            
            NotifySubcollectionsChanged(collection, -1, HierarchyChangeType.Removed);
        }

        private bool IsValidReparenting(LibraryCollection newParent)
        {
            if (newParent == null)
                return false;

            // Go through all of the ancestors of the collection trying to be parented to. 
            // If this collection is one of them return false as parenting to it would create a infinit loop.
            for (var ancestor = newParent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor == this)
                    return false;
            }

            if (newParent._subcollections.Contains(this))
                return false;

            return true;
        }

        /// <summary>
        /// Returns the index of the <see cref="LibraryCollection"/> reletive to it's siblings.
        /// </summary>
        /// <returns>The index of the <see cref="LibraryCollection"/> in it's parent <see cref="LibraryCollection"/>.</returns>
        public int GetSiblingIndex()
        {
            if (Parent == null)
                return -1;
            return Parent._subcollections.IndexOf(this);
        }

        public void MoveSubcollection(int newIndex, int oldIndex)
        {
            if (newIndex == oldIndex)
                return;

            var collection = _subcollections[oldIndex];

            PrepareChange();
            _subcollections.RemoveAt(oldIndex);
            _subcollectionIDs.RemoveAt(oldIndex);

            if (newIndex > oldIndex)
                newIndex--;

            _subcollections.Insert(newIndex, collection);
            _subcollectionIDs.Insert(newIndex, collection.ID);
            FinishChange();

            NotifySubcollectionsChanged(collection, oldIndex, HierarchyChangeType.Moved);
        }

        private void NotifySubcollectionsChanged(LibraryCollection subcollection, int index, HierarchyChangeType type)
        {
            LibraryDatabase.HandleLibraryHierarchyChanged(new LibraryHierarchyChangedEventArgs(subcollection, this, index, type));
        }

        /// <summary>
        /// Notifies the <see cref="LibraryDatabase"/> that items have been added/removed from the <see cref="LibraryCollection"/>.
        /// </summary>
        /// <param name="items">The <see cref="LibraryItem"/>s that were added/removed from the <see cref="LibraryCollection"/>.</param>
        /// <param name="changeType">Whether the <paramref name="items"/> where added or removed from the <see cref="LibraryCollection"/>.</param>
        protected void NotifyItemsChanged(IReadOnlyCollection<LibraryItem> items, LibraryItemsChangeType changeType)
        {
            LibraryDatabase.HandleLibraryItemsChanged(new LibraryItemsChangedEventArgs(items, this, changeType));
        }

        /// <summary>
        /// Performs the specified action recursively on each subcollection of the <see cref="LibraryCollection"/>.
        /// </summary>
        /// <param name="action">The <see cref="Action{T}"/> delegate to perform on each subcollection.</param>
        public void ForEachSubcollectionRecursive(Action<LibraryCollection> action)
        {
            if (action == null)
                return;

            foreach (var collection in _subcollections)
            {
                action(collection);
                collection.ForEachSubcollectionRecursive(action);
            }
        }

        public bool IsAncestorOf(LibraryCollection other)
        {
            LibraryCollection parent = other;
            while (parent != null)
            {
                if (parent == this)
                    return true;
                else
                    parent = parent.Parent;
            }

            return false;
        }

        public Transform GetCreateDefaultSceneParent()
        {
            if (DefaultSceneParent != null)
                return DefaultSceneParent;

            _defaultParentRef = new GameObject(name).transform;
            var defaultParent = _defaultParentRef.gameObject.AddComponent<CollectionDefaultParent>();
            defaultParent.hideFlags = HideFlags.DontSaveInBuild | HideFlags.HideInInspector;
            defaultParent.CollectionIds.Add(ID);
            return _defaultParentRef;
        }

        /// <summary>
        /// Call before making a modification to the <see cref="LibraryCollection"/> to insure the changes are saved and support undo/redo.
        /// </summary>
        /// <remarks>Registers undo, and sets instance dirty.</remarks>
        /// <returns>The current <see cref="Undo"/> group index.</returns>
        public int PrepareChange()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            CollectionUndoManager.RegisterUndo();
            Undo.RegisterCompleteObjectUndo(this, "Library Collection Modified");
            EditorUtility.SetDirty(this);
            
            return undoGroup;
        }

        /// <summary>
        /// Call before making a modification to the <see cref="LibraryCollection"/> to insure the changes are saved and support undo/redo.
        /// Use if changing the <see cref="LibraryItem"/>s in the <see cref="LibraryCollection"/> to insure that ALL items library list will also support undoing/redoing the add/remove operation.
        /// </summary>
        /// <returns>The current <see cref="Undo"/> group index.</returns>
        protected int PrepareItemChange()
        {
            int undoGroup = PrepareChange();

            Undo.RegisterCompleteObjectUndo(LibraryDatabase.RootCollection, "Library Items Modified");

            return undoGroup;
        }

        public void FinishChange(int undoGroup = -1)
        {
            if (undoGroup > -1)
                Undo.CollapseUndoOperations(undoGroup);
            
            Undo.IncrementCurrentGroup();
        }

        public IEnumerator<LibraryItem> GetEnumerator()
        {
            var enumerator = _items.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return LibraryItem.GetItemInstance(enumerator.Current);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

      
    } 
}
