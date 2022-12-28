using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    public class MaterialPreviewGenerator : PreviewGeneratorBase<Material>
    {
        private Mesh _previewMesh;
        
        public MaterialPreviewGenerator(PreviewRenderer renderer) : base(renderer)
        {
            _previewMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        }

        protected override bool BeforeRender(Material target)
        {
            PreviewEditorUtility.PositionCamera3D(Renderer.Camera, new Bounds(Vector3.zero, Vector3.one), 5.5f);
            
            PreviewEditorUtility.DrawMesh(Renderer.Camera, _previewMesh, Vector3.zero, Quaternion.identity, target, 0);

            return true;
        }
    }
}
