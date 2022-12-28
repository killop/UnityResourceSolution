using System.IO;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class SignatureReader : ISignatureReader
    {
        private readonly IProgressReporter reporter;
        private readonly BinaryReader reader;

        public SignatureReader(string signatureFileName, IProgressReporter reporter)
        {
            this.reporter = reporter;
            this.reader = new BinaryReader(new FileStream(signatureFileName, FileMode.Open, FileAccess.Read));
        }

        public Signature ReadSignature()
        {
            Progress();
            var header = reader.ReadBytes(BinaryFormat.SignatureHeader.Length);
            if (!BinaryComparer.CompareArray(BinaryFormat.SignatureHeader, header)) 
                throw new CorruptFileFormatException("The signature file appears to be corrupt.");

            var version = reader.ReadByte();
            if (version != BinaryFormat.Version)
                throw new CorruptFileFormatException("The signature file uses a newer file format than this program can handle.");

            var hashAlgorithm = reader.ReadString();
            var rollingChecksumAlgorithm = reader.ReadString();

            var endOfMeta = reader.ReadBytes(BinaryFormat.EndOfMetadata.Length);
            if (!BinaryComparer.CompareArray(BinaryFormat.EndOfMetadata, endOfMeta)) 
                throw new CorruptFileFormatException("The signature file appears to be corrupt.");

            Progress();

            var hashAlgo = SupportedAlgorithms.Hashing.Create(hashAlgorithm);
            var signature = new Signature(
                hashAlgo,
                SupportedAlgorithms.Checksum.Create(rollingChecksumAlgorithm));

            var expectedHashLength = hashAlgo.HashLength;
            long start = 0;

            var fileLength = reader.BaseStream.Length;
            var remainingBytes = fileLength - reader.BaseStream.Position;
            var signatureSize = sizeof (ushort) + sizeof (uint) + expectedHashLength;
            if (remainingBytes % signatureSize != 0)
                throw new CorruptFileFormatException("The signature file appears to be corrupt; at least one chunk has data missing.");

            while (reader.BaseStream.Position < fileLength - 1)
            {
                var length = reader.ReadInt16();
                var checksum = reader.ReadUInt32();
                var chunkHash = reader.ReadBytes(expectedHashLength);

                signature.Chunks.Add(new ChunkSignature
                {
                    StartOffset = start,
                    Length = length,
                    RollingChecksum = checksum,
                    Hash = chunkHash
                });

                start += length;

                Progress();
            }

            return signature;
        }

        void Progress()
        {
            reporter.ReportProgress("Reading signature", reader.BaseStream.Position, reader.BaseStream.Length);
        }
    }
}