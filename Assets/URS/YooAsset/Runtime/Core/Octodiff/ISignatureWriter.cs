using System.IO;

namespace MHLab.Patch.Core.Octodiff
{
    internal interface ISignatureWriter
    {
        void WriteMetadata(IHashAlgorithm hashAlgorithm, IRollingChecksum rollingChecksumAlgorithm, byte[] hash);
        void WriteChunk(ChunkSignature signature);
    }

    internal sealed class SignatureWriter : ISignatureWriter
    {
        private readonly BinaryWriter signatureStream;

        public SignatureWriter(Stream signatureStream)
        {
            this.signatureStream = new BinaryWriter(signatureStream);
        }

        public void WriteMetadata(IHashAlgorithm hashAlgorithm, IRollingChecksum rollingChecksumAlgorithm, byte[] hash)
        {
            signatureStream.Write(BinaryFormat.SignatureHeader);
            signatureStream.Write(BinaryFormat.Version);
            signatureStream.Write(hashAlgorithm.Name);
            signatureStream.Write(rollingChecksumAlgorithm.Name);
            signatureStream.Write(BinaryFormat.EndOfMetadata);
        }

        public void WriteChunk(ChunkSignature signature)
        {
            signatureStream.Write(signature.Length);
            signatureStream.Write(signature.RollingChecksum);
            signatureStream.Write(signature.Hash);
        }
    }
}