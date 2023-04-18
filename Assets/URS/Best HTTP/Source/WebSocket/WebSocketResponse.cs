#if !BESTHTTP_DISABLE_WEBSOCKET && (!UNITY_WEBGL || UNITY_EDITOR)

using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

using BestHTTP.Extensions;
using BestHTTP.WebSocket.Frames;
using BestHTTP.Core;
using BestHTTP.PlatformSupport.Memory;
using BestHTTP.Logger;
using BestHTTP.Connections;

namespace BestHTTP.WebSocket
{
    public sealed class WebSocketResponse : HTTPResponse, IProtocol
    {
        /// <summary>
        /// Capacity of the RTT buffer where the latencies are kept.
        /// </summary>
        public static int RTTBufferCapacity = 5;

        #region Public Interface

        /// <summary>
        /// A reference to the original WebSocket instance. Used for accessing extensions.
        /// </summary>
        public WebSocket WebSocket { get; internal set; }

        /// <summary>
        /// Called when a Text message received
        /// </summary>
        public Action<WebSocketResponse, string> OnText;

        /// <summary>
        /// Called when a Binary message received
        /// </summary>
        public Action<WebSocketResponse, byte[]> OnBinary;

        /// <summary>
        /// Called when a Binary message received. It's a more performant version than the OnBinary event, as the memory will be reused.
        /// </summary>
        public Action<WebSocketResponse, BufferSegment> OnBinaryNoAlloc;

        /// <summary>
        /// Called when an incomplete frame received. No attempt will be made to reassemble these fragments.
        /// </summary>
        public Action<WebSocketResponse, WebSocketFrameReader> OnIncompleteFrame;

        /// <summary>
        /// Called when the connection closed.
        /// </summary>
        public Action<WebSocketResponse, UInt16, string> OnClosed;

        /// <summary>
        /// IProtocol's ConnectionKey property.
        /// </summary>
        public HostConnectionKey ConnectionKey { get; private set; }

        /// <summary>
        /// Indicates whether the connection to the server is closed or not.
        /// </summary>
        public bool IsClosed { get { return closed; } }

        /// <summary>
        /// IProtocol.LoggingContext implementation.
        /// </summary>
        LoggingContext IProtocol.LoggingContext { get => this.Context; }

        /// <summary>
        /// On what frequency we have to send a ping to the server.
        /// </summary>
        public TimeSpan PingFrequnecy { get; private set; }

        /// <summary>
        /// Maximum size of a fragment's payload data. Its default value is WebSocket.MaxFragmentSize's value.
        /// </summary>
        public uint MaxFragmentSize { get; set; }

        /// <summary>
        /// Length of unsent, buffered up data in bytes.
        /// </summary>
        public int BufferedAmount { get { return this._bufferedAmount; } }
        private int _bufferedAmount;

        /// <summary>
        /// Calculated latency from the Round-Trip Times we store in the rtts field.
        /// </summary>
        public int Latency { get; private set; }

        /// <summary>
        /// When we received the last frame.
        /// </summary>
        public DateTime lastMessage = DateTime.MinValue;

        #endregion

        #region Private Fields

        private List<WebSocketFrameReader> IncompleteFrames = new List<WebSocketFrameReader>();
        private ConcurrentQueue<WebSocketFrameReader> CompletedFrames = new ConcurrentQueue<WebSocketFrameReader>();
        private WebSocketFrameReader CloseFrame;

        private ConcurrentQueue<WebSocketFrame> unsentFrames = new ConcurrentQueue<WebSocketFrame>();
        private volatile AutoResetEvent newFrameSignal = new AutoResetEvent(false);
        private int sendThreadCreated = 0;
        private int closedThreads = 0;

        /// <summary>
        /// True if we sent out a Close message to the server
        /// </summary>
        private volatile bool closeSent;

        /// <summary>
        /// True if this WebSocket connection is closed
        /// </summary>
        private volatile bool closed;

        /// <summary>
        /// When we sent out the last ping.
        /// </summary>
        private DateTime lastPing = DateTime.MinValue;

        /// <summary>
        /// True if waiting for an answer to our ping request. Ping timeout is used only why waitingForPong is true.
        /// </summary>
        private volatile bool waitingForPong = false;

        /// <summary>
        /// A circular buffer to store the last N rtt times calculated by the pong messages.
        /// </summary>
        private CircularBuffer<int> rtts = new CircularBuffer<int>(WebSocketResponse.RTTBufferCapacity);
        
