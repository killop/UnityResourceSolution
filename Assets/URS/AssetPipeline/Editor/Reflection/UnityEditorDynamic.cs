using System;
using System.Reflection;
using UnityEditor;
using Daihenka.AssetPipeline.ReflectionMagic;

namespace Daihenka.AssetPipeline
{
    internal static class UnityEditorDynamic
    {
        public static readonly Assembly UnityEditorAssembly;
        public static readonly dynamic EditorGUIUtility;
        public static readonly dynamic EditorGUILayout;
        public static readonly dynamic EditorStyles;
        public static readonly dynamic Build_BuildPlatforms;
        public static readonly Type Type_TextureImporterInspector;
        public static readonly dynamic TextureImporterInspector;
        public static readonly Type Type_TextureImportPlatformSettings;
        public static readonly dynamic TextureImportPlatformSettings;
        public static readonly Type Type_TextureImportPlatformSettingsData;
        public static readonly Type Type_BaseTextureImportPlatformSettings;
        public static readonly dynamic BaseTextureImportPlatformSettings;
        public static FieldInfo Reflection_Editor_m_Targets;
        public static MethodInfo Reflection_TextureImporterInspector_OnEnable;
        public static MethodInfo Reflection_TextureImporterInspector_OnDisable;
        public static FieldInfo Reflection_BuildPlatform_name;
        public static FieldInfo Reflection_BuildPlatform_defaultTarget;
        public static PropertyInfo Reflection_TextureImportPlatformSettings_model;
        
        public static PropertyInfo Reflection_TextureImportPlatformSettingsData_isDefault;
        public static PropertyInfo Reflection_TextureImportPlatformSettingsData_overriddenIsDifferent;
        public static PropertyInfo Reflection_TextureImportPlatformSettingsData_allAreOverridden;
        
        static UnityEditorDynamic()
        {
            UnityEditorAssembly = typeof(Editor).Assembly;
            EditorGUIUtility = typeof(EditorGUIUtility).AsDynamicType();
            EditorGUILayout = typeof(EditorGUILayout).AsDynamicType();
            EditorStyles = typeof(EditorStyles).AsDynamicType();
            Build_BuildPlatforms = typeof(UnityEditor.Build.BuildPlayerProcessor).Assembly?.GetType("UnityEditor.Build.BuildPlatforms").AsDynamicType();
            Type_TextureImporterInspector = UnityEditorAssembly.GetType("UnityEditor.TextureImporterInspector");
            TextureImporterInspector = Type_TextureImporterInspector.AsDynamicType();
            Type_TextureImportPlatformSettings = UnityEditorAssembly.GetType("UnityEditor.TextureImportPlatformSettings");
            TextureImportPlatformSettings = Type_TextureImportPlatformSettings.AsDynamicType();
            Type_TextureImportPlatformSettingsData = UnityEditorAssembly.GetType("UnityEditor.TextureImportPlatformSettingsData");
            Type_BaseTextureImportPlatformSettings = UnityEditorAssembly.GetType("UnityEditor.BaseTextureImportPlatformSettings");
            BaseTextureImportPlatformSettings = Type_BaseTextureImportPlatformSettings.AsDynamicType();
            Reflection_Editor_m_Targets = typeof(Editor).GetField("m_Targets", (BindingFlags)(-1));
            Reflection_TextureImporterInspector_OnEnable = Type_TextureImporterInspector.GetMethod("OnEnable", (BindingFlags)(-1));
            Reflection_TextureImporterInspector_OnDisable = Type_TextureImporterInspector.GetMethod("OnDisable", (BindingFlags)(-1));
            Reflection_BuildPlatform_name = typeof(UnityEditor.Build.BuildPlayerProcessor).Assembly.GetType("UnityEditor.Build.BuildPlatform").GetField("name", (BindingFlags)(-1));
            Reflection_BuildPlatform_defaultTarget = typeof(UnityEditor.Build.BuildPlayerProcessor).Assembly.GetType("UnityEditor.Build.BuildPlatform").GetField("defaultTarget", (BindingFlags)(-1));
            Reflection_TextureImportPlatformSettings_model = Type_TextureImportPlatformSettings.GetProperty("model", (BindingFlags)(-1));
            Reflection_TextureImportPlatformSettingsData_isDefault = Type_TextureImportPlatformSettingsData.GetProperty("isDefault", (BindingFlags)(-1));
            Reflection_TextureImportPlatformSettingsData_overriddenIsDifferent = Type_TextureImportPlatformSettingsData.GetProperty("overriddenIsDifferent", (BindingFlags)(-1));
            Reflection_TextureImportPlatformSettingsData_allAreOverridden = Type_TextureImportPlatformSettingsData.GetProperty("allAreOverridden", (BindingFlags)(-1));
        }
    }

}