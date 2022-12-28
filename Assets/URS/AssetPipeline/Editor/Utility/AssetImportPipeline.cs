using System.Collections.Generic;
using System.IO;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Video;

namespace Daihenka.AssetPipeline
{
    public static class AssetImportPipeline
    {
        internal static bool ForceRefreshAfterImport;

        internal static readonly Dictionary<ImportAssetType, Texture> AssetTypeIcons = new Dictionary<ImportAssetType, Texture>
        {
            {ImportAssetType.Textures, InternalEditorUtility.FindIconForFile("t.png")},
            {ImportAssetType.Models, EditorGUIUtility.FindTexture("PrefabModel Icon")},
            {ImportAssetType.Audio, InternalEditorUtility.FindIconForFile("t.mp3")},
            {ImportAssetType.Animations, (Texture2D) UnityEditorDynamic.EditorGUIUtility.FindTextureByType(typeof(AnimationClip))},
            {ImportAssetType.Materials, InternalEditorUtility.FindIconForFile("t.mat")},
            {ImportAssetType.Prefabs, InternalEditorUtility.FindIconForFile("t.prefab")},
            {ImportAssetType.SpriteAtlases, (Texture2D) UnityEditorDynamic.EditorGUIUtility.FindTextureByType(typeof(SpriteAtlas))},
            {ImportAssetType.Fonts, (Texture2D) UnityEditorDynamic.EditorGUIUtility.FindTextureByType(typeof(Font))},
            {ImportAssetType.Videos, (Texture2D) UnityEditorDynamic.EditorGUIUtility.FindTextureByType(typeof(VideoPlayer))},
            {ImportAssetType.Other, (Texture2D) UnityEditorDynamic.EditorGUIUtility.FindTextureByType(typeof(TextAsset))},
        };

        internal static string GetAssetType(string assetPath)
        {
            var extension = Path.GetExtension(assetPath).ToLowerInvariant();
            switch (extension)
            {
                case ".gradle":
                    return "Gradle Script";

                case ".groovy":
                    return "Groovy Script";

                case ".jslib":
                    return "WebGL JavaScript Library";

                case ".pdb":
                    return "Program Database";

                case ".rsp":
                    return "C# Response File";

                case ".aar":
                    return "Android Archive";

                case ".mm":
                case ".h":
                case ".m":
                    return "Native Plugin";

                case ".plist":
                    return "Property List (OSX)";

                case ".controller":
                    return "Animation Controller";

                case ".overridecontroller":
                    return "Animation Override Controller";

                case ".mask":
                    return "Avatar Mask";

                case ".playable":
                    return "Playable";

                case ".signal":
                    return "Timeline Signal";

                case ".spriteatlas":
                    return "SpriteAtlas";

                case ".preset":
                    return "Preset";

                case ".cs":
                    return "C# Script";

                case ".guiskin":
                    return "GUISkin";

                case ".dll":
                    return "C# Assembly";

                case ".asmdef":
                    return "Assembly Definition";

                case ".asmref":
                    return "Assembly Definition Reference";

                case ".mat":
                    return "Material";

                case ".physicmaterial":
                    return "Physics Material";

                case ".prefab":
                    return "Prefab";

                case ".compute":
                    return "Compute Shader";

                case ".shader":
                    return "Shader";

                case ".shadervariants":
                    return "Shader Variant Collection";

                case ".hlsl":
                case ".cginc":
                    return "Shader Include";

                case ".shadergraph":
                    return "ShaderGraph";

                case ".md":
                    return "Markdown File";

                case ".json":
                    return "JSON File";

                case ".xml":
                    return "XML File";

                case ".txt":
                    return "TextAsset";

                case ".unity":
                    return "Scene";

                case ".prefs":
                    return "Preferences";

                case ".anim":
                    return "AnimationClip";

                case ".meta":
                    return "MetaFile";

                case ".mixer":
                    return "Audio Mixer";

                case ".uxml":
                    return "UIElements Asset";

                case ".uss":
                    return "UIElements StyleSheet";

                case ".giparams":
                    return "GI Parameters";

                case ".lighting":
                    return "Lighting Settings";

                case ".ttf":
                case ".otf":
                case ".fon":
                case ".fnt":
                    return "Font";

                case ".aac":
                case ".aif":
                case ".aiff":
                case ".au":
                case ".mid":
                case ".midi":
                case ".mp3":
                case ".mpa":
                case ".ra":
                case ".ram":
                case ".wma":
                case ".wav":
                case ".wave":
                case ".ogg":
                case ".flac":
                    return "AudioClip";

                case ".ai":
                case ".apng":
                case ".png":
                case ".bmp":
                case ".cdr":
                case ".dib":
                case ".eps":
                case ".exif":
                case ".gif":
                case ".ico":
                case ".icon":
                case ".j":
                case ".j2c":
                case ".j2k":
                case ".jas":
                case ".jiff":
                case ".jng":
                case ".jp2":
                case ".jpc":
                case ".jpe":
                case ".jpeg":
                case ".jpf":
                case ".jpg":
                case ".jpw":
                case ".jpx":
                case ".jtf":
                case ".mac":
                case ".omf":
                case ".qif":
                case ".qti":
                case ".qtif":
                case ".tex":
                case ".tfw":
                case ".tga":
                case ".tif":
                case ".tiff":
                case ".wmf":
                case ".psd":
                case ".exr":
                case ".hdr":
                    return "Texture";

                case ".cubemap":
                    return "Cubemap";

                case ".rendertexture":
                    return "RenderTexture";

                case ".flare":
                    return "Flare";

                case ".3df":
                case ".3dm":
                case ".3dmf":
                case ".3ds":
                case ".3dv":
                case ".3dx":
                case ".blend":
                case ".c4d":
                case ".lwo":
                case ".lws":
                case ".ma":
                case ".max":
                case ".mb":
                case ".mesh":
                case ".obj":
                case ".vrl":
                case ".wrl":
                case ".wrz":
                case ".fbx":
                    return "Model";

                case ".dv":
                case ".mp4":
                case ".mpg":
                case ".mpeg":
                case ".m4v":
                case ".ogv":
                case ".vp8":
                case ".webm":
                case ".asf":
                case ".asx":
                case ".avi":
                case ".dat":
                case ".divx":
                case ".dvx":
                case ".mlv":
                case ".m2l":
                case ".m2t":
                case ".m2ts":
                case ".m2v":
                case ".m4e":
                case ".mjp":
                case ".mov":
                case ".movie":
                case ".mp21":
                case ".mpe":
                case ".mpv2":
                case ".ogm":
                case ".qt":
                case ".rm":
                case ".rmvb":
                case ".wmw":
                case ".xvid":
                    return "VideoClip";

                case ".asset":
                case ".colors":
                case ".gradients":
                case ".curves":
                case ".curvesnormalized":
                case ".particlecurves":
                case ".particlecurvessigned":
                case ".particledoublecurves":
                case ".particledoublecurvessigned":
                    return "ScriptableObject";

                case ".otl":
                case ".hda":
                    return "Houdini Digital Asset";

                default: return extension;
            }
        }

