using System.Collections.Generic;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class ChunkSignatureChecksumComparer : IComparer<ChunkSignature>
    {
        public int Compare(ChunkSignature x, ChunkSignature y)
        {
            return x.RollingChecksum.CompareTo(y.RollingChecksum);
        }
    }
}