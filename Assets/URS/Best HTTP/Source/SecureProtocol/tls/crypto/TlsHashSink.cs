#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    public class TlsHashSink
        : BaseOutputStream
    {
        private readonly TlsHash m_hash;

        public TlsHashSink(TlsHash hash)
        {
            this.m_hash = hash;
        }

        public virtual TlsHash Hash
        {
            get { return m_hash; }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Streams.ValidateBufferArguments(buffer, offset, count);

            if (count > 0)
            {
                m_hash.Update(buffer, offset, count);
            }
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!buffer.IsEmpty)
            {
                m_hash.Update(buffer);
            }
        }
#endif

        public override void WriteByte(byte value)
        {
            m_hash.Update(new byte[]{ value }, 0, 1);
        }
    }
}
#pragma warning restore
#endif
