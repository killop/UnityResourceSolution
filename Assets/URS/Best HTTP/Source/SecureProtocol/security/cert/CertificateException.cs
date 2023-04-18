#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security.Certificates
{
    [Serializable]
    public class CertificateException
		: GeneralSecurityException
	{
		public CertificateException()
			: base()
		{
		}

		public CertificateException(string message)
			: base(message)
		{
		}

		public CertificateException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected CertificateException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
