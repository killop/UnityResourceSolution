#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System.IO;

#if NET6_0_OR_GREATER
using System.IO.Compression;
#else
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Zlib;
#endif

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO.Compression
{
    internal static class Zip
    {
        internal static Stream CompressOutput(Stream stream, int zlibCompressionLevel, bool leaveOpen = false)
        {
#if NET6_0_OR_GREATER
            return new DeflateStream(stream, ZLib.GetCompressionLevel(zlibCompressionLevel), leaveOpen);
#else
            return leaveOpen
                ?   new ZOutputStreamLeaveOpen(stream, zlibCompressionLevel, true)
                :   new ZOutputStream(stream, zlibCompressionLevel, true);
#endif
        }

        internal static Stream DecompressInput(Stream stream)
        {
#if NET6_0_OR_GREATER
            return new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: false);
#else
            return new ZInputStream(stream, true);
#endif
        }
    }
}
#pragma warning restore
#endif
