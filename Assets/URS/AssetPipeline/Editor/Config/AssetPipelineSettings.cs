using System.Collections.Generic;
using System.IO;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    public class AssetPipelineSettings : ScriptableObject
    {
        const string kDefaultProfileStoragePath = "Assets/Editor/AssetPipeline/";
        static AssetPipelineSettings s_AssetPipelineSettings;
        public const string kSettingsPath = "Assets/Editor/AssetPipelineSettings.asset";
        public const string kDeveloperModeKey = "DaihenkaAssetPipelineDeveloperMode";

#pragma warning disable 0414
        [SerializeField] List<AssetTypeFileExtensions> m_AssetTypeFileExtensions;
        [SerializeField] string m_DefaultProfileStoragePath;
        [SerializeField] Color m_EnabledColor = new Color(0.2688679f, 1f, 0.2688679f);
        [SerializeField] Color m_DisabledColor = new Color(1f, 0.2688679f, 0.2688679f);
        [SerializeField] StringConvention m_DefaultConvention = StringConvention.None;
#pragma warning restore 0414

        public StringConvention DefaultPathVariableConvention => m_DefaultConvention;
        public string profileStoragePath => string.IsNullOrEmpty(m_DefaultProfileStoragePath) ? kDefaultProfileStoragePath : m_DefaultProfileStoragePath;
        public List<AssetTypeFileExtensions> assetTypeFileExtensions => m_AssetTypeFileExtensions;
        public static AssetPipelineSettings Settings => GetOrCreateSettings();

        public static bool DeveloperMode
        {
            get => EditorPrefs.GetBool(kDeveloperModeKey, false);
            set => EditorPrefs.SetBool(kDeveloperModeKey, value);
        }

        internal static AssetPipelineSettings GetOrCreateSettings()
        {
            if (!s_AssetPipelineSettings)
            {
                var folderPath = Path.GetDirectoryName(kSettingsPath);
                PathUtility.CreateDirectoryIfNeeded(folderPath);

                s_AssetPipelineSettings = AssetDatabase.LoadAssetAtPath<AssetPipelineSettings>(kSettingsPath);
                if (s_AssetPipelineSettings == null)
                {
                    s_AssetPipelineSettings = CreateInstance<AssetPipelineSettings>();
                    s_AssetPipelineSettings.m_AssetTypeFileExtensions = new List<AssetTypeFileExtensions>
                    {
                        new AssetTypeFileExtensions(ImportAssetType.Textures, ".ai", ".apng", ".png", ".bmp", ".cdr", ".dib", ".eps", ".exif", ".gif", ".ico", ".icon", ".j", ".j2c", ".j2k", ".jas", ".jiff", ".jng", ".jp2", ".jpc", ".jpe", ".jpeg", ".jpf", ".jpg", ".jpw", ".jpx", ".jtf", ".mac",
                            ".omf", ".qif", ".qti", ".qtif", ".tex", ".tfw", ".tga", ".tif", ".tiff", ".wmf", ".psd", ".exr", ".hdr"),
                        new AssetTypeFileExtensions(ImportAssetType.Models, ".3df", ".3dm", ".3dmf", ".3ds", ".3dv", ".3dx", ".blend", ".c4d", ".lwo", ".lws", ".ma", ".max", ".mb", ".mesh", ".obj", ".vrl", ".wrl", ".wrz", ".fbx"),
                        new AssetTypeFileExtensions(ImportAssetType.Audio, ".aac", ".aif", ".aiff", ".au", ".mid", ".midi", ".mp3", ".mpa", ".ra", ".ram", ".wma", ".wav", ".wave", ".ogg", ".flac"),
                        new AssetTypeFileExtensions(ImportAssetType.Videos, ".dv", ".mp4", ".mpg", ".mpeg", ".m4v", ".ogv", ".vp8", ".webm", ".asf", ".asx", ".avi", ".dat", ".divx", ".dvx", ".mlv", ".m2l", ".m2t", ".m2ts", ".m2v", ".m4e", ".mjp", ".mov", ".movie", ".mp21", ".mpe", ".mpv2", ".ogm",
                            ".qt", ".rm", ".rmvb", ".wmw", ".xvid"),
                        new AssetTypeFileExtensions(ImportAssetType.Fonts, ".ttf", ".otf", ".fon", ".fnt"),
                        new AssetTypeFileExtensions(ImportAssetType.Animations, ".anim"),
                        new AssetTypeFileExtensions(ImportAssetType.Materials, ".mat"),
                        new AssetTypeFileExtensions(ImportAssetType.Prefabs, ".prefab"),
                        new AssetTypeFileExtensions(ImportAssetType.SpriteAtlases, ".spriteatlas"),
                    };
                    s_AssetPipelineSettings.m_DefaultProfileStoragePath = kDefaultProfileStoragePath;
                    AssetDatabase.CreateAsset(s_AssetPipelineSettings, kSettingsPath);
                    AssetDatabase.SaveAssets();
                }
            }

            return s_AssetPipelineSettings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(Settings);
        }

        public static Color GetStatusColor(bool enabled)
        {
            return enabled ? Settings.m_EnabledColor : Settings.m_DisabledColor;
        }
    }
}