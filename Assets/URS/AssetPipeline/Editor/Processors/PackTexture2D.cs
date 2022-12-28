using System;
using System.IO;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("PreTextureRGB", ImportAssetTypeFlag.Textures)]
    public class PackTexture2D : AssetProcessor
    {
        const string kExtension = ".png";
        [SerializeField] string textureName;
        [SerializeField] Vector2Int textureSize = new Vector2Int(512, 512);
        [SerializeField] bool isLinear = true;
        [SerializeField] bool redChannel;
        [SerializeField] bool greenChannel;
        [SerializeField] bool blueChannel;
        [SerializeField] bool alphaChannel;
        [SerializeField] NamingConventionRule redChannelTextureFilter = new NamingConventionRule();
        [SerializeField] NamingConventionRule greenChannelTextureFilter = new NamingConventionRule();
        [SerializeField] NamingConventionRule blueChannelTextureFilter = new NamingConventionRule();
        [SerializeField] NamingConventionRule alphaChannelTextureFilter = new NamingConventionRule();

        public override void OnPostprocess(Object asset, string assetPath)
        {
            var channel = GetChannelForTexture(assetPath);
            if (channel == TextureChannel.None)
            {
                return;
            }

            var texture = (Texture2D) asset;
            var packedTextureFilename = ReplaceVariables(textureName, assetPath) + kExtension;
            var assetFolder = Path.GetDirectoryName(assetPath).FixPathSeparators();
            var packedTexturePath = Path.Combine(assetFolder, packedTextureFilename).FixPathSeparators();
            var packedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(packedTexturePath);
            var shouldOverwrite = false;
            if (!packedTexture)
            {
                packedTexture = new Texture2D(textureSize.x, textureSize.y, targetTextureFormat, false, isLinear);
                shouldOverwrite = true;
                if (File.Exists(packedTexturePath))
                {
                    ImageConversion.LoadImage(packedTexture, File.ReadAllBytes(packedTexturePath));
                }
            }
            else
            {
                packedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(packedTexturePath);
                if (!packedTexture.isReadable)
                {
                    packedTexture = GetResizedTexture(packedTexture, textureSize);
                    shouldOverwrite = true;
                }
            }

            var textureToPack = GetResizedTexture(texture, textureSize);
            var packedPixels = packedTexture.GetPixels();
            var pixels = textureToPack.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                var averageChannelValue = (pixels[i].r + pixels[i].g + pixels[i].b + pixels[i].a) / 4f;
                packedPixels[i][(int) channel] = averageChannelValue;
            }

            packedTexture.SetPixels(packedPixels);
            packedTexture.Apply();

            if (shouldOverwrite)
            {
                var isNew = string.IsNullOrEmpty(packedTexturePath);
                if (isNew)
                {
                    packedTexturePath = Path.Combine(assetFolder, packedTextureFilename + kExtension);
                }

                var bytes = GetEncodedBytes(kExtension, packedTexture);
                File.WriteAllBytes(packedTexturePath, bytes);
                AssetImportPipeline.ForceRefreshAfterImport = true;
            }
            else
            {
                EditorUtility.SetDirty(packedTexture);
                AssetDatabase.SaveAssets();
            }

            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Packed \"<b>{assetPath}</b>\" into {channel} Channel of \"<b>{packedTexturePath}</b>\"");
        }

        static byte[] GetEncodedBytes(string extension, Texture2D texture)
        {
            switch (extension)
            {
                case ".tga":
                    return texture.EncodeToTGA();
                case ".jpg":
                    return texture.EncodeToJPG();
                case ".exr":
                    return texture.EncodeToEXR();
                default:
                    return texture.EncodeToPNG();
            }
        }

        public static Texture2D LoadTGA(Stream tgaStream)
        {
            using (var r = new BinaryReader(tgaStream))
            {
                // Skip some header info we don't care about.
                // Even if we did care, we have to move the stream seek point to the beginning,
                // as the previous method in the workflow left it at the end.
                r.BaseStream.Seek(12, SeekOrigin.Begin);

                var width = r.ReadInt16();
                var height = r.ReadInt16();
                int bitDepth = r.ReadByte();

                // Skip a byte of header information we don't care about.
                r.BaseStream.Seek(1, SeekOrigin.Current);

                var tex = new Texture2D(width, height);
                var pulledColors = new Color32[width * height];

                if (bitDepth == 32)
                {
                    for (var i = 0; i < width * height; i++)
                    {
                        var red = r.ReadByte();
                        var green = r.ReadByte();
                        var blue = r.ReadByte();
                        var alpha = r.ReadByte();

                        pulledColors[i] = new Color32(blue, green, red, alpha);
                    }
                }
                else if (bitDepth == 24)
                {
                    for (var i = 0; i < width * height; i++)
                    {
                        var red = r.ReadByte();
                        var green = r.ReadByte();
                        var blue = r.ReadByte();

                        pulledColors[i] = new Color32(blue, green, red, 1);
                    }
                }
                else
                {
                    throw new Exception("TGA texture had non 32/24 bit depth.");
                }

                tex.SetPixels32(pulledColors);
                tex.Apply();
                return tex;
            }
        }

        TextureFormat targetTextureFormat
        {
            get
            {
                if (redChannel && !greenChannel && !blueChannel && !alphaChannel)
                {
                    return TextureFormat.RGB24;
                }

                if (!redChannel && !greenChannel && !blueChannel && alphaChannel)
                {
                    return TextureFormat.RGB24;
                }

                if (redChannel && greenChannel && !blueChannel && !alphaChannel)
                {
                    return TextureFormat.RGB24;
                }

                if (redChannel && greenChannel && blueChannel && !alphaChannel)
                {
                    return TextureFormat.RGB24;
                }

                return TextureFormat.ARGB32;
            }
        }

        Texture2D GetResizedTexture(Texture2D source, Vector2Int targetSize)
        {
            var rt = new RenderTexture(targetSize.x, targetSize.y, 0);
            var origActive = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            var resizedTexture = new Texture2D(targetSize.x, targetSize.y, TextureFormat.ARGB32, false, isLinear);
            resizedTexture.ReadPixels(new Rect(0, 0, targetSize.x, targetSize.y), 0, 0);
            resizedTexture.Apply(false, false);
            RenderTexture.active = origActive;
            DestroyImmediate(rt, true);
            return resizedTexture;
        }

        TextureChannel GetChannelForTexture(string assetPath)
        {
            var filename = Path.GetFileNameWithoutExtension(assetPath);
            if (redChannel && redChannelTextureFilter.IsMatch(filename))
            {
                return TextureChannel.Red;
            }

            if (greenChannel && greenChannelTextureFilter.IsMatch(filename))
            {
                return TextureChannel.Green;
            }

            if (blueChannel && blueChannelTextureFilter.IsMatch(filename))
            {
                return TextureChannel.Blue;
            }

            if (alphaChannel && alphaChannelTextureFilter.IsMatch(filename))
            {
                return TextureChannel.Alpha;
            }

            return TextureChannel.None;
        }

        enum TextureChannel
        {
            None = -1,
            Red = 0,
            Green = 1,
            Blue = 2,
            Alpha = 3
        }
    }
}