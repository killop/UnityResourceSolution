#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    public sealed class EdSecretBcpgKey
        : BcpgObject, IBcpgKey
    {
        internal readonly MPInteger m_x;

        public EdSecretBcpgKey(BcpgInputStream bcpgIn)
        {
            m_x = new MPInteger(bcpgIn);
        }

        public EdSecretBcpgKey(BigInteger x)
        {
            m_x = new MPInteger(x);
        }

        public string Format => "PGP";

        public override byte[] GetEncoded()
        {
            try
            {
                return base.GetEncoded();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override void Encode(BcpgOutputStream bcpgOut)
        {
            bcpgOut.WriteObject(m_x);
        }

        public BigInteger X => m_x.Value;
    }
}
#pragma warning restore
#endif