        static string[] s_CachedAssetPaths;

        static AssetImportPipeline()
        {
            EditorApplication.update += OnUpdate;
        }

        internal static string[] CachedAssetPaths
        {
            get
            {
                if (s_CachedAssetPaths == null || s_CachedAssetPaths.Length == 0)
                {
                    RefreshCachedAssetPaths();
                }

                return s_CachedAssetPaths;
            }
        }

        static void RefreshCachedAssetPaths()
        {
            s_CachedAssetPaths = AssetDatabase.GetAllAssetPaths();
        }

        internal static void ClearCachedAssetPaths()
        {
            s_CachedAssetPaths = null;
        }

        static void OnUpdate()
        {
            if (ForceRefreshAfterImport)
            {
                ForceRefreshAfterImport = false;
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        [MenuItem("Assets/Asset Pipeline/Force Apply Processors")]
        static void ForceApplyProcessors()
        {
            foreach (var obj in Selection.objects)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                AssetProcessor.SetForceApply(assetPath, true);
                AssetDatabase.ImportAsset(assetPath);
            }
        }

        [MenuItem("Assets/Asset Pipeline/Force Apply Processors", true)]
        static bool ValidateForceApplyProcessors()
        {
            return Selection.objects.Length > 0;
        }

        public static List<AssetProcessor> GetProcessorsForAsset(string assetPath)
        {
            var processors = new List<AssetProcessor>();
            var matches = AssetImportProfile.GetProfileMatches(assetPath);
            foreach (var match in matches)
            {
                match.RemoveNullFilters();
                foreach (var setting in match.assetFilters)
                {
                    if (setting.IsMatch(assetPath))
                    {
                        setting.RemoveNullProcessors();
                        foreach (var processor in setting.assetProcessors.OrderByDescending(x => x.Priority))
                        {
                            if (processor.enabled)
                            {
                                processors.Add(processor);
                            }
                        }
                    }
                }
            }

            return processors;
        }
    }
}