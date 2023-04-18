#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2

using BestHTTP.Core;
using BestHTTP.Extensions;
using BestHTTP.PlatformSupport.Memory;
using System;
using System.Collections.Generic;
using System.IO;

namespace BestHTTP.Connections.HTTP2
{
    public sealed class HTTP2Response : HTTPResponse
    {
        // For progress report
        public long ExpectedContentLength { get; private set; }
        public bool HasContentEncoding { get => !string.IsNullOrEmpty(this.contentEncoding); }

        private string contentEncoding = null;

        bool isPrepared;
        private Decompression.IDecompressor decompressor;

        public HTTP2Response(HTTPRequest request, bool isFromCache)
            : base(request, isFromCache)
        {
            this.VersionMajor = 2;
            this.VersionMinor = 0;
        }

        internal void AddHeaders(List<KeyValuePair<string, string>> headers)
        {
            this.ExpectedContentLength = -1;
            Dictionary<string, List<string>> newHeaders = this.baseRequest.OnHeadersReceived != null ? new Dictionary<string, List<string>>() : null;

            for (int i = 0; i < headers.Count; ++i)
            {
                KeyValuePair<string, string> header = headers[i];

                if (header.Key.Equals(":status", StringComparison.Ordinal))
                {
                    base.StatusCode = int.Parse(header.Value);
                    base.Message = string.Empty;
                }
                else
                {
                    if (!this.HasContentEncoding && header.Key.Equals("content-encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        this.contentEncoding = header.Value;
                    }
                    else if (base.baseRequest.OnDownloadProgress != null && header.Key.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                    {
                        long contentLength;
                        if (long.TryParse(header.Value, out contentLength))
                            this.ExpectedContentLength = contentLength;
                        else
                            HTTPManager.Logger.Information("HTTP2Response", string.Format("AddHeaders - Can't parse Content-Length as an int: '{0}'", header.Value), this.baseRequest.Context, this.Context);
                    }

                    base.AddHeader(header.Key, header.Value);
                }

                if (newHeaders != null)
                {
                    List<string> values;
                    if (!newHeaders.TryGetValue(header.Key, out values))
                        newHeaders.Add(header.Key, values = new List<string>(1));

                    values.Add(header.Value);
                }
            }

            if (this.ExpectedContentLength == -1 && base.baseRequest.OnDownloadProgress != null)
                HTTPManager.Logger.Information("HTTP2Response", "AddHeaders - No Content-Length header found!", this.baseRequest.Context, this.Context);

            RequestEventHelper.EnqueueRequestEvent(new RequestEventInfo(this.baseRequest, newHeaders));
        }

        internal void AddData(Stream stream)
        {
            if (this.HasContentEncoding)
            {
                Stream decoderStream = Decompression.DecompressorFactory.GetDecoderStream(stream, this.contentEncoding);

                if (decoderStream == null)
                {
                    base.Data = new byte[stream.Length];
                    stream.Read(base.Data, 0, (int)stream.Length);
                }
                else
                {
                    using (var ms = new BufferPoolMemoryStream((int)stream.Length))
                    {
                        var buf = BufferPool.Get(HTTPResponse.MinReadBufferSize, true);
                        int byteCount = 0;

                        while ((byteCount = decoderStream.Read(buf, 0, buf.Length)) > 0)
                            ms.Write(buf, 0, byteCount);

                        BufferPool.Release(buf);

                        base.Data = ms.ToArray();
                    }

                    decoderStream.Dispose();
                }
            }
            else
            {
                base.Data = new byte[stream.Length];
                stream.Read(base.Data, 0, (int)stream.Length);
            }
        }

       
        internal void ProcessData(byte[] payload, int payloadLength)
        {
            if (!this.isPrepared)
            {
                this.isPrepared = true;
                base.BeginReceiveStreamFragments();
            }

            if (this.HasContentEncoding)
            {
                if (this.decompressor == null)
                    this.decompressor = Decompression.DecompressorFactory.GetDecompressor(this.contentEncoding, this.Context);
                var result = this.decompressor.Decompress(payload, 0, payloadLength, true, true);

                base.FeedStreamFragment(result.Data, 0, result.Length);
            }
            else
                base.FeedStreamFragment(payload, 0, payloadLength);
        }

        internal void FinishProcessData()
        {
            base.FlushRemainingFragmentBuffer();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (this.decompressor != null)
                {
                    this.decompressor.Dispose();
                    this.decompressor = null;
                }
            }
        }
    }
}

#endif
