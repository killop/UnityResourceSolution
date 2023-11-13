using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Daihenka.AssetPipeline.Import;
using NinjaBeats;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

public class CustomAssetImportPostprocessor
{
    public const string VFXDirectoryPath = "Assets/GameResources/VFX/Texture";
    public const string VFXDirectoryPath2 = "Assets/GameResources/VFX/Scene/Texture";


    private static string[] Platforms = new string[] { "Android", "iOS" };

    public static void OnPostprocessTexture(string assetPath, AssetImporter assetImporter)
    {
        if (assetPath == null) return;
        if ((!assetPath.Contains(VFXDirectoryPath)) && (!assetPath.Contains(VFXDirectoryPath2))) return;

        var ti = assetImporter as TextureImporter;
        if (ti == null) return;

        bool showWarning = false;
        string message = "";
        foreach (var platform in Platforms)
        {
            var currentSetting = ti.GetPlatformTextureSettings(platform);
            // Debug.LogError("platform.name " + platform + " tPath ? " + (tPath));
            if (currentSetting == null)
            {
                message += $"  没有安装{platform}的扩展  ";
                showWarning = true;
            }

            if (!currentSetting.overridden)
            {
                message += $"   {platform}没有点击overrider  ";
                showWarning = true;
            }

            if (currentSetting.overridden && currentSetting.maxTextureSize > 1024)
            {
                message += $"  {platform}maxTextureSize 不能高于1024  ";
                showWarning = true;
            }

            if (currentSetting.overridden && currentSetting.format != TextureImporterFormat.ASTC_4x4
                                          && currentSetting.format != TextureImporterFormat.ASTC_5x5
                                          && currentSetting.format != TextureImporterFormat.ASTC_6x6
                                          && currentSetting.format != TextureImporterFormat.ASTC_8x8
                                          && currentSetting.format != TextureImporterFormat.ASTC_10x10
                                          && currentSetting.format != TextureImporterFormat.ASTC_12x12
               )
            {
                message += $"{platform}format 不是astc,当前的格式是 {currentSetting.format}";
                showWarning = true;
            }
        }

        if (showWarning)
        {
            if (EditorApplication.isUpdating && EditorUtility.DisplayDialog("警告", $"特效贴图没有设置,路径{assetPath}，原因 {message}", "确定"))
            {
            }
        }
        else
        {
            Debug.Log($"特效贴图设置OK,路径{assetPath}");
        }
    }

    public delegate void CheckVFXTextureDelegate(string assetPath, string message);

    public static void CheckVFXTexture(string assetPath, CheckVFXTextureDelegate func, int preferMaxTextureSize = 1024)
    {
        var assetImporter = AssetImporter.GetAtPath(assetPath);

        var ti = assetImporter as TextureImporter;

        //Debug.LogError("platform.name " + tPath + " is null? " + (ti==null));
        if (ti == null)
            return;

        string message = "";

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture != null)
        {
            if ((texture.width & texture.width - 1) != 0 || (texture.height & texture.height - 1) != 0)
            {
                message += "  不是2的幂次方";    
            }
        }

        if (ti.isReadable)
        {
            message += "  不能开启Read/Write Enabled";
        }
        
        // if (ti.mipmapEnabled)
        // {
        //     message += $"  不能开启mipmap";
        // }
        //
        // if (ti.streamingMipmaps)
        // {
        //     message += "  不能开启mipmap串流";
        // }

        int maxTextureSize = 0;
        TextureImporterFormat format = TextureImporterFormat.Automatic;
        bool isFirst = true;
        foreach (var platform in Platforms)
        {
            var currentSetting = ti.GetPlatformTextureSettings(platform);
            // Debug.LogError("platform.name " + platform + " tPath ? " + (tPath));
            if (currentSetting == null)
            {
                message += $"  没有安装\'{platform}\'的扩展";
            }

            if (!currentSetting.overridden)
            {
                message += $"  \'{platform}\'没有点击override";
            }

            if (currentSetting.overridden && currentSetting.maxTextureSize > preferMaxTextureSize)
            {
                message += $"  \'{platform}\'maxTextureSize 不能高于{preferMaxTextureSize}";
            }

            if (currentSetting.overridden && currentSetting.format != TextureImporterFormat.ASTC_4x4
                                          && currentSetting.format != TextureImporterFormat.ASTC_5x5
                                          && currentSetting.format != TextureImporterFormat.ASTC_6x6
                                          && currentSetting.format != TextureImporterFormat.ASTC_8x8
                                          && currentSetting.format != TextureImporterFormat.ASTC_10x10
                                          && currentSetting.format != TextureImporterFormat.ASTC_12x12
               )
            {
                message += $"  \'{platform}\'format 不是astc ,当前的格式是 {currentSetting.format}";
            }

            if (isFirst)
            {
                isFirst = false;
                maxTextureSize = currentSetting.maxTextureSize;
                format = currentSetting.format;
            }
            else
            {
                if (maxTextureSize != currentSetting.maxTextureSize)
                {
                    message += $"  \'Android\'与\'iOS\'maxTextureSize 不一致";
                }

                if (format != currentSetting.format)
                {
                    message += $"  \'Android\'与\'iOS\'format 不一致";
                }
            }
        }

        func(assetPath, message);
    }

    public static void CheckVFXTexture(CheckVFXTextureDelegate func)
    {
        var textures = AssetDatabase.FindAssets("t:Texture", new string[] { VFXDirectoryPath });
        if (textures == null) return;
        //Debug.LogError("platform.name " + textures.Length);
        foreach (var guid in textures)
        {
            var tPath = AssetDatabase.GUIDToAssetPath(guid);
            CheckVFXTexture(tPath, func);
        }
    }

    [MenuItem("Tools/检查特效贴图设置")]
    public static void CheckVFXTexture()
    {
        CheckVFXTexture((assetPath, message) =>
        {
            if (!string.IsNullOrWhiteSpace(message))
                Debug.LogError($"特效贴图没有设置,路径{assetPath},原因:{message}");
            else
                Debug.Log($"特效贴图设置OK,路径{assetPath}");
        });
    }
}