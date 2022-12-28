using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    public class PrefabPreviewGenerator : PreviewGeneratorBase<GameObject>
    {
        private GameObject _targetInstance;

        public PrefabPreviewGenerator(PreviewRenderer renderer) : base(renderer)
        {
            
        }

        protected override bool BeforeRender(GameObject target)
        {
            Bounds bounds = PreviewEditorUtility.GetRenderableBounds(target, out bool has2DRenderer);

            if (bounds.size == Vector3.zero)
                return false;
            
            if (PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.NotAPrefab)
                _targetInstance = Object.Instantiate(target);
            else
                _targetInstance = (GameObject)PrefabUtility.InstantiatePrefab(target, Renderer.Stage.Scene);
            
            Renderer.AddGameObject(_targetInstance);
            
            if (EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D && has2DRenderer)
                PreviewEditorUtility.PositionCamera2D(Renderer.Camera, bounds, 7.5f);
            else
                PreviewEditorUtility.PositionCamera3D(Renderer.Camera, bounds, 7.5f);

            return true;
        }

        protected override void AfterRender()
        {
            Object.DestroyImmediate(_targetInstance);
        }
    }
}
