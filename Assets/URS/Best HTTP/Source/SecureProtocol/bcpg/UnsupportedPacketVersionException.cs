#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    [Serializable]
    public class UnsupportedPacketVersionException
        : Exception
    {
		public UnsupportedPacketVersionException()
			: base()
		{
		}

		public UnsupportedPacketVersionException(string message)
			: base(message)
		{
		}

		public UnsupportedPacketVersionException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected UnsupportedPacketVersionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
    }
}
#pragma warning restore
#endif
