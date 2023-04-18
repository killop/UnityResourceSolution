#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    [Serializable]
    public class CmpException
        : Exception
    {
        public CmpException()
            : base()
        {
        }

        public CmpException(string message)
            : base(message)
        {
        }

        public CmpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CmpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
#pragma warning restore
#endif
