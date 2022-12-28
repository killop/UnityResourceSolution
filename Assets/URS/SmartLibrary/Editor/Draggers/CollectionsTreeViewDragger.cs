using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    internal class CollectionsTreeViewDragger : TreeViewDragger
    {
        public CollectionsTreeViewDragger(Action<TreeDragArgs> onDropped) : base(onDropped) { }

        protected override DragAndDropVisualMode GetDraggingVisualMode(Vector3 mousePosition, bool actionKey, bool altKey, bool ctrlKey, bool shfitKey)
        {
            var reletivePosition = GetReletiveDragPosition(mousePosition);
            // Set visual mode to reject if the current item is the AllItems item.
            if (IsAllItemsItem(GetItemFromPosition(mousePosition)) && reletivePosition != ReletiveDragPosition.BelowItem)
                return DragAndDropVisualMode.Rejected;

            return base.GetDraggingVisualMode(mousePosition, actionKey, altKey, ctrlKey, shfitKey);
        }

        protected override void ApplyDragUI(ReletiveDragPosition reletiveDragPosition, Vector3 mousePosition)
        {
            base.ApplyDragUI(reletiveDragPosition, mousePosition);

            var reletivePosition = GetReletiveDragPosition(mousePosition);
            //Hide the dragBar if over the AllItems item.
            if (IsAllItemsItem(GetItemFromPosition(mousePosition)) && reletivePosition != ReletiveDragPosition.BelowItem)
                _dragBarElement.style.visibility = Visibility.Hidden;
        }

        protected override bool CanStartDrag(Vector3 mousePosition)
        {
            int hoverItemWrapperIndex = GetItemIndex(mousePosition);

            return base.CanStartDrag(mousePosition) && TargetTreeView.ItemWrappers[hoverItemWrapperIndex].item is CollectionTreeViewItem;
        }

        private BTreeViewItem GetItemFromPosition(Vector3 mousePosition)
        {
            int itemWrapperIndex = GetItemIndex(mousePosition);
            if (itemWrapperIndex >= 0 && itemWrapperIndex < TargetTreeView.ItemWrappers.Count)
                return TargetTreeView.ItemWrappers[itemWrapperIndex].item;
            else
                return null;
        }

        private bool IsAllItemsItem(BTreeViewItem item)
        {
            if (item == null)
                return false;

            if (item.Parent != null)
                return false;

            return TargetTreeView.RootItems[0] == item;
        }
    } 
}
