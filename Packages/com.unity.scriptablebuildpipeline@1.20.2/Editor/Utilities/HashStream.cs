using System.IO;
using System.Security.Cryptography;

namespace UnityEditor.Build.Pipeline.Utilities
{
    internal class HashStream : Stream
    {
        HashAlgorithm m_Algorithm;

        public HashStream(HashAlgorithm algorithm)
        {
            m_Algorithm = algorithm;
        }

        public RawHash GetHash()
        {
            m_Algorithm.TransformFinalBlock(System.Array.Empty<byte>(), 0, 0);
            return new RawHash(m_Algorithm.Hash);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => Null.Length;

        public override long Position { get => Null.Position; set => Null.Position = value; }

        public override void Flush() {}

        public override int Read(byte[] buffer, int offset, int count) => Null.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => Null.Seek(offset, origin);

        public override void SetLength(long value) => Null.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_Algorithm.TransformBlock(buffer, offset, count, null, 0);
        }
    }
}
