using Daihenka.AssetPipeline.NamingConvention;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(NamingConventionRule))]
    public class NamingConventionRulePropertyDrawer : PropertyDrawer
    {
        const float kVerticalSpacer = 3;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + kVerticalSpacer;
        }

        public static class Styles
        {
            public static GUIContent anchor = new GUIContent("", "Where should the path match\nContains\tAnywhere in path\nStart\t\tStart of path\nEnd\t\tEnd of path\nExact\t\tExact match\nAll Paths\tAll paths will be a match");

            public static GUIContent pattern = new GUIContent("", @"Path pattern to match
Variables can be specified by wrapping the variable name in {} braces

Variable Examples
-----------------
""Assets/Textures/Characters/{name}/"" matches ""Assets/Textures/Characters/Alice/"" with the value of {name} equaling ""Alice""

""{textureName}_albedo""

""{textureName}_{textureType:albedo|emission}"" matches where {textureType} is albedo or emission

""{assetName}_v{version:\d\{3\}}"" matches where {version} is a 3 digit number (e.g. 001)


Variable String Convention
-------------------------
This optional variable parameter allows the option to match a specific casing convention.

This will override the default string convention set in the project settings for the variable.

{varName:\none}		Any case
{varName:\snake}	snake_case
{varName:\usnake}	UPPER_SNAKE_CASE
{varName:\kebab}	kebab-case
{varName:\camel}	camelCase
{varName:\pascal}	PascalCase
{varName:\upper}	UPPER CASE
{varName:\lower}	lower case");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            position.height = EditorGUIUtility.singleLineHeight;
            var anchorRect = position;
            anchorRect.width = 70;
            var patternRect = position;
            patternRect.xMin += anchorRect.width + 3;
            var anchorProp = property.FindPropertyRelative("anchor");
            EditorGUI.PropertyField(anchorRect, anchorProp, GUIContent.none);
            EditorGUI.LabelField(anchorRect, Styles.anchor);
            if (anchorProp.enumValueIndex != (int) Anchor.AllPaths)
            {
                EditorGUI.PropertyField(patternRect, property.FindPropertyRelative("pattern"), GUIContent.none);
                EditorGUI.LabelField(patternRect, Styles.pattern);
            }
            else
            {
                EditorGUI.LabelField(patternRect, "This will match all files of this asset type");
            }

            property.serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }
    }
}