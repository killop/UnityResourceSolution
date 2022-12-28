namespace MHLab.Patch.Core.Octodiff
{
    internal struct DataRange
    {
        public DataRange(long startOffset, long length)
        {
            StartOffset = startOffset;
            Length = length;
        }

        public long StartOffset;
        public long Length;
    }
}