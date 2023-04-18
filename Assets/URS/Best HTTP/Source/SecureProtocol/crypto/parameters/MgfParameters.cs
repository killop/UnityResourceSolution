#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters
{
	/// <remarks>Parameters for mask derivation functions.</remarks>
    public sealed class MgfParameters
		: IDerivationParameters
    {
        private readonly byte[] m_seed;

		public MgfParameters(byte[] seed)
			: this(seed, 0, seed.Length)
        {
        }

		public MgfParameters(byte[] seed, int off, int len)
        {
            m_seed = Arrays.CopyOfRange(seed, off, len);
        }

        public byte[] GetSeed()
        {
            return (byte[])m_seed.Clone();
        }

        public void GetSeed(byte[] buffer, int offset)
        {
            m_seed.CopyTo(buffer, offset);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        public void GetSeed(Span<byte> output)
        {
            m_seed.CopyTo(output);
        }
#endif

        public int SeedLength => m_seed.Length;
    }
}
#pragma warning restore
#endif
