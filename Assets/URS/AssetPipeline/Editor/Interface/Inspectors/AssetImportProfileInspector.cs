using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Inspectors
{
    [CustomEditor(typeof(AssetImportProfile))]
    internal class AssetImportProfileInspector : Editor
    {
        AssetImportProfile m_Target;

        void OnEnable()
        {
            m_Target = (AssetImportProfile) target;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(50);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Import Profile", DaiGUIStyles.sectionHeader);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(m_Target.name, DaiGUIStyles.sectionSubHeader);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(24);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Import Profile Editor", DaiGUIStyles.buttonLarge))
            {
                ImportProfileWindow.ShowWindow(m_Target);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}