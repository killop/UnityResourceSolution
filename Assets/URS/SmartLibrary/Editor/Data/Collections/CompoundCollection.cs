using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    public enum AutoSourceOption { None, TopSubcollectionsOnly, AllSubcollections }

    public class CompoundCollection : LibraryCollection
    {
        [SerializeField] private List<LibraryCollection> _autoSourceCollections = new List<LibraryCollection>();
        [SerializeField] private List<LibraryCollection> _sourceCollections = new List<LibraryCollection>();
        [SerializeField] private SerializableDictionary<string, List<LibraryCollection>> _itemsSourcesMap = new SerializableDictionary<string, List<LibraryCollection>>();
        [SerializeField] private AutoSourceOption _autoSource = AutoSourceOption.TopSubcollectionsOnly;

        public AutoSourceOption AutoSource
        {
            get { return _autoSource; }
            set { UpdateGroupOption(value); }
        }

        private void OnEnable()
        {
            LibraryDatabase.ItemsChanged += OnLibraryItemsChanged;
            LibraryDatabase.HierarchyChanged += OnLibraryHierarchyChanged;
        }

        private void OnDisable()
        {
            LibraryDatabase.ItemsChanged -= OnLibraryItemsChanged;
            LibraryDatabase.HierarchyChanged -= OnLibraryHierarchyChanged;
        }

        /// <inheritdoc/>
        public override void UpdateItems(bool syn=false)
        {
            // The collection can be added through the inspector so we make sure to remove it incase that has happened.
            _sourceCollections.Remove(this);

            PrepareItemChange();

            var currentItems = new HashSet<LibraryItem>(this);
            var addedItems = new HashSet<LibraryItem>();
            var removedItems = new HashSet<LibraryItem>(this);

            ClearItems(false);
            _itemsSourcesMap.Clear();

            foreach (var source in _sourceCollections)
            {
                AddItemsFromSource(source);
            }

            foreach (var source in _autoSourceCollections)
            {
                AddItemsFromSource(source);
            }

            FinishChange();
            
            if (addedItems.Count > 0)
                NotifyItemsChanged(addedItems, LibraryItemsChangeType.Added);

            if (removedItems.Count > 0)
                NotifyItemsChanged(removedItems, LibraryItemsChangeType.Removed);

            // If the current sources have no items but did, we need to notify the removal of all items.
            if (Count == 0 && currentItems.Count > 0)
                NotifyItemsChanged(currentItems, LibraryItemsChangeType.Removed);

            void AddItemsFromSource(LibraryCollection source)
            {
                foreach (var item in source)
                {
                    if (AddSourcedItem(item, source))
                    {
                        removedItems.Remove(item);
                        if (!currentItems.Contains(item))
                            addedItems.Add(item);
                    }
                    else if (!addedItems.Contains(item) && currentItems.Contains(item))
                    {
                        removedItems.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="LibraryCollection"/> is in the <see cref="CompoundCollection"/> as an source of <see cref="LibraryItem"/>s.
        /// </summary>
        /// <param name="source">The <see cref="LibraryCollection"/> to locate in the <see cref="CompoundCollection"/> sources.</param>
        /// <returns><c>true</c> if <paramref name="source"/> is a <see cref="LibraryItem"/> source for the <see cref="CompoundCollection"/>; otherwise <c>false</c>.</returns>
        public bool ContainsItemSource(LibraryCollection source)
        {
            return _sourceCollections.Contains(source) || _autoSourceCollections.Contains(source);
        }

        /// <summary>
        /// Add the specified <see cref="LibraryCollection"/> as <see cref="LibraryItem"/> source for the <see cref="CompoundCollection"/>.
        /// </summary>
        /// <param name="source">The <see cref="LibraryCollection"/> to add as a <see cref="LibraryItem"/> source.</param>
        public void AddItemSource(LibraryCollection source)
        {
            if (ContainsItemSource(source) || source == this)
                return;

            int undoGroup = PrepareChange();
            _sourceCollections.Add(source);

            AddItemsFromSource(source, source);
            
            FinishChange(undoGroup);
        }

        private void AddItemAutoSource(LibraryCollection source)
        {
            int currentGroup = PrepareChange();
            if (!_autoSourceCollections.Contains(source))
            {
                if (_sourceCollections.Contains(source))
                    _sourceCollections.Remove(source);

                _autoSourceCollections.Add(source);

                AddItemsFromSource(source, source);
            }

            if (_autoSource == AutoSourceOption.AllSubcollections)
            {
                foreach (var subcollection in source.Subcollections)
                {
                    AddItemAutoSource(subcollection);
                }
            }
            FinishChange(currentGroup);
        }

        /// <summary>
        /// Add the specified items to the <see cref="CompoundCollection"/> if it does not already contain them, and registers the provided source with the items regardless.
        /// </summary>
        /// <param name="items">The <see cref="LibraryItem"/>s to add to the <see cref="CompoundCollection"/>.</param>
        /// <param name="source">The <see cref="LibraryCollection"/> to register with the items.</param>
        private void AddItemsFromSource(IEnumerable<LibraryItem> items, LibraryCollection source)
        {
            var addedItems = new List<LibraryItem>();
            PrepareItemChange();
            foreach (var item in items)
            {
                if (AddSourcedItem(item, source))
                    addedItems.Add(item);
            }
            FinishChange();

            if (addedItems.Count > 0)
                NotifyItemsChanged(addedItems, LibraryItemsChangeType.Added);
        }

        /// <summary>
        /// Adds the specified <see cref="LibraryItem"/> to the <see cref="CompoundCollection"/> if it does not already contain the item, registers the source with the item regardless.
        /// </summary>
        /// <remarks>Does not handle <see cref="Undo"/> and does not register the source if the item does not match the rules.</remarks>
        private bool AddSourcedItem(LibraryItem item, LibraryCollection source)
        {
            if (!Rules.Evaluate(item))
                return false;

            // Register the source with the item so we can keep track of which items are added from which collections.
            if (_itemsSourcesMap.TryGetValue(item.GUID, out List<LibraryCollection> sources))
            {
                // Add the source to the list of sources for the item if it isn't already in the list.
                if (!sources.Contains(source))
                    sources.Add(source);
            }
            else
            {
                // Add a new entry in the sources map for the item if it is not already in the group collection.
                sources = new List<LibraryCollection>();
                sources.Add(source);
                _itemsSourcesMap.Add(item.GUID, sources);
            }

            return AddItem(item, false);
        }

        public void RemoveItemsSource(LibraryCollection source)
        {
            if (_sourceCollections.Remove(source))
                RemoveSourceItems(source, source, true);
        }

        private void RemoveItemAutoSource(LibraryCollection source)
        {
            if (_autoSourceCollections.Remove(source))
                RemoveSourceItems(source, source, true);

            if (_autoSource != AutoSourceOption.AllSubcollections)
            {
                for (int i = _autoSourceCollections.Count - 1; i >= 0; i--)
                {
                    if (source.IsAncestorOf(_autoSourceCollections[i]))
                    {
                        _autoSourceCollections.RemoveAt(i);
                        RemoveSourceItems(source, source, true);
                    }
                }
            }
        }

        private List<LibraryItem> RemoveSourceItems(IEnumerable<LibraryItem> items, LibraryCollection source, bool notifyChange)
        {
            var removedItems = new List<LibraryItem>();

            PrepareItemChange();
            foreach (var item in items)
            {
                if (RemoveSourcedItem(item, source))
                    removedItems.Add(item); // Keep track of the items that were successfully removed so they can be used in the ItemsChanged event.
            }
            FinishChange();

            if (removedItems.Count > 0 && notifyChange)
                NotifyItemsChanged(removedItems, LibraryItemsChangeType.Removed);

            return removedItems;
        }

        private bool RemoveSourcedItem(LibraryItem item, LibraryCollection source)
        {
            if (!Contains(item))
                return false;

            if (_itemsSourcesMap.TryGetValue(item.GUID, out List<LibraryCollection> sources))
            {
                sources.Remove(source);
                if (sources.Count == 0)
                {
                    _itemsSourcesMap.Remove(item.GUID);
                    return RemoveItem(item, false);
                }
            }

            return false;
        }

        private void UpdateGroupOption(AutoSourceOption autoGrouping)
        {
            if (autoGrouping == _autoSource)
                return;

            int undoGroup = PrepareChange();

            AutoSourceOption previousAutoGroup = _autoSource;
            _autoSource = autoGrouping;

            if (autoGrouping == AutoSourceOption.TopSubcollectionsOnly)
            {
                if (previousAutoGroup == AutoSourceOption.AllSubcollections)
                {
                    // No need to add the subcollections if the last was AllSubcollections since they would already be added.
                    for (int i = _autoSourceCollections.Count - 1; i >= 0; i--)
                    {
                        if (_autoSourceCollections[i].Parent != this)
                            RemoveItemAutoSource(_autoSourceCollections[i]);
                    }
                }
                else if (previousAutoGroup == AutoSourceOption.None)
                {
                    foreach (var subcollection in Subcollections)
                    {
                        AddItemAutoSource(subcollection);
                    }
                }
            }
            else if (autoGrouping == AutoSourceOption.AllSubcollections)
            {
                foreach (var subcollection in Subcollections)
                {
                    AddItemAutoSource(subcollection);
                }
            }
            else if (autoGrouping == AutoSourceOption.None)
            {
                ClearAutoSources();
            }

            FinishChange(undoGroup);
        }

        private void ClearAutoSources()
        {
            var removedItems = new List<LibraryItem>(Count);
            for (int i = _autoSourceCollections.Count - 1; i >= 0; i--)
            {
                removedItems.AddRange(RemoveSourceItems(_autoSourceCollections[i], _autoSourceCollections[i], false));
                _autoSourceCollections.RemoveAt(i);
            }

            if (removedItems.Count > 0)
                NotifyItemsChanged(removedItems, LibraryItemsChangeType.Removed);
        }

        private void OnLibraryItemsChanged(LibraryItemsChangedEventArgs args)
        {
            // If the collection that whos items changed is not an items source we don't care about it.
            if (!ContainsItemSource(args.collection))
                return;

            // We collapse add/remove undo with the one that caused the change event because the group's items are tied to the source items.
            // PrepareItemChange increments the undo group after registering the undo, so to get the same undo group that caused the item change we subtract 2.
            int currentGroup = PrepareItemChange() - 2;
            if (args.type == LibraryItemsChangeType.Added)
            {
                AddItemsFromSource(args.items, args.collection);
            }
            else
            {
                RemoveSourceItems(args.items, args.collection, true);
            }
            FinishChange(currentGroup);
        }

        private void OnLibraryHierarchyChanged(LibraryHierarchyChangedEventArgs args)
        {
            // No auto grouping so no action is required and we can exit out now.
            if (_autoSource == AutoSourceOption.None)
                return;

            int currentGroup = Undo.GetCurrentGroup();

            // If the changed collection is in the autoSource list but is nolonger a decendent, we need to remove it from the list since only decendents can be in the list.
            if (_autoSourceCollections.Contains(args.subcollection))
            {
                if (!IsAncestorOf(args.subcollection) || args.type == HierarchyChangeType.Removed ||
                    (_autoSource == AutoSourceOption.TopSubcollectionsOnly && !Subcollections.Contains(args.subcollection)))
                {
                    // There are 5 undo increments between the start of a hierarchy remove change and the end. So we need to go back 5 to properly group the operations.
                    currentGroup -= 5;

                    RemoveItemAutoSource(args.subcollection);
                }
            }

            if ((_autoSource == AutoSourceOption.TopSubcollectionsOnly && args.collection == this) ||
                (_autoSource == AutoSourceOption.AllSubcollections && IsAncestorOf(args.subcollection)))
            {
                if (args.type == HierarchyChangeType.Added)
                {
                    // There are 4 undo increments between the start of a hierarchy add change and the end. So we need to go back 4 to properly group the operations.
                    currentGroup -= 4;

                    AddItemAutoSource(args.subcollection);
                }
            }

            Undo.CollapseUndoOperations(currentGroup);
        }
    }
}