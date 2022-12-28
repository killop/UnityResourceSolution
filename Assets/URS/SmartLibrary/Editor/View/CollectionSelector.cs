using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class CollectionSelector : EditorWindow
    {
        private Action<LibraryCollection> _onSelect;
        private LibraryCollection _currentCollection;
        private int _nextId = 0;
        private int _currentCollectionItemId;

        public static void Open(Action<LibraryCollection> onSelect, LibraryCollection currentCollection = null)
        {
            var window = CreateInstance<CollectionSelector>();
            window.minSize = new Vector2(250, 350);
            window._onSelect = onSelect;
            window._currentCollection = currentCollection;
            window.ShowAuxWindow();
        }

        private void OnEnable()
        {
            // Setup basic window settings.
            titleContent = new GUIContent("Folder");
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("Common"));

            var collectionsTree = new BTreeView(CreateTreeItems(), 18, MakeItem, BindItem);
            collectionsTree.style.flexGrow = 1;
            collectionsTree.OnSelectionChanged += items => _onSelect?.Invoke(((CollectionTreeViewItem)items.First()).Collection);
            rootVisualElement.Add(collectionsTree);

            if (_currentCollection != null)
                collectionsTree.SetSelection(_currentCollectionItemId);
        }

        private List<BTreeViewItem> CreateTreeItems()
        {
            var items = new List<BTreeViewItem>();
            _nextId = 0;
            
            foreach (var collection in LibraryDatabase.BaseCollections)
            {
                if (collection == null)
                    continue;

                var item = new CollectionTreeViewItem(collection, NextId());
                items.Add(item);
                
                if (collection == _currentCollection)
                    _currentCollectionItemId = item.Id;
                
                if (collection.Subcollections.Count > 0)
                    CreateTreeItem(item, collection.Subcollections);
            }

            return items;
        }

        private void CreateTreeItem(BTreeViewItem parent, IEnumerable<LibraryCollection> childCollections)
        {
            foreach (var collection in childCollections)
            {
                var item = new CollectionTreeViewItem(collection, NextId());
                parent.AddChild(item);
                
                if (collection == _currentCollection)
                    _currentCollectionItemId = item.Id;
                
                if (collection.Subcollections.Count > 0)
                    CreateTreeItem(item, collection.Subcollections);
            }
        }

        private VisualElement MakeItem()
        {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;

            var icon = new Image();
            icon.AddToClassList(LibraryConstants.IconUssClassName);
            element.Add(icon);

            var name = new Label();
            element.Add(name);

            return element;
        }

        private void BindItem(VisualElement element, BTreeViewItem item)
        {
            var collectionItem = (CollectionTreeViewItem)item;
            element.Q<Image>().image = collectionItem.Collection.Icon;
            element.Q<Label>().text = collectionItem.Collection.name;
        }

        private int NextId()
        {
            return _nextId++;
        }
    } 
}
