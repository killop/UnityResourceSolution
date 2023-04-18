#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig
{
    /**
    * packet giving signature creation time.
    */
    public class IssuerKeyId
        : SignatureSubpacket
    {
        protected static byte[] KeyIdToBytes(long keyId)
        {
            return Pack.UInt64_To_BE((ulong)keyId);
        }

        public IssuerKeyId(
            bool    critical,
            bool    isLongLength,
            byte[]  data)
            : base(SignatureSubpacketTag.IssuerKeyId, critical, isLongLength, data)
        {
        }

        public IssuerKeyId(
            bool    critical,
            long    keyId)
            : base(SignatureSubpacketTag.IssuerKeyId, critical, false, KeyIdToBytes(keyId))
        {
        }

        public long KeyId => (long)Pack.BE_To_UInt64(data);
    }
}
#pragma warning restore
#endif
