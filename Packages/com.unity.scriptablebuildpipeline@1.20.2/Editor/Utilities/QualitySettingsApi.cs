using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    static class QualitySettingsApi
    {
        static SerializedObject m_Target;
        static SerializedProperty m_QualitySettingsProperty;

        static QualitySettingsApi()
        {
            var qualitySettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            m_Target = new SerializedObject(qualitySettings);
            m_QualitySettingsProperty = m_Target.FindProperty("m_QualitySettings");
        }

        internal static int GetNumberOfLODsStripped()
        {
            m_Target.Update();
            int strippedLODs = int.MaxValue;
            int count = m_QualitySettingsProperty.arraySize;
            for (int i = 0; i < count; i++)
            {
                var element = m_QualitySettingsProperty.GetArrayElementAtIndex(i);
                var maximumLODLevel = element.FindPropertyRelative("maximumLODLevel");
                strippedLODs = Mathf.Min(strippedLODs, maximumLODLevel.intValue);
            }
            return strippedLODs == int.MaxValue ? 0 : strippedLODs;
        }
    }
}
