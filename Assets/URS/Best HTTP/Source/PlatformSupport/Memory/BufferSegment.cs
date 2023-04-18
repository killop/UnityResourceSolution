using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using BestHTTP.PlatformSupport.Threading;
using System.Collections.Concurrent;
using BestHTTP.PlatformSupport.Text;

#if NET_STANDARD_2_0 || NETFX_CORE
using System.Runtime.CompilerServices;
#endif

namespace BestHTTP.PlatformSupport.Memory
{
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public struct BufferSegment
    {
        private const int ToStringMaxDumpLength = 128;

        public static readonly BufferSegment Empty = new BufferSegment(null, 0, 0);

        public readonly byte[] Data;
        public readonly int Offset;
        public readonly int Count;

        public BufferSegment(byte[] data, int offset, int count)
        {
            this.Data = data;
            this.Offset = offset;
            this.Count = count;
        }

        public BufferSegment Slice(int newOffset)
        {
            int diff = newOffset - this.Offset;
            return new BufferSegment(this.Data, newOffset, this.Count - diff);
        }

        public BufferSegment Slice(int offset, int count)
        {
            return new BufferSegment(this.Data, offset, count);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is BufferSegment))
                return false;

            return Equals((BufferSegment)obj);
        }

        public bool Equals(BufferSegment other)
        {
            return this.Data == other.Data &&
                   this.Offset == other.Offset &&
                   this.Count == other.Count;
        }

        public override int GetHashCode()
        {
            return (this.Data != null ? this.Data.GetHashCode() : 0) * 21 + this.Offset + this.Count;
        }

        public static bool operator ==(BufferSegment left, BufferSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BufferSegment left, BufferSegment right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            var sb = StringBuilderPool.Get(this.Count + 5);
            sb.Append("[BufferSegment ");
            sb.AppendFormat("Offset: {0:N0} ", this.Offset);
            sb.AppendFormat("Count: {0:N0} ", this.Count);
            sb.Append("Data: [");

            if (this.Count > 0)
            {
                if (this.Count <= ToStringMaxDumpLength)
                {
                    sb.AppendFormat("{0:X2}", this.Data[this.Offset]);
                    for (int i = 1; i < this.Count; ++i)
                        sb.AppendFormat(", {0:X2}", this.Data[this.Offset + i]);
                }
                else
                    sb.Append("...");
            }

            sb.Append("]]");
            return StringBuilderPool.ReleaseAndGrab(sb);
        }
    }
}
