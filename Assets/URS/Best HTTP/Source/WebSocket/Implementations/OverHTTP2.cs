#if (!UNITY_WEBGL || UNITY_EDITOR) && !BESTHTTP_DISABLE_ALTERNATE_SSL && !BESTHTTP_DISABLE_HTTP2 && !BESTHTTP_DISABLE_WEBSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BestHTTP.Connections.HTTP2;
using BestHTTP.Extensions;
using BestHTTP.Logger;
using BestHTTP.PlatformSupport.Memory;
using BestHTTP.WebSocket.Frames;

namespace BestHTTP.WebSocket
{
    /// <summary>
    /// Implements RFC 8441 (https://tools.ietf.org/html/rfc8441) to use Websocket over HTTP/2
    /// </summary>
    public sealed class OverHTTP2 : WebSocketBaseImplementation, IHeartbeat
    {
        public override int BufferedAmount { get => this._bufferedAmount; }
        internal volatile int _bufferedAmount;

        public override bool IsOpen => this.State == WebSocketStates.Open;

        public override int Latency { get { return this.Parent.StartPingThread ? base.Latency : (int)this.http2Handler.Latency; } }

        private List<WebSocketFrameReader> IncompleteFrames = new List<WebSocketFrameReader>();
        private HTTP2Handler http2Handler;

        /// <summary>
        /// True if we sent out a Close message to the server
        /// </summary>
        internal volatile bool closeSent;

        /// <summary>
        /// When we sent out the last ping.
        /// </summary>
        private DateTime lastPing = DateTime.MinValue;

        private bool waitingForPong = false;

        /// <summary>
        /// A circular buffer to store the last N rtt times calculated by the pong messages.
        /// </summary>
        private CircularBuffer<int> rtts = new CircularBuffer<int>(WebSocketResponse.RTTBufferCapacity);

        private PeekableIncomingSegmentStream incomingSegmentStream = new PeekableIncomingSegmentStream();
        private ConcurrentQueue<WebSocketFrameReader> CompletedFrames = new ConcurrentQueue<WebSocketFrameReader>();
        internal ConcurrentQueue<WebSocketFrame> frames = new ConcurrentQueue<WebSocketFrame>();

        public OverHTTP2(WebSocket parent, Uri uri, string origin, string protocol) : base(parent, uri, origin, protocol)
        {
            // use https scheme so it will be served over HTTP/2. Thre request's Tag will be set to this class' instance so HTTP2Handler will know it has to create a HTTP2WebSocketStream instance to
            // process the request.
            string scheme = "https";
            int port = uri.Port != -1 ? uri.Port : 443;

            base.Uri = new Uri(scheme + "://" + uri.Host + ":" + port + uri.GetRequestPathAndQueryURL());
        }

        internal void SetHTTP2Handler(HTTP2Handler handler) => this.http2Handler = handler;

