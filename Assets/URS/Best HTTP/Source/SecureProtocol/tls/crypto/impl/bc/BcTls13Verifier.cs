#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.IO;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    internal sealed class BcTls13Verifier
        : Tls13Verifier
    {
        private readonly SignerSink m_output;

        internal BcTls13Verifier(ISigner verifier)
        {
            if (verifier == null)
                throw new ArgumentNullException("verifier");

            this.m_output = new SignerSink(verifier);
        }

        public Stream Stream
        {
            get { return m_output; }
        }

        public bool VerifySignature(byte[] signature)
        {
            return m_output.Signer.VerifySignature(signature);
        }
    }
}
#pragma warning restore
#endif