        #endregion

        internal WebSocketResponse(HTTPRequest request, Stream stream, bool isStreamed, bool isFromCache)
            : base(request, stream, isStreamed, isFromCache)
        {
            base.IsClosedManually = true;
            this.ConnectionKey = new HostConnectionKey(this.baseRequest.CurrentUri.Host, HostDefinition.GetKeyForRequest(this.baseRequest));

            closed = false;
            MaxFragmentSize = WebSocket.MaxFragmentSize;
        }

        internal void StartReceive()
        {
            if (IsUpgraded)
                BestHTTP.PlatformSupport.Threading.ThreadedRunner.RunLongLiving(ReceiveThreadFunc);
        }

        internal void CloseStream()
        {
            if (base.Stream != null)
            {
                try
                {
                    base.Stream.Dispose();
                }
                catch
                { }
            }
        }

        #region Public interface for interacting with the server

        /// <summary>
        /// It will send the given message to the server in one frame.
        /// </summary>
        public void Send(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message must not be null!");

            int count = System.Text.Encoding.UTF8.GetByteCount(message);
            byte[] data = BufferPool.Get(count, true);
            System.Text.Encoding.UTF8.GetBytes(message, 0, message.Length, data, 0);

            Send(WebSocketFrameTypes.Text, data.AsBuffer(count));
        }

        /// <summary>
        /// It will send the given data to the server in one frame.
        /// </summary>
        public void Send(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data must not be null!");

            WebSocketFrame frame = new WebSocketFrame(this.WebSocket, WebSocketFrameTypes.Binary, new BufferSegment(data, 0, data.Length));
            Send(frame);
        }

        /// <summary>
        /// Will send count bytes from a byte array, starting from offset.
        /// </summary>
        public void Send(byte[] data, ulong offset, ulong count)
        {
            if (data == null)
                throw new ArgumentNullException("data must not be null!");
            if (offset + count > (ulong)data.Length)
                throw new ArgumentOutOfRangeException("offset + count >= data.Length");

            WebSocketFrame frame = new WebSocketFrame(this.WebSocket, WebSocketFrameTypes.Binary, new BufferSegment(data, (int)offset, (int)count), true, true);
            Send(frame);
        }

        public void Send(WebSocketFrameTypes type, BufferSegment data)
        {
            WebSocketFrame frame = new WebSocketFrame(this.WebSocket, type, data, true, true, false);
            Send(frame);
        }

        /// <summary>
        /// It will send the given frame to the server.
        /// </summary>
        public void Send(WebSocketFrame frame)
        {
            if (closed || closeSent)
                return;

            this.unsentFrames.Enqueue(frame);

            if (Interlocked.CompareExchange(ref this.sendThreadCreated, 1, 0) == 0)
            {
                HTTPManager.Logger.Information("WebSocketResponse", "Send - Creating thread", this.Context);

                BestHTTP.PlatformSupport.Threading.ThreadedRunner.RunLongLiving(SendThreadFunc);
            }

            Interlocked.Add(ref this._bufferedAmount, frame.Data.Count);

            //if (HTTPManager.Logger.Level <= Logger.Loglevels.All)
            //    HTTPManager.Logger.Information("WebSocketResponse", "Signaling SendThread!", this.Context);

            newFrameSignal.Set();
        }

        /// <summary>
        /// It will initiate the closing of the connection to the server.
        /// </summary>
        public void Close()
        {
            Close(1000, "Bye!");
        }

        /// <summary>
        /// It will initiate the closing of the connection to the server.
        /// </summary>
        public void Close(UInt16 code, string msg)
        {
            if (closed)
                return;

            HTTPManager.Logger.Verbose("WebSocketResponse", string.Format("Close({0}, \"{1}\")", code, msg), this.Context);

            WebSocketFrame frame;
            while (this.unsentFrames.TryDequeue(out frame))
            {
                if (frame.Data.Data != null)
                {
                    BufferPool.Release(frame.Data);
                    Interlocked.Add(ref this._bufferedAmount, -frame.Data.Count);
                }
            }

            Send(new WebSocketFrame(this.WebSocket, WebSocketFrameTypes.ConnectionClose, WebSocket.EncodeCloseData(code, msg)));
        }

