#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters
{
    public class ParametersWithIV
        : ICipherParameters
    {
        private readonly ICipherParameters m_parameters;
        private readonly byte[] m_iv;

        public ParametersWithIV(ICipherParameters parameters, byte[] iv)
            : this(parameters, iv, 0, iv.Length)
        {
        }

        public ParametersWithIV(ICipherParameters parameters, byte[] iv, int ivOff, int ivLen)
        {
            m_parameters = parameters;
            m_iv = Arrays.CopyOfRange(iv, ivOff, ivOff + ivLen);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        public ParametersWithIV(ICipherParameters parameters, ReadOnlySpan<byte> iv)
        {
            m_parameters = parameters;
            m_iv = iv.ToArray();
        }
#endif

        public byte[] GetIV()
        {
            return (byte[])m_iv.Clone();
        }

        public ICipherParameters Parameters => m_parameters;
    }
}
#pragma warning restore
#endif
