#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl;

namespace BestHTTP.Connections.TLS.Crypto.Impl
{
    internal sealed class FastTlsBlockCipherImpl
        : TlsBlockCipherImpl
    {
        private readonly bool m_isEncrypting;
        private readonly IBlockCipher m_cipher;

        private KeyParameter key;

        internal FastTlsBlockCipherImpl(IBlockCipher cipher, bool isEncrypting)
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

        public void Init(byte[] iv, int ivOff, int ivLen)
        {
            m_cipher.Init(m_isEncrypting, new ParametersWithIV(key, iv, ivOff, ivLen));
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        public void Init(ReadOnlySpan<byte> iv)
        {
            m_cipher.Init(m_isEncrypting, new ParametersWithIV(key, iv));
        }
#endif

        public int DoFinal(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset)
        {
            int blockSize = m_cipher.GetBlockSize();

            for (int i = 0; i < inputLength; i += blockSize)
            {
                m_cipher.ProcessBlock(input, inputOffset + i, output, outputOffset + i);
            }

            return inputLength;
        }

        public int GetBlockSize()
        {
            return m_cipher.GetBlockSize();
        }
    }
}
#pragma warning restore
#endif
