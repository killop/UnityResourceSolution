using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    public class MeshPreviewGenerator : PreviewGeneratorBase<Mesh>
    {
        private Material _wireframeMaterial;
        private Material _standardMaterial;
        private CameraClearFlags _cachedClearFlags;
        private bool _cachedAllowScriptableRenderPipeline;

        public MeshPreviewGenerator(PreviewRenderer renderer) : base(renderer)
        {
            
        }

        protected override bool BeforeRender(Mesh target)
        {
            if (_wireframeMaterial == null)
                _wireframeMaterial = CreateWireframeMaterial();

            if (_standardMaterial == null)
                _standardMaterial = CreateStandardMaterial();

            // We position the camera so the mesh is fully visible in the camera.
            PreviewEditorUtility.PositionCamera3D(Renderer.Camera, target.bounds, 8);

            _cachedClearFlags = Renderer.Camera.clearFlags;
            Renderer.Camera.clearFlags = CameraClearFlags.Nothing;

            _cachedAllowScriptableRenderPipeline = Renderer.AllowScriptableRenderPipeline;
            Renderer.AllowScriptableRenderPipeline = false;
            
            PreviewEditorUtility.DrawMesh(Renderer.Camera, target, Vector3.zero, Quaternion.identity, _standardMaterial, 0);
            
            Renderer.Render();
            
            GL.wireframe = true;
            PreviewEditorUtility.DrawMesh(Renderer.Camera, target, Vector3.zero, Quaternion.identity, _wireframeMaterial, 0);

            return true;
        }

        protected override void AfterRender()
        {
            GL.wireframe = false;
            Renderer.AllowScriptableRenderPipeline = _cachedAllowScriptableRenderPipeline;
            Renderer.Camera.clearFlags = _cachedClearFlags;
        }

        public override void Cleanup()
        {
            if (_wireframeMaterial != null)
                Object.DestroyImmediate(_wireframeMaterial);
            
            if (_standardMaterial != null)
                Object.DestroyImmediate(_standardMaterial);
        }

        private static Material CreateWireframeMaterial()
        {
            Shader shader = PreviewEditorUtility.FindBuiltinShader("Internal-Colored.shader");
            bool flag = !shader;
            Material result;
            if (flag)
            {
                Debug.LogWarning("Could not find the built-in Colored shader");
                result = null;
            }
            else
            {
                Material material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
                material.SetColor("_Color", new Color(0f, 0f, 0f, 0.3f));
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_ZBias", -1f);
                result = material;
            }
            return result;
        }

        private static Material CreateStandardMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.hideFlags = HideFlags.HideAndDontSave;
            
            return material;
        }
    }
}
