#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp
{
	public class CertConfirmContent
		: Asn1Encodable
	{
		public static CertConfirmContent GetInstance(object obj)
		{
			if (obj is CertConfirmContent content)
				return content;

			if (obj is Asn1Sequence seq)
				return new CertConfirmContent(seq);

            throw new ArgumentException("Invalid object: " + Org.BouncyCastle.Utilities.Platform.GetTypeName(obj), nameof(obj));
		}

        private readonly Asn1Sequence m_content;

        private CertConfirmContent(Asn1Sequence seq)
        {
            m_content = seq;
        }

        public virtual CertStatus[] ToCertStatusArray()
		{
			return m_content.MapElements(CertStatus.GetInstance);
		}

		/**
		 * <pre>
		 * CertConfirmContent ::= SEQUENCE OF CertStatus
		 * </pre>
		 * @return a basic ASN.1 object representation.
		 */
		public override Asn1Object ToAsn1Object()
		{
			return m_content;
		}
	}
}
#pragma warning restore
#endif
