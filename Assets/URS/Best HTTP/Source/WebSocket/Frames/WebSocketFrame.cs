#if !BESTHTTP_DISABLE_WEBSOCKET && (!UNITY_WEBGL || UNITY_EDITOR)

using System;
using BestHTTP.Extensions;
using BestHTTP.PlatformSupport.Memory;
using System.Runtime.CompilerServices;
using BestHTTP.PlatformSupport.IL2CPP;
using BestHTTP.Logger;

#if BESTHTTP_WITH_BURST
using Unity.Burst;
using Unity.Burst.Intrinsics;
using static Unity.Burst.Intrinsics.X86.Avx2;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.Arm.Neon;
#endif

namespace BestHTTP.WebSocket.Frames
{
    /// <summary>
    /// Denotes a binary frame. The "Payload data" is arbitrary binary data whose interpretation is solely up to the application layer.
    /// This is the base class of all other frame writers, as all frame can be represented as a byte array.
    /// </summary>
#if BESTHTTP_WITH_BURST
    [BurstCompile]
#endif
    [Il2CppEagerStaticClassConstruction]
    public struct WebSocketFrame
    {
        public WebSocketFrameTypes Type { get; private set; }

        public bool IsFinal { get; private set; }

        public byte Header { get; private set; }

        public BufferSegment Data { get; private set; }

        public WebSocket Websocket { get; private set; }

        public bool UseExtensions { get; private set; }

        public WebSocketFrame(WebSocket webSocket, WebSocketFrameTypes type, BufferSegment data)
            :this(webSocket, type, data, true)
        { }

        public WebSocketFrame(WebSocket webSocket, WebSocketFrameTypes type, BufferSegment data, bool useExtensions)
            : this(webSocket, type, data, true, useExtensions)
        {
        }

        public WebSocketFrame(WebSocket webSocket, WebSocketFrameTypes type, BufferSegment data, bool isFinal, bool useExtensions)
            :this(webSocket, type, data, isFinal, useExtensions, copyData: true)
        {

        }

        public WebSocketFrame(WebSocket webSocket, WebSocketFrameTypes type, BufferSegment data, bool isFinal, bool useExtensions, bool copyData)
        {
            this.Type = type;
            this.IsFinal = isFinal;
            this.Websocket = webSocket;
            this.UseExtensions = useExtensions;

            this.Data = data;

            if (this.Data.Data != null)
            {
                if (copyData)
                {
                    var from = this.Data;

                    var buffer = BufferPool.Get(this.Data.Count, true);
                    this.Data = new BufferSegment(buffer, 0, this.Data.Count);

                    Array.Copy(from.Data, (int)from.Offset, this.Data.Data, this.Data.Offset, this.Data.Count);
                }
            }
            else
                this.Data = BufferSegment.Empty;

            // First byte: Final Bit + Rsv flags + OpCode
            byte finalBit = (byte)(IsFinal ? 0x80 : 0x0);
            this.Header = (byte)(finalBit | (byte)Type);
        }

        public override string ToString()
        {
            return string.Format("[WebSocketFrame Type: {0}, IsFinal: {1}, Header: {2:X2}, Data: {3}, UseExtensions: {4}]",
                this.Type, this.IsFinal, this.Header, this.Data, this.UseExtensions);
        }

        public void WriteTo(Action<BufferSegment, BufferSegment> callback, uint maxFragmentSize, bool mask, LoggingContext context)
        {
            DoExtensions();

            if (HTTPManager.Logger.Level <= Logger.Loglevels.All)
                HTTPManager.Logger.Verbose("WebSocketFrame", "WriteTo - Frame: " + ToString(), context);

            if ((this.Type == WebSocketFrameTypes.Binary || this.Type == WebSocketFrameTypes.Text) && this.Data.Count > maxFragmentSize)
            {
                FragmentAndSend(callback, maxFragmentSize, mask, context);
            }
            else
            {
                WriteFragment(callback, this.Type, this.Header, this.Data, mask, context);
            }
        }

