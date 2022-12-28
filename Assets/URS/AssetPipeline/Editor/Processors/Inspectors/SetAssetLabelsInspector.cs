using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(SetAssetLabels))]
    internal class SetAssetLabelsInspector : AssetProcessorInspector
    {
        ReorderableList m_List;

        protected override void OnEnable()
        {
            m_List = new ReorderableList(serializedObject, serializedObject.FindProperty("labels"), true, true, true, true);
            m_List.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Asset Labels");
            m_List.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = m_List.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBaseProperties();
            GUI.enabled = true;
            m_List.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}