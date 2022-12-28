using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal struct TreeDragArgs
    {
        public DragAndDropPosition dropPosition;
        public int insertIndex;
        public BTreeViewItem parentItem;
        public BTreeViewItem targetItem;
    }

    internal class TreeViewDragger : ListViewDragManipulator
    {
        private BTreeView _targetTreeView;

        protected BTreeView TargetTreeView
        {
            get { return _targetTreeView; }
        }

        public event Action<TreeDragArgs> OnDropped;

        public TreeViewDragger(Action<TreeDragArgs> onDropped)
        {
            OnDropped += onDropped;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _targetTreeView = target.Q<BTreeView>();
            if (_targetTreeView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to BTreeView or element with a BTreeView decendent");
            }

            base.RegisterCallbacksOnTarget();
        }

        protected override void ApplyDragUI(ReletiveDragPosition reletiveDragPosition, Vector3 mousePosition)
        {
            if (reletiveDragPosition == ReletiveDragPosition.OutsideItem)
            {
                // Set left offset to default indent without any indentation.
                var element = targetScrollView.ElementAt(0);
                var userContentContaienr = element.Q(className: "bewildered-tree-view__item-content");
                var indentsContainer = element.Q(className: "bewildered-tree-view__item-indents-container");
                _dragBarElement.style.left = userContentContaienr.worldBound.x - indentsContainer.localBound.width;
                return;
            }

            int hoverItemWrapperIndex = GetItemIndex(mousePosition);
            BTreeViewItem hoverItem = TargetTreeView.ItemWrappers[hoverItemWrapperIndex].item;
            VisualElement hoverElement = GetElementFromPosition(mousePosition);

            if (reletiveDragPosition == ReletiveDragPosition.BelowItem)
            {
                if (hoverItemWrapperIndex + 1 < TargetTreeView.ItemWrappers.Count)
                {
                    if (TargetTreeView.ItemWrappers[hoverItemWrapperIndex + 1].item.Parent == hoverItem)
                    {
                        mousePosition.y += TargetTreeView.ItemHeight;
                        hoverElement = GetElementFromPosition(mousePosition);
                    }
                }
            }

            var hoverItemContentElement = hoverElement.Q(className: "bewildered-tree-view__item-content");
            _dragBarElement.style.left = hoverElement.WorldToLocal(hoverItemContentElement.worldBound).x;
        }

        protected override void OnDrop(ListDragArgs args, Vector3 mousePosition)
        {
            var treeArgs = new TreeDragArgs
            {
                dropPosition = args.dropPosition,
                targetItem = ((BTreeView.ItemWrapper)args.target).item
            };

            var reletivePosition = GetReletiveDragPosition(mousePosition);

            var hoverItemWrapperIndex = GetItemIndex(mousePosition);
            BTreeViewItem hoverItem = args.dropPosition != DragAndDropPosition.OutsideItems ? TargetTreeView.ItemWrappers[hoverItemWrapperIndex].item : null;

            switch (reletivePosition)
            {
                case ReletiveDragPosition.AboveItem:
                    treeArgs.parentItem = hoverItem.Parent;
                    treeArgs.insertIndex = GetSiblingIndex(hoverItem);
                    break;
                case ReletiveDragPosition.BelowItem:
                    treeArgs.parentItem = hoverItem.Parent;
                    treeArgs.insertIndex = GetSiblingIndex(hoverItem) + 1;

                    if (hoverItemWrapperIndex + 1 < TargetTreeView.ItemWrappers.Count)
                    {
                        if (TargetTreeView.ItemWrappers[hoverItemWrapperIndex + 1].item.Parent == hoverItem)
                        {
                            treeArgs.parentItem = hoverItem;
                            treeArgs.insertIndex = 0;
                        }
                    }
                    break;
                case ReletiveDragPosition.OverItem:
                    treeArgs.parentItem = hoverItem;
                    treeArgs.insertIndex = hoverItem.ChildCount;
                    break;
                case ReletiveDragPosition.OutsideItem:
                    treeArgs.parentItem = null;
                    treeArgs.insertIndex = TargetTreeView.RootItems.Count;
                    break;
            }

            if (!IsValidReparent(treeArgs.parentItem, treeArgs.targetItem))
                return;

            OnDropped?.Invoke(treeArgs);
        }

        protected int GetSiblingIndex(BTreeViewItem item)
        {
            if (item.Parent == null)
            {
                for (int i = 0; i < TargetTreeView.RootItems.Count; i++)
                {
                    if (TargetTreeView.RootItems[i] == item)
                        return i;
                }

                return -1;
            }
            else
            {
                return item.GetSiblingIndex();
            }
        }

        private bool IsValidReparent(BTreeViewItem parent, BTreeViewItem target)
        {
            if (parent == target)
                return false;

            // Go through all of the ancestors of the collection trying to be parented to. 
            // If this collection is one of them return false as parenting to it would create a infinit loop.
            for (var ancestor = parent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor == target)
                    return false;
            }

            return true;
        }
    } 
}
