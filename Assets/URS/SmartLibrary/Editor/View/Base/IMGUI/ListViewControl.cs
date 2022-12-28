using UnityEngine;

namespace Bewildered.SmartLibrary.UI
{
    internal class ListViewControl : ItemsViewControlBase
    {
        private float _contentViewWidth;

        public override int MaxVisibleItems
        {
            get { return Mathf.CeilToInt(ViewportRect.height / ItemSize.y); }
        }

        public override void Draw(Rect rect)
        {
            // The rect height will be 1 on layout, so we need to only calculate the width when it is not a layout event.
            if (Event.current.type != EventType.Layout)
            {
                // If there is a scroll bar then we subtract 10 from the width to account for it.
                var totalHeight = GetTotalScrollableHeight();
                _contentViewWidth = totalHeight > rect.height ? rect.width - 12 : rect.width;
            }

            ItemSize = new Vector2(_contentViewWidth, ItemSize.y);

            base.Draw(rect);
        }

        public override int IndexFromPosition(Vector3 position)
        {
            return (int)Mathf.Max(0, Mathf.Floor((position.y + ScrollPosition.y) / ItemSize.y));
        }

        public override void GetFirstLastVisibleIndices(Rect rect, out int first, out int last)
        {
            first = FirstVisibleRowIndex();
            last = Mathf.Min(Items.Count - 1, first + Mathf.CeilToInt(rect.height / ItemSize.y));
        }

        protected override void OnKeyDown(KeyCode keyCode, Event evt)
        {
            bool wasMovementEvent = false;

            switch (keyCode)
            {
                case KeyCode.UpArrow:
                    NonePreviouslySelected();
                    if (LastSelectedIndex > 0)
                    {
                        LastSelectedIndex--;
                        SelectRangeFromOriginToLast();
                    }

                    wasMovementEvent = true;
                    break;
                case KeyCode.DownArrow:
                    NonePreviouslySelected();
                    if (LastSelectedIndex < Items.Count - 1)
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
                    if (SelectedIndex >= 0 && SelectedIndex <= Items.Count - 1)
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

        protected override Rect GetItemRect(int index)
        {
            return new Rect
            {
                x = 0,
                y = ItemSize.y * index,
                width = _contentViewWidth,
                height = ItemSize.y
            };
        }
    }

}