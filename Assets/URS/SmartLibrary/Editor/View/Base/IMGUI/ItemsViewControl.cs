using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    /// <summary>
    /// The base class for IMGUI controls for viewing items in a <see cref="IList"/>.
    /// </summary>
    internal abstract class ItemsViewControlBase
    {
        public delegate void DrawItemCallbackDelegate(Rect rect, int index, bool isActive, bool isHovering, bool isFocused);

        [SerializeField] private Vector2 _scrollPosition;
        [SerializeField] private List<int> _selectedIndices = new List<int>();
        [SerializeField] private bool _multiSelect = true;
        [SerializeField] private float _padding = 1;
        [SerializeField] private float _margin = 1;

        private bool _containsMouse;
        private int _hoveredIndex = -1;
        private Vector2 _itemSize = new Vector2(50, 50);
        private List<object> _selectedItems = new List<object>();
        private Rect _contentViewRect;
        private Rect _viewportRect;
        private bool _needsRepaint = false;

        protected bool HasLostFocus { get; private set; } = false;

        protected int RangeSelectOriginIndex { get; set; } = -1;

        protected int LastSelectedIndex { get; set; } = -1;

        /// <summary>
        /// Callback drawing an item in the <see cref="ItemsViewControlBase"/>.
        /// </summary>
        public DrawItemCallbackDelegate OnDrawItem { get; set; }

        /// <summary>
        /// The positions of the scroll bars of the area that draws items.
        /// </summary>
        public Vector2 ScrollPosition
        {
            get { return _scrollPosition; }
            set { _scrollPosition = value; }
        }

        /// <summary>
        /// The size of the <see cref="Rect"/> of items.
        /// </summary>
        public Vector2 ItemSize
        {
            get { return _itemSize; }
            set 
            {
                if (value == _itemSize)
                    return;

                _itemSize = value;
                Repaint();
            }
        }

        /// <summary>
        /// The items to display in the <see cref="ItemsViewControlBase"/>.
        /// </summary>
        public IList Items { get; set; }

        /// <summary>
        /// The indices of the currently selected items.
        /// </summary>
        public IEnumerable<int> SelectedIndices
        {
            get { return _selectedIndices; }
        }

        /// <summary>
        /// The index of the currently selected item. If multiple items are selected, the index of the first one is returned.
        /// </summary>
        public int SelectedIndex
        {
            get { return _selectedIndices.Count == 0 ? -1 : _selectedIndices[0]; }
            set { SetSelection(value); }
        }

        /// <summary>
        /// The values of the currently selected items.
        /// </summary>
        public IEnumerable<object> SelectedItems
        {
            get { return _selectedItems; }
        }

        public Rect ViewportRect
        {
            get { return _viewportRect; }
        }

        /// <summary>
        /// The padding between an item's <see cref="Rect"/> and it's content.
        /// </summary>
        public float Padding
        {
            get { return _padding; }
            set { _padding = value; }
        }

        public float Margin
        {
            get { return _margin; }
            set { _margin = value; }
        }

        public Color ItemSelectedColor { get; set; } = new Color(0.24f, 0.37f, 0.58f);

        public Color ItemHoverColor { get; set; } = new Color(0.187f, 0.187f, 0.187f);

        public Color ItemLostFocusColor { get; set; } = new Color(0.3f, 0.3f, 0.3f);

        public abstract int MaxVisibleItems { get; }

        public event Action<IEnumerable<object>> OnSelectionChange;
        public event Action<IEnumerable<object>> OnItemsChosen;

      
        public void DrawLayout()
        {
            Draw(GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)));
        }

        public virtual void Draw(Rect rect)
        {
            var evt = Event.current;

            if (evt.type != EventType.Layout)
                _viewportRect = rect;

            _containsMouse = _viewportRect.Contains(Event.current.mousePosition);

            GetFirstLastVisibleIndices(_viewportRect, out int first, out int last);

            DrawItems(_viewportRect, first, last);
            

            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Return)
                    HandleOnItemsChosen();

                OnKeyDown(evt.keyCode, evt);
            }

            if (!LibraryUtility.HasCurrentWindowKeyFocus())
            {
                HasLostFocus = true;
            }
            else
            {
                HasLostFocus = false;
            }

            if (_needsRepaint && EditorWindow.mouseOverWindow != null)
            {
                EditorWindow.mouseOverWindow.Repaint();
                _needsRepaint = false;
            }
        }

        protected void DrawItems(Rect rect, int firstVisibleIndex, int lastVisibleIndex)
        {
            _contentViewRect = new Rect(rect.x, rect.y, rect.width - 20, GetTotalScrollableHeight());
            _scrollPosition = GUI.BeginScrollView(rect, _scrollPosition, _contentViewRect, false, false);
            
            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                DrawItem(MarginItemRect(GetItemRect(i)), i);
            }
            
            GUI.EndScrollView();

            if (Event.current.type == EventType.MouseMove)
            {
                var index = IndexFromPosition(Event.current.mousePosition);

                var itemRect = MarginItemRect(GetItemRect(index));
                itemRect.y -= _scrollPosition.y;
                bool contains = itemRect.Contains(Event.current.mousePosition);

                if (contains && index != _hoveredIndex)
                {
                    _hoveredIndex = index;
                    Repaint();
                }

                if (!contains && _hoveredIndex != -1)
                {
                    _hoveredIndex = -1;
                    Repaint();
                }
            }
        }

        private void DrawItem(Rect rect, int index)
        {
            int controlID = GUIUtility.GetControlID("ItemsViewItem".GetHashCode(), FocusType.Keyboard, rect);

            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlID);
            bool containsMouse = rect.Contains(evt.mousePosition) && _containsMouse;

            bool previouslyLostFocus = false;
            
            switch (eventType)
            {
                case EventType.MouseDown:
                    if (containsMouse)
                    {
                        HasLostFocus = false;
                        previouslyLostFocus = true;

                        if (containsMouse && evt.button == 0 && evt.clickCount == 2)
                        {
                            HandleOnItemsChosen();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (containsMouse && evt.button == 0)
                    {
                        if (_multiSelect && evt.control) // Ctrl-click item.
                        {
                            RangeSelectOriginIndex = index;
                            if (_selectedIndices.Contains(index))
                                RemoveFromSelection(index);
                            else
                                AddToSelection(index);
                        }
                        else if (_multiSelect && evt.shift) // Shift-click item.
                        {
                            if (RangeSelectOriginIndex == -1)
                            {
                                RangeSelectOriginIndex = index;
                                SetSelection(index);
                            }
                            else
                            {
                                SelectRange(index, RangeSelectOriginIndex);
                            }
                        }
                        else if (_multiSelect && _selectedIndices.Contains(index) && previouslyLostFocus)
                        {
                            SetSelection(index);
                        }
                        else
                        {
                            HandleSingleClick(index);
                        }

                        LastSelectedIndex = index;
                        GUIUtility.keyboardControl = controlID;
                        HasLostFocus = false;
                        
                        Repaint();
                    }
                    break;
                case EventType.Repaint:
                    if (_selectedIndices.Contains(index))
                    {
                        if (HasLostFocus && containsMouse)
                        {
                            EditorGUI.DrawRect(rect, ItemHoverColor);
                            DrawRectBorder(rect, 1, new Color(0.5f, 0.5f, 0.5f));
                        }
                        else if (HasLostFocus)
                        {
                            DrawRectBorder(rect, 1, new Color(0.5f, 0.5f, 0.5f));
                        }
                        else
                        {
                            EditorGUI.DrawRect(rect, ItemHoverColor);
                            DrawRectBorder(rect, 1, ItemSelectedColor);
                        }
                    }
                    else if (containsMouse)
                    {
                        EditorGUI.DrawRect(rect, ItemHoverColor);
                    }
                    break;
            }

            OnDrawItem(PadItemRect(rect), index, _selectedIndices.Contains(index), containsMouse, !HasLostFocus);
        }

        private void DrawRectBorder(Rect rect, float width, Color color)
        {
            var top = new Rect(rect)
            {
                height = width
            };

            var bottom = new Rect(rect)
            {
                y = rect.y + rect.height - width,
                height = width
            };

            var left = new Rect(rect)
            {
                y = rect.y + width,
                width = width,
                height = rect.height - width * 2
            };

            var right = new Rect(rect)
            {
                x = rect.x + rect.width - width,
                y = rect.y + width,
                width = width,
                height = rect.height - width * 2
            };

            EditorGUI.DrawRect(top, color);
            EditorGUI.DrawRect(bottom, color);
            EditorGUI.DrawRect(left, color);
            EditorGUI.DrawRect(right, color);
        }

        protected void SelectRangeFromOriginToLast()
        {
            if (_multiSelect && Event.current.shift)
            {
                int fromIndex = Mathf.Min(LastSelectedIndex, RangeSelectOriginIndex);
                int toIndex = Mathf.Max(LastSelectedIndex, RangeSelectOriginIndex);
                ClearSelectionWithoutNotify();
                for (int i = fromIndex; i <= toIndex; i++)
                {
                    AddToSelectionWithoutNotify(i);
                }
                HandleSelectionChange();
            }
            else
            {
                RangeSelectOriginIndex = LastSelectedIndex;

                SetSelection(LastSelectedIndex);
            }
        }

        protected void Repaint()
        {
            _needsRepaint = true;
        }

        /// <summary>
        /// Returns the total height of the ScrollView area.
        /// </summary>
        protected virtual float GetTotalScrollableHeight()
        {
            return Items.Count * ItemSize.y;
        }

        protected virtual void OnKeyDown(KeyCode keyCode, Event evt)
        {

        }

        protected int FirstVisibleRowIndex()
        {
            if (_scrollPosition.y > 0)
                return (int)Mathf.Max(0, Mathf.Floor(_scrollPosition.y / ItemSize.y));
            else
                return 0;
        }

        private void HandleSingleClick(int index)
        {
            RangeSelectOriginIndex = index;
            SetSelection(index);
        }

        protected abstract Rect GetItemRect(int index);

        private Rect PadItemRect(Rect rect)
        {
            return new Rect(rect)
            {
                x = rect.x + Padding,
                y = rect.y + Padding,
                width = rect.width - (Padding * 2),
                height = rect.height - (Padding * 2)
            };
        }

        private Rect MarginItemRect(Rect rect)
        {
            return new Rect(rect)
            {
                x = rect.x + Margin,
                y = rect.y + Margin,
                width = rect.width - (Margin * 2),
                height = rect.height - (Margin * 2)
            };
        }

        /// <summary>
        /// Gets the first visible item index and teh last visible index.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public abstract void GetFirstLastVisibleIndices(Rect rect, out int first, out int last);

        public abstract int IndexFromPosition(Vector3 position);


        public void ScrollToRect(Rect rect)
        {
            float top = rect.y;
            float bottom = rect.yMax;
            float viewHeight = _viewportRect.height;

            if (bottom > viewHeight + _scrollPosition.y)
            {
                _scrollPosition.y = bottom - viewHeight;
            }
            if (top < _scrollPosition.y)
            {
                _scrollPosition.y = top;
            }

            _scrollPosition.y = Mathf.Max(_scrollPosition.y, 0f);
        }

        protected void SelectRange(int indexA, int indexB)
        {
            ClearSelectionWithoutNotify();
            int fromIndex = Mathf.Min(indexA, indexB);
            int toIndex = Mathf.Max(indexA, indexB);
            for (int i = fromIndex; i <= toIndex; i++)
            {
                AddToSelectionWithoutNotify(i);
            }
            HandleSelectionChange();
        }

        public void SetSelection(int index)
        {
            SetSelection(new int[] { index });
        }

        public void SetSelection(IEnumerable<int> indices)
        {
            SetSelectionWithoutNotify(indices);

            HandleSelectionChange();
        }

        public void SetSelectionWithoutNotify(IEnumerable<int> indices)
        {
            ClearSelectionWithoutNotify();

            foreach (var index in indices)
                AddToSelectionWithoutNotify(index);
        }

        public void AddToSelection(int index)
        {
            AddToSelectionWithoutNotify(index);
            HandleSelectionChange();
        }

        public void AddToSelectionWithoutNotify(int index)
        {
            // The index can be out of range if it was the last item and the asset was deleted or otherwise removed.
            if (index > Items.Count - 1 || index < 0 || _selectedIndices.Contains(index))
                return;

            _selectedIndices.Add(index);
            _selectedItems.Add(Items[index]);
            Repaint();
        }

        public void ClearSelection()
        {
            ClearSelectionWithoutNotify();
            HandleSelectionChange();
        }

        public void ClearSelectionWithoutNotify()
        {
            _selectedIndices.Clear();
            _selectedItems.Clear();
            Repaint();
        }

        public void RemoveFromSelection(int index)
        {
            if (!_selectedIndices.Contains(index))
                return;

            var item = Items[index];

            _selectedIndices.Remove(index);
            _selectedItems.Remove(item);
            HandleSelectionChange();
            Repaint();
        }

        public bool IsSelected(int index)
        {
            return _selectedIndices.Contains(index);
        }

        protected void HandleSelectionChange()
        {
            OnSelectionChange?.Invoke(_selectedItems);
        }

        protected void HandleOnItemsChosen()
        {
            if (_selectedItems.Count > 0)
                OnItemsChosen?.Invoke(SelectedItems);
        }
    } 
}
