using System;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal class EditorTexture
    {
        Material iconMat;

        SkinnedColor inactiveColor = new SkinnedColor(0.72f, 0.4f);
        SkinnedColor highlightedColor = new SkinnedColor(0.9f, 0.2f);
        SkinnedColor activeColor = new SkinnedColor(0.4f, 0.55f);

        GUIContent inactiveGUIContent;
        GUIContent highlightedGUIContent;
        GUIContent activeGUIContent;

        Texture2D icon;
        Texture inactive;
        Texture active;
        Texture highlighted;
        string data;
        int width;
        int height;

        public EditorTexture(int width, int height, string base64ImageData)
        {
            this.width = width;
            this.height = height;
            data = base64ImageData;
        }

        public Texture Highlighted
        {
            get
            {
                if (!highlighted)
                {
                    highlighted = RenderIcon(highlightedColor);
                }

                return highlighted;
            }
        }

        public Texture Active
        {
            get
            {
                if (!active)
                {
                    active = RenderIcon(activeColor);
                }

                return active;
            }
        }

        public Texture Inactive
        {
            get
            {
                if (!inactive)
                {
                    inactive = RenderIcon(inactiveColor);
                }

                return inactive;
            }
        }

        public Texture2D Raw
        {
            get
            {
                if (!icon)
                {
                    var bytes = Convert.FromBase64String(data);
                    icon = TextureUtility.LoadImage(width, height, bytes);
                }

                return icon;
            }
        }

        public Texture Get(Rect rect)
        {
            if (!GUI.enabled)
            {
                return Inactive;
            }

            if (rect.Contains(Event.current.mousePosition))
            {
                return Highlighted;
            }

            return Active;
        }

        Texture RenderIcon(Color color)
        {
            if (!iconMat || !iconMat.shader)
            {
                var shader = ShaderUtil.CreateShaderAsset(System.IO.File.ReadAllText("Assets/Packages/URS/AssetPipeline/Editor/Assets/GUIIcon.shader"));
                iconMat = new Material(shader);
            }

            iconMat.SetColor("_Color", color);
            var prevSRGB = GL.sRGBWrite;
            GL.sRGBWrite = true;
            var prev = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(width, height, 0);
            RenderTexture.active = rt;
            GL.Clear(false, true, new Color(1, 1, 1, 0));
            Graphics.Blit(Raw, rt, iconMat);

            var texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.alphaIsTransparency = true;
            texture.Apply();

            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.active = prev;
            GL.sRGBWrite = prevSRGB;
            return texture;
        }
    }
}