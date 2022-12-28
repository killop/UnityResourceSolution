using System.Collections.Generic;
using System.IO;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Daihenka.AssetPipeline
{
    internal class AssetPipelineSettingsProvider : SettingsProvider
    {
        SerializedObject m_Settings;
        Dictionary<ImportAssetType, ReorderableList> m_FileExtensionLists = new Dictionary<ImportAssetType, ReorderableList>();

        class Styles
        {
            public static GUIContent fileExtensionsByType = new GUIContent("File Extensions by Type");
            public static GUIContent profileStoragePath = new GUIContent("Default Profile Storage Path");
            public static GUIContent openImportProfiles = new GUIContent("Open Import Profiles");
            public static GUIContent defaultConvention = new GUIContent("Path Variable Convention");
            public static GUIContent enabledStatusColor = new GUIContent("Enabled Profile Color");
            public static GUIContent disabledStatusColor = new GUIContent("Disabled Profile Color");
        }

        AssetPipelineSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
        {
        }

        static bool IsSettingsAvailable()
        {
            return File.Exists(AssetPipelineSettings.kSettingsPath);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_Settings = AssetPipelineSettings.GetSerializedSettings();

            for (var assetType = ImportAssetType.Textures; assetType <= ImportAssetType.Fonts; assetType++)
            {
                if (!m_FileExtensionLists.ContainsKey(assetType))
                {
                    var type = assetType;
                    var assetTypeName = assetType.ToString();
                    var fileExtList = m_Settings.FindProperty("m_AssetTypeFileExtensions").GetArrayElementAtIndex((int) assetType).FindPropertyRelative("m_FileExtensions");
                    m_FileExtensionLists[assetType] = new ReorderableList(m_Settings, fileExtList, true, true, true, true);
                    m_FileExtensionLists[assetType].drawHeaderCallback = rect => EditorGUI.LabelField(rect, assetTypeName, DaiGUIStyles.boldLabel);
                    m_FileExtensionLists[assetType].drawElementCallback = (rect, index, active, focused) => EditorGUI.PropertyField(rect, fileExtList.GetArrayElementAtIndex(index), GUIContent.none);
                    m_FileExtensionLists[assetType].drawFooterCallback = rect =>
                    {
                        ReorderableList.defaultBehaviours.DrawFooter(rect, m_FileExtensionLists[type]);
                        var buttonRect = new Rect(rect.x + 20, rect.y, 76, rect.height);
                        if (Event.current.type == EventType.Repaint)
                        {
                            ((GUIStyle) "RL Footer").Draw(buttonRect, false, false, false, false);
                        }

                        buttonRect.height = 16;
                        buttonRect.xMin += 2;
                        buttonRect.xMax -= 2;
                        if (GUI.Button(buttonRect, "Clear All", (GUIStyle) "RL FooterButton"))
                        {
                            fileExtList.ClearArray();
                            m_Settings.ApplyModifiedProperties();
                        }
                    };
                }
            }
        }

        public override void OnGUI(string searchContext)
        {
            m_Settings.Update();
            GUILayout.BeginVertical(DaiGUIStyles.settingsPadding);
            if (GUILayout.Button(Styles.openImportProfiles))
            {
                ImportProfilesWindow.ShowWindow();
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_Settings.FindProperty("m_DefaultProfileStoragePath"), Styles.profileStoragePath);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_Settings.FindProperty("m_DefaultConvention"), Styles.defaultConvention);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.fileExtensionsByType, DaiGUIStyles.boldLabel);
            for (var assetType = ImportAssetType.Textures; assetType <= ImportAssetType.Fonts; assetType++)
            {
                m_FileExtensionLists[assetType].DoLayoutList();
                EditorGUILayout.Space();
            }

            GUILayout.EndVertical();

            m_Settings.ApplyModifiedProperties();
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectSettingsProvider()
        {
            if (!IsSettingsAvailable())
            {
                AssetPipelineSettings.GetOrCreateSettings();
            }

            return new AssetPipelineSettingsProvider("Project/Daihenka/Asset Pipeline", SettingsScope.Project) {keywords = GetSearchKeywordsFromGUIContentProperties<Styles>()};
        }
    }
}