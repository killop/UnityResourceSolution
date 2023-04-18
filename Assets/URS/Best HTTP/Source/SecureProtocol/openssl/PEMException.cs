#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.OpenSsl
{
    [Serializable]
    public class PemException
		: IOException
	{
		public PemException()
			: base()
		{
		}

		public PemException(string message)
			: base(message)
		{
		}

		public PemException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected PemException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
