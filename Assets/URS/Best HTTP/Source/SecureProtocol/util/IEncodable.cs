#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System.IO;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities
{
    public interface IEncodable
    {
        /// <summary>Return a byte array representing the implementing object.</summary>
        /// <returns>An encoding of this object as a byte array.</returns>
        /// <exception cref="IOException"/>
        byte[] GetEncoded();
    }
}
#pragma warning restore
#endif
