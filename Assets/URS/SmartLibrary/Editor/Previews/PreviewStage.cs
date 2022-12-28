using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    public class PreviewStage : IDisposable
    {
        //private static readonly float InitialLightIntensity = 0.7f;
        //private static readonly Color InitialLightColor = new Color32(204, 204, 204, Byte.MaxValue);
        private static readonly Quaternion InitialMainLightRotation = Quaternion.Euler(48, -65, 0);
        
        private Scene _scene;
        private Camera _camera;
        private Light _mainLight;
        private List<GameObject> _gameObjects = new List<GameObject>();
        private bool _hasBeenDisposed = false;

        public Scene Scene
        {
            get { return _scene; }
        }
        
        public Camera Camera
        {
            get { return _camera; }
        }

        public Light MainLight
        {
            get { return _mainLight; }
        }

        public PreviewStage()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            
            var cameraGO = CreateGameObject("Preview Camera");
            _camera = cameraGO.AddComponent<Camera>();
            
            _camera.cameraType = CameraType.Preview;
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.Depth;
            _camera.fieldOfView = 15f;
            _camera.farClipPlane = 10f;
            _camera.nearClipPlane = 2f;
            _camera.renderingPath = RenderingPath.Forward;
            _camera.useOcclusionCulling = false;
            _camera.scene = _scene;

            var lightGO = CreateGameObject("Main Light");
            _mainLight = lightGO.AddComponent<Light>();
            
            _mainLight.type = LightType.Directional;
            _mainLight.intensity = 0.7f;
            _mainLight.color = new Color32(255, 255, 255, Byte.MaxValue);
            _mainLight.transform.rotation = InitialMainLightRotation;
        }
        
        /// <summary>
        /// Creates a new <see cref="GameObject"/> in the <see cref="PreviewStage"/>,
        /// with the <see cref="HideFlags.HideAndDontSave"/> flag.
        /// </summary>
        /// <param name="name">The name of the <see cref="GameObject"/>.</param>
        /// <returns>The newly created <see cref="GameObject"/>.</returns>
        public GameObject CreateGameObject(string name)
        {
            var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            AddGameObject(go);
            
            return go;
        }

        /// <summary>
        /// Moves the specified <see cref="GameObject"/> to the <see cref="PreviewStage"/>.
        /// </summary>
        /// <param name="go">The <see cref="GameObject"/> to move.</param>
        public void AddGameObject(GameObject go)
        {
            if (_gameObjects.Contains(go))
                return;
            
            _gameObjects.Add(go);
            SceneManager.MoveGameObjectToScene(go, _scene);
        }

        ~PreviewStage()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            if (_hasBeenDisposed)
                return;
            
            for (int i = _gameObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_gameObjects[i]);
            }

            EditorSceneManager.ClosePreviewScene(_scene);
                
            _hasBeenDisposed = true;
        }
    }
}
