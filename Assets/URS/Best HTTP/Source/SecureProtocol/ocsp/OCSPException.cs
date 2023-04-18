#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Ocsp
{
    [Serializable]
    public class OcspException
		: Exception
	{
		public OcspException()
			: base()
		{
		}

		public OcspException(string message)
			: base(message)
		{
		}

		public OcspException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected OcspException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
