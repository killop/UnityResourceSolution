#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto
{
    public interface ISecretWithEncapsulation
        : IDisposable 
    {
        
        ///<summary>
        /// Return the secret associated with the encapsulation.
        /// </summary>
        /// <returns> the secret the encapsulation is for.</returns>
        byte[] GetSecret();

        /// <summary>
        /// Return the data that carries the secret in its encapsulated form.
        /// </summary>
        /// <returns> the encapsulation of the secret.</returns>
        byte[] GetEncapsulation();
    }
}
#pragma warning restore
#endif
