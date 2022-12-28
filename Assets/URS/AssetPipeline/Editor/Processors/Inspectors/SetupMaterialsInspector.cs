using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(SetupMaterials))]
    internal class SetupMaterialsInspector : AssetProcessorInspector
    {
        SerializedProperty m_MaterialSetups;
        ReorderableList m_MaterialSetupList;
        internal static string[] m_ShaderNames;

        protected override void OnEnable()
        {
            m_MaterialSetups = serializedObject.FindProperty("materialSetups");
            m_MaterialSetupList = new ReorderableList(serializedObject, m_MaterialSetups, false, true, true, true);
            m_MaterialSetupList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Material Setups", DaiGUIStyles.boldLabel);
            m_MaterialSetupList.elementHeightCallback = elementIndex => EditorGUI.GetPropertyHeight(m_MaterialSetups.GetArrayElementAtIndex(elementIndex));
            m_MaterialSetupList.drawElementCallback = DrawMaterialSetupElement;
            m_ShaderNames = ShaderUtil.GetAllShaderInfo().Where(x => x.name != "Hidden/Daihenka/Editor/ColoredTexture").Select(x => x.name).ToArray();
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBaseProperties();
            m_MaterialSetupList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawMaterialSetupElement(Rect rect, int index, bool active, bool focused)
        {
            EditorGUI.PropertyField(rect, m_MaterialSetups.GetArrayElementAtIndex(index), GUIContent.none);
            DrawSeparatorLine(rect);
            rect.y += rect.height - 1;
            DrawSeparatorLine(rect);
        }

        static void DrawSeparatorLine(Rect rect)
        {
            EditorGUI.DrawRect(new Rect(rect.x - 6, rect.y, rect.width + 12, 1), ColorPalette.BackgroundDarker);
        }
    }
}