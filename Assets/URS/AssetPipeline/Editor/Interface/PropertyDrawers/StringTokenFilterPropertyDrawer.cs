using System.Text.RegularExpressions;
using Daihenka.AssetPipeline.Filters;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(StringTokenFilter))]
    internal class StringTokenFilterPropertyDrawer : PropertyDrawer
    {
        const int kLineSpacing = 2;
        const string kImbalancedError = "\nImbalanced parentheses detected\n";
        static GUIStyle s_HelpboxStyle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + kLineSpacing;
            height += CalculateHelpBoxHeight(GetTokenMessage(property));
            if (!IsValid(property))
            {
                height += CalculateHelpBoxHeight(kImbalancedError) + kLineSpacing;
            }

            return height;
        }

        static float CalculateHelpBoxHeight(string text)
        {
            if (s_HelpboxStyle == null)
            {
                s_HelpboxStyle = GUI.skin.GetStyle("helpbox");
            }

            return s_HelpboxStyle.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth);
        }

        static string GetTokenMessage(SerializedProperty property)
        {
            var pattern = property.FindPropertyRelative("rulePattern").stringValue;
            var groupCount = new Regex(pattern).GetGroupNumbers().Length;
            var message = "The following tokens can be used:\n(assetName)\tAsset Filename\n(assetExt)\tAsset File Extension\n(.)\t\tAsset Folder Name\n(../)\t\tRelative Parent Folder Name";
            for (var i = 1; i < groupCount; i++)
            {
                message += $"\n(${i})\t\tWildcard {i}";
            }

            return message;
        }

        static bool IsValid(SerializedProperty property)
        {
            var str = property.FindPropertyRelative("name").stringValue;
            return str.Split('(').Length == str.Split(')').Length;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rawPosition = position;
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            position.height = EditorGUIUtility.singleLineHeight;
            var rect = position;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("name"), GUIContent.none);

            rect = rawPosition;
            rect.y += kLineSpacing + EditorGUIUtility.singleLineHeight;
            if (!IsValid(property))
            {
                rect.height = CalculateHelpBoxHeight(kImbalancedError);
                EditorGUI.HelpBox(rect, kImbalancedError, MessageType.Error);
                rect.y += kLineSpacing + rect.height;
            }

            var message = GetTokenMessage(property);
            rect.height = CalculateHelpBoxHeight(message);

            EditorGUI.HelpBox(rect, message, MessageType.None);
            EditorGUI.EndProperty();
        }
    }
}