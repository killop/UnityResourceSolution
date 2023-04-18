#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System.Collections.Generic;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Collections;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Pkcs
{
    public abstract class Pkcs12Entry
    {
		private readonly IDictionary<DerObjectIdentifier, Asn1Encodable> m_attributes;

		protected internal Pkcs12Entry(IDictionary<DerObjectIdentifier, Asn1Encodable> attributes)
        {
            m_attributes = attributes;
        }

		public Asn1Encodable this[DerObjectIdentifier oid]
		{
			get { return CollectionUtilities.GetValueOrNull(m_attributes, oid); }
		}

		public IEnumerable<DerObjectIdentifier> BagAttributeKeys
		{
			get { return CollectionUtilities.Proxy(m_attributes.Keys); }
		}
    }
}
#pragma warning restore
#endif
