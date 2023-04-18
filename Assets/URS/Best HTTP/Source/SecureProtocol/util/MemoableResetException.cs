#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities
{
	/**
     * Exception to be thrown on a failure to reset an object implementing Memoable.
     * <p>
     * The exception extends InvalidCastException to enable users to have a single handling case,
     * only introducing specific handling of this one if required.
     * </p>
     */
	[Serializable]
	public class MemoableResetException
        : InvalidCastException
    {
		public MemoableResetException()
			: base()
		{
		}

		public MemoableResetException(string message)
			: base(message)
		{
		}

		public MemoableResetException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected MemoableResetException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}

#pragma warning restore
#endif
