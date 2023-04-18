#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security.Certificates
{
    [Serializable]
    public class CertificateExpiredException
		: CertificateException
	{
		public CertificateExpiredException()
			: base()
		{
		}

		public CertificateExpiredException(string message)
			: base(message)
		{
		}

		public CertificateExpiredException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected CertificateExpiredException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
