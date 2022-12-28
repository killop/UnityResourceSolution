using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class RectUtility
    {
        public static Rect Pad(this Rect rect, float left, float right, float top, float bottom)
        {
            rect.xMin += left;
            rect.xMax -= right;
            rect.yMin += top;
            rect.yMax -= bottom;
            return rect;
        }

        public static Rect Pad(this Rect rect, float horizontal, float vertical)
        {
            return Pad(rect, horizontal, horizontal, vertical, vertical);
        }

        public static Rect Pad(this Rect rect, float padding)
        {
            return Pad(rect, padding, padding, padding, padding);
        }
    }
}