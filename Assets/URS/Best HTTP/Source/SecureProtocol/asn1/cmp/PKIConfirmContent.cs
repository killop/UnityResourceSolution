#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp
{
    /**
     *  PKIConfirmContent ::= NULL
     */
    public class PkiConfirmContent
		: Asn1Encodable
	{
		public static PkiConfirmContent GetInstance(object obj)
		{
			if (obj == null)
				return null;

			if (obj is PkiConfirmContent pkiConfirmContent)
				return pkiConfirmContent;

			if (obj is Asn1Null asn1Null)
				return new PkiConfirmContent(asn1Null);

            throw new ArgumentException("Invalid object: " + Org.BouncyCastle.Utilities.Platform.GetTypeName(obj), nameof(obj));
		}

        private readonly Asn1Null m_val;

        public PkiConfirmContent()
            : this(DerNull.Instance)
        {
        }

        private PkiConfirmContent(Asn1Null val)
        {
            m_val = val;
        }

		/**
		 * <pre>
		 * PkiConfirmContent ::= NULL
		 * </pre>
		 * @return a basic ASN.1 object representation.
		 */
		public override Asn1Object ToAsn1Object()
		{
			return m_val;
		}
	}
}
#pragma warning restore
#endif
