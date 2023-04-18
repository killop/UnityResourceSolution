#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security
{
    [Serializable]
    public class EncryptionException
		: IOException
	{
		public EncryptionException()
			: base()
		{
		}

		public EncryptionException(string message)
			: base(message)
		{
		}

		public EncryptionException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected EncryptionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
