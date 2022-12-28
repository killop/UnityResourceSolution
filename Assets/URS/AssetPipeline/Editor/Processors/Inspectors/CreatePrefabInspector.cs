using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(CreatePrefab))]
    internal class CreatePrefabInspector : AssetProcessorInspector
    {
        SerializedProperty m_PrefabPathType;
        SerializedProperty m_PrefabPath;
        SerializedProperty m_TargetFolder;
        SerializedProperty m_ApplyTag;
        SerializedProperty m_Tag;
        SerializedProperty m_ApplyLayer;
        SerializedProperty m_ApplyLayerRecursively;
        SerializedProperty m_Layer;
        SerializedProperty m_LightProbeUsage;
        SerializedProperty m_ReflectionProbeUsage;
        SerializedProperty m_ShadowCastingMode;
        SerializedProperty m_AllowOcclusionWhenDynamic;
        SerializedProperty m_ReceiveShadows;
        SerializedProperty m_ContributeGlobalIllumination;
        SerializedProperty m_CreateAnchorOverride;
        SerializedProperty m_ReceivedGlobalIllumination;
        SerializedProperty m_MotionVectorGenerationMode;

        protected override void OnEnable()
        {
            m_PrefabPathType = serializedObject.FindProperty("prefabPathType");
            m_PrefabPath = serializedObject.FindProperty("prefabPath");
            m_TargetFolder = serializedObject.FindProperty("targetFolder");
            m_ApplyTag = serializedObject.FindProperty("applyTag");
            m_Tag = serializedObject.FindProperty("tag");
            m_ApplyLayer = serializedObject.FindProperty("applyLayer");
            m_ApplyLayerRecursively = serializedObject.FindProperty("applyLayerRecursively");
            m_Layer = serializedObject.FindProperty("layer");
            m_LightProbeUsage = serializedObject.FindProperty("mrLightProbeUsage");
            m_ReflectionProbeUsage = serializedObject.FindProperty("mrReflectionProbeUsage");
            m_ShadowCastingMode = serializedObject.FindProperty("mrShadowCastingMode");
            m_AllowOcclusionWhenDynamic = serializedObject.FindProperty("mrAllowOcclusionWhenDynamic");
            m_ReceiveShadows = serializedObject.FindProperty("mrReceiveShadows");
            m_ContributeGlobalIllumination = serializedObject.FindProperty("mrContributeGlobalIllumination");
            m_CreateAnchorOverride = serializedObject.FindProperty("mrCreateAnchorOverride");
            m_ReceivedGlobalIllumination = serializedObject.FindProperty("mrReceivedGlobalIllumination");
            m_MotionVectorGenerationMode = serializedObject.FindProperty("mrMotionVectorGenerationMode");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBaseProperties();

            EditorGUILayout.LabelField("Prefab", DaiGUIStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PrefabPathType);
            var enumValueName = m_PrefabPathType.enumNames[m_PrefabPathType.enumValueIndex];
            if (enumValueName == TargetPathType.Relative.ToString())
            {
                EditorGUILayout.PropertyField(m_PrefabPath, DaiGUIContent.destination);
            }
            else if (enumValueName == TargetPathType.TargetFolder.ToString())
            {
                EditorGUILayout.PropertyField(m_TargetFolder, DaiGUIContent.destination);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GameObject", DaiGUIStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ApplyTag);
            if (m_ApplyTag.boolValue)
            {
                EditorGUI.indentLevel++;
                var tags = InternalEditorUtility.tags.ToList();
                var tagIndex = tags.IndexOf(m_Tag.stringValue);
                var newTagIndex = EditorGUILayout.Popup("Tag", tagIndex, tags.ToArray());
                if (tagIndex != newTagIndex)
                {
                    m_Tag.stringValue = tags[newTagIndex];
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_ApplyLayer);
            if (m_ApplyLayer.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ApplyLayerRecursively, DaiGUIContent.applyRecursively);
                var layers = InternalEditorUtility.layers.ToList();
                var layerIndex = layers.IndexOf(LayerMask.LayerToName(m_Layer.intValue));
                var newLayerIndex = EditorGUILayout.Popup("Layer", layerIndex, layers.ToArray());
                if (layerIndex != newLayerIndex)
                {
                    m_Layer.intValue = LayerMask.NameToLayer(layers[newLayerIndex]);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MeshRenderers", DaiGUIStyles.boldLabel);
            EditorGUILayout.PropertyField(m_LightProbeUsage, DaiGUIContent.lightProbeUsage);
            EditorGUILayout.PropertyField(m_ReflectionProbeUsage, DaiGUIContent.reflectionProbeUsage);
            EditorGUILayout.PropertyField(m_ShadowCastingMode, DaiGUIContent.shadowCastingMode);
            EditorGUILayout.PropertyField(m_AllowOcclusionWhenDynamic, DaiGUIContent.allowOcclusionWhenDynamic);
            EditorGUILayout.PropertyField(m_ReceiveShadows, DaiGUIContent.receiveShadows);
            EditorGUILayout.PropertyField(m_ContributeGlobalIllumination, DaiGUIContent.contributeGI);
            EditorGUILayout.PropertyField(m_CreateAnchorOverride, DaiGUIContent.createAnchorOverride);
            EditorGUILayout.PropertyField(m_ReceivedGlobalIllumination, DaiGUIContent.receiveGI);
            EditorGUILayout.PropertyField(m_MotionVectorGenerationMode, DaiGUIContent.motionVectorGenerationMode);

            serializedObject.ApplyModifiedProperties();
        }
    }
}