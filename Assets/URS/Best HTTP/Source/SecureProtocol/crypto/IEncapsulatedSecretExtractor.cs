#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto
{
    public interface IEncapsulatedSecretExtractor
    {
        /// <summary>
        /// Generate an exchange pair based on the recipient public key.
        /// </summary>
        /// <param name="encapsulation"> the encapsulated secret.</param>
        byte[] ExtractSecret(byte[] encapsulation);

        /// <summary>
        /// The length in bytes of the encapsulation.
        /// </summary>
        int EncapsulationLength { get;  }
    }
}
#pragma warning restore
#endif
