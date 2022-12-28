using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(ExtractMaterials))]
    internal class ExtractMaterialsInspector : AssetProcessorInspector
    {
        SerializedProperty m_PathType;
        SerializedProperty m_Destination;
        SerializedProperty m_TargetFolder;
        SerializedProperty m_FileExistsAction;

        protected override void OnEnable()
        {
            m_PathType = serializedObject.FindProperty("pathType");
            m_Destination = serializedObject.FindProperty("destination");
            m_TargetFolder = serializedObject.FindProperty("targetFolder");
            m_FileExistsAction = serializedObject.FindProperty("fileExistsAction");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBaseProperties();
            EditorGUILayout.PropertyField(m_PathType);
            var enumValueIndex = m_PathType.enumValueIndex;
            if (enumValueIndex == (int) MaterialPathType.Relative || enumValueIndex == (int) MaterialPathType.Absolute)
            {
                EditorGUILayout.PropertyField(m_Destination);
                DrawTemplateVariables();
            }
            else if (enumValueIndex == (int) MaterialPathType.TargetFolder)
            {
                EditorGUILayout.PropertyField(m_TargetFolder, DaiGUIContent.destination);
            }

            EditorGUILayout.PropertyField(m_FileExistsAction, new GUIContent("Material Exists Action"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}