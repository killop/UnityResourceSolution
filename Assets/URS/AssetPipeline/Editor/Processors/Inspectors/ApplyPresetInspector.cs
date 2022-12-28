using Daihenka.AssetPipeline.Import;
using UnityEditor;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(ApplyPreset))]
    internal class ApplyPresetInspector : AssetProcessorInspector
    {
        SerializedProperty m_Preset;
        Editor m_CachedEditor;

        protected override void OnEnable()
        {
            m_Preset = serializedObject.FindProperty("preset");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("This processor will only execute when a new asset is added.", MessageType.Warning);
            EditorGUILayout.Space();
            if (!m_CachedEditor)
            {
                CreateCachedEditor(m_Preset.objectReferenceValue, System.Type.GetType("UnityEditor.Presets.PresetEditor, UnityEditor"), ref m_CachedEditor);
            }

            m_CachedEditor.OnInspectorGUI();

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}