using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class DaiGUIUtility
    {
        public static bool useLinearTextures => PlayerSettings.colorSpace == ColorSpace.Linear;

        public static void HorizontalSeparator()
        {
            GUILayout.Box(GUIContent.none, DaiGUIStyles.horizontalSeparator);
        }

        public static void HorizontalSeparator(float x, float y, float width)
        {
            var darkColor = ColorPalette.DarkLineColor.WithAlphaMultiplied(0.5f);
            var lightColor = ColorPalette.LightLineColor.WithAlphaMultiplied(0.5f);
            var rect = new Rect(x, y - 1, width, 1);
            EditorGUI.DrawRect(rect, darkColor);
            rect.y += 1;
            EditorGUI.DrawRect(rect, lightColor);
        }

        public static void VerticalSeparator()
        {
            GUILayout.Box(GUIContent.none, DaiGUIStyles.verticalSeparator);
        }

        public static void VerticalSeparator(float x, float y, float height)
        {
            var darkColor = ColorPalette.DarkLineColor.WithAlphaMultiplied(0.5f);
            var lightColor = ColorPalette.LightLineColor.WithAlphaMultiplied(0.5f);
            var rect = new Rect(x - 1, y, 1, height);
            EditorGUI.DrawRect(rect, darkColor);
            rect.x += 1;
            EditorGUI.DrawRect(rect, lightColor);
        }

        public static void CenterMiddleLabel(string text, GUIStyle style = null)
        {
            BeginCenterLayout();
            EditorGUILayout.LabelField(text, style);
            EndCenterLayout();
        }

        public static void BeginCenterLayout()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        }

        public static void EndCenterLayout()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        public static bool IconButton(Rect rect, EditorTexture icon)
        {
            return IconButton(rect, icon, null, "");
        }

        public static bool IconButton(Rect rect, EditorTexture icon, string tooltip)
        {
            return IconButton(rect, icon, null, tooltip);
        }

        public static bool IconButton(Rect rect, EditorTexture icon, GUIStyle style, string tooltip)
        {
            style = style ?? DaiGUIStyles.iconButton;
            var tex = icon.Get(rect);
            return GUI.Button(rect, EditorGUIUtility.TrIconContent(tex, tooltip), style);
        }
    }
}