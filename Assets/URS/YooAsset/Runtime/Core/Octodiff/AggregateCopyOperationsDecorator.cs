using System.IO;

namespace MHLab.Patch.Core.Octodiff
{
    // This decorator turns any sequential copy operations into a single operation, reducing 
    // the size of the delta file.
    // For example:
    //   Copy: 0x0000 - 0x0400
    //   Copy: 0x0401 - 0x0800
    //   Copy: 0x0801 - 0x0C00
    // Gets turned into:
    //   Copy: 0x0000 - 0x0C00
    internal sealed class AggregateCopyOperationsDecorator : IDeltaWriter
    {
        private readonly IDeltaWriter decorated;
        private DataRange bufferedCopy;

        public AggregateCopyOperationsDecorator(IDeltaWriter decorated)
        {
            this.decorated = decorated;
        }

        public void WriteDataCommand(Stream source, long offset, long length)
        {
            FlushCurrentCopyCommand();
            decorated.WriteDataCommand(source, offset, length);
        }

        public void WriteMetadata(IHashAlgorithm hashAlgorithm, byte[] expectedNewFileHash)
        {
            decorated.WriteMetadata(hashAlgorithm, expectedNewFileHash);
        }

        public void WriteCopyCommand(DataRange chunk)
        {
            if (bufferedCopy.Length > 0 && bufferedCopy.StartOffset + bufferedCopy.Length == chunk.StartOffset)
            {
                bufferedCopy.Length += chunk.Length;
            }
            else
            {
                FlushCurrentCopyCommand();
                bufferedCopy = chunk;
            }
        }

        void FlushCurrentCopyCommand()
        {
            if (bufferedCopy.Length <= 0) return;

            decorated.WriteCopyCommand(bufferedCopy);
            bufferedCopy = new DataRange();
        }

        public void Finish()
        {
            FlushCurrentCopyCommand();
            decorated.Finish();
        }
    }
}