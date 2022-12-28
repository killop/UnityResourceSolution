using Daihenka.AssetPipeline.Filters;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(PathFilter))]
    internal class PathFilterPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var ignoreCaseProp = property.FindPropertyRelative("ignoreCase");

            var dropdownRect = position;
            dropdownRect.width = 80;
            var textFieldRect = position;
            textFieldRect.xMin += 83;
            textFieldRect.xMax -= 24;
            var toggleRect = position;
            toggleRect.xMin = toggleRect.xMax - 20;
            toggleRect.height = 20;
            toggleRect.y -= 1;

            EditorGUI.PropertyField(dropdownRect, property.FindPropertyRelative("matchType"), GUIContent.none);
            EditorGUI.PropertyField(textFieldRect, property.FindPropertyRelative("pattern"), GUIContent.none);
            var ignoreCase = ignoreCaseProp.boolValue;
            if (GUI.Button(toggleRect, DaiGUIContent.ignoreCase, ignoreCase ? DaiGUIStyles.ignoreCaseOn : DaiGUIStyles.ignoreCaseOff))
            {
                ignoreCaseProp.boolValue = !ignoreCase;
            }

            EditorGUI.EndProperty();
        }
    }
}