#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System.IO;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO.Compression
{
    using Impl = Utilities.Bzip2;

    internal static class Bzip2
    {
        internal static Stream CompressOutput(Stream stream, bool leaveOpen = false)
        {
            return leaveOpen
                ?   new Impl.CBZip2OutputStreamLeaveOpen(stream)
                :   new Impl.CBZip2OutputStream(stream);
        }

        internal static Stream DecompressInput(Stream stream)
        {
            return new Impl.CBZip2InputStream(stream);
        }
    }
}
#pragma warning restore
#endif
