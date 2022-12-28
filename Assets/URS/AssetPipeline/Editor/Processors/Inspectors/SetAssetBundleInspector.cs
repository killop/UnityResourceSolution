using Daihenka.AssetPipeline.Import;
using UnityEditor;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(SetAssetBundle))]
    internal class SetAssetBundleInspector : AssetProcessorInspector
    {
        SerializedProperty m_AssetBundleName;
        SerializedProperty m_AssetBundleVariant;

        protected override void OnEnable()
        {
            m_AssetBundleName = serializedObject.FindProperty("assetBundleName");
            m_AssetBundleVariant = serializedObject.FindProperty("assetBundleVariant");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBaseProperties();
            EditorGUILayout.LabelField("Asset Bundle", DaiGUIStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AssetBundleName);
            DrawTemplateVariables();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_AssetBundleVariant);
            serializedObject.ApplyModifiedProperties();
        }
    }
}