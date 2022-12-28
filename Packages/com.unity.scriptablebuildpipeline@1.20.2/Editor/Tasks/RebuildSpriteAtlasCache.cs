using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Sprites;
using UnityEditor.U2D;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Builds the cache data for all sprite atlases.
    /// </summary>
    public class RebuildSpriteAtlasCache : IBuildTask
    {
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;
#pragma warning restore 649

        /// <inheritdoc />
        public ReturnCode Run()
        {
            // TODO: Need a return value if this ever can fail
#if !UNITY_2020_1_OR_NEWER
            Packer.RebuildAtlasCacheIfNeeded(m_Parameters.Target, true, Packer.Execution.Normal);
#endif
            SpriteAtlasUtility.PackAllAtlases(m_Parameters.Target);
            return ReturnCode.Success;
        }
    }
}
