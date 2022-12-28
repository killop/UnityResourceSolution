using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    internal class LibrarySetManipulator : Manipulator
    {
        private LibraryItemsView _itemsView;
        private bool _canAcceptDrag = false;

        private ILibrarySet LibrarySet
        {
            get { return _itemsView.Items as ILibrarySet; }
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _itemsView = target.Q<LibraryItemsView>();
            
            _itemsView.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _itemsView.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _itemsView.RegisterCallback<DragPerformEvent>(OnDragPerform);
            _itemsView.RegisterCallback<DragEnterEvent>(OnDragEnter);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            _itemsView.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            _itemsView.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _itemsView.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            _itemsView.UnregisterCallback<DragEnterEvent>(OnDragEnter);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (LibrarySet == null)
            {
                _itemsView.ShowNotification("Collection does not support manually removing items.");
                return;
            }
            
            // TODO: Does not currently work because the IMGUI view eats the key event...
            //if (evt.keyCode == KeyCode.Delete && _itemsView.SelectedIndices.Any())
            //{
            //    LibrarySet.ExceptWith(_itemsView.SelectedItems.Select(e => e.Item));
            //}
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (!_canAcceptDrag)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }
            
            if (DragAndDrop.objectReferences.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (LibrarySet == null)
            {
                _itemsView.ShowNotification("Collection does not support manually adding items.");
                return;
            }

            if (DragAndDrop.objectReferences.Length == 0)
            {
                return;
            }

            if (DragAndDrop.objectReferences.Length == 1)
            {
                if (!EditorUtility.IsPersistent(DragAndDrop.objectReferences[0]))
                    _itemsView.ShowNotification("Cannot add non-persistant objects to the library.");
                else if (!LibrarySet.Add(DragAndDrop.objectReferences[0]))
                    _itemsView.ShowNotification($"'{DragAndDrop.objectReferences[0].name}' could not be added to the collection");
            }
            else
            {
                int previousCount = LibrarySet.Count;
                int addCount = DragAndDrop.objectReferences.Length;
                
                LibrarySet.UnionWith(DragAndDrop.objectReferences);
                
                if (LibrarySet.Count < previousCount + addCount)
                    _itemsView.ShowNotification($"{previousCount + addCount - LibrarySet.Count} of {addCount} assets could not be added to the collection");
            }
            
            DragAndDrop.AcceptDrag();
        }
        
        private void OnDragEnter(DragEnterEvent evt)
        {
            var dragData = DragAndDrop.GetGenericData(LibraryConstants.ItemDragDataName);
         
            // Drag data is only set when dragging assets from a collection.
            if (dragData != null && _itemsView.Items is LibraryCollection collection)
            {
                _canAcceptDrag = collection.ID != (UniqueID)dragData;
                return;
            }

            _canAcceptDrag = true;
        }
    } 
}
