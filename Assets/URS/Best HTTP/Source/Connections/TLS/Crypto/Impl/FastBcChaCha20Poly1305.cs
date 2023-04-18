#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Macs;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls;
using BestHTTP.PlatformSupport.Memory;

namespace BestHTTP.Connections.TLS.Crypto.Impl
{
    public sealed class FastBcChaCha20Poly1305
        : TlsAeadCipherImpl
    {
        private static readonly byte[] Zeroes = new byte[15];

        private readonly FastChaCha7539Engine m_cipher = new FastChaCha7539Engine();
        private readonly FastPoly1305 m_mac = new FastPoly1305();

        private readonly bool m_isEncrypting;

        private int m_additionalDataLength;

        public FastBcChaCha20Poly1305(bool isEncrypting)
        {
            this.m_isEncrypting = isEncrypting;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        unsafe
#endif
        public int DoFinal(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset)
        {
            if (m_isEncrypting)
            {
                int ciphertextLength = inputLength;

                m_cipher.DoFinal(input, inputOffset, inputLength, output, outputOffset);
                int outputLength = inputLength;

                if (ciphertextLength != outputLength)
                    throw new InvalidOperationException();

                UpdateMac(output, outputOffset, ciphertextLength);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
                Span<byte> lengths = stackalloc byte[16];

                Pack.UInt64_To_LE((ulong)m_additionalDataLength, lengths);
                Pack.UInt64_To_LE((ulong)ciphertextLength, lengths[8..]);

                m_mac.BlockUpdate(lengths);
                m_mac.DoFinal(output.AsSpan(outputOffset + ciphertextLength));
#else
                byte[] lengths = BufferPool.Get(16, true);
                using (var _ = new PooledBuffer(lengths))
                {
                    Pack.UInt64_To_LE((ulong)m_additionalDataLength, lengths, 0);
                    Pack.UInt64_To_LE((ulong)ciphertextLength, lengths, 8);

                    m_mac.BlockUpdate(lengths, 0, 16);
                    m_mac.DoFinal(output, outputOffset + ciphertextLength);
                }
#endif

                return ciphertextLength + 16;
            }
            else
            {
                int ciphertextLength = inputLength - 16;

                UpdateMac(input, inputOffset, ciphertextLength);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
                Span<byte> expectedMac = stackalloc byte[16];

                Pack.UInt64_To_LE((ulong)m_additionalDataLength, expectedMac);
                Pack.UInt64_To_LE((ulong)ciphertextLength, expectedMac[8..]);

                m_mac.BlockUpdate(expectedMac);
                m_mac.DoFinal(expectedMac);

                bool badMac = !TlsUtilities.ConstantTimeAreEqual(16, expectedMac, 0, input, inputOffset + ciphertextLength);

                if (badMac)
                    throw new TlsFatalAlert(AlertDescription.bad_record_mac);
#else
                byte[] expectedMac = BufferPool.Get(16, true);
                using (var _ = new PooledBuffer(expectedMac))
                {
                    Pack.UInt64_To_LE((ulong)m_additionalDataLength, expectedMac, 0);
                    Pack.UInt64_To_LE((ulong)ciphertextLength, expectedMac, 8);

                    m_mac.BlockUpdate(expectedMac, 0, 16);
                    m_mac.DoFinal(expectedMac, 0);

                    bool badMac = !TlsUtilities.ConstantTimeAreEqual(16, expectedMac, 0, input, inputOffset + ciphertextLength);

                    if (badMac)
                        throw new TlsFatalAlert(AlertDescription.bad_record_mac);
                }
#endif

                m_cipher.DoFinal(input, inputOffset, ciphertextLength, output, outputOffset);
                int outputLength = ciphertextLength;

                if (ciphertextLength != outputLength)
                    throw new InvalidOperationException();

                return ciphertextLength;
            }
        }

        public int GetOutputSize(int inputLength)
        {
            return m_isEncrypting ? inputLength + 16 : inputLength - 16;
        }

        public void Init(byte[] nonce, int macSize, byte[] additionalData)
        {
            if (nonce == null || nonce.Length != 12 || macSize != 16)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            m_cipher.Init(m_isEncrypting, new ParametersWithIV(null, nonce));
            InitMac();
            if (additionalData == null)
            {
                this.m_additionalDataLength = 0;
            }
            else
            {
                this.m_additionalDataLength = additionalData.Length;
                UpdateMac(additionalData, 0, additionalData.Length);
            }
        }

        public void Reset()
        {
            m_cipher.Reset();
            m_mac.Reset();
        }

        public void SetKey(byte[] key, int keyOff, int keyLen)
        {
            KeyParameter cipherKey = new KeyParameter(key, keyOff, keyLen);
            m_cipher.Init(m_isEncrypting, new ParametersWithIV(cipherKey, Zeroes, 0, 12));
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        public void SetKey(ReadOnlySpan<byte> key)
        {
            KeyParameter cipherKey = new KeyParameter(key);
            m_cipher.Init(m_isEncrypting, new ParametersWithIV(cipherKey, Zeroes[..12]));
        }
#endif

        byte[] firstBlock = new byte[64];
        private void InitMac()
        {
            m_cipher.ProcessBytes(firstBlock, 0, 64, firstBlock, 0);
            m_mac.Init(new KeyParameter(firstBlock, 0, 32));
            Array.Clear(firstBlock, 0, firstBlock.Length);
        }

        private void UpdateMac(byte[] buf, int off, int len)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
            m_mac.BlockUpdate(buf.AsSpan(off, len));
#else
            m_mac.BlockUpdate(buf, off, len);
#endif

            int partial = len % 16;
            if (partial != 0)
            {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
                m_mac.BlockUpdate(Zeroes.AsSpan(0, 16 - partial));
#else
                m_mac.BlockUpdate(Zeroes, 0, 16 - partial);
#endif
            }
        }
    }
}
#pragma warning restore
#endif
