using System;

namespace MHLab.Patch.Core.Octodiff
{
    internal interface IRollingChecksum
    {
        string Name { get; }
        UInt32 Calculate(byte[] block, int offset, int count);
        UInt32 Rotate(UInt32 checksum, byte remove, byte add, int chunkSize);
    }
}