using Daihenka.AssetPipeline.Import;
using UnityEditor;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(ResetTransform))]
    internal class ResetTransformInspector : AssetProcessorInspector
    {
        SerializedProperty m_Position;
        SerializedProperty m_Rotation;
        SerializedProperty m_Scale;

        protected override void OnEnable()
        {
            m_Position = serializedObject.FindProperty("position");
            m_Rotation = serializedObject.FindProperty("rotation");
            m_Scale = serializedObject.FindProperty("scale");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBaseProperties();
            m_Position.boolValue = EditorGUILayout.ToggleLeft(m_Position.displayName, m_Position.boolValue);
            m_Rotation.boolValue = EditorGUILayout.ToggleLeft(m_Rotation.displayName, m_Rotation.boolValue);
            m_Scale.boolValue = EditorGUILayout.ToggleLeft(m_Scale.displayName, m_Scale.boolValue);

            serializedObject.ApplyModifiedProperties();
        }
    }
}