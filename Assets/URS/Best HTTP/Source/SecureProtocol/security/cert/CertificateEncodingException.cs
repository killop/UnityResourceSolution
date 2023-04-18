#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security.Certificates
{
    [Serializable]
    public class CertificateEncodingException
		: CertificateException
	{
		public CertificateEncodingException()
			: base()
		{
		}

		public CertificateEncodingException(string message)
			: base(message)
		{
		}

		public CertificateEncodingException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected CertificateEncodingException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
