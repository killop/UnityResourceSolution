#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
	public interface Asn1OctetStringParser
		: IAsn1Convertible
	{
        /// <summary>Return the content of the OCTET STRING as a <see cref="Stream"/>.</summary>
        /// <returns>A <see cref="Stream"/> represnting the OCTET STRING's content.</returns>
        Stream GetOctetStream();
	}
}
#pragma warning restore
#endif
