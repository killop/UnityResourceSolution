using System;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class ChunkSignature
    {
        public long StartOffset;            // 8 (but not included in the file on disk)
        public short Length;                // 2
        public byte[] Hash;                 // 20
        public UInt32 RollingChecksum;      // 4
                                            // 26 bytes on disk
                                            // 34 bytes in memory

        public override string ToString()
        {
            return string.Format("{0,6}:{1,6} |{2,20}| {3}", StartOffset, Length, RollingChecksum, BitConverter.ToString(Hash).ToLowerInvariant().Replace("-", ""));
        }
    }
}