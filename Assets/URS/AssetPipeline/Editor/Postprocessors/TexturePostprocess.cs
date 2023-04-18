using System;
using System.Collections.Generic;
using System.Reflection;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

class CustomAssetImportPostprocessor  
{
    public const string VFXDirectoryPath = "Assets/GameResources/VFX/Texture";

    private static string[] Platforms = new string[] {"Android","iOS" };
   public static void OnPostprocessTexture(string assetPath,AssetImporter assetImporter)
   {
        if (assetPath == null) return;
        if (!assetPath.Contains(VFXDirectoryPath)) return;
   
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
            if (currentSetting.overridden&&currentSetting.maxTextureSize > 1024)
            {

                message += $"  {platform}maxTextureSize 不能高于1024  ";
                showWarning = true;
            }
            if (currentSetting.overridden&&currentSetting.format != TextureImporterFormat.ASTC_4x4
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

    [MenuItem("Tools/检查特效贴图设置")]
    public static void CheckVFXTexture()
    {
        var textures= AssetDatabase.FindAssets("t:Texture", new string[] { VFXDirectoryPath });
        if (textures == null) return;
        //Debug.LogError("platform.name " + textures.Length);
        foreach (var guid in textures)
        {
            var tPath = AssetDatabase.GUIDToAssetPath(guid);
            var assetImporter= AssetImporter.GetAtPath(tPath);
         
            var ti = assetImporter as TextureImporter;

            //Debug.LogError("platform.name " + tPath + " is null? " + (ti==null));
            if (ti == null) continue;

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
                if (currentSetting.overridden&&currentSetting.maxTextureSize > 1024)
                {

                    message += $"  {platform}maxTextureSize 不能高于1024 ";
                    showWarning = true;
                }
                if (currentSetting.overridden&&currentSetting.format != TextureImporterFormat.ASTC_4x4
                    && currentSetting.format != TextureImporterFormat.ASTC_5x5
                    && currentSetting.format != TextureImporterFormat.ASTC_6x6
                    && currentSetting.format != TextureImporterFormat.ASTC_8x8
                    && currentSetting.format != TextureImporterFormat.ASTC_10x10
                    && currentSetting.format != TextureImporterFormat.ASTC_12x12
                         )
                {

                    message += $"{platform}format 不是astc ,当前的格式是 {currentSetting.format}";
                    showWarning = true;
                }
                
            }
            if (showWarning)
            {
                Debug.LogError($"特效贴图没有设置,路径{tPath},原因:{message}");
            }
            else
            {
                Debug.Log($"特效贴图设置OK,路径{tPath}");
            }

        }
    }
}