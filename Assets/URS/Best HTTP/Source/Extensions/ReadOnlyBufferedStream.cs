using BestHTTP.PlatformSupport.Memory;
using System;
using System.IO;

namespace BestHTTP.Extensions
{
    public sealed class ReadOnlyBufferedStream : Stream
    {
        Stream stream;
        public const int READBUFFER = 8192;
        byte[] buf;
        int available = 0;
        int pos = 0;
        
        public ReadOnlyBufferedStream(Stream nstream)
            :this(nstream, READBUFFER)
        {
        }

        public ReadOnlyBufferedStream(Stream nstream, int bufferSize)
        {
            stream = nstream;
            buf = BufferPool.Get(bufferSize, true);
        }

        public override int Read(byte[] buffer, int offset, int size)
        {
            if (available > 0)
            {
                // copy & return
                int copyCount = Math.Min(available, size);
                Array.Copy(buf, pos, buffer, offset, copyCount);
                pos += copyCount;
                available -= copyCount;
                return copyCount;
            }
            else
            {
                if (size >= buf.Length)
                {
                    // read directly to buffer
                    return stream.Read(buffer, offset, size);
                }
                else
                {
                    // read to buf and copy
                    pos = 0;
                    available = stream.Read(buf, 0, buf.Length);

                    if (available > 0)
                        return Read(buffer, offset, size);
                    else
                        return 0;
                }
            }
        }

        public override int ReadByte()
        {
            if (available > 0)
            {
                available -= 1;
                pos += 1;
                return buf[pos - 1];
            }
            else
            {
                try
                {
                    available = stream.Read(buf, 0, buf.Length);
                    pos = 0;
                }
                catch
                {
                    return -1;
                }
                if (available < 1)
                {
                    return -1;
                }
                else
                {
                    available -= 1;
                    pos += 1;
                    return buf[pos - 1];
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && buf != null)
                BufferPool.Release(buf);

            buf = null;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
