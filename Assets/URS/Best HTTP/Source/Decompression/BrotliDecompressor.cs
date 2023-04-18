using System;

using BestHTTP.Extensions;
using BestHTTP.PlatformSupport.Memory;

namespace BestHTTP.Decompression
{
    public sealed class BrotliDecompressor : IDecompressor
    {
#if (NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER) && (!(ENABLE_MONO && UNITY_ANDROID) || (!UNITY_WEBGL || UNITY_EDITOR))
        private BufferPoolMemoryStream decompressorInputStream;
        private BufferPoolMemoryStream decompressorOutputStream;
        private System.IO.Compression.BrotliStream decompressorStream;
#endif

        private int MinLengthToDecompress = 256;

        public static bool IsSupported()
        {
            // Not enabled under android with the mono runtime
#if (NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER) && (!(ENABLE_MONO && UNITY_ANDROID) || (!UNITY_WEBGL || UNITY_EDITOR))
            return true;
#else
            return false;
#endif
        }

        public BrotliDecompressor(int minLengthToDecompress)
        {
            this.MinLengthToDecompress = minLengthToDecompress;
        }

        public DecompressedData Decompress(byte[] data, int offset, int count, bool forceDecompress = false, bool dataCanBeLarger = false)
        {
#if (NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER) && (!(ENABLE_MONO && UNITY_ANDROID) || (!UNITY_WEBGL || UNITY_EDITOR))
            if (decompressorInputStream == null)
                decompressorInputStream = new BufferPoolMemoryStream(count);

            if (data != null)
                decompressorInputStream.Write(data, offset, count);

            if (!forceDecompress && decompressorInputStream.Length < MinLengthToDecompress)
                return new DecompressedData(null, 0);

            decompressorInputStream.Position = 0;

            if (decompressorStream == null)
            {
                decompressorStream = new System.IO.Compression.BrotliStream(decompressorInputStream,
                                                             System.IO.Compression.CompressionMode.Decompress,
                                                             true);
            }

            if (decompressorOutputStream == null)
                decompressorOutputStream = new BufferPoolMemoryStream();
            decompressorOutputStream.SetLength(0);

            byte[] copyBuffer = BufferPool.Get(1024, true);

            int readCount;
            int sumReadCount = 0;
            while ((readCount = decompressorStream.Read(copyBuffer, 0, copyBuffer.Length)) != 0)
            {
                decompressorOutputStream.Write(copyBuffer, 0, readCount);
                sumReadCount += readCount;
            }

            BufferPool.Release(copyBuffer);

            // If no read is done (returned with any data) don't zero out the input stream, as it would delete any not yet used data.
            if (sumReadCount > 0)
                decompressorStream.SetLength(0);

            byte[] result = decompressorOutputStream.ToArray(dataCanBeLarger);

            return new DecompressedData(result, dataCanBeLarger ? (int)decompressorOutputStream.Length : result.Length);
#else
            return default(DecompressedData);
#endif
        }

        public void Dispose()
        {
#if (NET_STANDARD_2_1 || UNITY_2021_2_OR_NEWER) && (!(ENABLE_MONO && UNITY_ANDROID) || (!UNITY_WEBGL || UNITY_EDITOR))
            this.decompressorStream?.Dispose();
            this.decompressorStream = null;
#endif
        }
    }
}
