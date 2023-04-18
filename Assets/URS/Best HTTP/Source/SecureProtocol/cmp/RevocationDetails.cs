#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    public struct RevocationDetails
    {
        private readonly RevDetails m_revDetails;

        public RevocationDetails(RevDetails revDetails)
        {
            m_revDetails = revDetails;
        }

        public X509Name Subject => m_revDetails.CertDetails.Subject;

        public X509Name Issuer => m_revDetails.CertDetails.Issuer;

        public BigInteger SerialNumber => m_revDetails.CertDetails.SerialNumber.Value;

        public RevDetails ToASN1Structure() => m_revDetails;
    }
}
#pragma warning restore
#endif
