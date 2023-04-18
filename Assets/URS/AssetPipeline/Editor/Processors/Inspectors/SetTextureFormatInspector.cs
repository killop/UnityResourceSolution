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
using NinjaBeats;
using IList = System.Collections.IList;
using NinjaBeats.ReflectionHelper;

namespace Daihenka.AssetPipeline.Processors
{
    [CustomEditor(typeof(SetTextureFormat))]
    internal class SetTextureFormatInspector : AssetProcessorInspector
    {
        private static UnityEditor_TextureImporterInspector s_DummyEditor;
        private static IList s_PlatformSettings;
        private SetTextureFormat m_Target;
        private readonly GUIContent GUIContent_defaultPlatform = EditorGUIUtility.TrTextContent("Default");
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            m_Target = target as SetTextureFormat;
            if (m_Target == null)
                return;
            
            m_Target.DataToDummy();

            s_DummyEditor.__self__ ??= Editor.CreateEditor(SetTextureFormat.DummyImporter, UnityEditor_TextureImporterInspector.__type__);
            s_PlatformSettings ??= (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new[] { UnityEditor_BaseTextureImportPlatformSettings.__type__ }));
            
            s_DummyEditor.OnEnable();
        }

        protected void OnDisable()
        {
            if (m_Target == null)
                return;
            
            s_DummyEditor.OnDisable();

            m_Target = null;
        }

        public override void OnInspectorGUI()
        {
            if (m_Target == null)
                return;
            
            serializedObject.Update();
            DrawBaseProperties();
            serializedObject.ApplyModifiedProperties();

            s_PlatformSettings.Clear();
            s_PlatformSettings.AddRange(s_DummyEditor.m_PlatformSettings);
            var m_PlatformSettingsArrProp = s_DummyEditor.m_PlatformSettingsArrProp;
            UnityEditor_BaseTextureImportPlatformSettings.InitPlatformSettings(s_PlatformSettings);
            foreach (var v in s_PlatformSettings)
            {
                var settings = new UnityEditor_TextureImportPlatformSettings(v);
                settings.CacheSerializedProperties(m_PlatformSettingsArrProp);
            }
            GUILayout.Space(10f);
            int selected = UnityEditor_EditorGUILayout.BeginPlatformGrouping(
                UnityEditor_BaseTextureImportPlatformSettings.GetBuildPlayerValidPlatforms(),
                GUIContent_defaultPlatform,
                UnityEditor_EditorStyles.frameBox, (Func<int, bool>)(idx =>
                {
                    UnityEditor_TextureImportPlatformSettings setting = new(s_PlatformSettings[idx + 1]);
                    var model = setting.model;
                    var isDefault = model.isDefault;
                    var overriddenIsDifferent = model.overriddenIsDifferent;
                    var allAreOverridden = model.allAreOverridden;
                    return !isDefault && (overriddenIsDifferent || allAreOverridden);
                }));
            using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                UnityEditor_BaseTextureImportPlatformSettings.ShowPlatformSpecificSettings(s_PlatformSettings,
                    selected);
                if (changeCheckScope.changed)
                {
                    Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { this.m_Target, SetTextureFormat.DummyImporter }, "Inspector");
                    UnityEditor_BaseTextureImportPlatformSettings.ApplyPlatformSettings(s_PlatformSettings);
                    m_Target.DataFromDummy();
                    
                }
            }
        }
    }
}