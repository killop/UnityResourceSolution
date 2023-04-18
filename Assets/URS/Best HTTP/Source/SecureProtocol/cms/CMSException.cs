#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cms
{
    [Serializable]
    public class CmsException
		: Exception
	{
		public CmsException()
			: base()
		{
		}

		public CmsException(string message)
			: base(message)
		{
		}

		public CmsException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected CmsException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
