using UnityEngine.UIElements;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    internal class CollectionTreeItem : VisualElement
    {
        public static readonly string CollectionTreeItemUssClassName = "bewildered-collection-tree-item";

        private static readonly string CollectionIconElementName = "collectionIcon";
        private static readonly string CollectionNameFieldElementName = "collectionName";
        private static readonly string ItemCountElementName = "itemCount";
        private static readonly string _collectionSettingsUssClassName = "bewildered-library-collection-settings";

        private LibraryTreeViewItem _treeItem;

        internal RenamableLabel Label { get; }

        public LibraryCollection Collection
        {
            get 
            {
                if (_treeItem is CollectionTreeViewItem collectionTreeViewItem)
                    return collectionTreeViewItem.Collection;
                else
                    return null;
            }
        }

        public CollectionTreeItem()
        {
            AddToClassList(CollectionTreeItemUssClassName);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            // Icon for the collection.
            var collectionIcon = new Image();
            collectionIcon.name = CollectionIconElementName;
            Add(collectionIcon);

            // Name label for collection.
            Label = new RenamableLabel();
            Label.name = CollectionNameFieldElementName;
            Label.RegisterValueChangedCallback(OnLabelValueChanged);
            Add(Label);

            var itemCountLabel = new Label();
            itemCountLabel.name = ItemCountElementName;
            Add(itemCountLabel);

            var settingsElement = new Image();
            settingsElement.AddToClassList(_collectionSettingsUssClassName);
            settingsElement.AddToClassList(LibraryConstants.IconUssClassName);
            settingsElement.image = LibraryConstants.CollectionSettingsIcon;
            settingsElement.RegisterCallback<MouseDownEvent>(OnSettingsSelected);
            Add(settingsElement);
        }

        public void BindToItem(LibraryTreeViewItem treeViewItem)
        {
            _treeItem = treeViewItem;
            if (_treeItem == null)
                return;
            
            Label.text = _treeItem.Name;
            if (_treeItem is CollectionTreeViewItem collectionItem)
                this.Q<Image>(CollectionIconElementName).image = collectionItem.Collection.Icon;
            else
                this.Q<Image>(CollectionIconElementName).image = LibraryConstants.DefaultCollectionIcon;
            this.Q<Label>(ItemCountElementName).text = $"{_treeItem.Count}";
            Label.canBeRenamed = _treeItem is CollectionTreeViewItem;
        }

        private void OnSettingsSelected(MouseDownEvent evt)
        {
            if (Collection != null)
                Selection.activeObject = Collection;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            LibraryDatabase.ItemsChanged += OnLibraryItemsChanged;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            LibraryDatabase.ItemsChanged -= OnLibraryItemsChanged;
        }

        private void OnLibraryItemsChanged(LibraryItemsChangedEventArgs args)
        {
            if (args.collection == Collection || _treeItem is AllItemsTreeViewItem)
                this.Q<Label>(ItemCountElementName).text = $"{_treeItem.Count}";
        }

        private void OnLabelValueChanged(ChangeEvent<string> evt)
        {
            if (string.IsNullOrEmpty(evt.newValue))
            {
                evt.PreventDefault();
                evt.StopPropagation();
                return;
            }
            else if (_treeItem != null)
            {
                _treeItem.Name = evt.newValue;
            }
        }
        
        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (DragAndDrop.objectReferences.Length <= 0 || !(Collection is ILibrarySet))
                return;
            
            var data =  DragAndDrop.GetGenericData(LibraryConstants.ItemDragDataName);
            if (data is UniqueID fromCollectionId)
            {
                if (fromCollectionId != UniqueID.Empty)
                {
                    // We reject the drag if it is the same collection the assets are from since non can added.
                    if (fromCollectionId == Collection.ID)
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    else if (!evt.ctrlKey)
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    
                    return;
                }
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (DragAndDrop.objectReferences.Length <= 0 || !(Collection is ILibrarySet librarySet))
                return;

            bool isFromCollection = false;
            var data = DragAndDrop.GetGenericData(LibraryConstants.ItemDragDataName);
            
            if (data is UniqueID fromCollectionId)
            {
                isFromCollection = true;
                // If we are dropping and dragging from the same collection, no assets can be added so we exit out.
                if (fromCollectionId == Collection.ID)
                    return;
            }
            else
            {
                // We set to -1000 because -1 is the id of the root collection, so we just need it to not be a valid id.
                fromCollectionId = UniqueID.Empty;
            }
            
            DragAndDrop.AcceptDrag();

            // If CTRL is not held, then we try to move the assets from their current collection if it supports it,
            // and if the item can be added to the this collection.
            if (!evt.ctrlKey)
            {
                // Check if we are dragging objects from another collection.
                if (isFromCollection) 
                {
                    // Check if the collection we are dragging from implements the ILibrarySet interface
                    // and thus can have items removed.
                    var fromCollection = LibraryDatabase.FindCollectionByID(fromCollectionId);
                    if (fromCollection is ILibrarySet fromLibrarySet && fromCollection.ID != Collection.ID)
                    {
                        // We add each item to the collection individually because if they are not added,
                        // then we don't remove them from their current collection.
                        var undoGroup = Undo.GetCurrentGroup();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (librarySet.Add(obj))
                                fromLibrarySet.Remove(obj);
                        }

                        // Clear the selection when moving items.
                        if (SmartLibraryWindow.LibraryWindows.Count > 0)
                        {
                            SmartLibraryWindow.LibraryWindows[0].ClearItemSelection();
                        }
                        
                        if (Undo.GetCurrentGroup() != undoGroup)
                            Undo.CollapseUndoOperations(undoGroup);

                        return;
                    }
                } 
            }
                
            // Default if CTRL is not held, or the assets are not from another collection,
            // or the collection does not support remove items.
            librarySet.UnionWith(DragAndDrop.objectReferences);
        }
    } 
}
