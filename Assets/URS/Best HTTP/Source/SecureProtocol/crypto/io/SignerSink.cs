#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.IO
{
    public sealed class SignerSink
		: BaseOutputStream
	{
		private readonly ISigner m_signer;

        public SignerSink(ISigner signer)
		{
            m_signer = signer;
		}

		public ISigner Signer => m_signer;

		public override void Write(byte[] buffer, int offset, int count)
		{
			Streams.ValidateBufferArguments(buffer, offset, count);

			if (count > 0)
			{
				m_signer.BlockUpdate(buffer, offset, count);
			}
		}

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
		public override void Write(ReadOnlySpan<byte> buffer)
		{
			if (!buffer.IsEmpty)
			{
				m_signer.BlockUpdate(buffer);
			}
		}
#endif

		public override void WriteByte(byte value)
		{
			m_signer.Update(value);
		}
	}
}
#pragma warning restore
#endif
