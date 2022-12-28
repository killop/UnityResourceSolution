using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    public enum PreviewResolution
    {
        x64 = 64,
        x128 = 128,
        x256 = 256,
        x512 = 512
    };
    
    public class PreviewRenderer
    {
        private ColorSpace _colorSpace;
        private PreviewStage _stage;
        private PreviewResolution _resolution;
        private RenderTexture _renderTexture;
        private RenderTargetState _renderTargetState;

        public Camera Camera
        {
            get { return _stage.Camera; }
        }

        public PreviewStage Stage
        {
            get { return _stage; }
        }

        public PreviewResolution Resolution
        {
            get { return _resolution; }
            set
            {
                _resolution = value;
                InitializeNewRenderTexture();
            }
        }
        
        public bool AllowScriptableRenderPipeline { get; set; }

        public PreviewRenderer(PreviewResolution resolution)
        {
            _resolution = resolution;
            _stage = new PreviewStage();
            
            _colorSpace = QualitySettings.activeColorSpace;
        }

        public void BeginStaticRender()
        {
            InitPreview();
            
            SphericalHarmonicsL2 ambientProbe = RenderSettings.ambientProbe;
            Texture defaultTexture = ReflectionProbe.defaultTexture;
            if (Unsupported.SetOverrideLightingSettings(_stage.Scene))
            {
                RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.4f, 0); //new Color32(122, 132, 143, 255);//new Color32(128, 128, 128, Byte.MaxValue);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientProbe = ambientProbe;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflection = defaultTexture as Cubemap;
                RenderSettings.fog = false;
            }
        }
        
        private void InitPreview()
        {
            int size = (int) _resolution;
            if (_renderTexture == null || _renderTexture.width != size || _renderTexture.height != size)
            {
                InitializeNewRenderTexture();
            }

            _renderTargetState = RenderTargetState.Capture();
            PreviewEditorUtility.SetRenderTextureNoViewport(_renderTexture);
            GL.LoadOrtho();
            GL.LoadPixelMatrix(0.0f, _renderTexture.width,  _renderTexture.height, 0.0f);
            PreviewEditorUtility.ShaderRawViewport = new Rect(0.0f, 0.0f, _renderTexture.width, _renderTexture.height);
            PreviewEditorUtility.ShaderRawScissor = new Rect(0.0f, 0.0f, _renderTexture.width, _renderTexture.height);
            GL.Clear(true, true, Color.clear);
        }

        private void InitializeNewRenderTexture()
        {
            if (_renderTexture != null)
                Object.DestroyImmediate(_renderTexture);
            
            int size = (int) _resolution;

            // We don't use GetTemporary to get a render texture so that we can manage its lifetime.
            GraphicsFormat format = Camera.allowHDR
                ? GraphicsFormat.R16G16B16A16_SFloat
                : GraphicsFormat.R8G8B8A8_UNorm;
            var rtd = new RenderTextureDescriptor(size, size) { depthBufferBits = 24, msaaSamples = 8, useMipMap = false, sRGB = true, graphicsFormat = format };
            _renderTexture = new RenderTexture(rtd);
            _renderTexture.hideFlags = HideFlags.HideAndDontSave;
            Camera.targetTexture = _renderTexture;
        }
        
        public void Render()
        {
            bool scriptableRenderPipeline = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = AllowScriptableRenderPipeline;
            
            Camera.Render();
            
            Unsupported.useScriptableRenderPipeline = scriptableRenderPipeline;
        }
        
        public Texture2D EndStaticPreview()
        {
            if (!EditorApplication.isUpdating)
                Unsupported.RestoreOverrideLightingSettings();
            
            int size = (int) _resolution;
            
            RenderTexture temporary = RenderTexture.GetTemporary(size, size, 0, GraphicsFormat.R8G8B8A8_UNorm);
            Graphics.Blit(_renderTexture, temporary, PreviewEditorUtility.GUITextureBlit2SRGBMaterial);
            RenderTexture.active = temporary;
            
            Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            texture2D.ReadPixels(new Rect(0.0f, 0.0f, size, size), 0, 0);
            texture2D.Apply();
            
            RenderTexture.ReleaseTemporary(temporary);
            FinishFrame();
            
            return texture2D;
        }

        public void FinishFrame()
        {
            _renderTargetState.Restore();
            Unsupported.RestoreOverrideLightingSettings();
        }

        public void Cleanup()
        {
            if (_renderTexture != null)
                Object.DestroyImmediate(_renderTexture);
            
            _stage.Dispose();
        }

        public void AddGameObject(GameObject go)
        {
            _stage.AddGameObject(go);
        }
    }

    internal class RenderTargetState
    {
        private RenderTexture _renderTexture;
        private Rect _viewportRect;
        private Rect _scissorRect;
        
        private RenderTargetState() { }
        
        public static RenderTargetState Capture()
        {
            GL.PushMatrix();
            return new RenderTargetState()
            {
                _renderTexture = RenderTexture.active,
                _viewportRect = PreviewEditorUtility.ShaderRawViewport,
                _scissorRect = PreviewEditorUtility.ShaderRawScissor
            };
        }

        public void Restore()
        {
            PreviewEditorUtility.SetRenderTextureNoViewport(_renderTexture);
            PreviewEditorUtility.ShaderRawViewport = _viewportRect;
            PreviewEditorUtility.ShaderRawScissor = _scissorRect;
            GL.PopMatrix();
        }
    }
}