        private void DoExtensions()
        {
            if (this.UseExtensions && this.Websocket != null && this.Websocket.Extensions != null)
            {
                for (int i = 0; i < this.Websocket.Extensions.Length; ++i)
                {
                    var ext = this.Websocket.Extensions[i];
                    if (ext != null)
                    {
                        this.Header |= ext.GetFrameHeader(this, this.Header);
                        BufferSegment newData = ext.Encode(this);

                        if (newData != this.Data)
                        {
                            BufferPool.Release(this.Data);

                            this.Data = newData;
                        }
                    }
                }
            }
        }

        private void FragmentAndSend(Action<BufferSegment, BufferSegment> callback, uint maxFragmentSize, bool mask, LoggingContext context)
        {
            int pos = this.Data.Offset;
            int endPos = this.Data.Offset + this.Data.Count;

            while (pos < endPos)
            {
                int chunkLength = Math.Min((int)maxFragmentSize, endPos - pos);

                WriteFragment(callback: callback,
                    Type: pos == this.Data.Offset ? this.Type : WebSocketFrameTypes.Continuation,
                    IsFinal: pos + chunkLength >= this.Data.Count,
                    Data: this.Data.Slice((int)pos, (int)chunkLength),
                    mask: mask,
                    context: context);

                pos += chunkLength;
            }
        }

        private static void WriteFragment(Action<BufferSegment, BufferSegment> callback, WebSocketFrameTypes Type, bool IsFinal, BufferSegment Data, bool mask, LoggingContext context)
        {
            // First byte: Final Bit + Rsv flags + OpCode
            byte finalBit = (byte)(IsFinal ? 0x80 : 0x0);
            byte Header = (byte)(finalBit | (byte)Type);

            WriteFragment(callback, Type, Header, Data, mask, context);
        }

