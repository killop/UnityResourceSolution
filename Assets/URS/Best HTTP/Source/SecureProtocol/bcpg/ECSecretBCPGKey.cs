#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	/// <remarks>Base class for an EC Secret Key.</remarks>
    public class ECSecretBcpgKey
        : BcpgObject, IBcpgKey
    {
        internal readonly MPInteger m_x;

        public ECSecretBcpgKey(
            BcpgInputStream bcpgIn)
        {
            m_x = new MPInteger(bcpgIn);
        }

        public ECSecretBcpgKey(
            BigInteger x)
        {
            m_x = new MPInteger(x);
        }

		/// <summary>The format, as a string, always "PGP".</summary>
		public string Format
		{
			get { return "PGP"; }
		}

		/// <summary>Return the standard PGP encoding of the key.</summary>
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

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WriteObject(m_x);
        }

        public virtual BigInteger X
        {
            get { return m_x.Value; }
        }
    }
}
#pragma warning restore
#endif
