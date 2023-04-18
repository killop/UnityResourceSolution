#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security
{
    [Serializable]
    public class PasswordException
		: IOException
	{
		public PasswordException()
			: base()
		{
		}

		public PasswordException(string message)
			: base(message)
		{
		}

		public PasswordException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected PasswordException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
