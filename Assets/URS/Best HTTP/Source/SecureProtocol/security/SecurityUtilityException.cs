#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Security
{
    [Serializable]
    public class SecurityUtilityException
		: Exception
    {
		public SecurityUtilityException()
			: base()
		{
		}

		public SecurityUtilityException(string message)
			: base(message)
		{
		}

		public SecurityUtilityException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected SecurityUtilityException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
