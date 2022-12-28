using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(StripMeshData))]
    internal class StripMeshDataInspector : AssetProcessorInspector
    {
        SerializedProperty m_Normals;
        SerializedProperty m_Tangents;
        SerializedProperty m_VertexColor;
        SerializedProperty m_UV;
        SerializedProperty m_UV2;
        SerializedProperty m_UV3;
        SerializedProperty m_UV4;
        SerializedProperty m_UV5;
        SerializedProperty m_UV6;
        SerializedProperty m_UV7;
        SerializedProperty m_UV8;
        SerializedProperty m_BindPoses;
        SerializedProperty m_BoneWeights;
        SerializedProperty m_BlendShapes;
        SerializedProperty m_NormalsIfDefault;
        SerializedProperty m_TangentsIfDefault;
        SerializedProperty m_VertexColorIfDefault;
        SerializedProperty m_UVIfDefault;
        SerializedProperty m_UV2IfDefault;
        SerializedProperty m_UV3IfDefault;
        SerializedProperty m_UV4IfDefault;
        SerializedProperty m_UV5IfDefault;
        SerializedProperty m_UV6IfDefault;
        SerializedProperty m_UV7IfDefault;
        SerializedProperty m_UV8IfDefault;
        SerializedProperty m_BindPosesIfDefault;
        SerializedProperty m_BoneWeightsIfDefault;

        protected override void OnEnable()
        {
            m_Normals = serializedObject.FindProperty("normals");
            m_Tangents = serializedObject.FindProperty("tangents");
            m_VertexColor = serializedObject.FindProperty("vertexColor");
            m_UV = serializedObject.FindProperty("uv");
            m_UV2 = serializedObject.FindProperty("uv2");
            m_UV3 = serializedObject.FindProperty("uv3");
            m_UV4 = serializedObject.FindProperty("uv4");
            m_UV5 = serializedObject.FindProperty("uv5");
            m_UV6 = serializedObject.FindProperty("uv6");
            m_UV7 = serializedObject.FindProperty("uv7");
            m_UV8 = serializedObject.FindProperty("uv8");
            m_BindPoses = serializedObject.FindProperty("bindPoses");
            m_BoneWeights = serializedObject.FindProperty("boneWeights");
            m_BlendShapes = serializedObject.FindProperty("blendShapes");
            m_NormalsIfDefault = serializedObject.FindProperty("normalsIfDefault");
            m_TangentsIfDefault = serializedObject.FindProperty("tangentsIfDefault");
            m_VertexColorIfDefault = serializedObject.FindProperty("vertexColorIfDefault");
            m_UVIfDefault = serializedObject.FindProperty("uvIfDefault");
            m_UV2IfDefault = serializedObject.FindProperty("uv2IfDefault");
            m_UV3IfDefault = serializedObject.FindProperty("uv3IfDefault");
            m_UV4IfDefault = serializedObject.FindProperty("uv4IfDefault");
            m_UV5IfDefault = serializedObject.FindProperty("uv5IfDefault");
            m_UV6IfDefault = serializedObject.FindProperty("uv6IfDefault");
            m_UV7IfDefault = serializedObject.FindProperty("uv7IfDefault");
            m_UV8IfDefault = serializedObject.FindProperty("uv8IfDefault");
            m_BindPosesIfDefault = serializedObject.FindProperty("bindPosesIfDefault");
            m_BoneWeightsIfDefault = serializedObject.FindProperty("boneWeightsIfDefault");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBaseProperties();
            DrawIfDefaultProperty(m_Normals, m_NormalsIfDefault);
            DrawIfDefaultProperty(m_Tangents, m_TangentsIfDefault);
            DrawIfDefaultProperty(m_VertexColor, m_VertexColorIfDefault);
            EditorGUILayout.Space();
            DrawIfDefaultProperty(m_UV, m_UVIfDefault, "UV");
            DrawIfDefaultProperty(m_UV2, m_UV2IfDefault, "UV2");
            DrawIfDefaultProperty(m_UV3, m_UV3IfDefault, "UV3");
            DrawIfDefaultProperty(m_UV4, m_UV4IfDefault, "UV4");
            DrawIfDefaultProperty(m_UV5, m_UV5IfDefault, "UV5");
            DrawIfDefaultProperty(m_UV6, m_UV6IfDefault, "UV6");
            DrawIfDefaultProperty(m_UV7, m_UV7IfDefault, "UV7");
            DrawIfDefaultProperty(m_UV8, m_UV8IfDefault, "UV8");
            EditorGUILayout.Space();
            DrawIfDefaultProperty(m_BindPoses, m_BindPosesIfDefault);
            DrawIfDefaultProperty(m_BoneWeights, m_BoneWeightsIfDefault);
            m_BlendShapes.boolValue = EditorGUILayout.ToggleLeft(m_BlendShapes.displayName, m_BlendShapes.boolValue);

            serializedObject.ApplyModifiedProperties();
        }

        static void DrawIfDefaultProperty(SerializedProperty prop, SerializedProperty ifNotUsedProp, string label = null)
        {
            EditorGUILayout.BeginHorizontal();
            prop.boolValue = EditorGUILayout.ToggleLeft(label ?? prop.displayName, prop.boolValue, GUILayout.Width(130));
            GUI.enabled = prop.boolValue;
            ifNotUsedProp.boolValue = EditorGUILayout.ToggleLeft("Only If Default", ifNotUsedProp.boolValue);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}