        public void StartPinging(int frequency)
        {
            if (frequency < 100)
                throw new ArgumentException("frequency must be at least 100 milliseconds!");

            PingFrequnecy = TimeSpan.FromMilliseconds(frequency);
            lastMessage = DateTime.UtcNow;

            SendPing();
        }

        #endregion

        #region Private Threading Functions

        private void SendThreadFunc()
        {
            PlatformSupport.Threading.ThreadedRunner.SetThreadName("BestHTTP.WebSocket Send");

            try
            {
                bool mask = !HTTPProtocolFactory.IsSecureProtocol(this.baseRequest.CurrentUri);
                using (WriteOnlyBufferedStream bufferedStream = new WriteOnlyBufferedStream(this.Stream, 16 * 1024))
                {
                    while (!closed && !closeSent)
                    {
                        //if (HTTPManager.Logger.Level <= Logger.Loglevels.All)
                        //    HTTPManager.Logger.Information("WebSocketResponse", "SendThread - Waiting...", this.Context);

                        TimeSpan waitTime = TimeSpan.FromMilliseconds(int.MaxValue);

                        if (this.PingFrequnecy != TimeSpan.Zero)
                        {
                            DateTime now = DateTime.UtcNow;
                            waitTime = lastMessage + PingFrequnecy - now;

                            if (waitTime <= TimeSpan.Zero)
                            {
                                if (!waitingForPong && now - lastMessage >= PingFrequnecy)
                                {
                                    if (!SendPing())
                                        continue;
                                }

                                waitTime = PingFrequnecy;
                            }

                            if (waitingForPong && now - lastPing > this.WebSocket.CloseAfterNoMessage)
                            {
                                HTTPManager.Logger.Warning("WebSocketResponse",
                                    string.Format("No message received in the given time! Closing WebSocket. LastPing: {0}, PingFrequency: {1}, Close After: {2}, Now: {3}",
                                    this.lastPing, this.PingFrequnecy, this.WebSocket.CloseAfterNoMessage, now), this.Context);

                                CloseWithError(HTTPRequestStates.Error, "No message received in the given time!");
                                continue;
                            }
                        }

                        newFrameSignal.WaitOne(waitTime);

                        try
                        {
                            //if (HTTPManager.Logger.Level <= Logger.Loglevels.All)
                            //    HTTPManager.Logger.Information("WebSocketResponse", "SendThread - Wait is over, about " + this.unsentFrames.Count.ToString() + " new frames!", this.Context);

                            WebSocketFrame frame;
                            while (this.unsentFrames.TryDequeue(out frame))
                            {
                                // save data count as per-message deflate can compress, and it would be different after calling WriteTo
                                int originalFrameDataLength = frame.Data.Count;

                                if (!closeSent)
                                {
                                    frame.WriteTo((header, chunk) =>
                                    {
                                        bufferedStream.Write(header.Data, header.Offset, header.Count);
                                        BufferPool.Release(header);

                                        if (chunk != BufferSegment.Empty)
                                            bufferedStream.Write(chunk.Data, chunk.Offset, chunk.Count);
                                    }, MaxFragmentSize, mask, this.Context);
                                    BufferPool.Release(frame.Data);

                                    if (frame.Type == WebSocketFrameTypes.ConnectionClose)
                                        closeSent = true;
                                }

                                Interlocked.Add(ref this._bufferedAmount, -originalFrameDataLength);
                            }

                            bufferedStream.Flush();
                        }
                        catch (Exception ex)
                        {
                            if (HTTPUpdateDelegator.IsCreated)
                            {
                                this.baseRequest.Exception = ex;
                                this.baseRequest.State = HTTPRequestStates.Error;
                            }
                            else
                                this.baseRequest.State = HTTPRequestStates.Aborted;

                            closed = true;
                        }
                    }

                    HTTPManager.Logger.Information("WebSocketResponse", string.Format("Ending Send thread. Closed: {0}, closeSent: {1}", closed, closeSent), this.Context);
                }
            }
            catch (Exception ex)
            {
                if (HTTPManager.Logger.Level == Loglevels.All)
                    HTTPManager.Logger.Exception("WebSocketResponse", "SendThread", ex);
            }
            finally
            {
                Interlocked.Exchange(ref sendThreadCreated, 0);

                HTTPManager.Logger.Information("WebSocketResponse", "SendThread - Closed!", this.Context);

                TryToCleanup();
            }
        }