        protected override void CreateInternalRequest()
        {
            HTTPManager.Logger.Verbose("OverHTTP2", "CreateInternalRequest", this.Parent.Context);

            base._internalRequest = new HTTPRequest(base.Uri, HTTPMethods.Connect, OnInternalRequestCallback);
            base._internalRequest.Context.Add("WebSocket", this.Parent.Context);

            base._internalRequest.SetHeader(":protocol", "websocket");

            // The request MUST include a header field with the name |Sec-WebSocket-Key|.  The value of this header field MUST be a nonce consisting of a
            // randomly selected 16-byte value that has been base64-encoded (see Section 4 of [RFC4648]).  The nonce MUST be selected randomly for each connection.
            base._internalRequest.SetHeader("sec-webSocket-key", WebSocket.GetSecKey(new object[] { this, InternalRequest, base.Uri, new object() }));

            // The request MUST include a header field with the name |Origin| [RFC6454] if the request is coming from a browser client.
            // If the connection is from a non-browser client, the request MAY include this header field if the semantics of that client match the use-case described here for browser clients.
            // More on Origin Considerations: http://tools.ietf.org/html/rfc6455#section-10.2
            if (!string.IsNullOrEmpty(base.Origin))
                base._internalRequest.SetHeader("origin", base.Origin);

            // The request MUST include a header field with the name |Sec-WebSocket-Version|.  The value of this header field MUST be 13.
            base._internalRequest.SetHeader("sec-webSocket-version", "13");

            if (!string.IsNullOrEmpty(base.Protocol))
                base._internalRequest.SetHeader("sec-webSocket-protocol", base.Protocol);

            // Disable caching
            base._internalRequest.SetHeader("cache-control", "no-cache");
            base._internalRequest.SetHeader("pragma", "no-cache");

#if !BESTHTTP_DISABLE_CACHING
            base._internalRequest.DisableCache = true;
#endif


            base._internalRequest.OnHeadersReceived += OnHeadersReceived;

            // set a fake upload stream, so HPACKEncoder will not set the END_STREAM flag
            base._internalRequest.UploadStream = new MemoryStream(0);
            base._internalRequest.UseUploadStreamLength = false;

            this.LastMessageReceived = DateTime.Now;
            base._internalRequest.Tag = this;

            if (this.Parent.OnInternalRequestCreated != null)
            {
                try
                {
                    this.Parent.OnInternalRequestCreated(this.Parent, base._internalRequest);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception("OverHTTP2", "CreateInternalRequest", ex, this.Parent.Context);
                }
            }
        }

        private void OnHeadersReceived(HTTPRequest req, HTTPResponse resp, Dictionary<string, List<string>> newHeaders)
        {
            HTTPManager.Logger.Verbose("OverHTTP2", $"OnHeadersReceived - StatusCode: {resp?.StatusCode}", this.Parent.Context);

            if (resp != null && resp.StatusCode == 200)
            {
                if (this.Parent.Extensions != null)
                {
                    for (int i = 0; i < this.Parent.Extensions.Length; ++i)
                    {
                        var ext = this.Parent.Extensions[i];

                        try
                        {
                            if (ext != null && !ext.ParseNegotiation(resp))
                                this.Parent.Extensions[i] = null; // Keep extensions only that successfully negotiated
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("OverHTTP2", "ParseNegotiation", ex, this.Parent.Context);

                            // Do not try to use a defective extension in the future
                            this.Parent.Extensions[i] = null;
                        }
                    }
                }

                this.State = WebSocketStates.Open;

                if (this.Parent.OnOpen != null)
                {
                    try
                    {
                        this.Parent.OnOpen(this.Parent);
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("OverHTTP2", "OnOpen", ex, this.Parent.Context);
                    }
                }

                if (this.Parent.StartPingThread)
                {
                    this.LastMessageReceived = DateTime.Now;
                    SendPing();
                }
            }
            else
                req.Abort();
        }

        private static bool CanReadFullFrame(PeekableIncomingSegmentStream stream)
        {
            if (stream.Length < 2)
                return false;

            stream.BeginPeek();

            if (stream.PeekByte() == -1)
                return false;

            int maskAndLength = stream.PeekByte();
            if (maskAndLength == -1)
                return false;

            // The second byte is the Mask Bit and the length of the payload data
            var HasMask = (maskAndLength & 0x80) != 0;

            // if 0-125, that is the payload length.
            var Length = (UInt64)(maskAndLength & 127);

            // If 126, the following 2 bytes interpreted as a 16-bit unsigned integer are the payload length.
            if (Length == 126)
            {
                byte[] rawLen = BufferPool.Get(2, true);

                for (int i = 0; i < 2; i++)
                {
                    int data = stream.PeekByte();
                    if (data < 0)
                        return false;

                    rawLen[i] = (byte)data;
                }

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(rawLen, 0, 2);

                Length = (UInt64)BitConverter.ToUInt16(rawLen, 0);

                BufferPool.Release(rawLen);
            }
            else if (Length == 127)
            {
                // If 127, the following 8 bytes interpreted as a 64-bit unsigned integer (the
                // most significant bit MUST be 0) are the payload length.

                byte[] rawLen = BufferPool.Get(8, true);

                for (int i = 0; i < 8; i++)
                {
                    int data = stream.PeekByte();
                    if (data < 0)
                        return false;

                    rawLen[i] = (byte)data;
                }

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(rawLen, 0, 8);

                Length = (UInt64)BitConverter.ToUInt64(rawLen, 0);

                BufferPool.Release(rawLen);
            }

            // Header + Mask&Length
            Length += 2;

            // 4 bytes for Mask if present
            if (HasMask)
                Length += 4;

            return stream.Length >= (long)Length;
        }

