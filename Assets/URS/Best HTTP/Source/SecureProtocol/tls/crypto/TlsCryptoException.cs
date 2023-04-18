#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
	/// <summary>Basic exception class for crypto services to pass back a cause.</summary>
	[Serializable]
	public class TlsCryptoException
        : TlsException
    {
		public TlsCryptoException()
			: base()
		{
		}

		public TlsCryptoException(string message)
			: base(message)
		{
		}

		public TlsCryptoException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected TlsCryptoException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
