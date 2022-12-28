using System;

namespace MHLab.Patch.Core.Octodiff
{
    internal interface IDeltaReader
    {
        byte[] ExpectedHash { get; }
        IHashAlgorithm HashAlgorithm { get; }
        void Apply(
            Action<byte[]> writeData,
            Action<long, long> copy
            );
    }
}