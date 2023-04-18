#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crmf
{
	[Serializable]
	public class CrmfException
        : Exception
    {
		public CrmfException()
			: base()
		{
		}

		public CrmfException(string message)
			: base(message)
		{
		}

		public CrmfException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected CrmfException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
