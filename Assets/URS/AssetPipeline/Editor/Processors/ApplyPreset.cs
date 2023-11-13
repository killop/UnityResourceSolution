using System;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription(typeof(Preset), ImportAssetTypeFlag.Textures | ImportAssetTypeFlag.Models | ImportAssetTypeFlag.Audio | ImportAssetTypeFlag.SpriteAtlases | ImportAssetTypeFlag.Fonts | ImportAssetTypeFlag.Videos)]
    public class ApplyPreset : AssetProcessor
    {
        [SerializeField] Preset preset;
        public override int Priority => int.MaxValue;

        protected override Object[] PrepareEmbeddedObjects(ImportAssetType assetType)
        {
            preset = CreatePresetForType(assetType);
            preset.name = $"Preset_{assetType}";
            return new[] {preset};
        }

        public override void OnPostprocess(Object asset, string assetPath)
        {
            ApplyPresetToSpriteAtlas(asset, assetPath);
        }
        
        public override void OnPostprocessTexture(string assetPath, TextureImporter importer, Texture2D tex)
        {
            OnPostprocessTexture(assetPath, importer);
        }
        
        public override void OnPostprocessCubemap(string assetPath, TextureImporter importer, Cubemap texture)
        {
            OnPostprocessTexture(assetPath, importer);
        }

        public override bool ShouldImport(string assetPath)
        {
            return IsForceApply(assetPath) || !ImportProfileUserData.HasProcessor(assetPath, this);
        }

        public override bool IsConfigOK(AssetImporter importer)
        {
            if (importer == null || preset == null) return false;
            bool isEqual=  DataEquals(preset, importer);
            if (isEqual) 
            {
                return true;
            } 
            else
            {
                if (importer is TextureImporter textureImporter)
                {
                    string androidPlatform = "Android";
                    string iPhonePlatform = "iPhone";
                    bool isAndroidPresetOverrider = IsPresetPlatformTextureMaxSizeIsBiggerThanOrign(androidPlatform, preset, textureImporter, out var orignAndroidSetting, out var orignAndroidMaxSize,out var presetAndroidMaxSize);
                    bool isIPhonePresetOverrider = IsPresetPlatformTextureMaxSizeIsBiggerThanOrign(iPhonePlatform, preset, textureImporter, out var orignIPhoneSetting, out var orignIPhoneMaxSize, out var presetIPhoneMaxSize);
                    bool androidOk = (!isAndroidPresetOverrider) || (orignAndroidMaxSize <= presetAndroidMaxSize);
                    bool isIPhoneOk = (!isIPhonePresetOverrider) || (orignIPhoneMaxSize <= presetIPhoneMaxSize);
                    return androidOk & isIPhoneOk;
                }
                else
                {
                    return true;
                }
            }
        }

        public override void OnPreprocessAsset(string assetPath, AssetImporter importer)
        {
            if (preset == null || !preset.CanBeAppliedTo(importer) || !ShouldImport(importer))
            {
                return;
            }

            var importerSo = new SerializedObject(importer);
            var assetBundleNameProp = importerSo.FindProperty("m_AssetBundleName");
            var assetBundleVariantProp = importerSo.FindProperty("m_AssetBundleVariant");
            var assetBundleName = assetBundleNameProp.stringValue;
            var assetBundleVariant = assetBundleVariantProp.stringValue;

            var textureImporter = importer as TextureImporter;
            if (textureImporter != null)
            {

                var widthProp = importerSo.FindProperty("m_Output.sourceTextureInformation.width");
                var heightProp = importerSo.FindProperty("m_Output.sourceTextureInformation.height");

                var prevSpriteBorder = textureImporter.spriteBorder;
                var prevTextureType = textureImporter.textureType;
                var prevSpriteImportMode = textureImporter.spriteImportMode;
                var prevSpritesheet = textureImporter.spritesheet;
                var prevW = widthProp.intValue;
                var prevH = heightProp.intValue;

               
                string androidPlatform = "Android";
                string iPhonePlatform = "iPhone";
                bool isAndroidPresetOverrider = IsPresetPlatformTextureMaxSizeIsBiggerThanOrign(androidPlatform, preset, textureImporter, out var orignAndroidSetting, out var orignAndroidMaxSize, out var presetAndroidMaxSize);
                bool isIPhonePresetOverrider = IsPresetPlatformTextureMaxSizeIsBiggerThanOrign(iPhonePlatform, preset, textureImporter, out var orignIPhoneSetting, out var orignIPhoneMaxSize, out var presetIPhoneMaxSize);
                preset.ApplyTo(importer);
                if (isAndroidPresetOverrider && (orignAndroidMaxSize < presetAndroidMaxSize))
                {
                    var currentSetting = textureImporter.GetPlatformTextureSettings(androidPlatform);
                    currentSetting.maxTextureSize = orignAndroidMaxSize;
                    textureImporter.SetPlatformTextureSettings(currentSetting);
                }
                if (isIPhonePresetOverrider && (orignIPhoneMaxSize < presetIPhoneMaxSize))
                {
                    var currentSetting = textureImporter.GetPlatformTextureSettings(iPhonePlatform);
                    currentSetting.maxTextureSize = orignIPhoneMaxSize;
                    textureImporter.SetPlatformTextureSettings(currentSetting);
                }
                importerSo.Update();
                widthProp.intValue = prevW;
                heightProp.intValue = prevH;

                if (prevTextureType == TextureImporterType.Sprite && textureImporter.textureType == TextureImporterType.Sprite) {
                    if (textureImporter.spriteBorder == Vector4.zero && prevSpriteBorder != Vector4.zero) {
                        textureImporter.spriteBorder = prevSpriteBorder;
                    }
                    if (textureImporter.spriteImportMode != prevSpriteImportMode) {
                        textureImporter.spriteImportMode = prevSpriteImportMode;
                    }
                    if (textureImporter.spriteImportMode == SpriteImportMode.Multiple) {
                        textureImporter.spritesheet = prevSpritesheet;
                    }
                }
            }
            else
            {
                preset.ApplyTo(importer);
                importerSo.Update();
            }

            if (!string.IsNullOrEmpty(assetBundleName))
            {
                assetBundleNameProp.stringValue = assetBundleName;
                assetBundleVariantProp.stringValue = assetBundleVariant;
            }
            importerSo.ApplyModifiedProperties();

            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Preset applied for <b>{assetPath}</b>");
        }

        static Preset CreatePresetForType(ImportAssetType importAssetType)
        {
            if (importAssetType == ImportAssetType.SpriteAtlases)
            {
                return new Preset(new SpriteAtlas());
            }

            var dummyAsset = AssetDatabaseUtility.FindAssetPaths($"__importer{importAssetType.ToString().ToLowerInvariant()}dummy__").FirstOrDefault();
            var defaultImporter = AssetImporter.GetAtPath(dummyAsset);
            return new Preset(defaultImporter);
        }

        void ApplyPresetToSpriteAtlas(Object asset, string assetPath)
        {
            if (!asset || asset.GetType() != typeof(SpriteAtlas) || preset == null || !preset.CanBeAppliedTo(asset) || !ShouldImport(assetPath))
            {
                return;
            }

            var atlas = (SpriteAtlas)asset;
            var isVariant = atlas.isVariant;
            var so = new SerializedObject(atlas);
            var includeInBuild = so.FindProperty("m_EditorData.bindAsDefault").boolValue;
            var masterAtlas = (SpriteAtlas)so.FindProperty("m_MasterAtlas").objectReferenceValue;
            var variantScale = so.FindProperty("m_EditorData.variantMultiplier").floatValue;
            var packables = atlas.GetPackables();
            preset.ApplyTo(atlas);
            atlas.SetIsVariant(isVariant);
            atlas.SetIncludeInBuild(includeInBuild);
            if (isVariant)
            {
                atlas.SetMasterAtlas(masterAtlas);
                atlas.SetVariantScale(variantScale);
            }

            atlas.Add(packables);
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{GetName()}] Preset applied for <b>{assetPath}</b>");
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
        }

        static bool DataEquals(Preset preset, UnityEngine.Object obj)
        {
            if (preset == null || obj == null)
                return false;
            var properties = preset.PropertyModifications;
            var so = new SerializedObject(obj);
            foreach (var prop in properties)
            {
                if (prop.propertyPath == "m_UserData" || prop.propertyPath == "m_PSDShowRemoveMatteOption")
                    continue;
                var value = so.FindProperty(prop.propertyPath);
                if (value.GetPropertyValueAsString() != prop.value)
                    return false;
            }

            return true;
        }
        public bool IsPresetPlatformTextureMaxSizeIsBiggerThanOrign(
            string platform,
            Preset preset, 
            TextureImporter importer, 
            out TextureImporterPlatformSettings orignSetting,
            out int orginTextureSize,
            out int presetTetureSize)
        {
            var so = new SerializedObject(importer);
            SerializedProperty property = so.FindProperty("m_PlatformSettings");
            string targetPresetPropertyPath = null;
            bool orignHasOverride = false;
            int orignMaxTextureSize = 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                var buildTarget = element.FindPropertyRelative("m_BuildTarget").stringValue;
              
                if (buildTarget == platform)
                {
                    targetPresetPropertyPath = element.propertyPath;
                    orignHasOverride = element.FindPropertyRelative("m_Overridden").boolValue;
                    orignMaxTextureSize = element.FindPropertyRelative("m_MaxTextureSize").intValue;
                    break;
                }
               
            }
            bool presetOverrider = false;
            int presetMaxTextureSize = 0;
            if (!string.IsNullOrEmpty(targetPresetPropertyPath))
            {
                var modify = preset.PropertyModifications;
                var presetOverriderPath = $"{targetPresetPropertyPath}.m_Overridden";
                var presetMaxSizePath = $"{targetPresetPropertyPath}.m_MaxTextureSize";
                foreach (var proper in modify)
                {
                    if (proper.propertyPath == presetOverriderPath)
                    {
                        presetOverrider = (proper.value == "1");
                    }
                    else if (proper.propertyPath == presetMaxSizePath)
                    {
                        presetMaxTextureSize = int.Parse(proper.value);
                    }
                }
            }
            //Debug.LogError($"targetPresetPropertyPath {targetPresetPropertyPath} orignHasOverride {orignHasOverride} orignMaxTextureSize {orignMaxTextureSize}  presetOverrider {presetOverrider} presetMaxTextureSize {presetMaxTextureSize}");
            
           orginTextureSize = orignMaxTextureSize;
           presetTetureSize = presetMaxTextureSize;
           orignSetting = importer.GetPlatformTextureSettings(platform);

           return presetOverrider;
        }
        
        void OnPostprocessTexture(string assetPath, TextureImporter importer)
        {
            if (DataEquals(preset, importer))
                return;
            string androidPlatform = "Android";
            string iPhonePlatform = "iPhone";
            bool isAndroidPresetOverrider = IsPresetPlatformTextureMaxSizeIsBiggerThanOrign(
                androidPlatform,
                preset, 
                importer ,
                out var orignAndroidSetting,
                out var orignAndroidMaxSize,
                out var presetAndroidMaxSize);
            bool isIPhonePresetOverrider = IsPresetPlatformTextureMaxSizeIsBiggerThanOrign(
                iPhonePlatform, 
                preset, 
                importer, 
                out var orignIPhoneSetting, 
                out var orignIPhoneMaxSize, 
                out var presetIPhoneMaxSize);

            if (!preset.ApplyTo(importer))
                return;
            if (isAndroidPresetOverrider&& (orignAndroidMaxSize<presetAndroidMaxSize)) 
            {
                var currentSetting=  importer.GetPlatformTextureSettings(androidPlatform);
                currentSetting.maxTextureSize = orignAndroidMaxSize;
                importer.SetPlatformTextureSettings(currentSetting);
            }
            if (isIPhonePresetOverrider && (orignIPhoneMaxSize < presetIPhoneMaxSize))
            {
                var currentSetting = importer.GetPlatformTextureSettings(iPhonePlatform);
                currentSetting.maxTextureSize = orignIPhoneMaxSize;
                importer.SetPlatformTextureSettings(currentSetting);
            }
            EditorUtility.SetDirty(importer);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{GetName()}] Preset applied for <b>{assetPath}</b>");
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
        }
    }
}