#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.Serialization;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cms
{
    [Serializable]
    public class CmsAttributeTableGenerationException
		: CmsException
	{
        public CmsAttributeTableGenerationException()
            : base()
        {
        }

        public CmsAttributeTableGenerationException(string message)
            : base(message)
        {
        }

        public CmsAttributeTableGenerationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CmsAttributeTableGenerationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
	}
}
#pragma warning restore
#endif
