using System;
using System.Collections.Generic;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("FilterByLabel@2x", ImportAssetTypeFlag.Textures)]
    public class SetTextureFormat : AssetProcessor
    {
        [NonSerialized] private static TextureImporter m_DummyImporter;
        public static TextureImporter DummyImporter => m_DummyImporter ??= (TextureImporter)AssetImporter.GetAtPath("Assets/Editor/AssetPipeline/__SetTextureFormat.png");
        [SerializeField] private List<TextureImporterPlatformSettings> m_SettingList = new();

        public void DataToDummy()
        {
            if (DummyImporter == null)
                return;
            
            foreach (var platform in UnityEditorDynamic.Build_BuildPlatforms.instance.GetValidPlatforms())
            {
                var platformName = (string)UnityEditorDynamic.Reflection_BuildPlatform_name.GetValue((object)platform);
                var setting = m_SettingList?.Find(x => x.name == platformName);
                if (setting != null)
                    DummyImporter.SetPlatformTextureSettings(setting);
                else
                    DummyImporter.ClearPlatformTextureSettings(platformName);
            }
        }

        public void DataFromDummy()
        {
            if (DummyImporter == null)
                return;

            m_SettingList ??= new();
            m_SettingList.Clear();
            
            foreach (var platform in UnityEditorDynamic.Build_BuildPlatforms.instance.GetValidPlatforms())
            {
                var platformName = (string)UnityEditorDynamic.Reflection_BuildPlatform_name.GetValue((object)platform);
                var setting = DummyImporter.GetPlatformTextureSettings(platformName);
                if (setting != null)
                    m_SettingList.Add(setting);
            }
        }
        
        public override void OnPostprocessTexture(string assetPath, TextureImporter importer, Texture2D tex)
        {
            if (m_SettingList != null)
            {
                foreach (var platform in UnityEditorDynamic.Build_BuildPlatforms.instance.GetValidPlatforms())
                {
                    var platformName = (string)UnityEditorDynamic.Reflection_BuildPlatform_name.GetValue((object)platform);
                    var setting = m_SettingList.Find(x => x.name == platformName);
                    if (setting != null)
                        importer.SetPlatformTextureSettings(setting);
                }
            }
            
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Preset applied for <b>{assetPath}</b>");
        }
        
    }
}