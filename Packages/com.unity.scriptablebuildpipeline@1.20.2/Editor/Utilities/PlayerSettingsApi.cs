using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    static class PlayerSettingsApi
    {
        static SerializedObject m_Target;
#if UNITY_2020_1_OR_NEWER
        static SerializedProperty m_NumberOfMipsStripped;
#endif

        static PlayerSettingsApi()
        {
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
#if UNITY_2020_1_OR_NEWER
            m_Target = new SerializedObject(playerSettings);
            m_NumberOfMipsStripped = m_Target.FindProperty("numberOfMipsStripped");
#endif
        }

#if UNITY_2020_1_OR_NEWER
        internal static int GetNumberOfMipsStripped()
        {
            m_Target.Update();
            return m_NumberOfMipsStripped.intValue;
        }

#endif
    }
}
