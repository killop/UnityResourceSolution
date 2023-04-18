#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto
{
    [Serializable]
    public class OutputLengthException
        : DataLengthException
    {
		public OutputLengthException()
			: base()
		{
		}

		public OutputLengthException(string message)
			: base(message)
		{
		}

		public OutputLengthException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected OutputLengthException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
#pragma warning restore
#endif
