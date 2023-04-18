#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
	[Serializable]
	public class TlsException
        : IOException
    {
		public TlsException()
			: base()
		{
		}

		public TlsException(string message)
			: base(message)
		{
		}

		public TlsException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected TlsException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
