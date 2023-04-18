#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Operators
{
    public class DefaultSignatureResult
        : IBlockResult
    {
        private readonly ISigner mSigner;

        public DefaultSignatureResult(ISigner signer)
        {
            this.mSigner = signer;
        }

        public byte[] Collect()
        {
            return mSigner.GenerateSignature();
        }

        public int Collect(byte[] sig, int sigOff)
        {
            byte[] signature = Collect();
            signature.CopyTo(sig, sigOff);
            return signature.Length;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        public int Collect(Span<byte> destination)
        {
            byte[] result = Collect();
            result.CopyTo(destination);
            return result.Length;
        }
#endif
    }
}
#pragma warning restore
#endif
