// Copyright 2021 by Hextant Studios. https://HextantStudios.com
// This work is licensed under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/
using UnityEditor;

namespace Hextant.Editor
{
    // A custom inspector for Settings that does not draw the "Script" field.
    [CustomEditor( typeof( Settings<> ), true )]
    public class SettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI() => DrawDefaultInspector();

        // Draws the UI for exposed properties *without* the "Script" field.
        protected new bool DrawDefaultInspector()
        {
            if( serializedObject.targetObject == null ) return false;

            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();

            DrawPropertiesExcluding( serializedObject, _excludedFields );

            serializedObject.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }

        static readonly string[] _excludedFields = { "m_Script" };
    }
}