        private void ReceiveThreadFunc()
        {
            PlatformSupport.Threading.ThreadedRunner.SetThreadName("BestHTTP.WebSocket Receive");

            try
            {
                while (!closed)
                {
                    try
                    {
                        WebSocketFrameReader frame = new WebSocketFrameReader();
                        frame.Read(this.Stream);

                        if (HTTPManager.Logger.Level == Logger.Loglevels.All)
                            HTTPManager.Logger.Information("WebSocketResponse", "Frame received: " + frame.ToString(), this.Context);

                        lastMessage = DateTime.UtcNow;

                        if (!frame.IsFinal)
                        {
                            if (OnIncompleteFrame == null)
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
                                if (OnIncompleteFrame == null)
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
                                    ProtocolEventHelper.EnqueueProtocolEvent(new ProtocolEventInfo(this));
                                }
                                break;

                            case WebSocketFrameTypes.Text:
                            case WebSocketFrameTypes.Binary:
                                frame.DecodeWithExtensions(WebSocket);
                                CompletedFrames.Enqueue(frame);
                                ProtocolEventHelper.EnqueueProtocolEvent(new ProtocolEventInfo(this));
                                break;

                            // Upon receipt of a Ping frame, an endpoint MUST send a Pong frame in response, unless it already received a Close frame.
                            case WebSocketFrameTypes.Ping:
                                if (!closeSent && !closed)
                                    Send(new WebSocketFrame(this.WebSocket, WebSocketFrameTypes.Pong, frame.Data, true, true));
                                break;

                            case WebSocketFrameTypes.Pong:
                                try
                                {
                                    // the difference between the current time and the time when the ping message is sent
                                    TimeSpan diff = TimeSpan.FromTicks(this.lastMessage.Ticks - this.lastPing.Ticks);

                                    // add it to the buffer
                                    this.rtts.Add((int)diff.TotalMilliseconds);

                                    // and calculate the new latency
                                    this.Latency = CalculateLatency();
                                }
                                catch
                                {
                                    // https://tools.ietf.org/html/rfc6455#section-5.5
                                    // A Pong frame MAY be sent unsolicited.  This serves as a
                                    // unidirectional heartbeat.  A response to an unsolicited Pong frame is
                                    // not expected. 
                                }
                                finally
                                {
                                    waitingForPong = false;
                                }

                                break;

                            // If an endpoint receives a Close frame and did not previously send a Close frame, the endpoint MUST send a Close frame in response.
                            case WebSocketFrameTypes.ConnectionClose:
                                HTTPManager.Logger.Information("WebSocketResponse", "ConnectionClose packet received!", this.Context);

                                CloseFrame = frame;
                                if (!closeSent)
                                    Send(new WebSocketFrame(this.WebSocket, WebSocketFrameTypes.ConnectionClose, BufferSegment.Empty));
                                closed = true;
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        if (HTTPUpdateDelegator.IsCreated)
                        {
                            this.baseRequest.Exception = e;
                            this.baseRequest.State = HTTPRequestStates.Error;
                        }
                        else
                            this.baseRequest.State = HTTPRequestStates.Aborted;

                        closed = true;
                        newFrameSignal.Set();
                    }
                }

                HTTPManager.Logger.Information("WebSocketResponse", "Ending Read thread! closed: " + closed, this.Context);
            }
            finally
            {
                HTTPManager.Logger.Information("WebSocketResponse", "ReceiveThread - Closed!", this.Context);

                TryToCleanup();
            }
        }

        #endregion

        #region Sending Out Events

