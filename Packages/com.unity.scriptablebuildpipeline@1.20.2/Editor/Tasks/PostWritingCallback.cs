using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Processes all callbacks after the writing task.
    /// </summary>
    public class PostWritingCallback : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext]
        IBuildParameters m_Parameters;

        [InjectContext]
        IDependencyData m_DependencyData;

        [InjectContext]
        IWriteData m_WriteData;

        [InjectContext]
        IBuildResults m_Results;

        [InjectContext(ContextUsage.In)]
        IWritingCallback m_Callback;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            return m_Callback.PostWriting(m_Parameters, m_DependencyData, m_WriteData, m_Results);
        }
    }
}
