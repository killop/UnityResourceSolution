using System;
using System.Collections.Generic;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using NinjaBeats;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using NinjaBeats.ReflectionHelper;

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
            
            foreach (var _platform in UnityEditor_Build_BuildPlatforms.instance.GetValidPlatforms())
            {
                var platform = new UnityEditor_Build_BuildPlatform(_platform);
                var platformName = platform.name;
                var setting = m_SettingList?.Find(x => x.name == platformName);
                if (setting == null)
                {
                    setting = new TextureImporterPlatformSettings();
                    setting.name = platformName;
                    setting.overridden = false;
                }
                DummyImporter.SetPlatformTextureSettings(setting);
            }
        }

        public void DataFromDummy()
        {
            if (DummyImporter == null)
                return;

            m_SettingList ??= new();
            m_SettingList.Clear();
            
            foreach (var _platform in UnityEditor_Build_BuildPlatforms.instance.GetValidPlatforms())
            {
                var platform = new UnityEditor_Build_BuildPlatform(_platform);
                var platformName = platform.name;
                var setting = DummyImporter.GetPlatformTextureSettings(platformName);
                if (setting != null)
                    m_SettingList.Add(setting);
            }
        }

        public override bool IsConfigOK(AssetImporter importer)
        {
            if (importer == null) return false;
            var ti = importer as TextureImporter;
            if (ti==null) return false;
            if (m_SettingList == null || m_SettingList.Count == 0) return true;
            
            for (int i = 0; i < m_SettingList.Count; i++)
            {
                var myConfig= m_SettingList[i];
                var currentSetting = ti.GetPlatformTextureSettings(myConfig.name);
                if (currentSetting == null) { 
                    return false;
                }
                if (myConfig.overridden != currentSetting.overridden) {
                    return false;
                }
                if (myConfig.maxTextureSize != currentSetting.maxTextureSize)
                {
                    return false;
                }
                if (myConfig.resizeAlgorithm != currentSetting.resizeAlgorithm)
                {
                    return false;
                }
                if (myConfig.format != currentSetting.format)
                {
                    return false;
                }
                if (myConfig.textureCompression != currentSetting.textureCompression)
                {
                    return false;
                }
                if (myConfig.compressionQuality != currentSetting.compressionQuality)
                {
                    return false;
                }
                if (myConfig.crunchedCompression != currentSetting.crunchedCompression)
                {
                    return false;
                }
                if (myConfig.allowsAlphaSplitting != currentSetting.allowsAlphaSplitting)
                {
                    return false;
                }
                if (myConfig.androidETC2FallbackOverride != currentSetting.androidETC2FallbackOverride)
                {
                    return false;
                }
            }
            return true;
        }

        public override void OnPostprocessTexture(string assetPath, TextureImporter importer, Texture2D tex)
        {
            if (m_SettingList != null)
            {
                foreach (var _platform in UnityEditor_Build_BuildPlatforms.instance.GetValidPlatforms())
                {
                    var platform = new UnityEditor_Build_BuildPlatform(_platform);
                    var platformName = platform.name;
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