#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
    internal class DerOutputStream
        : Asn1OutputStream
    {
        internal DerOutputStream(Stream os)
            : base(os)
        {
        }

        internal override int Encoding
        {
            get { return EncodingDer; }
        }
    }
}
#pragma warning restore
#endif
