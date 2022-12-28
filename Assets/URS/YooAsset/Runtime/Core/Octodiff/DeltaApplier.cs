using System;
using System.IO;

namespace MHLab.Patch.Core.Octodiff
{
    internal sealed class DeltaApplier
    {
        public DeltaApplier()
        {
            SkipHashCheck = false;
        }

        public bool SkipHashCheck { get; set; }

        public void Apply(Stream basisFileStream, IDeltaReader delta, Stream outputStream)
        {
            delta.Apply(
                writeData: (data) => outputStream.Write(data, 0, data.Length),
                copy: (startPosition, length) =>
                {
                    basisFileStream.Seek(startPosition, SeekOrigin.Begin);

                    var buffer = new byte[4*1024*1024];
                    int read;
                    long soFar = 0;
                    while ((read = basisFileStream.Read(buffer, 0, (int)Math.Min(length - soFar, buffer.Length))) > 0)
                    {
                        soFar += read;
                        outputStream.Write(buffer, 0, read);
                    }
                });

            if (!SkipHashCheck)
            {
                outputStream.Seek(0, SeekOrigin.Begin);

                var sourceFileHash = delta.ExpectedHash;
                var algorithm = delta.HashAlgorithm;

                var actualHash = algorithm.ComputeHash(outputStream);

                if (!BinaryComparer.CompareArray(sourceFileHash, actualHash))
                    throw new UsageException("Verification of the patched file failed. The SHA1 hash of the patch result file, and the file that was used as input for the delta, do not match. This can happen if the basis file changed since the signatures were calculated.");
            }
        }
    }
}