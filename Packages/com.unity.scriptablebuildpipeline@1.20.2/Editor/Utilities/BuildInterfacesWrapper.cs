using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Internal interface so switch platform build task can initialize editor build callbacks
    /// </summary>
    internal interface IEditorBuildCallbacks : IContextObject
    {
        /// <summary>
        /// Callbacks need to be Initialized after platform switch
        /// </summary>
        void InitializeCallbacks();
    }

    /// <summary>
    /// Manages initialization and cleanup of Unity Editor IPreprocessShaders, IProcessScene, &amp; IProcessSceneWithReport build callbacks.
    /// </summary>
    public class BuildInterfacesWrapper : IDisposable, IEditorBuildCallbacks
    {
        Type m_Type = null;
        bool m_Disposed = false;

        internal static Hash128 SceneCallbackVersionHash = new Hash128();
        internal static Hash128 ShaderCallbackVersionHash = new Hash128();

        /// <summary>
        /// Default constructor, initializes properties to defaults
        /// </summary>
        public BuildInterfacesWrapper()
        {
            m_Type = Type.GetType("UnityEditor.Build.BuildPipelineInterfaces, UnityEditor");
            InitializeCallbacks();
        }

        /// <summary>
        /// Public dispose function when instance is not in a using statement and manual dispose is required
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the build interfaces wrapper instance.
        /// </summary>
        /// <param name="disposing">Obsolete parameter.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            CleanupCallbacks();
            m_Disposed = true;
        }

        /// <summary>
        /// Initializes Unity Editor IPreprocessShaders, IPreprocessComputeShaders, IProcessScene, &amp; IProcessSceneWithReport build callbacks.
        /// </summary>
        public void InitializeCallbacks()
        {
            var init = m_Type.GetMethod("InitializeBuildCallbacks", BindingFlags.NonPublic | BindingFlags.Static);
#if UNITY_2020_2_OR_NEWER
            init.Invoke(null, new object[] { 274 }); // 274 = BuildCallbacks.SceneProcessors | BuildCallbacks.ShaderProcessors | BuildCallbacks.ComputeShader
#else
            init.Invoke(null, new object[] { 18 }); // 18 = BuildCallbacks.SceneProcessors | BuildCallbacks.ShaderProcessors
#endif

#if UNITY_2019_4_OR_NEWER
            GatherCallbackVersions();
#endif
        }

#if UNITY_2019_4_OR_NEWER
        internal void GatherCallbackVersions()
        {
            var versionedType = typeof(VersionedCallbackAttribute);
            var typeCollection = TypeCache.GetTypesWithAttribute(versionedType);
            List<Hash128> sceneInputs = new List<Hash128>();
            List<Hash128> shaderInputs = new List<Hash128>();
            foreach (var type in typeCollection)
            {
                var attribute = (VersionedCallbackAttribute)Attribute.GetCustomAttribute(type, versionedType);
#if UNITY_2020_2_OR_NEWER
                if (typeof(IPreprocessShaders).IsAssignableFrom(type) || typeof(IPreprocessComputeShaders).IsAssignableFrom(type))
#else
                if (typeof(IPreprocessShaders).IsAssignableFrom(type))
#endif
                {
                    shaderInputs.Add(HashingMethods.Calculate(type.AssemblyQualifiedName, attribute.version).ToHash128());
                }
#pragma warning disable CS0618 // Type or member is obsolete
                else if (typeof(IProcessScene).IsAssignableFrom(type) || typeof(IProcessSceneWithReport).IsAssignableFrom(type))
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    sceneInputs.Add(HashingMethods.Calculate(type.AssemblyQualifiedName, attribute.version).ToHash128());
                }
            }

            SceneCallbackVersionHash = new Hash128();
            if (sceneInputs.Count > 0)
            {
                sceneInputs.Sort();
                SceneCallbackVersionHash = HashingMethods.Calculate(sceneInputs).ToHash128();
            }

            ShaderCallbackVersionHash = new Hash128();
            if (shaderInputs.Count > 0)
            {
                shaderInputs.Sort();
                ShaderCallbackVersionHash = HashingMethods.Calculate(shaderInputs).ToHash128();
            }
        }
#endif

        /// <summary>
        /// Cleanup Unity Editor IPreprocessShaders, IProcessScene, &amp; IProcessSceneWithReport build callbacks.
        /// </summary>
        public void CleanupCallbacks()
        {
            var clean = m_Type.GetMethod("CleanupBuildCallbacks", BindingFlags.NonPublic | BindingFlags.Static);
            clean.Invoke(null, null);
        }
    }
}
