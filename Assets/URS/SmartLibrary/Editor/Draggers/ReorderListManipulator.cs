using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    internal enum ReletiveDragPosition { AboveItem, BelowItem, OverItem }

    internal class ReorderListManipulator : MouseManipulator
    {
        private const int minDistanceToStartDrag = 5;
        private const float betweenElementSize = 5;

        private static readonly string dragDataKey = "smart-library__reorderable-list";
        private static readonly string dragName = "Smart Library ReorderableList - Reorder";

        private static readonly string dragBarUssClassName = "bewildered-reorderable-list__drag-bar";

        private ReorderableList _list;
        private VisualElement _dragBar;
        private bool _isDragging;
        private bool _canStartDrag;
        private Vector3 _dragStartPosition;

        private int _dragStartIndex;
        private int _currentDragIndex;
        private ReletiveDragPosition _currentDragPosition;
        private ReorderableList.ListItemElement _targetItemElement;

        public ReorderListManipulator(ReorderableList list)
        {
            _list = list;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            _list.ScrollView.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            _list.ScrollView.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            _list.ScrollView.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            _list.ScrollView.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            _list.ScrollView.RegisterCallback<DragEnterEvent>(OnDragEnter);
            _list.ScrollView.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _list.ScrollView.RegisterCallback<DragPerformEvent>(OnDragPerform);


            _dragBar = new VisualElement();
            _dragBar.style.visibility = Visibility.Hidden;
            _dragBar.style.position = Position.Absolute;
            _dragBar.pickingMode = PickingMode.Ignore;
            _dragBar.AddToClassList(dragBarUssClassName);

            target.hierarchy.Add(_dragBar);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            _list.ScrollView.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            _list.ScrollView.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            _list.ScrollView.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            _list.ScrollView.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);

            _list.ScrollView.UnregisterCallback<DragEnterEvent>(OnDragEnter);
            _list.ScrollView.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _list.ScrollView.UnregisterCallback<DragPerformEvent>(OnDragPerform);

            target.hierarchy.Remove(_dragBar);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!_isDragging && !_canStartDrag && evt.button == (int)MouseButton.LeftMouse)
            {
                // The evt.target is the first element to recive the event.
                var targetElement = evt.target as VisualElement;
                
                // We get the item that was clicked so we can get the index of it.
                _targetItemElement = targetElement.GetFirstOfType<ReorderableList.ListItemElement>();

                _canStartDrag = true;
                _dragStartPosition = evt.mousePosition;
                _dragStartIndex = _list.ScrollView.IndexOf(_targetItemElement);
                _currentDragIndex = _dragStartIndex;
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            // If there is something like a button or field, the mouseUp event will not be trigged but MouseDown will causing a drag to start even if mouse is up.
            // So we need to check if the mouse is still down when moving the mouse.
            // 1 == LeftMouse. The docs say it should be 0 but that doesn't work and 1 does.
            bool isLeftMouseDown = (1 & evt.pressedButtons) != 0;
            if (!_canStartDrag || _isDragging || !isLeftMouseDown)
                return;

            // Only start the drag after the mouse has moved a certain distance from the mouse down position. 
            // Otherwise a drag could be easily started if the mouse just moved a tiny bit when clicking.
            if (Mathf.Abs(_dragStartPosition.x - evt.mousePosition.x) < minDistanceToStartDrag && Mathf.Abs(_dragStartPosition.y - evt.mousePosition.y) < minDistanceToStartDrag)
                return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(dragDataKey, target);
            DragAndDrop.StartDrag(dragName);

            _isDragging = true;
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            _canStartDrag = false;
            _isDragging = false;
            _dragBar.style.visibility = Visibility.Hidden;
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _canStartDrag = false;
            _isDragging = false;
            _dragBar.style.visibility = Visibility.Hidden;
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (!_isDragging)
                return;

            _currentDragIndex = GetItemIndex(evt.mousePosition);

            if (_currentDragIndex == -1)
            {
                _currentDragIndex = _list.ScrollView.childCount - 1;
                _currentDragPosition = ReletiveDragPosition.BelowItem;
            }
            else
            {
                _currentDragPosition = GetDragPosition(evt.mousePosition, _list.ScrollView[_currentDragIndex]);
            }
            
            UpdateDragBarPosition();

            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            evt.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (!_isDragging)
                return;

            DragAndDrop.AcceptDrag();

            if (_currentDragPosition == ReletiveDragPosition.BelowItem && _dragStartIndex > _currentDragIndex)
                _currentDragIndex++;

            if (_currentDragPosition == ReletiveDragPosition.AboveItem && _currentDragIndex > _dragStartIndex)
                _currentDragIndex--;

            if (_currentDragIndex != _dragStartIndex)
                _list.Swap(_dragStartIndex, _currentDragIndex, _currentDragPosition);

            _isDragging = false;
            _canStartDrag = false;

            _dragBar.style.visibility = Visibility.Hidden;

            evt.StopPropagation();
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            // 'Resume' the drag if it was a Reorderable drag and it was this reorderable that was being dragged.
            if (DragAndDrop.GetGenericData(dragDataKey) == target)
            {
                _isDragging = true;
            }
        }

        private void UpdateDragBarPosition()
        {
            if (_currentDragIndex < 0)
            {
                _dragBar.style.visibility = Visibility.Hidden;
                return;
            }

            if (_currentDragPosition == ReletiveDragPosition.BelowItem)
                _dragBar.style.top = _list.WorldToLocal(_list.ScrollView[_currentDragIndex].worldBound).yMax;
            else
                _dragBar.style.top = _list.WorldToLocal(_list.ScrollView[_currentDragIndex].worldBound).yMin;

            _dragBar.style.visibility = Visibility.Visible;
        }

        private ReletiveDragPosition GetDragPosition(Vector3 mousePosition, VisualElement element)
        {
            if (mousePosition.y > element.worldBound.yMax - betweenElementSize)
            {
                return ReletiveDragPosition.BelowItem;
            }
            else if (mousePosition.y < element.worldBound.yMin + betweenElementSize)
            {
                return ReletiveDragPosition.AboveItem;
            }
            else
            {
                return ReletiveDragPosition.OverItem;
            }
        }

        private int GetItemIndex(Vector3 mousePosition)
        {
            for (int i = 0; i < _list.ScrollView.childCount; i++)
            {
                if (_list.ScrollView[i].worldBound.Contains(mousePosition))
                    return i;
            }

            return -1;
        }
    }
}
