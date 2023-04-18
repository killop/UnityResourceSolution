#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security.Certificates
{
    [Serializable]
    public class CertificateParsingException
		: CertificateException
	{
		public CertificateParsingException()
			: base()
		{
		}

		public CertificateParsingException(string message)
			: base(message)
		{
		}

		public CertificateParsingException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected CertificateParsingException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
