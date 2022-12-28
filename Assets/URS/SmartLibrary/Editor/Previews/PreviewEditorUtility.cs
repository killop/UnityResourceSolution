using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace Bewildered.SmartLibrary
{
    public static class PreviewEditorUtility
    {
        private static readonly Quaternion _rotation = Quaternion.Euler(20, -120, 0);
        
        private static PropertyInfo _shaderViewportInfo;
        private static PropertyInfo _shaderScissorInfo;
        private static Action<RenderTexture> _setRenderTextureNoViewport;
        private static PropertyInfo _guiTextureBlit2SrgbMaterialInfo;
        private static Func<string, Shader> _findBuildinShader;


        internal static Rect ShaderRawViewport
        {
            get { return (Rect)_shaderViewportInfo.GetValue(null); }
            set { _shaderViewportInfo.SetValue(null, value); }
        }
        
        internal static Rect ShaderRawScissor
        {
            get { return (Rect)_shaderScissorInfo.GetValue(null); }
            set { _shaderScissorInfo.SetValue(null, value); }
        }

        internal static Material GUITextureBlit2SRGBMaterial
        {
            get { return (Material)_guiTextureBlit2SrgbMaterialInfo.GetValue(null); }
        }

        static PreviewEditorUtility()
        {
            _shaderViewportInfo = TypeAccessor.GetProperty(typeof(ShaderUtil), "rawViewportRect");
            _shaderScissorInfo = TypeAccessor.GetProperty(typeof(ShaderUtil), "rawScissorRect");
            _setRenderTextureNoViewport = TypeAccessor.GetMethod<EditorGUIUtility>("SetRenderTextureNoViewport")
                .CreateDelegate<Action<RenderTexture>>();


             _guiTextureBlit2SrgbMaterialInfo =
                 TypeAccessor.GetProperty(typeof(EditorGUIUtility), "GUITextureBlit2SRGBMaterial");
        }


        internal static void SetRenderTextureNoViewport(RenderTexture renderTexture)
        {
            _setRenderTextureNoViewport(renderTexture);
        }

        internal static Shader FindBuiltinShader(string shaderName)
        {
            if (_findBuildinShader == null)
                _findBuildinShader = TypeAccessor.GetMethod<Shader>("FindBuiltin").CreateDelegate<Func<string, Shader>>();

            return _findBuildinShader(shaderName);
        }
        
        public static void PositionCamera3D(Camera camera, Bounds bounds, float distMultiplier)
        {
            float halfSize = Mathf.Max(bounds.extents.magnitude, 0.0001f);
            float distance = halfSize * distMultiplier;

            Vector3 cameraPosition = bounds.center - _rotation * (Vector3.forward * distance);

            camera.transform.SetPositionAndRotation(cameraPosition, _rotation);
            camera.nearClipPlane = distance - halfSize * 1.1f;
            camera.farClipPlane = distance + halfSize * 1.1f;
        }

        public static void PositionCamera2D(Camera camera, Bounds bounds, float distMultiplier)
        {
            float halfSize = Mathf.Max(bounds.extents.magnitude, 0.0001f);
            float distance = halfSize * distMultiplier;

            Vector3 cameraPosition = bounds.center - Vector3.forward * distance;

            camera.transform.position = cameraPosition;
            camera.nearClipPlane = distance - halfSize * 1.1f;
            camera.farClipPlane = distance + halfSize * 1.1f;
        }

        public static void DrawMesh(Camera camera, Mesh mesh, Vector3 position, Quaternion rotation, Material mat, int subMeshIndex)
        {
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), mat, subMeshIndex, camera);
        }

        public static Bounds GetRenderableBounds(GameObject go)
        {
            return GetRenderableBoundsRecursive(new Bounds(), go, out _);
        }
        
        public static Bounds GetRenderableBounds(GameObject go, out bool has2DRenderer)
        {
            return GetRenderableBoundsRecursive(new Bounds(), go, out has2DRenderer);
        }

        private static Bounds GetRenderableBoundsRecursive(Bounds bounds, GameObject go, out bool has2DRenderer)
        {
            has2DRenderer = false;
            
            // Do we have a mesh?
            var renderer = go.GetComponent<MeshRenderer>();
            var filter = go.GetComponent<MeshFilter>();
            if (renderer && filter && filter.sharedMesh)
            {
                // To prevent origin from always being included in bounds we initialize it
                // with renderer.bounds. This ensures correct bounds for meshes with origo outside the mesh.
                if (bounds.extents == Vector3.zero)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            // Do we have a skinned mesh?
            var skin = go.GetComponent<SkinnedMeshRenderer>();
            if (skin && skin.sharedMesh)
            {
                if (bounds.extents == Vector3.zero)
                    bounds = skin.bounds;
                else
                    bounds.Encapsulate(skin.bounds);
            }

            // Do we have a Sprite?
            var sprite = go.GetComponent<SpriteRenderer>();
            if (sprite && sprite.sprite)
            {
                has2DRenderer = true;
                if (bounds.extents == Vector3.zero)
                    bounds = sprite.bounds;
                else
                    bounds.Encapsulate(sprite.bounds);
            }

            // Do we have a billboard?
            var billboard = go.GetComponent<BillboardRenderer>();
            if (billboard && billboard.billboard && billboard.sharedMaterial)
            {
                has2DRenderer = true;
                if (bounds.extents == Vector3.zero)
                    bounds = billboard.bounds;
                else
                    bounds.Encapsulate(billboard.bounds);
            }

            // Recurse into children
            foreach (Transform t in go.transform)
            {
                bounds = GetRenderableBoundsRecursive(bounds, t.gameObject, out bool childHas2DRenderer);

                if (childHas2DRenderer)
                    has2DRenderer = true;
            }

            return bounds;
        }
    }
}
