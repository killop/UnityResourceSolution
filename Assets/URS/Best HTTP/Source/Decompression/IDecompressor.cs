using System;
using System.IO;

using BestHTTP.Logger;

namespace BestHTTP.Decompression
{
    public struct DecompressedData
    {
        public readonly byte[] Data;
        public readonly int Length;

        internal DecompressedData(byte[] data, int length)
        {
            this.Data = data;
            this.Length = length;
        }
    }

    public interface IDecompressor : IDisposable
    {
        DecompressedData Decompress(byte[] data, int offset, int count, bool forceDecompress = false, bool dataCanBeLarger = false);
    }

    public static class DecompressorFactory
    {
        public const int MinLengthToDecompress = 256;

        public static void SetupHeaders(HTTPRequest request)
        {
            if (!request.HasHeader("Accept-Encoding"))
            {
#if BESTHTTP_DISABLE_GZIP
                request.AddHeader("Accept-Encoding", "identity");
#elif NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER
                if (BrotliDecompressor.IsSupported())
                    request.AddHeader("Accept-Encoding", "br, gzip, identity");
                else
                    request.AddHeader("Accept-Encoding", "gzip, identity");
#else
                request.AddHeader("Accept-Encoding", "gzip, identity");
#endif
            }
        }

        public static IDecompressor GetDecompressor(string encoding, LoggingContext context)
        {
            if (encoding == null)
                return null;

            switch (encoding.ToLowerInvariant())
            {
                case "identity":
                case "utf-8":
                    break;

                case "gzip": return new Decompression.GZipDecompressor(MinLengthToDecompress);

#if NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER
                case "br":
                    if (Decompression.BrotliDecompressor.IsSupported())
                        return new Decompression.BrotliDecompressor(MinLengthToDecompress);
                    else
                        goto default;
#endif
                default:
                    HTTPManager.Logger.Warning("DecompressorFactory", "GetDecompressor - unsupported encoding: " + encoding, context);
                    break;
            }

            return null;
        }

        /// <summary>
        /// Returns with a properly set up GZip/Deflate/Brotli stream, or null if the encoding is null or compiled for WebGl.
        /// </summary>
        public static Stream GetDecoderStream(Stream streamToDecode, string encoding)
        {
            if (streamToDecode == null)
                throw new ArgumentNullException(nameof(streamToDecode));

            if (string.IsNullOrEmpty(encoding))
                return null;

            switch (encoding)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                case "gzip": return new Decompression.Zlib.GZipStream(streamToDecode, Decompression.Zlib.CompressionMode.Decompress);
                case "deflate": return new Decompression.Zlib.DeflateStream(streamToDecode, Decompression.Zlib.CompressionMode.Decompress);
#if NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER
                case "br": return new System.IO.Compression.BrotliStream(streamToDecode, System.IO.Compression.CompressionMode.Decompress, true);
#endif
#endif
                //identity, utf-8, etc. Or compiled for WebGl.
                default:
                    // Do not copy from one stream to an other, just return with the raw bytes
                    return null;
            }
        }
    }

}
