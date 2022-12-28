using System.IO;
using System.Linq;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Append a hash to each bundle name.
    /// </summary>
    public class AppendBundleHash : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBundleBuildParameters m_Parameters;

        [InjectContext]
        IBundleBuildResults m_Results;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            if (!m_Parameters.AppendHash)
                return ReturnCode.SuccessNotRun;

            string[] bundles = m_Results.BundleInfos.Keys.ToArray();
            foreach (string bundle in bundles)
            {
                var details = m_Results.BundleInfos[bundle];
                var oldFileName = details.FileName;
                var newFileName = string.Format("{0}_{1}", details.FileName, details.Hash.ToString());
                details.FileName = newFileName;
                m_Results.BundleInfos[bundle] = details;

                File.Delete(newFileName);
                File.Move(oldFileName, newFileName);
            }

            return ReturnCode.Success;
        }
    }
}
