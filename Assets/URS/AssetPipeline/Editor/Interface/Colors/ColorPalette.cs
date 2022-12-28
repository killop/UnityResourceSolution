using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class ColorPalette
    {
        public static readonly SkinnedColor BackgroundDarker = new SkinnedColor(0.33f, 0.1f);
        public static readonly SkinnedColor BackgroundDark = new SkinnedColor(0.64f, 0.16f);
        public static readonly SkinnedColor Background = new SkinnedColor(0.76f, 0.22f);
        public static readonly SkinnedColor BackgroundLight = new SkinnedColor(0.87f, 0.24f);
        public static readonly SkinnedColor BackgroundLighter = new SkinnedColor(0.957f, 0.264f);
        public static readonly SkinnedColor Foreground = new SkinnedColor(0.0f, 0.81f);
        public static readonly SkinnedColor ForegroundDim = Foreground.WithAlphaMultiplied(0.40f, 0.40f);
        public static readonly SkinnedColor ForegroundDimer = Foreground.WithAlphaMultiplied(0.20f, 0.20f);
        public static readonly SkinnedColor ForegroundSelected = new SkinnedColor(1.0f, 1.0f);
        public static readonly SkinnedColor SelectionHighlight = new SkinnedColor(new Color(0.24f, 0.49f, 0.91f), new Color(0.20f, 0.38f, 0.57f));

        public static readonly SkinnedColor CardBackgroundColor = new SkinnedColor(1, 0.45f, 1, 0.25f);
        public static readonly SkinnedColor BorderColor = new SkinnedColor(0.38f, 0.6f, 0.11f, 0.8f);
        public static readonly SkinnedColor BoxBackgroundColor = new SkinnedColor(1, 0.5f, 1, 0.05f);
        public static readonly SkinnedColor DarkEditorBackground = new SkinnedColor(0, 0, 0.192f, 1);
        public static readonly SkinnedColor EditorWindowBackgroundColor = new SkinnedColor(0.76f, 0.22f);
        public static readonly SkinnedColor MenuBackgroundColor = new SkinnedColor(0.87f, 1, 1, 0.035f);
        public static readonly SkinnedColor HeaderBoxBackgroundColor = new SkinnedColor(1, 0.26f, 1, 0.06f);
        public static readonly SkinnedColor HighlightedButtonColor = new SkinnedColor(Color.green);
        public static readonly SkinnedColor HighlightedTextColor = new SkinnedColor(Color.black, Color.white);
        public static readonly SkinnedColor HighlightPropertyColor = new SkinnedColor(0, 0.6f, 1, 0.6f);
        public static readonly SkinnedColor ListItemColorEven = new SkinnedColor(0.838f, 0.235f);
        public static readonly SkinnedColor ListItemColorHoverEven = new SkinnedColor(0.89f, 0.2232f);
        public static readonly SkinnedColor ListItemColorHoverOdd = new SkinnedColor(0.904f, 0.2472f);
        public static readonly SkinnedColor ListItemColorOdd = new SkinnedColor(0.788f, 0.216f);
        public static readonly SkinnedColor ListItemDragBg = new SkinnedColor(0.1f);
        public static readonly SkinnedColor ListItemDragBgColor = new SkinnedColor(0.338f, 0.1f);
        public static readonly SkinnedColor ColumnTitleBg = new SkinnedColor(1, 0.019f, 1, 0.019f);
        public static readonly SkinnedColor ListItemEven = new SkinnedColor(0.4f);
        public static readonly SkinnedColor ListItemOdd = new SkinnedColor(0.4f);
        public static readonly SkinnedColor MenuButtonActiveBgColor = new SkinnedColor(new Color(0.243f, 0.49f, 0.9f, 1.000f), new Color(0.243f, 0.373f, 0.588f, 1.000f));
        public static readonly SkinnedColor MenuButtonBorderColor = new SkinnedColor(0.608f);
        public static readonly SkinnedColor MenuButtonColor = Color.clear;
        public static readonly SkinnedColor MenuButtonHoverColor = new SkinnedColor(1, 0.08f, 1, 0.08f);
        public static readonly SkinnedColor LightBorderColor = new SkinnedColor(0.3529412f);
        public static readonly SkinnedColor DarkLineColor = new SkinnedColor(new Color(0, 0, 0, 0.2f), BorderColor);
        public static readonly SkinnedColor LightLineColor = new SkinnedColor(Color.white, new Color(1, 1, 1, 0.103f));
    }
}