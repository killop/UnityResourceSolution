#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters
{
    /**
     * parameters for Key derivation functions for IEEE P1363a
     */
    public class KdfParameters
        : IDerivationParameters
    {
        private readonly byte[] m_iv;
        private readonly byte[] m_shared;

        public KdfParameters(byte[] shared, byte[] iv)
        {
            m_shared = shared;
            m_iv = iv;
        }

        public byte[] GetSharedSecret()
        {
            return m_shared;
        }

        public byte[] GetIV()
        {
            return m_iv;
        }
    }
}
#pragma warning restore
#endif
