#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes
{
    public class EcbBlockCipher
        : IBlockCipherMode
    {
        internal static IBlockCipherMode GetBlockCipherMode(IBlockCipher blockCipher)
        {
            if (blockCipher is IBlockCipherMode blockCipherMode)
                return blockCipherMode;

            return new EcbBlockCipher(blockCipher);
        }

        private readonly IBlockCipher m_cipher;

        public EcbBlockCipher(IBlockCipher cipher)
        {
            if (cipher == null)
                throw new ArgumentNullException(nameof(cipher));

            m_cipher = cipher;
        }

        public bool IsPartialBlockOkay => false;

        public string AlgorithmName => m_cipher.AlgorithmName + "/ECB";

        public int GetBlockSize()
        {
            return m_cipher.GetBlockSize();
        }

        public IBlockCipher UnderlyingCipher => m_cipher;

        public void Init(bool forEncryption, ICipherParameters parameters)
        {
            m_cipher.Init(forEncryption, parameters);
        }

        public int ProcessBlock(byte[] inBuf, int inOff, byte[] outBuf, int outOff)
        {
            return m_cipher.ProcessBlock(inBuf, inOff, outBuf, outOff);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        public int ProcessBlock(ReadOnlySpan<byte> input, Span<byte> output)
        {
            return m_cipher.ProcessBlock(input, output);
        }
#endif

        public void Reset()
        {
        }
    }
}
#pragma warning restore
#endif
