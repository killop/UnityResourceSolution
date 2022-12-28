using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Sets the target build platform based on the build parameters.
    /// </summary>
    public class SwitchToBuildPlatform : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In, true)]
        IEditorBuildCallbacks m_InterfaceWrapper;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            if (EditorUserBuildSettings.SwitchActiveBuildTarget(m_Parameters.Group, m_Parameters.Target))
            {
                if (m_InterfaceWrapper != null)
                    m_InterfaceWrapper.InitializeCallbacks();
                return ReturnCode.Success;
            }
            return ReturnCode.Error;
        }
    }
}
