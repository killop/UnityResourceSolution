#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security.Certificates
{
    [Serializable]
    public class CertificateNotYetValidException
		: CertificateException
	{
		public CertificateNotYetValidException()
			: base()
		{
		}

		public CertificateNotYetValidException(string message)
			: base(message)
		{
		}

		public CertificateNotYetValidException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected CertificateNotYetValidException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
