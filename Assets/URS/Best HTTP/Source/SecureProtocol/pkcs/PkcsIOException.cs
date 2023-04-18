#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Pkcs
{
	/// <summary>Base exception for parsing related issues in the PKCS namespace.</summary>
	[Serializable]
	public class PkcsIOException
		: IOException
    {
		public PkcsIOException()
			: base()
		{
		}

		public PkcsIOException(string message)
			: base(message)
		{
		}

		public PkcsIOException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected PkcsIOException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
