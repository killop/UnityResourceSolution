#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
    internal interface IAsn1Encoding
    {
        void Encode(Asn1OutputStream asn1Out);

        int GetLength();
    }
}
#pragma warning restore
#endif
