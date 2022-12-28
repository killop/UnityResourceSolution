using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(PackTexture2D))]
    internal class PackTexture2DInspector : AssetProcessorInspector
    {
        SerializedProperty m_TextureName;
        SerializedProperty m_TextureSize;
        SerializedProperty m_IsLinear;
        SerializedProperty m_RedChannel;
        SerializedProperty m_RedChannelTextureFilter;
        SerializedProperty m_GreenChannel;
        SerializedProperty m_GreenChannelTextureFilter;
        SerializedProperty m_BlueChannel;
        SerializedProperty m_BlueChannelTextureFilter;
        SerializedProperty m_AlphaChannel;
        SerializedProperty m_AlphaChannelTextureFilter;

        protected override void OnEnable()
        {
            m_TextureName = serializedObject.FindProperty("textureName");
            m_TextureSize = serializedObject.FindProperty("textureSize");
            m_IsLinear = serializedObject.FindProperty("isLinear");
            m_RedChannel = serializedObject.FindProperty("redChannel");
            m_RedChannelTextureFilter = serializedObject.FindProperty("redChannelTextureFilter");
            m_GreenChannel = serializedObject.FindProperty("greenChannel");
            m_GreenChannelTextureFilter = serializedObject.FindProperty("greenChannelTextureFilter");
            m_BlueChannel = serializedObject.FindProperty("blueChannel");
            m_BlueChannelTextureFilter = serializedObject.FindProperty("blueChannelTextureFilter");
            m_AlphaChannel = serializedObject.FindProperty("alphaChannel");
            m_AlphaChannelTextureFilter = serializedObject.FindProperty("alphaChannelTextureFilter");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBaseProperties();
            EditorGUILayout.PropertyField(m_TextureName, DaiGUIContent.packedTextureName);
            DrawTemplateVariables();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_TextureSize);
            EditorGUILayout.PropertyField(m_IsLinear);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Channels", DaiGUIStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_RedChannel, GUIContent.none, GUILayout.Width(20));
            EditorGUILayout.LabelField("Red", GUILayout.Width(40));
            EditorGUILayout.PropertyField(m_RedChannelTextureFilter, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_GreenChannel, GUIContent.none, GUILayout.Width(20));
            EditorGUILayout.LabelField("Green", GUILayout.Width(40));
            EditorGUILayout.PropertyField(m_GreenChannelTextureFilter, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_BlueChannel, GUIContent.none, GUILayout.Width(20));
            EditorGUILayout.LabelField("Blue", GUILayout.Width(40));
            EditorGUILayout.PropertyField(m_BlueChannelTextureFilter, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_AlphaChannel, GUIContent.none, GUILayout.Width(20));
            EditorGUILayout.LabelField("Alpha", GUILayout.Width(40));
            EditorGUILayout.PropertyField(m_AlphaChannelTextureFilter, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}