        private static unsafe void WriteFragment(Action<BufferSegment, BufferSegment> callback, WebSocketFrameTypes Type, byte Header, BufferSegment Data, bool mask, LoggingContext context)
        {
            // For the complete documentation for this section see:
            // http://tools.ietf.org/html/rfc6455#section-5.2

            // Header(1) + Len(8) + Mask (4)
            byte[] wsHeader = BufferPool.Get(13, true);
            int pos = 0;

            // Write the header
            wsHeader[pos++] = Header;

            // The length of the "Payload data", in bytes: if 0-125, that is the payload length.  If 126, the following 2 bytes interpreted as a
            // 16-bit unsigned integer are the payload length.  If 127, the following 8 bytes interpreted as a 64-bit unsigned integer (the
            // most significant bit MUST be 0) are the payload length.  Multibyte length quantities are expressed in network byte order.
            if (Data.Count < 126)
            {
                wsHeader[pos++] = (byte)(0x80 | (byte)Data.Count);
            }
            else if (Data.Count < UInt16.MaxValue)
            {
                wsHeader[pos++] = (byte)(0x80 | 126);
                var count = (UInt16)Data.Count;
                wsHeader[pos++] = (byte)(count >> 8);
                wsHeader[pos++] = (byte)(count);
            }
            else
            {
                wsHeader[pos++] = (byte)(0x80 | 127);

                var count = (UInt64)Data.Count;
                wsHeader[pos++] = (byte)(count >> 56);
                wsHeader[pos++] = (byte)(count >> 48);
                wsHeader[pos++] = (byte)(count >> 40);
                wsHeader[pos++] = (byte)(count >> 32);
                wsHeader[pos++] = (byte)(count >> 24);
                wsHeader[pos++] = (byte)(count >> 16);
                wsHeader[pos++] = (byte)(count >> 8);
                wsHeader[pos++] = (byte)(count);
            }

            if (Data != BufferSegment.Empty)
            {
                // All frames sent from the client to the server are masked by a 32-bit value that is contained within the frame.  This field is
                // present if the mask bit is set to 1 and is absent if the mask bit is set to 0.
                // If the data is being sent by the client, the frame(s) MUST be masked.

                uint hash = mask ? (uint)wsHeader.GetHashCode() : 0;

                wsHeader[pos++] = (byte)(hash >> 24);
                wsHeader[pos++] = (byte)(hash >> 16);
                wsHeader[pos++] = (byte)(hash >> 8);
                wsHeader[pos++] = (byte)(hash);

                // Do the masking.
                if (mask)
                {
                    fixed (byte* pData = Data.Data/*, pmask = &wsHeader[pos - 4]*/)
                    {
                        byte* alignedMask = stackalloc byte[4];
                        alignedMask[0] = wsHeader[pos - 4];
                        alignedMask[1] = wsHeader[pos - 3];
                        alignedMask[2] = wsHeader[pos - 2];
                        alignedMask[3] = wsHeader[pos - 1];

                        ApplyMask(pData, Data.Offset, Data.Count, alignedMask);
                    }
                }
            }
            else
            {
                wsHeader[pos++] = 0;
                wsHeader[pos++] = 0;
                wsHeader[pos++] = 0;
                wsHeader[pos++] = 0;
            }

            var header = wsHeader.AsBuffer(pos);

            if (HTTPManager.Logger.Level <= Logger.Loglevels.All)
                HTTPManager.Logger.Verbose("WebSocketFrame", string.Format("WriteFragment -  Header: {0}, data chunk: {1}", header.ToString(), Data.ToString()), context);

            callback(header, Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if BESTHTTP_WITH_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        public unsafe static void ApplyMask(
#if BESTHTTP_WITH_BURST
            [NoAlias]
#endif
            byte* pData,
            int DataOffset,
            int DataCount,
#if BESTHTTP_WITH_BURST
            [NoAlias]
#endif
            byte* pmask
            )
        {
            int targetOffset = DataOffset + DataCount;
            uint umask = *(uint*)pmask;

#if BESTHTTP_WITH_BURST
            if (targetOffset - DataOffset >= 32)
            {
                if (IsAvx2Supported)
                {
                    v256 mask = new v256(umask);
                    v256 ldstrMask = new v256((byte)0xFF);
                
                    while (targetOffset - DataOffset >= 32)
                    {
                        // load data
                        v256 data = mm256_maskload_epi32(pData + DataOffset, ldstrMask);

                        // xor
                        v256 result = mm256_xor_si256(data, mask);

                        // store
                        mm256_maskstore_epi32(pData + DataOffset, ldstrMask, result);

                        // advance
                        DataOffset += 32;
                    }
                }
            }

            if (targetOffset - DataOffset >= 16)
            {
                v128 mask = new v128(umask);

                if (IsSse2Supported)
                {
                    while (targetOffset - DataOffset >= 16)
                    {
                        // load data
                        v128 data = loadu_si128(pData + DataOffset);

                        // xor
                        var result = xor_si128(data, mask);

                        // store
                        storeu_si128(pData + DataOffset, result);

                        // advance
                        DataOffset += 16;
                    }
                }
                else if (IsNeonSupported)
                {
                    while (targetOffset - DataOffset >= 16)
                    {
                        // load data
                        v128 data = vld1q_u8(pData + DataOffset);

                        // xor
                        v128 result = veorq_u8(data, mask);

                        // store
                        vst1q_u8(pData + DataOffset, result);

                        // advance
                        DataOffset += 16;
                    }
                }
            }
#endif

            // fallback to calculate by reinterpret-casting to ulong
            if (targetOffset - DataOffset >= 8)
            {
                ulong* ulpData = (ulong*)(pData + DataOffset);

                // duplicate the mask to fill up a whole ulong.
                ulong ulmask = (((ulong)umask << 32) | umask);

                while (targetOffset - DataOffset >= 8)
                {
                    *ulpData = *ulpData ^ ulmask;

                    ulpData++;
                    DataOffset += 8;
                }
            }

            // process remaining bytes (0..7)
            for (int i = DataOffset; i < targetOffset; ++i)
                pData[i] = (byte)(pData[i] ^ pmask[(i - DataOffset) % 4]);
        }
    }
}

#endif
