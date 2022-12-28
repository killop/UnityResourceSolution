using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    internal class SplitView : VisualElement
    {
        public static readonly string UssClassName = "bewildered-split-view";
        public static readonly string ContentContainerClassName = "bewildered-split-view__content-container";
        public static readonly string HandleDragLineAnchorClassName = "bewildered-split-view__dragline-anchor";

        private static readonly string _handleDragLineClassName = "bewildered-split-view__dragline";
        private static readonly string _handleDragLineVerticalClassName = _handleDragLineClassName + "--vertical";
        private static readonly string _handleDragLineHorizontalClassName = _handleDragLineClassName + "--horizontal";

        private static readonly string _handleDragLineAnchorVerticalClassName = HandleDragLineAnchorClassName + "--vertical";
        private static readonly string _handleDragLineAnchorHorizontalClassName = HandleDragLineAnchorClassName + "--horizontal";
        private static readonly string _verticalClassName = "bewildered-split-view--vertical";
        private static readonly string _horizontalClassName = "bewildered-split-view--horizontal";

        public enum Orientation
        {
            Horizontal,
            Vertical
        }

        public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlIntAttributeDescription m_FixedPaneIndex = new UxmlIntAttributeDescription { name = "fixed-pane-index", defaultValue = 0 };
            UxmlIntAttributeDescription m_FixedPaneInitialSize = new UxmlIntAttributeDescription { name = "fixed-pane-initial-size", defaultValue = 100 };
            UxmlEnumAttributeDescription<Orientation>  m_Orientation = new UxmlEnumAttributeDescription<Orientation> { name = "orientation", defaultValue = Orientation.Horizontal };
            
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var fixedPaneIndex = m_FixedPaneIndex.GetValueFromBag(bag, cc);
                var fixedPaneInitialSize = m_FixedPaneInitialSize.GetValueFromBag(bag, cc);
                var orientation = m_Orientation.GetValueFromBag(bag, cc);

                ((SplitView)ve).Init(fixedPaneIndex, fixedPaneInitialSize, orientation);
            }
        }

        private VisualElement _leftPane;
        private VisualElement _rightPane;

        private VisualElement _fixedPane;
        private VisualElement _flexedPane;

        private VisualElement _dragLine;
        private VisualElement _dragLineAnchor;

        private VisualElement _content;

        private Orientation _orientation;
        private int _fixedPaneIndex;
        private float _fixedPaneInitialDimension;

        private SquareResizer _resizer;

        private float _minDimension;

        public override VisualElement contentContainer
        {
            get { return _content; }
        }

        public SplitView()
        {
            AddToClassList(UssClassName);

            _content = new VisualElement();
            _content.name = "bewildered-content-container";
            _content.AddToClassList(ContentContainerClassName);
            hierarchy.Add(_content);

            // Create drag anchor line.
            _dragLineAnchor = new VisualElement();
            _dragLineAnchor.name = "bewildered-dragline-anchor";
            _dragLineAnchor.AddToClassList(HandleDragLineAnchorClassName);
            hierarchy.Add(_dragLineAnchor);

            // Create drag
            _dragLine = new VisualElement();
            _dragLine.name = "bewildered-dragline";
            _dragLine.AddToClassList(_handleDragLineClassName);
            _dragLineAnchor.Add(_dragLine);
        }

        public SplitView(int fixedPaneIndex, float fixedPaneStartDimension, Orientation orientation) : this()
        {
            Init(fixedPaneIndex, fixedPaneStartDimension, orientation);
        }

        public void Init(int fixedPaneIndex, float fixedPaneInitialDimension, Orientation orientation)
        {
            _orientation = orientation;
            _minDimension = 125;
            _fixedPaneIndex = fixedPaneIndex;
            _fixedPaneInitialDimension = fixedPaneInitialDimension;

            if (_orientation == Orientation.Horizontal)
                style.minWidth = _fixedPaneInitialDimension;
            else
                style.minHeight = _fixedPaneInitialDimension;

            _content.RemoveFromClassList(_horizontalClassName);
            _content.RemoveFromClassList(_verticalClassName);
            if (_orientation == Orientation.Horizontal)
                _content.AddToClassList(_horizontalClassName);
            else
                _content.AddToClassList(_verticalClassName);

            // Create drag anchor line.
            _dragLineAnchor.RemoveFromClassList(_handleDragLineAnchorHorizontalClassName);
            _dragLineAnchor.RemoveFromClassList(_handleDragLineAnchorVerticalClassName);
            if (_orientation == Orientation.Horizontal)
                _dragLineAnchor.AddToClassList(_handleDragLineAnchorHorizontalClassName);
            else
                _dragLineAnchor.AddToClassList(_handleDragLineAnchorVerticalClassName);

            // Create drag
            _dragLine.RemoveFromClassList(_handleDragLineHorizontalClassName);
            _dragLine.RemoveFromClassList(_handleDragLineVerticalClassName);
            if (_orientation == Orientation.Horizontal)
                _dragLine.AddToClassList(_handleDragLineHorizontalClassName);
            else
                _dragLine.AddToClassList(_handleDragLineVerticalClassName);

            if (_resizer != null)
            {
                _dragLineAnchor.RemoveManipulator(_resizer);
                _resizer = null;
            }

            if (_content.childCount != 2)
                RegisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            else
                PostDisplaySetup();
        }

        private void OnPostDisplaySetup(GeometryChangedEvent evt)
        {
            if (_content.childCount != 2)
            {
                Debug.LogError("SplitView needs exactly 2 chilren.");
                return;
            }

            PostDisplaySetup();

            UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            RegisterCallback<GeometryChangedEvent>(OnSizeChange);
        }

        private void PostDisplaySetup()
        {
            if (_content.childCount != 2)
            {
                Debug.LogError("SplitView needs exactly 2 children.");
                return;
            }

            _leftPane = _content[0];
            if (_fixedPaneIndex == 0)
            {
                _fixedPane = _leftPane;
                if (_orientation == Orientation.Horizontal)
                    _leftPane.style.width = _fixedPaneInitialDimension;
                else
                    _leftPane.style.height = _fixedPaneInitialDimension;
            }
            else
            {
                _flexedPane = _leftPane;
            }

            _rightPane = _content[1];
            if (_fixedPaneIndex == 1)
            {
                _fixedPane = _rightPane;
                if (_orientation == Orientation.Horizontal)
                    _rightPane.style.width = _fixedPaneInitialDimension;
                else
                    _rightPane.style.height = _fixedPaneInitialDimension;
            }
            else
            {
                _flexedPane = _rightPane;
            }

            _fixedPane.style.flexShrink = 0;
            _flexedPane.style.flexGrow = 1;
            _flexedPane.style.flexShrink = 0;
            _flexedPane.style.flexBasis = 0;

            if (_orientation == Orientation.Horizontal)
            {
                if (_fixedPaneIndex == 0)
                    _dragLineAnchor.style.left = _fixedPaneInitialDimension;
                else
                    _dragLineAnchor.style.left = this.resolvedStyle.width - _fixedPaneInitialDimension;
            }
            else
            {
                if (_fixedPaneIndex == 0)
                    _dragLineAnchor.style.top = _fixedPaneInitialDimension;
                else
                    _dragLineAnchor.style.top = this.resolvedStyle.height - _fixedPaneInitialDimension;
            }

            int direction = 1;
            if (_fixedPaneIndex > 0)
                direction = -1;

            _resizer = new SquareResizer(this, direction, _minDimension, _orientation);

            _dragLineAnchor.AddManipulator(_resizer);

            UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            RegisterCallback<GeometryChangedEvent>(OnSizeChange);
        }

        private void OnSizeChange(GeometryChangedEvent evt)
        {
            var maxLength = this.resolvedStyle.width;
            var dragLinePos = _dragLineAnchor.resolvedStyle.left;
            var activeElementPos = _fixedPane.resolvedStyle.left;
            if (_orientation == Orientation.Vertical)
            {
                maxLength = this.resolvedStyle.height;
                dragLinePos = _dragLineAnchor.resolvedStyle.top;
                activeElementPos = _fixedPane.resolvedStyle.top;
            }

            if (_fixedPaneIndex == 0 && dragLinePos > maxLength)
            {
                var delta = maxLength - dragLinePos;
                _resizer.ApplyDelta(delta);
            }
            else if (_fixedPaneIndex == 1)
            {
                if (activeElementPos < 0)
                {
                    var delta = -dragLinePos;
                    _resizer.ApplyDelta(delta);
                }
                else
                {
                    if (_orientation == Orientation.Horizontal)
                        _dragLineAnchor.style.left = activeElementPos;
                    else
                        _dragLineAnchor.style.top = activeElementPos;
                }
            }
        }

        private class SquareResizer : MouseManipulator
        {
            private Vector2 _start;
            protected bool _active;
            private SplitView _splitView;
            private VisualElement _pane;
            private int _direction;
            private float _minWidth;
            private Orientation _orientation;

            public SquareResizer(SplitView splitView, int dir, float minWidth, Orientation orientation)
            {
                _orientation = orientation;
                _minWidth = minWidth;
                _splitView = splitView;
                _pane = splitView._fixedPane;
                _direction = dir;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                _active = false;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }

            public void ApplyDelta(float delta)
            {
                float oldDimension = _orientation == Orientation.Horizontal
                    ? _pane.resolvedStyle.width
                    : _pane.resolvedStyle.height;
                float newDimension = oldDimension + delta;

                if (newDimension < oldDimension && newDimension < _minWidth)
                    newDimension = _minWidth;

                float maxLength = _orientation == Orientation.Horizontal
                    ? _splitView.resolvedStyle.width
                    : _splitView.resolvedStyle.height;
                if (newDimension > oldDimension && newDimension > maxLength)
                    newDimension = maxLength;

                if (_orientation == Orientation.Horizontal)
                {
                    _pane.style.width = newDimension;
                    if (_splitView._fixedPaneIndex == 0)
                        target.style.left = newDimension;
                    else
                        target.style.left = _splitView.resolvedStyle.width - newDimension;
                }
                else
                {
                    _pane.style.height = newDimension;
                    if (_splitView._fixedPaneIndex == 0)
                        target.style.top = newDimension;
                    else
                        target.style.top = _splitView.resolvedStyle.height - newDimension;
                }
            }

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (_active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (CanStartManipulation(e))
                {
                    _start = e.localMousePosition;

                    _active = true;
                    target.CaptureMouse();
                    e.StopPropagation();
                }
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (!_active || !target.HasMouseCapture())
                    return;

                Vector2 diff = e.localMousePosition - _start;
                float mouseDiff = diff.x;
                if (_orientation == Orientation.Vertical)
                    mouseDiff = diff.y;

                float delta = _direction * mouseDiff;

                ApplyDelta(delta);

                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!_active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;

                _active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }
    }
}