using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;
using System.IO;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Compiles all player scripts.
    /// </summary>
    public class BuildPlayerScripts : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext]
        IBuildParameters m_Parameters;

        [InjectContext]
        IBuildResults m_Results;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            if (m_Parameters.ScriptInfo != null)
            {
                BuildCacheUtility.SetTypeDB(m_Parameters.ScriptInfo);
                return ReturnCode.SuccessNotRun;
            }

            // We need to ensure the directory is empty so prior results or other artifacts in this directory do not influence the build result
            if (Directory.Exists(m_Parameters.ScriptOutputFolder))
            {
                Directory.Delete(m_Parameters.ScriptOutputFolder, true);
                Directory.CreateDirectory(m_Parameters.ScriptOutputFolder);
            }

            m_Results.ScriptResults = PlayerBuildInterface.CompilePlayerScripts(m_Parameters.GetScriptCompilationSettings(), m_Parameters.ScriptOutputFolder);
            m_Parameters.ScriptInfo = m_Results.ScriptResults.typeDB;
            BuildCacheUtility.SetTypeDB(m_Parameters.ScriptInfo);

            if (m_Results.ScriptResults.assemblies.IsNullOrEmpty() && m_Results.ScriptResults.typeDB == null)
                return ReturnCode.Error;
            return ReturnCode.Success;
        }
    }
}
