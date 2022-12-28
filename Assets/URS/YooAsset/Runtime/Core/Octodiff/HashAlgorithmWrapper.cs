using System.IO;
using System.Security.Cryptography;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class HashAlgorithmWrapper : IHashAlgorithm
    {
        private readonly HashAlgorithm algorithm;

        public HashAlgorithmWrapper(string name, HashAlgorithm algorithm)
        {
            Name = name;
            this.algorithm = algorithm;
        }

        public string Name { get; private set; }
        public int HashLength { get { return algorithm.HashSize / 8; } }

        public byte[] ComputeHash(Stream stream)
        {
            return algorithm.ComputeHash(stream);
        }

        public byte[] ComputeHash(byte[] buffer, int offset, int length)
        {
            return algorithm.ComputeHash(buffer, offset, length);
        }
    }
}