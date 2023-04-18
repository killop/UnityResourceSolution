#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security.Certificates
{
    [Serializable]
    public class CrlException
		: GeneralSecurityException
	{
		public CrlException()
			: base()
		{
		}

		public CrlException(string message)
			: base(message)
		{
		}

		public CrlException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected CrlException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
