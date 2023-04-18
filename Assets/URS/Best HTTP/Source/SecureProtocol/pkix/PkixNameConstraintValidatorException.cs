#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Pkix
{
    [Serializable]
    public class PkixNameConstraintValidatorException
        : Exception
    {
		public PkixNameConstraintValidatorException()
			: base()
		{
		}

		public PkixNameConstraintValidatorException(string message)
			: base(message)
		{
		}

		public PkixNameConstraintValidatorException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected PkixNameConstraintValidatorException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
    }
}
#pragma warning restore
#endif
