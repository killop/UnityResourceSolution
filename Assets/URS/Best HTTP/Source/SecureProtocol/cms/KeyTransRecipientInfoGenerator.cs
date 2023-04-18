#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cms;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
using BestHTTP.SecureProtocol.Org.BouncyCastle.X509;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cms
{
    public class KeyTransRecipientInfoGenerator
        : RecipientInfoGenerator
    {
        private readonly IKeyWrapper m_keyWrapper;

        private IssuerAndSerialNumber m_issuerAndSerialNumber;
        private Asn1OctetString m_subjectKeyIdentifier;

        public KeyTransRecipientInfoGenerator(X509Certificate recipCert, IKeyWrapper keyWrapper)
            : this(new IssuerAndSerialNumber(recipCert.IssuerDN, new DerInteger(recipCert.SerialNumber)), keyWrapper)
        {
        }

        public KeyTransRecipientInfoGenerator(IssuerAndSerialNumber issuerAndSerial, IKeyWrapper keyWrapper)
        {
            m_issuerAndSerialNumber = issuerAndSerial;
            m_keyWrapper = keyWrapper;
        }

        public KeyTransRecipientInfoGenerator(byte[] subjectKeyID, IKeyWrapper keyWrapper)
        {
            m_subjectKeyIdentifier = new DerOctetString(subjectKeyID);
            m_keyWrapper = keyWrapper;
        }

        public RecipientInfo Generate(KeyParameter contentEncryptionKey, SecureRandom random)
        {
            AlgorithmIdentifier keyEncryptionAlgorithm = AlgorithmDetails;

            byte[] encryptedKeyBytes = GenerateWrappedKey(contentEncryptionKey);

            RecipientIdentifier recipId;
            if (m_issuerAndSerialNumber != null)
            {
                recipId = new RecipientIdentifier(m_issuerAndSerialNumber);
            }
            else
            {
                recipId = new RecipientIdentifier(m_subjectKeyIdentifier);
            }

            return new RecipientInfo(new KeyTransRecipientInfo(recipId, keyEncryptionAlgorithm,
                new DerOctetString(encryptedKeyBytes)));
        }

        protected virtual AlgorithmIdentifier AlgorithmDetails
        {
            get { return (AlgorithmIdentifier)m_keyWrapper.AlgorithmDetails; }
        }

        protected virtual byte[] GenerateWrappedKey(KeyParameter contentEncryptionKey)
        {
            return m_keyWrapper.Wrap(contentEncryptionKey.GetKey()).Collect();
        }
    }
}
#pragma warning restore
#endif
