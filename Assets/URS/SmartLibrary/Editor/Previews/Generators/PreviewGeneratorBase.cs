using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    public interface IPreviewGenerator
    {
        public PreviewRenderer Renderer
        {
            get;
        }

        public Texture2D Generate(Object target);

        public void Cleanup();
    }
    
    public abstract class PreviewGeneratorBase<T> : IPreviewGenerator where T : Object
    {
        public PreviewRenderer Renderer { get; }

        public PreviewGeneratorBase(PreviewRenderer renderer)
        {
            Renderer = renderer;
        }

        Texture2D IPreviewGenerator.Generate(Object target)
        {
            if (target is T obj)
                return Generate(obj);

            return null;
        }

        public Texture2D Generate(T target)
        {
            Vector3 cachedCameraPos = Renderer.Camera.transform.position;
            Quaternion cachedCameraRot = Renderer.Camera.transform.rotation;
            
            Renderer.BeginStaticRender();
            bool doRender = BeforeRender(target);
            
            if (doRender)
                Renderer.Render();
            
            AfterRender();

            Texture2D result = null;

            if (doRender)
                result = Renderer.EndStaticPreview();
            else
                Renderer.FinishFrame();

            Renderer.Camera.transform.position = cachedCameraPos;
            Renderer.Camera.transform.rotation = cachedCameraRot;
            
            return result;
        }

        protected abstract bool BeforeRender(T target);
        
        protected virtual void AfterRender() { }
        
        public virtual void Cleanup()
        {
            
        }
    }
}