        /// <summary>
        /// Internal function to send out received messages.
        /// </summary>
        void IProtocol.HandleEvents()
        {
            WebSocketFrameReader frame;
            while (CompletedFrames.TryDequeue(out frame))
            {
                // Bugs in the clients shouldn't interrupt the code, so we need to try-catch and ignore any exception occurring here
                try
                {
                    switch (frame.Type)
                    {
                        case WebSocketFrameTypes.Continuation:
                            if (HTTPManager.Logger.Level == Loglevels.All)
                                HTTPManager.Logger.Verbose("WebSocketResponse", "HandleEvents - OnIncompleteFrame: " + frame.ToString(), this.Context);

                            if (OnIncompleteFrame != null)
                                OnIncompleteFrame(this, frame);
                            break;

                        case WebSocketFrameTypes.Text:
                            // Any not Final frame is handled as a fragment
                            if (!frame.IsFinal)
                                goto case WebSocketFrameTypes.Continuation;

                            if (HTTPManager.Logger.Level == Loglevels.All)
                                HTTPManager.Logger.Verbose("WebSocketResponse", "HandleEvents - OnText: " + frame.DataAsText, this.Context);

                            if (OnText != null)
                                OnText(this, frame.DataAsText);
                            break;

                        case WebSocketFrameTypes.Binary:
                            // Any not Final frame is handled as a fragment
                            if (!frame.IsFinal)
                                goto case WebSocketFrameTypes.Continuation;

                            if (HTTPManager.Logger.Level == Loglevels.All)
                                HTTPManager.Logger.Verbose("WebSocketResponse", "HandleEvents - OnBinary: " + frame.ToString(), this.Context);

                            if (OnBinary != null)
                            {
                                var data = new byte[frame.Data.Count];
                                Array.Copy(frame.Data.Data, frame.Data.Offset, data, 0, frame.Data.Count);
                                OnBinary(this, data);
                            }

                            if (OnBinaryNoAlloc != null)
                                OnBinaryNoAlloc(this, frame.Data);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception("WebSocketResponse", string.Format("HandleEvents({0})", frame.ToString()), ex, this.Context);
                }
                finally
                {
                    frame.ReleaseData();
                }
            }

            // 2015.05.09
            // State checking added because if there is an error the OnClose called first, and then the OnError.
            // Now, when there is an error only the OnError event will be called!
            if (IsClosed && OnClosed != null && baseRequest.State == HTTPRequestStates.Processing)
            {
                HTTPManager.Logger.Verbose("WebSocketResponse", "HandleEvents - Calling OnClosed", this.Context);
                try
                {
                    UInt16 statusCode = 0;
                    string msg = string.Empty;

                    // If we received any data, we will get the status code and the message from it
                    if (/*CloseFrame != null && */CloseFrame.Data != BufferSegment.Empty && CloseFrame.Data.Count >= 2)
                    {
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(CloseFrame.Data.Data, CloseFrame.Data.Offset, 2);
                        statusCode = BitConverter.ToUInt16(CloseFrame.Data.Data, CloseFrame.Data.Offset);

                        if (CloseFrame.Data.Count > 2)
                            msg = Encoding.UTF8.GetString(CloseFrame.Data.Data, CloseFrame.Data.Offset + 2, CloseFrame.Data.Count - 2);

                        CloseFrame.ReleaseData();
                    }

                    OnClosed(this, statusCode, msg);
                    OnClosed = null;
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception("WebSocketResponse", "HandleEvents - OnClosed", ex, this.Context);
                }
            }
        }

        #endregion

        private bool SendPing()
        {
            HTTPManager.Logger.Information("WebSocketResponse", "Sending Ping frame, waiting for a pong...", this.Context);

            lastPing = DateTime.UtcNow;
            waitingForPong = true;

            try
            {
                var pingFrame = new WebSocketFrame(this.WebSocket, WebSocketFrameTypes.Ping, BufferSegment.Empty);

                Send(pingFrame);
            }
            catch
            {
                HTTPManager.Logger.Information("WebSocketResponse", "Error while sending PING message! Closing WebSocket.", this.Context);
                CloseWithError(HTTPRequestStates.Error, "Error while sending PING message!");

                return false;
            }

            return true;
        }

        private void CloseWithError(HTTPRequestStates state, string message)
        {
            if (!string.IsNullOrEmpty(message))
                this.baseRequest.Exception = new Exception(message);
            this.baseRequest.State = state;

            this.closed = true;

            CloseStream();
            ProtocolEventHelper.EnqueueProtocolEvent(new ProtocolEventInfo(this));
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

        void IProtocol.CancellationRequested()
        {
            CloseWithError(HTTPRequestStates.Aborted, null);
        }

        private void TryToCleanup()
        {
            if (Interlocked.Increment(ref this.closedThreads) == 2)
            {
                ProtocolEventHelper.EnqueueProtocolEvent(new ProtocolEventInfo(this));
                (newFrameSignal as IDisposable).Dispose();
                newFrameSignal = null;

                CloseStream();

                HTTPManager.Logger.Information("WebSocketResponse", "TryToCleanup - finished!", this.Context);
            }
        }

        public override string ToString()
        {
            return this.ConnectionKey.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            IncompleteFrames.Clear();
            CompletedFrames.Clear();
            unsentFrames.Clear();
        }
    }
}

#endif
