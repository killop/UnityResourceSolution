#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto
{
    public interface IEncapsulatedSecretGenerator
    {
        /// <summary>
        /// Generate an exchange pair based on the recipient public key.
        /// </summary>
        /// <param name="recipientKey"></param>
        /// <returns> An SecretWithEncapsulation derived from the recipient public key.</returns>
        ISecretWithEncapsulation GenerateEncapsulated(AsymmetricKeyParameter recipientKey);
    }
}
#pragma warning restore
#endif
