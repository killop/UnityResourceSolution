#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Pkcs
{
	/// <summary>Base exception for PKCS related issues.</summary>
	[Serializable]
	public class PkcsException
        : Exception
    {
		public PkcsException()
			: base()
		{
		}

		public PkcsException(string message)
			: base(message)
		{
		}

		public PkcsException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected PkcsException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
