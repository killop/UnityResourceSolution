using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    public static class Previewer
    {
        private static PreviewRenderer _renderer;
        private static Dictionary<Type, IPreviewGenerator> _generators = new Dictionary<Type, IPreviewGenerator>();
        private static List<Type> _supportedTypes = new List<Type>();

        public static ReadOnlyCollection<Type> SupportedTypes
        {
            get { return _supportedTypes.AsReadOnly(); }
        }

        public static PreviewResolution Resolution
        {
            get { return _renderer.Resolution; }
            set { _renderer.Resolution = value; }
        }

        // We initialize on load so that we can call render() the next update to get correct lighting.
        // Whatever updates HDRP does to the lighting happen after the end of the update so
        // we can't just call two Render()s right after one another.
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_renderer != null)
                return;

            _renderer = new PreviewRenderer(LibraryPreferences.PreviewResolution);

            var types = TypeCache.GetTypesDerivedFrom<IPreviewGenerator>();
            
            foreach (Type type in types)
            {
                if (type.IsAbstract)
                    continue;
                
                if (type.BaseType == null)
                    continue;
                
                if (type.BaseType.GenericTypeArguments.Length != 1)
                {
                    continue;
                }

                Type objectType = type.BaseType.GenericTypeArguments[0];
                _generators[objectType] = (IPreviewGenerator)Activator.CreateInstance(type, new object[] { _renderer });
                _supportedTypes.Add(objectType);
            }
            
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;

            // This is really ugly workaround be required to force HDRP
            // to update the lighting in the preview scene.
            // Otherwise the first previews would have wrong (different) lighting than the rest.
#if HDRP_1_OR_NEWER
            EditorApplication.delayCall += () =>
            {
                _renderer.Render();
            };
#endif
        }

        public static Texture2D GenerateFromGuid(string guid)
        {
            using (var assetScope = new AssetUtility.LoadAssetScope(guid))
            {
                return Generate(assetScope.Asset);
            }
        }

        public static Texture2D Generate(Object obj)
        {
            if (obj is null)
                return null;
            
            Initialize();

            if (_generators.TryGetValue(obj.GetType(), out IPreviewGenerator generator))
            {
                return generator.Generate(obj);
            }

            return null;
        }

        private static void Cleanup()
        {
            _renderer.Cleanup();
            foreach (var generator in _generators.Values)
            {
                generator.Cleanup();
            }
        }
    }
}
