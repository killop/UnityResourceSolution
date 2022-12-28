using System.Collections.Generic;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class ColorUtility
    {
        static ColorUtility()
        {
            pixels = new Dictionary<Color, Texture2D>();
        }

        static readonly Dictionary<Color, Texture2D> pixels;

        public static Color Gray(float brightness)
        {
            return new Color(brightness, brightness, brightness, 1f);
        }

        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color WithAlphaMultiplied(this Color color, float alphaMultiplier)
        {
            color.a *= alphaMultiplier;
            return color;
        }


        public static Texture2D GetPixel(this SkinnedColor color)
        {
            return GetPixel(color.color);
        }

        public static Texture2D GetPixel(this Color color)
        {
            if (!pixels.ContainsKey(color))
            {
                var pixel = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
                pixel.SetPixel(0, 0, color);
                pixel.hideFlags = HideFlags.HideAndDontSave;
                pixel.filterMode = FilterMode.Point;
                pixel.Apply();
                pixels.Add(color, pixel);
            }

            return pixels[color];
        }

        public static string ToHexString(this Color color)
        {
            return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", (byte) (color.r * 255), (byte) (color.g * 255), (byte) (color.b * 255), (byte) (color.a * 255));
        }

        public static GUIStyle CreateBackground(this Color color)
        {
            return new GUIStyle {normal = {background = color.GetPixel()}};
        }

        public static GUIStyle CreateBackground(this SkinnedColor skinnedColor)
        {
            return skinnedColor.color.CreateBackground();
        }
    }
}