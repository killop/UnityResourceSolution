using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(SetTextureFormat))]
    internal class SetTextureFormatInspector : AssetProcessorInspector
    {
        private static dynamic m_DummyEditor;
        private dynamic m_PlatformSettings;
        private SetTextureFormat m_Target;
        private readonly GUIContent GUIContent_defaultPlatform = EditorGUIUtility.TrTextContent("Default");
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            m_Target = target as SetTextureFormat;
            if (m_Target == null)
                return;
            
            m_Target.DataToDummy();

            m_DummyEditor ??= Editor.CreateEditor(SetTextureFormat.DummyImporter, UnityEditorDynamic.Type_TextureImporterInspector);
            m_PlatformSettings = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new[] { UnityEditorDynamic.Type_BaseTextureImportPlatformSettings }));
            var v = Activator.CreateInstance(UnityEditorDynamic.Type_TextureImportPlatformSettings,
                UnityEditorDynamic.TextureImporterInspector.s_DefaultPlatformName, BuildTarget.StandaloneWindows, m_DummyEditor);
            ((IList)m_PlatformSettings).Add((object)v);
            foreach (var buildPlatform in UnityEditorDynamic.Build_BuildPlatforms.instance.GetValidPlatforms())
            {
                var name = (string)UnityEditorDynamic.Reflection_BuildPlatform_name.GetValue((object)buildPlatform);
                var defaultTarget = (BuildTarget)UnityEditorDynamic.Reflection_BuildPlatform_defaultTarget.GetValue((object)buildPlatform);
                var vv = Activator.CreateInstance(UnityEditorDynamic.Type_TextureImportPlatformSettings, name, defaultTarget, m_DummyEditor);
                ((IList)m_PlatformSettings).Add((object)vv);
            }

            if (m_DummyEditor != null)
                UnityEditorDynamic.Reflection_TextureImporterInspector_OnEnable.Invoke((object)m_DummyEditor, Array.Empty<object>());
        }

        protected void OnDisable()
        {
            if (m_Target == null)
                return;
            
            if (m_DummyEditor != null)
                UnityEditorDynamic.Reflection_TextureImporterInspector_OnDisable.Invoke((object)m_DummyEditor, Array.Empty<object>());

            m_Target = null;
        }

        public override void OnInspectorGUI()
        {
            if (m_Target == null)
                return;
            
            serializedObject.Update();
            DrawBaseProperties();
            serializedObject.ApplyModifiedProperties();
            
            UnityEditorDynamic.BaseTextureImportPlatformSettings.InitPlatformSettings(m_PlatformSettings);
            GUILayout.Space(10f);
            int selected = (int)UnityEditorDynamic.EditorGUILayout.BeginPlatformGrouping(UnityEditorDynamic.BaseTextureImportPlatformSettings.GetBuildPlayerValidPlatforms(), GUIContent_defaultPlatform, UnityEditorDynamic.EditorStyles.frameBox, (Func<int, bool>) (idx =>
            {
                var setting = ((IList)this.m_PlatformSettings)[idx + 1];
                var model = (dynamic)UnityEditorDynamic.Reflection_TextureImportPlatformSettings_model.GetValue(setting);
                var isDefault = (bool)(UnityEditorDynamic.Reflection_TextureImportPlatformSettingsData_isDefault.GetValue(model));
                var overriddenIsDifferent = (bool)(UnityEditorDynamic.Reflection_TextureImportPlatformSettingsData_overriddenIsDifferent.GetValue(model));
                var allAreOverridden = (bool)(UnityEditorDynamic.Reflection_TextureImportPlatformSettingsData_allAreOverridden.GetValue(model));
                return !isDefault && (overriddenIsDifferent || allAreOverridden);
            }));
            using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                UnityEditorDynamic.BaseTextureImportPlatformSettings.ShowPlatformSpecificSettings(m_PlatformSettings, selected);
                if (changeCheckScope.changed)
                {
                    Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { this.m_Target, SetTextureFormat.DummyImporter }, "Inspector");
                    UnityEditorDynamic.BaseTextureImportPlatformSettings.ApplyPlatformSettings(m_PlatformSettings);
                    m_Target.DataFromDummy();
                    
                }
            }
        }
    }
}