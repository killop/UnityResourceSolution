using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal struct SkinnedColor
    {
        static bool isProSkin => EditorGUIUtility.isProSkin;

        readonly Color lightColor;
        readonly Color darkColor;

        public Color color => isProSkin ? darkColor : lightColor;

        public SkinnedColor(float gray) : this(gray, gray)
        {
        }

        public SkinnedColor(float lightGray, float darkGray)
        {
            lightColor = ColorUtility.Gray(lightGray);
            darkColor = ColorUtility.Gray(darkGray);
        }

        public SkinnedColor(float lightGray, float lightAlpha, float darkGray, float darkAlpha)
        {
            lightColor = ColorUtility.Gray(lightGray).WithAlpha(lightAlpha);
            darkColor = ColorUtility.Gray(darkGray).WithAlpha(darkAlpha);
        }

        public SkinnedColor(Color light, Color dark)
        {
            lightColor = light;
            darkColor = dark;
        }

        public SkinnedColor(Color color) : this(color, color)
        {
        }

        public static implicit operator Color(SkinnedColor skinnedColor)
        {
            return skinnedColor.color;
        }

        public static implicit operator SkinnedColor(Color color)
        {
            return new SkinnedColor(color);
        }

        public string ToHexString()
        {
            return color.ToHexString();
        }

        public override string ToString()
        {
            return ToHexString();
        }

        public SkinnedColor WithAlpha(float alpha)
        {
            return new SkinnedColor(lightColor.WithAlpha(alpha), darkColor.WithAlpha(alpha));
        }

        public SkinnedColor WithAlpha(float lightAlpha, float darkAlpha)
        {
            return new SkinnedColor(lightColor.WithAlpha(lightAlpha), darkColor.WithAlpha(darkAlpha));
        }

        public SkinnedColor WithAlphaMultiplied(float alphaMultiplier)
        {
            return new SkinnedColor(lightColor.WithAlphaMultiplied(alphaMultiplier), darkColor.WithAlphaMultiplied(alphaMultiplier));
        }

        public SkinnedColor WithAlphaMultiplied(float lightAlphaMultiplier, float darkAlphaMultiplier)
        {
            return new SkinnedColor(lightColor.WithAlphaMultiplied(lightAlphaMultiplier), darkColor.WithAlphaMultiplied(darkAlphaMultiplier));
        }
    }
}