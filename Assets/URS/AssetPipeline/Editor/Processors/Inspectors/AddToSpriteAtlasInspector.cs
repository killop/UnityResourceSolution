using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(AddToSpriteAtlas))]
    internal class AddToSpriteAtlasInspector : AssetProcessorInspector
    {
        SerializedProperty m_AddFolderToSpriteAtlas;
        SerializedProperty m_PathType;
        SerializedProperty m_Destination;
        SerializedProperty m_TargetFolder;
        SerializedProperty m_SpriteAtlasName;
        SerializedProperty m_IncludeInBuild;
        SerializedProperty m_CreateVariantAtlas;
        SerializedProperty m_VariantSuffix;
        SerializedProperty m_IncludeVariantInBuild;
        SerializedProperty m_VariantScale;

        protected override void OnEnable()
        {
            m_AddFolderToSpriteAtlas = serializedObject.FindProperty("addFolderToSpriteAtlas");
            m_PathType = serializedObject.FindProperty("pathType");
            m_Destination = serializedObject.FindProperty("destination");
            m_TargetFolder = serializedObject.FindProperty("targetFolder");
            m_SpriteAtlasName = serializedObject.FindProperty("spriteAtlasName");
            m_IncludeInBuild = serializedObject.FindProperty("includeInBuild");
            m_CreateVariantAtlas = serializedObject.FindProperty("createVariantAtlas");
            m_VariantSuffix = serializedObject.FindProperty("variantSuffix");
            m_IncludeVariantInBuild = serializedObject.FindProperty("includeVariantInBuild");
            m_VariantScale = serializedObject.FindProperty("variantScale");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBaseProperties();
            EditorGUILayout.PropertyField(m_AddFolderToSpriteAtlas);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SpriteAtlas Creation Settings (If SpriteAtlas Missing)", DaiGUIStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_PathType);
            var enumValueIndex = m_PathType.enumValueIndex;
            if (enumValueIndex == (int) TargetPathType.Relative || enumValueIndex == (int) TargetPathType.Absolute)
            {
                EditorGUILayout.PropertyField(m_Destination);
                DrawTemplateVariables();
            }
            else if (enumValueIndex == (int) TargetPathType.TargetFolder)
            {
                EditorGUILayout.PropertyField(m_TargetFolder, DaiGUIContent.destination);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Master SpriteAtlas", DaiGUIStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SpriteAtlasName);
            DrawTemplateVariables();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_IncludeInBuild);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Variant SpriteAtlas", DaiGUIStyles.boldLabel);
            EditorGUILayout.PropertyField(m_CreateVariantAtlas);
            if (m_CreateVariantAtlas.boolValue)
            {
                EditorGUILayout.PropertyField(m_VariantSuffix);
                EditorGUILayout.PropertyField(m_IncludeVariantInBuild);
                EditorGUILayout.Slider(m_VariantScale, 0.01f, 1f);
                // Test if the multiplier scale a power of two size (1024) into another power of 2 size.
                if (!Mathf.IsPowerOfTwo((int) (m_VariantScale.floatValue * 1024)))
                {
                    EditorGUILayout.HelpBox(DaiGUIContent.notPowerOfTwoWarning.text, MessageType.Warning, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}