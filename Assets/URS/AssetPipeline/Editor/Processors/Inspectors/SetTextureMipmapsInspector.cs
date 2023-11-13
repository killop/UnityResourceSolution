using Daihenka.AssetPipeline.Import;
using UnityEditor;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(SetTextureMipmaps))]
    internal class SetTextureMipmapsInspector : AssetProcessorInspector
    {
        SerializedProperty m_EnableMipmaps;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            m_EnableMipmaps = serializedObject.FindProperty(nameof(SetTextureMipmaps.enableMipmaps));
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBaseProperties();
            EditorGUILayout.PropertyField(m_EnableMipmaps);
            serializedObject.ApplyModifiedProperties();

        }
    }
}