        internal void OnReadThread(BufferSegment buffer)
        {
            this.LastMessageReceived = DateTime.Now;

            this.incomingSegmentStream.Write(buffer);

            while (CanReadFullFrame(this.incomingSegmentStream))
            {
                WebSocketFrameReader frame = new WebSocketFrameReader();
                frame.Read(this.incomingSegmentStream);

                if (HTTPManager.Logger.Level == Logger.Loglevels.All)
                    HTTPManager.Logger.Verbose("OverHTTP2", "Frame received: " + frame.ToString(), this.Parent.Context);

                if (!frame.IsFinal)
                {
                    if (this.Parent.OnIncompleteFrame == null)
                        IncompleteFrames.Add(frame);
                    else
                        CompletedFrames.Enqueue(frame);
                    continue;
                }

                switch (frame.Type)
                {
                    // For a complete documentation and rules on fragmentation see http://tools.ietf.org/html/rfc6455#section-5.4
                    // A fragmented Frame's last fragment's opcode is 0 (Continuation) and the FIN bit is set to 1.
                    case WebSocketFrameTypes.Continuation:
                        // Do an assemble pass only if OnFragment is not set. Otherwise put it in the CompletedFrames, we will handle it in the HandleEvent phase.
                        if (this.Parent.OnIncompleteFrame == null)
                        {
                            frame.Assemble(IncompleteFrames);

                            // Remove all incomplete frames
                            IncompleteFrames.Clear();

                            // Control frames themselves MUST NOT be fragmented. So, its a normal text or binary frame. Go, handle it as usual.
                            goto case WebSocketFrameTypes.Binary;
                        }
                        else
                        {
                            CompletedFrames.Enqueue(frame);
                        }
                        break;

                    case WebSocketFrameTypes.Text:
                    case WebSocketFrameTypes.Binary:
                        frame.DecodeWithExtensions(this.Parent);
                        CompletedFrames.Enqueue(frame);
                        break;

                    // Upon receipt of a Ping frame, an endpoint MUST send a Pong frame in response, unless it already received a Close frame.
                    case WebSocketFrameTypes.Ping:
                        if (!closeSent && this.State != WebSocketStates.Closed)
                        {
                            // copy data set to true here, as the frame's data is released back to the pool after the switch
                            Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.Pong, frame.Data, true, true, true));
                        }
                        break;

                    case WebSocketFrameTypes.Pong:
                        // https://tools.ietf.org/html/rfc6455#section-5.5
                        // A Pong frame MAY be sent unsolicited.  This serves as a
                        // unidirectional heartbeat.  A response to an unsolicited Pong frame is
                        // not expected. 
                        if (!waitingForPong)
                            break;

                        waitingForPong = false;
                        // the difference between the current time and the time when the ping message is sent
                        TimeSpan diff = DateTime.Now - lastPing;

                        // add it to the buffer
                        this.rtts.Add((int)diff.TotalMilliseconds);

                        // and calculate the new latency
                        base.Latency = CalculateLatency();
                        break;

                    // If an endpoint receives a Close frame and did not previously send a Close frame, the endpoint MUST send a Close frame in response.
                    case WebSocketFrameTypes.ConnectionClose:
                        HTTPManager.Logger.Information("OverHTTP2", "ConnectionClose packet received!", this.Parent.Context);

                        CompletedFrames.Enqueue(frame);

                        if (!closeSent)
                            Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.ConnectionClose, BufferSegment.Empty));
                        
