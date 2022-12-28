using UnityEngine;

namespace Bewildered.SmartLibrary.UI
{
    internal class GridViewControl : ItemsViewControlBase
    {
        private int _columnCount;
        private int _rowCount;

        private float _contentViewWidth;

        public override int MaxVisibleItems
        {
            get 
            {
                int columns = Mathf.Max(1, Mathf.FloorToInt(ViewportRect.width / ItemSize.x));
                int rows = Mathf.Max(1, Mathf.CeilToInt(ViewportRect.height / ItemSize.y));
                return columns * rows;
            }
        }

        public override void Draw(Rect rect)
        {
            // The rect height will be 1 on layout, so we need to only calculate the width when it is not a layout event.
            if (Event.current.type != EventType.Layout)
            {
                // If there is a scroll bar then we subtract 11 from the width to account for it.
                var totalHeight = GetTotalScrollableHeight();
                _contentViewWidth = totalHeight > rect.height ? rect.width - 11 : rect.width;
            }
            
            _columnCount = Mathf.Max(1, Mathf.FloorToInt(_contentViewWidth / ItemSize.x));
            _rowCount = Mathf.Max(1, Mathf.CeilToInt(Items.Count / (float)_columnCount));

            base.Draw(rect);
        }

        public override int IndexFromPosition(Vector3 position)
        {
            int columnIndex = (int)(position.x / ItemSize.x);
            if (columnIndex >= _columnCount)
                return -1;
            
            int rowIndex = (int)Mathf.Max(0, Mathf.Floor((position.y + ScrollPosition.y) / ItemSize.y));

            return (rowIndex * _columnCount) + columnIndex;
        }

        public override void GetFirstLastVisibleIndices(Rect rect, out int first, out int last)
        {
            first = FirstVisibleRowIndex() * _columnCount;

            int visibleRowCount = Mathf.CeilToInt(rect.height / ItemSize.y);

            // Add an extra row to avoid poping in and out when scrolling.
            visibleRowCount++;

            last = Mathf.Min(Items.Count, visibleRowCount * _columnCount + first) - 1;
        }

        protected override void OnKeyDown(KeyCode keyCode, Event evt)
        {
            bool wasMovementEvent = false;
            switch (keyCode)
            {
                case KeyCode.UpArrow:
                    NonePreviouslySelected();

                    if (LastSelectedIndex >= _columnCount)
                    {
                        LastSelectedIndex -= _columnCount;
                        SelectRangeFromOriginToLast();
                    }

                    wasMovementEvent = true;
                    break;
                case KeyCode.DownArrow:
                    NonePreviouslySelected();

                    int unusedColumCount = _columnCount - (Items.Count % _columnCount);
                    if (LastSelectedIndex + _columnCount < Items.Count + unusedColumCount)
                    {
                        LastSelectedIndex = Mathf.Min(LastSelectedIndex + _columnCount, Items.Count - 1);
                        SelectRangeFromOriginToLast();
                    }

                    wasMovementEvent = true;
                    break;
                case KeyCode.LeftArrow:
                    NonePreviouslySelected();

                    if (LastSelectedIndex > 0)
                    {
                        LastSelectedIndex--;
                        SelectRangeFromOriginToLast();
                    }

                    wasMovementEvent = true;
                    break;
                case KeyCode.RightArrow:
                    NonePreviouslySelected();

                    if (LastSelectedIndex + 1 < Items.Count)
                    {
                        LastSelectedIndex++;
                        SelectRangeFromOriginToLast();
                    }

                    wasMovementEvent = true;
                    break;
                case KeyCode.Home:
                    LastSelectedIndex = 0;
                    SetSelection(LastSelectedIndex);

                    wasMovementEvent = true;
                    break;
                case KeyCode.End:
                    LastSelectedIndex = Items.Count - 1;
                    SetSelection(LastSelectedIndex);

                    wasMovementEvent = true;
                    break;
                case KeyCode.Return:
                    HandleOnItemsChosen();
                    evt.Use();
                    break;
            }

            if (wasMovementEvent)
            {
                ScrollToRect(GetItemRect(LastSelectedIndex));
                Repaint();
                evt.Use();
            }
        }

        private void NonePreviouslySelected()
        {
            if (SelectedIndex == -1)
                LastSelectedIndex = 0;
        }

        protected override Rect GetItemRect(int itemIndex)
        {
            float row = Mathf.Floor(itemIndex / _columnCount);
            float column = itemIndex - (row * _columnCount);

            return new Rect()
            {
                x = ItemSize.x * column,
                y = ItemSize.y * row,
                width = ItemSize.x,
                height = ItemSize.y
            };
        }

        protected override float GetTotalScrollableHeight()
        {
            return _rowCount * ItemSize.y;
        }
    }
}