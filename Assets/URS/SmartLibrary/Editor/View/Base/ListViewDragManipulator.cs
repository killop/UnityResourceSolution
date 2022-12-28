using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    internal enum DragAndDropPosition
    {
        OverItem,
        BetweenItems,
        OutsideItems
    }

    internal struct ListDragArgs
    {
        public int insertIndex;
        public object target;
        public DragAndDropPosition dropPosition;
    }

    internal abstract class ListViewDragManipulator : MouseManipulator
    {
        protected enum ReletiveDragPosition { AboveItem, BelowItem, OverItem, OutsideItem }

        protected const int minDistanceToActive = 5;
        protected const int betweenElementsSize = 5;

        private static readonly string dragBarUssClassName = "bewildered-list-view__drag_bar";

        private bool _canStart = false;
        private Vector3 _startPosition;
        private bool _isDragging = false;
        protected VisualElement _dragBarElement;
        private ListView _targetListView;
        private ScrollView _targetScrollView;

        protected ListView targetListView
        {
            get { return _targetListView; }
        }

        protected ScrollView targetScrollView
        {
            get  { return _targetScrollView; }
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _targetListView = target.Q<ListView>();
            _targetScrollView = target.Q<ScrollView>();
            if (_targetListView == null)
                throw new System.InvalidOperationException("Manipulator can only be added to ListView or element with a ListView decendent");

            targetScrollView.RegisterCallback<MouseDownEvent>(OnMouseDown);
            targetScrollView.RegisterCallback<MouseUpEvent>(OnMouseUp);
            targetScrollView.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            targetScrollView.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            targetScrollView.RegisterCallback<DragEnterEvent>(OnDragEnter);
            targetScrollView.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            targetScrollView.RegisterCallback<DragPerformEvent>(OnDragPerform);

            targetListView.bindItem += BindIndex;

            _dragBarElement = new VisualElement();
            _dragBarElement.style.width = targetListView.localBound.width;
            _dragBarElement.style.visibility = Visibility.Hidden;
            _dragBarElement.pickingMode = PickingMode.Ignore;
            _dragBarElement.AddToClassList(dragBarUssClassName);

            targetListView.RegisterCallback<GeometryChangedEvent>(evt => _dragBarElement.style.width = targetListView.localBound.width);
            targetScrollView.contentViewport.Add(_dragBarElement);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            if (targetScrollView == null)
                return;

            targetScrollView.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            targetScrollView.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            targetScrollView.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            targetScrollView.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);

            targetScrollView.UnregisterCallback<DragEnterEvent>(OnDragEnter);
            targetScrollView.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            targetScrollView.UnregisterCallback<DragPerformEvent>(OnDragPerform);

            targetListView.bindItem -= BindIndex;

            targetScrollView.contentViewport.Remove(_dragBarElement);
        }

        private void BindIndex(VisualElement element, int index)
        {
            element.userData = index;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                _canStart = false;
                _isDragging = false;
            }
            else if (CanStartDrag(evt.mousePosition))
            {
                _canStart = true;
                _startPosition = evt.mousePosition;
            }
        }

        /// <summary>
        /// Whether a drag can be started. Called on mouse down.
        /// </summary>
        /// <param name="mousePosition">Worldspace position of the mouse.</param>
        /// <returns><c>true</c> if a drag can be started; otherwise, <c>false</c>.</returns>
        protected virtual bool CanStartDrag(Vector3 mousePosition)
        {
            return targetScrollView.contentContainer.worldBound.Contains(mousePosition);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            _canStart = false;
            _isDragging = false;
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_canStart)
                return;

            // Only start the drag after the mouse has moved a certain distance from the mouse down position. 
            // Otherwise a drag could be easily started if the mouse just moved a tiny bit when clicking.
            if (Mathf.Abs(_startPosition.x - evt.mousePosition.x) < minDistanceToActive && Mathf.Abs(_startPosition.y - evt.mousePosition.y) < minDistanceToActive)
                return;
            
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("BEWILDERED_LISTVIEW", targetListView);
            DragAndDrop.StartDrag("BewilderedListViewReorder");

            _canStart = false;
            _isDragging = true;
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            CancelDrag();
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            // 'Resume' the drag if it was a ListView drag and it was this list that was being dragged.
            if (DragAndDrop.GetGenericData("BEWILDERED_LISTVIEW") is ListView dragListView)
            {
                if (dragListView == targetListView)
                {
                    _isDragging = true;
                    return;
                }
            }
        }

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (!_isDragging)
                return;

            var element = GetElementFromPosition(evt.mousePosition);
            var reletiveDragPosition = GetReletiveDragPosition(evt.mousePosition);

            if (reletiveDragPosition != ReletiveDragPosition.OverItem)
            {
                MoveDragBarToElement(element, reletiveDragPosition);
                ApplyDragUI(reletiveDragPosition, evt.mousePosition);
            }
            else
            {
                _dragBarElement.style.visibility = Visibility.Hidden;
            }

            DragAndDrop.visualMode = GetDraggingVisualMode(evt.mousePosition, evt.actionKey, evt.altKey, evt.ctrlKey, evt.shiftKey);
        }

        /// <summary>
        /// Returns the visual mode for the current drag operation. Only called while is dragging items from the <see cref="ListView"/>.
        /// </summary>
        /// <param name="mousePosition">Worldposition mouse position.</param>
        protected virtual DragAndDropVisualMode GetDraggingVisualMode(Vector3 mousePosition, bool actionKey, bool altKey, bool ctrlKey, bool shfitKey)
        {
            return DragAndDropVisualMode.Move;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (!_isDragging)
                return;

            DragAndDrop.AcceptDrag();

            OnDrop(MakeDragArgs(evt.mousePosition), evt.mousePosition);

            CancelDrag();
        }

        /// <summary>
        /// Called when a <see cref="ListView"/> element is dropped.
        /// </summary>
        /// <param name="args">The <see cref="ListDragArgs"/> for the drag and drop action.</param>
        /// <param name="mousePosition">Worldspace position of the mouse.</param>
        protected abstract void OnDrop(ListDragArgs args, Vector3 mousePosition);

        protected virtual void ApplyDragUI(ReletiveDragPosition reletiveDragPosition, Vector3 mousePosition)
        {

        }

        private void MoveDragBarToElement(VisualElement element, ReletiveDragPosition reletiveDragPosition)
        {
            if (reletiveDragPosition == ReletiveDragPosition.OutsideItem)
            {
                MoveDragBar(targetScrollView.contentContainer.localBound.yMax);
                return;
            }

            var contentViewport = targetScrollView.contentViewport;
            var elementBounds = contentViewport.WorldToLocal(element.worldBound);

            if (reletiveDragPosition == ReletiveDragPosition.BelowItem)
            {
                MoveDragBar(Mathf.Min(elementBounds.yMax, contentViewport.localBound.yMax - _dragBarElement.style.height.value.value));
            }
            else if (reletiveDragPosition == ReletiveDragPosition.AboveItem)
            {
                MoveDragBar(Mathf.Max(elementBounds.yMin, contentViewport.localBound.yMin + _dragBarElement.style.height.value.value));
            }
        }

        /// <summary>
        /// Sets the drag bar element's position. Sets the elements <c>style.top</c> to the provided value.
        /// </summary>
        private void MoveDragBar(float position)
        {
            _dragBarElement.style.top = position - 1;
            _dragBarElement.style.visibility = Visibility.Visible;
        }

        /// <summary>
        /// Create <see cref="ListDragArgs"/> for a provided world space mouse position.
        /// </summary>
        protected ListDragArgs MakeDragArgs(Vector3 mousePosition)
        {
            ListDragArgs args = new ListDragArgs();
            args.target = targetListView.selectedItem;

            switch (GetReletiveDragPosition(mousePosition))
            {
                case ReletiveDragPosition.AboveItem:
                    args.insertIndex = GetItemIndex(mousePosition);
                    args.dropPosition = DragAndDropPosition.BetweenItems;
                    break;
                case ReletiveDragPosition.BelowItem:
                    args.insertIndex = GetItemIndex(mousePosition) + 1;
                    args.dropPosition = DragAndDropPosition.BetweenItems;
                    break;
                case ReletiveDragPosition.OverItem:
                    args.insertIndex = GetItemIndex(mousePosition);
                    args.dropPosition = DragAndDropPosition.OverItem;
                    break;
                case ReletiveDragPosition.OutsideItem:
                    args.dropPosition = DragAndDropPosition.OutsideItems;

                    if (mousePosition.y >= targetScrollView.contentContainer.worldBound.yMax)
                        args.insertIndex = targetListView.itemsSource.Count;
                    else
                        args.insertIndex = 0;
                    break;
            }

            return args;

            //var itemElement = GetElementFromPosition(mousePosition);
            //if (itemElement != null)
            //{
            //    // Below item.
            //    if (itemElement.worldBound.yMax - mousePosition.y < betweenElementsSize)
            //    {
            //        args.insertIndex = GetItemIndex(mousePosition) + 1;
            //        args.dropPosition = DragAndDropPosition.BetweenItems;
            //        return args;
            //    }

            //    // Over item.
            //    if (mousePosition.y - itemElement.worldBound.yMin > betweenElementsSize)
            //    {
            //        args.insertIndex = GetItemIndex(mousePosition);
            //        args.dropPosition = DragAndDropPosition.OverItem;
            //        return args;
            //    }

            //    args.insertIndex = GetItemIndex(mousePosition);
            //    args.dropPosition = DragAndDropPosition.BetweenItems;
            //    return args;
            //}
            //else
            //{
            //    args.dropPosition = DragAndDropPosition.OutsideItems;

            //    // If the mouse is still within the ScrollView, then add the item at the end.
            //    // We don't need to do a check if the mouse is below a certain item becauase we already know that it is not over an item. And it can't be above items.
            //    if (targetListView.worldBound.Contains(mousePosition))
            //        args.insertIndex = targetListView.itemsSource.Count;
            //}
        }

        protected ReletiveDragPosition GetReletiveDragPosition(Vector3 mousePosition)
        {
            if (!targetScrollView.contentContainer.worldBound.Contains(mousePosition))
                return ReletiveDragPosition.OutsideItem;

            var itemElement = GetElementFromPosition(mousePosition);

            if (itemElement.worldBound.yMax - mousePosition.y < betweenElementsSize)
            {
                return ReletiveDragPosition.BelowItem;
            }
            else if (mousePosition.y - itemElement.worldBound.yMin < betweenElementsSize)
            {
                return ReletiveDragPosition.AboveItem;
            }
            else if (itemElement.worldBound.Contains(mousePosition))
            {
                return ReletiveDragPosition.OverItem;
            }
            else
            {
                return ReletiveDragPosition.OutsideItem;
            }
        }

        private void CancelDrag()
        {
            _canStart = false;
            _isDragging = false;

            if (_dragBarElement != null)
                _dragBarElement.style.visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Get an element in the <see cref="ListView"/> from the provided position.
        /// </summary>
        /// <param name="mousePosition">The world space position to find an element.</param>
        protected VisualElement GetElementFromPosition(Vector3 mousePosition)
        {
            // Iterate over all elements in the ScrollView to find one that contains the provided position.
            // Can't convert position to an index because the order of the child elements is not guaranteed.
            foreach (var itemElement in targetScrollView.Children())
            {
                if (itemElement.worldBound.Contains(mousePosition))
                    return itemElement;
            }

            return null;
        }

        /// <summary>
        /// Get the itemsSource index of the item under the specified localPosition.
        /// </summary>
        protected int GetItemIndex(Vector3 mousePosition)
        {
            //float scrollVerticalOffset = targetScrollView.scrollOffset.y;

            //float offset = scrollVerticalOffset % targetListView.itemHeight;
            //// Index of the element the mouse is over.
            //int elementIndex = (int)((localPosition.y + offset) / targetListView.itemHeight);

            //int index = (int)(scrollVerticalOffset / targetListView.itemHeight) + elementIndex;
            //return Mathf.Min(index, targetListView.itemsSource.Count);

            foreach (var element in targetScrollView.Children())
            {
                if (element.worldBound.Contains(mousePosition))
                    return (int)element.userData;
            }

            return targetListView.itemsSource.Count;
        }
    }

}