                        this.State = WebSocketStates.Closed;
                        break;
                }
            }
        }

        private void OnInternalRequestCallback(HTTPRequest req, HTTPResponse resp)
        {
            HTTPManager.Logger.Verbose("OverHTTP2", $"OnInternalRequestCallback - this.State: {this.State}", this.Parent.Context);

            // If it's already closed, all events are called too.
            if (this.State == WebSocketStates.Closed)
                return;

            if (this.State == WebSocketStates.Connecting && HTTPManager.HTTP2Settings.WebSocketOverHTTP2Settings.EnableImplementationFallback)
            {
                this.Parent.FallbackToHTTP1();
                return;
            }

            string reason = string.Empty;

            switch (req.State)
            {
                case HTTPRequestStates.Finished:
                    HTTPManager.Logger.Information("OverHTTP2", string.Format("Request finished. Status Code: {0} Message: {1}", resp.StatusCode.ToString(), resp.Message), this.Parent.Context);

                    if (resp.StatusCode == 101)
                    {
                        // The request upgraded successfully.
                        return;
                    }
                    else
                        reason = string.Format("Request Finished Successfully, but the server sent an error. Status Code: {0}-{1} Message: {2}",
                                                        resp.StatusCode,
                                                        resp.Message,
                                                        resp.DataAsText);
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
                    reason = "Request Finished with Error! " + (req.Exception != null ? ("Exception: " + req.Exception.Message + req.Exception.StackTrace) : string.Empty);
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    reason = "Request Aborted!";
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    reason = "Connection Timed Out!";
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    reason = "Processing the request Timed Out!";
                    break;

                default:
                    return;
            }

            if (this.State != WebSocketStates.Connecting || !string.IsNullOrEmpty(reason))
            {
                if (this.Parent.OnError != null)
                {
                    try
                    {
                        this.Parent.OnError(this.Parent, reason);
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception("OverHTTP2", "OnError", ex, this.Parent.Context);
                    }
                }
                else if (!HTTPManager.IsQuitting)
                    HTTPManager.Logger.Error("OverHTTP2", reason, this.Parent.Context);
            }
            else if (this.Parent.OnClosed != null)
            {
                try
                {
                    this.Parent.OnClosed(this.Parent, (ushort)WebSocketStausCodes.NormalClosure, "Closed while opening");
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception("OverHTTP2", "OnClosed", ex, this.Parent.Context);
                }
            }

            this.State = WebSocketStates.Closed;
        }

        public override void StartOpen()
        {
            HTTPManager.Logger.Verbose("OverHTTP2", "StartOpen", this.Parent.Context);

            if (this.Parent.Extensions != null)
            {
                try
                {
                    for (int i = 0; i < this.Parent.Extensions.Length; ++i)
                    {
                        var ext = this.Parent.Extensions[i];
                        if (ext != null)
                            ext.AddNegotiation(base.InternalRequest);
                    }
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception("OverHTTP2", "Open", ex, this.Parent.Context);
                }
            }

            base.InternalRequest.Send();
            HTTPManager.Heartbeats.Subscribe(this);

            this.State = WebSocketStates.Connecting;
        }

        public override void StartClose(ushort code, string message)
        {
            HTTPManager.Logger.Verbose("OverHTTP2", "StartClose", this.Parent.Context);

            if (this.State == WebSocketStates.Connecting)
            {
                if (this.InternalRequest != null)
                    this.InternalRequest.Abort();

                this.State = WebSocketStates.Closed;
                if (this.Parent.OnClosed != null)
                    this.Parent.OnClosed(this.Parent, code, message);
            }
            else
            {
                Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.ConnectionClose, WebSocket.EncodeCloseData(code, message), true, false, false));
                this.State = WebSocketStates.Closing;
            }
        }

        public override void Send(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message must not be null!");

            int count = System.Text.Encoding.UTF8.GetByteCount(message);
            byte[] data = BufferPool.Get(count, true);
            System.Text.Encoding.UTF8.GetBytes(message, 0, message.Length, data, 0);

            SendAsText(data.AsBuffer(0, count));
        }

        public override void Send(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("data must not be null!");

            Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.Binary, new BufferSegment(buffer, 0, buffer.Length)));
        }

        public override void Send(byte[] data, ulong offset, ulong count)
        {
            if (data == null)
                throw new ArgumentNullException("data must not be null!");
            if (offset + count > (ulong)data.Length)
                throw new ArgumentOutOfRangeException("offset + count >= data.Length");

            Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.Binary, new BufferSegment(data, (int)offset, (int)count), true, true));
        }

        public override void Send(WebSocketFrame frame)
        {
            if (this.State == WebSocketStates.Closed || closeSent)
                return;

            this.frames.Enqueue(frame);
            this.http2Handler.SignalRunnerThread();
            this._bufferedAmount += frame.Data.Count;

            if (frame.Type == WebSocketFrameTypes.ConnectionClose)
                this.closeSent = true;
        }

        public override void SendAsBinary(BufferSegment data)
        {
            Send(WebSocketFrameTypes.Binary, data);
        }

        public override void SendAsText(BufferSegment data)
        {
            Send(WebSocketFrameTypes.Text, data);
        }

        private void Send(WebSocketFrameTypes type, BufferSegment data)
        {
            Send(new WebSocketFrame(this.Parent, type, data, true, true, false));
        }

        private int CalculateLatency()
        {
            if (this.rtts.Count == 0)
                return 0;

            int sumLatency = 0;
            for (int i = 0; i < this.rtts.Count; ++i)
                sumLatency += this.rtts[i];

            return sumLatency / this.rtts.Count;
        }

        internal void PreReadCallback()
        {
            if (this.Parent.StartPingThread)
            {
                DateTime now = DateTime.Now;

                if (!waitingForPong && now - LastMessageReceived >= TimeSpan.FromMilliseconds(this.Parent.PingFrequency))
                    SendPing();

                if (waitingForPong && now - lastPing > this.Parent.CloseAfterNoMessage)
                {
                    if (this.State != WebSocketStates.Closed)
                    {
                        HTTPManager.Logger.Warning("OverHTTP2",
                            string.Format("No message received in the given time! Closing WebSocket. LastPing: {0}, PingFrequency: {1}, Close After: {2}, Now: {3}",
                            this.lastPing, TimeSpan.FromMilliseconds(this.Parent.PingFrequency), this.Parent.CloseAfterNoMessage, now), this.Parent.Context);

                        CloseWithError("No message received in the given time!");
                    }
                }
            }
        }

        public void OnHeartbeatUpdate(TimeSpan dif)
        {
            DateTime now = DateTime.Now;

            switch (this.State)
            {
                case WebSocketStates.Connecting:
                    if (now - this.InternalRequest.Timing.Start >= this.Parent.CloseAfterNoMessage)
                    {
                        if (HTTPManager.HTTP2Settings.WebSocketOverHTTP2Settings.EnableImplementationFallback)
                        {
                            this.State = WebSocketStates.Closed;
                            this.InternalRequest.OnHeadersReceived = null;
                            this.InternalRequest.Callback = null;
                            this.Parent.FallbackToHTTP1();
                        }
                        else
                        {
                            CloseWithError("WebSocket Over HTTP/2 Implementation failed to connect in the given time!");
                        }
                    }
                    break;

                default:
                    while (CompletedFrames.TryDequeue(out var frame))
                    {
                        // Bugs in the clients shouldn't interrupt the code, so we need to try-catch and ignore any exception occurring here
                        try
                        {
                            switch (frame.Type)
                            {
                                case WebSocketFrameTypes.Continuation:
                                    if (HTTPManager.Logger.Level == Loglevels.All)
                                        HTTPManager.Logger.Verbose("OverHTTP2", "HandleEvents - OnIncompleteFrame", this.Parent.Context);
                                    if (this.Parent.OnIncompleteFrame != null)
                                        this.Parent.OnIncompleteFrame(this.Parent, frame);
                                    break;

                                case WebSocketFrameTypes.Text:
                                    // Any not Final frame is handled as a fragment
                                    if (!frame.IsFinal)
                                        goto case WebSocketFrameTypes.Continuation;

                                    if (HTTPManager.Logger.Level == Loglevels.All)
                                        HTTPManager.Logger.Verbose("OverHTTP2", $"HandleEvents - OnText(\"{frame.DataAsText}\")", this.Parent.Context);

                                    if (this.Parent.OnMessage != null)
                                        this.Parent.OnMessage(this.Parent, frame.DataAsText);
                                    break;

                                case WebSocketFrameTypes.Binary:
                                    // Any not Final frame is handled as a fragment
                                    if (!frame.IsFinal)
                                        goto case WebSocketFrameTypes.Continuation;

                                    if (HTTPManager.Logger.Level == Loglevels.All)
                                        HTTPManager.Logger.Verbose("OverHTTP2", $"HandleEvents - OnBinary({frame.Data})", this.Parent.Context);

                                    if (this.Parent.OnBinary != null)
                                    {
                                        var data = new byte[frame.Data.Count];
                                        Array.Copy(frame.Data.Data, frame.Data.Offset, data, 0, frame.Data.Count);
                                        this.Parent.OnBinary(this.Parent, data);
                                    }

                                    if (this.Parent.OnBinaryNoAlloc != null)
                                        this.Parent.OnBinaryNoAlloc(this.Parent, frame.Data);
                                    break;

                                case WebSocketFrameTypes.ConnectionClose:
                                    HTTPManager.Logger.Verbose("OverHTTP2", "HandleEvents - Calling OnClosed", this.Parent.Context);
                                    if (this.Parent.OnClosed != null)
                                    {
                                        try
                                        {
                                            UInt16 statusCode = 0;
                                            string msg = string.Empty;

                                            // If we received any data, we will get the status code and the message from it
                                            if (/*CloseFrame != null && */ frame.Data != BufferSegment.Empty && frame.Data.Count >= 2)
                                            {
                                                if (BitConverter.IsLittleEndian)
                                                    Array.Reverse(frame.Data.Data, frame.Data.Offset, 2);
                                                statusCode = BitConverter.ToUInt16(frame.Data.Data, frame.Data.Offset);

                                                if (frame.Data.Count > 2)
                                                    msg = Encoding.UTF8.GetString(frame.Data.Data, frame.Data.Offset + 2, frame.Data.Count - 2);

                                                frame.ReleaseData();
                                            }

                                            this.Parent.OnClosed(this.Parent, statusCode, msg);
                                            this.Parent.OnClosed = null;
                                        }
                                        catch (Exception ex)
                                        {
                                            HTTPManager.Logger.Exception("OverHTTP2", "HandleEvents - OnClosed", ex, this.Parent.Context);
                                        }
                                    }

                                    HTTPManager.Heartbeats.Unsubscribe(this);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            HTTPManager.Logger.Exception("OverHTTP2", string.Format("HandleEvents({0})", frame.ToString()), ex, this.Parent.Context);
                        }
                        finally
                        {
                            frame.ReleaseData();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Next interaction relative to *now*.
        /// </summary>
        public TimeSpan GetNextInteraction()
        {
            if (waitingForPong)
                return TimeSpan.MaxValue;

            return (LastMessageReceived + TimeSpan.FromMilliseconds(this.Parent.PingFrequency)) - DateTime.Now;
        }

        private void SendPing()
        {
            HTTPManager.Logger.Information("OverHTTP2", "Sending Ping frame, waiting for a pong...", this.Parent.Context);

            lastPing = DateTime.Now;
            waitingForPong = true;

            Send(new WebSocketFrame(this.Parent, WebSocketFrameTypes.Ping, BufferSegment.Empty));
        }

        private void CloseWithError(string message)
        {
            HTTPManager.Logger.Verbose("OverHTTP2", $"CloseWithError(\"{message}\")", this.Parent.Context);

            this.State = WebSocketStates.Closed;

            if (this.Parent.OnError != null)
            {
                try
                {
                    this.Parent.OnError(this.Parent, message);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception("OverHTTP2", "OnError", ex, this.Parent.Context);
                }
            }

            this.InternalRequest.Abort();
        }
    }
}
#endif
