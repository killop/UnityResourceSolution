using System.IO;

namespace MHLab.Patch.Core.Octodiff
{
    internal interface IHashAlgorithm
    {
        string Name { get; }
        int HashLength { get; }
        byte[] ComputeHash(Stream stream);
        byte[] ComputeHash(byte[] buffer, int offset, int length);
    }
}