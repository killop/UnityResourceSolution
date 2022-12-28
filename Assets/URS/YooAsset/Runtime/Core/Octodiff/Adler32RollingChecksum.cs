using System;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class Adler32RollingChecksum : IRollingChecksum
    {
        //private ushort a = 1;
        //private ushort b;

        public string Name { get { return "Adler32"; } }

        public UInt32 Calculate(byte[] block, int offset, int count)
        {
            var a = 1;
            var b = 0;
            for (var i = offset; i < offset + count; i++)
            {
                var z = block[i];
                a = (ushort)(z + a);
                b = (ushort)(b + a);
            }
            return (UInt32) ((b << 16) | a);
        }

        public UInt32 Rotate(UInt32 checksum, byte remove, byte add, int chunkSize)
        {
            var b = (ushort)(checksum >> 16 & 0xffff);
            var a = (ushort)(checksum & 0xffff);

            a = (ushort)((a - remove + add));
            b = (ushort)((b - (chunkSize * remove) + a - 1));

            return (UInt32) ((b << 16) | a);
        }
    }
}