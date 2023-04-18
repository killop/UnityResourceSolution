#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    internal sealed class BcTlsAeadCipherImpl
        : TlsAeadCipherImpl
    {
        private readonly bool m_isEncrypting;
        private readonly IAeadCipher m_cipher;

        private KeyParameter key;

        internal BcTlsAeadCipherImpl(IAeadCipher cipher, bool isEncrypting)
        {
            this.m_cipher = cipher;
            this.m_isEncrypting = isEncrypting;
        }

        public void SetKey(byte[] key, int keyOff, int keyLen)
        {
            this.key = new KeyParameter(key, keyOff, keyLen);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        public void SetKey(ReadOnlySpan<byte> key)
        {
            this.key = new KeyParameter(key);
        }
#endif

        public void Init(byte[] nonce, int macSize, byte[] additionalData)
        {
            m_cipher.Init(m_isEncrypting, new AeadParameters(key, macSize * 8, nonce, additionalData));
        }

        public int GetOutputSize(int inputLength)
        {
            return m_cipher.GetOutputSize(inputLength);
        }

        public int DoFinal(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset)
        {
            int len = m_cipher.ProcessBytes(input, inputOffset, inputLength, output, outputOffset);

            try
            {
                len += m_cipher.DoFinal(output, outputOffset + len);
            }
            catch (InvalidCipherTextException e)
            {
                throw new TlsFatalAlert(AlertDescription.bad_record_mac, e);
            }

            return len;
        }

        public void Reset()
        {
            m_cipher.Reset();
        }
    }
}
#pragma warning restore
#endif
