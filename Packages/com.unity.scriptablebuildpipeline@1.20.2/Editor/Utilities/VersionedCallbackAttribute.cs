#if UNITY_2019_4_OR_NEWER
using System;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Attribute provides the version details for IProcessScene, IProcessSceneWithReport, IPreprocessShaders, and IPreprocessComputeShaders callbacks.
    /// Increment the version number when the callback changes and the build result needs to change.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class VersionedCallbackAttribute : Attribute
    {
        public readonly float version;

        /// <summary>
        /// Attribute provides the version details for IProcessScene, IProcessSceneWithReport, IPreprocessShaders, and IPreprocessComputeShaders callbacks.
        /// Increment the version number when the callback changes and the build result needs to change.
        /// </summary>
        /// <param name="version">The version of this callback.</param>
        public VersionedCallbackAttribute(float version)
        {
            this.version = version;
        }
    }
}
#endif