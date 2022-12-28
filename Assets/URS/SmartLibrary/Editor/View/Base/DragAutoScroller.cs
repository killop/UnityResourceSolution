using UnityEngine;
using UnityEngine.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    /// <summary>
    /// Add to a <see cref="ScrollView"/> or parent of one to auto scroll up/down when dragging and mouse is on the edge.
    /// </summary>
    internal class DragAutoScroller : Manipulator
    {
        protected const int scrollSpeed = 20;
        protected const int autoScrollSize = 5;

        private ScrollView _targetScrollView;

        protected override void RegisterCallbacksOnTarget()
        {
            _targetScrollView = target.Q<ScrollView>();
            if (_targetScrollView == null)
                throw new System.InvalidOperationException("Manipulator can only be added to ScrollView or an element with a ScrollView decendent");

            _targetScrollView.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            if (_targetScrollView == null)
                return;

            _targetScrollView.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            bool scrollUp = evt.mousePosition.y < _targetScrollView.worldBound.yMin + autoScrollSize && _targetScrollView.verticalScroller.value > 0.0f;
            bool scrollDown = evt.mousePosition.y > _targetScrollView.worldBound.yMax - autoScrollSize && _targetScrollView.verticalScroller.value < _targetScrollView.verticalScroller.highValue;

            if (scrollUp || scrollDown)
            {
                _targetScrollView.scrollOffset += (scrollUp ? Vector2.down : Vector2.up) * scrollSpeed;
            }
        }
    } 
}
