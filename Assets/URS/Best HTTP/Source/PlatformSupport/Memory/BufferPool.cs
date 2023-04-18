using System;
using System.Collections.Generic;
using System.Threading;

using BestHTTP.PlatformSupport.Threading;
using System.Linq;

#if NET_STANDARD_2_0 || NETFX_CORE
using System.Runtime.CompilerServices;
#endif

namespace BestHTTP.PlatformSupport.Memory
{
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public static class BufferPool
    {
        public static readonly byte[] NoData = new byte[0];

        /// <summary>
        /// Setting this property to false the pooling mechanism can be disabled.
        /// </summary>
        public static bool IsEnabled {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;

                // When set to non-enabled remove all stored entries
                if (!_isEnabled)
                    Clear();
            }
        }
        private static volatile bool _isEnabled = true;

        /// <summary>
        /// Buffer entries that released back to the pool and older than this value are moved when next maintenance is triggered.
        /// </summary>
        public static TimeSpan RemoveOlderThan = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How often pool maintenance must run.
        /// </summary>
        public static TimeSpan RunMaintenanceEvery = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Minimum buffer size that the plugin will allocate when the requested size is smaller than this value, and canBeLarger is set to true.
        /// </summary>
        public static long MinBufferSize = 32;

        /// <summary>
        /// Maximum size of a buffer that the plugin will store.
        /// </summary>
        public static long MaxBufferSize = long.MaxValue;

        /// <summary>
        /// Maximum accumulated size of the stored buffers.
        /// </summary>
        public static long MaxPoolSize = 30 * 1024 * 1024;

        /// <summary>
        /// Whether to remove empty buffer stores from the free list.
        /// </summary>
        public static bool RemoveEmptyLists = false;

        /// <summary>
        /// If it set to true and a byte[] is released more than once it will log out an error.
        /// </summary>
        public static bool IsDoubleReleaseCheckEnabled = false;

        // It must be sorted by buffer size!
        private readonly static List<BufferStore> FreeBuffers = new List<BufferStore>();
        private static DateTime lastMaintenance = DateTime.MinValue;

        // Statistics
        private static long PoolSize = 0;
        private static long GetBuffers = 0;
        private static long ReleaseBuffers = 0;
        private static long Borrowed = 0;
        private static long ArrayAllocations = 0;

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
        private static Dictionary<byte[], string> BorrowedBuffers = new Dictionary<byte[], string>();
#endif

        private readonly static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        static BufferPool()
        {
#if UNITY_EDITOR
            IsDoubleReleaseCheckEnabled = true;
#else
            IsDoubleReleaseCheckEnabled = false;
#endif
        }

        /// <summary>
        /// Get byte[] from the pool. If canBeLarge is true, the returned buffer might be larger than the requested size.
        /// </summary>
        public static byte[] Get(long size, bool canBeLarger)
        {
            if (!_isEnabled)
                return new byte[size];

            // Return a fix reference for 0 length requests. Any resize call (even Array.Resize) creates a new reference
            //  so we are safe to expose it to multiple callers.
            if (size == 0)
                return BufferPool.NoData;

            if (canBeLarger)
            {
                if (size < MinBufferSize)
                    size = MinBufferSize;
                else if (!IsPowerOfTwo(size))
                    size = NextPowerOf2(size);
            }
            else
            {
                if (size < MinBufferSize)
                    return new byte[size];
            }

            if (FreeBuffers.Count == 0)
            {
                Interlocked.Add(ref Borrowed, size);
                Interlocked.Increment(ref ArrayAllocations);

                var result = new byte[size];

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
                lock (FreeBuffers)
                    BorrowedBuffers.Add(result, ProcessStackTrace(Environment.StackTrace));
#endif

                return result;
            }

            BufferDesc bufferDesc = FindFreeBuffer(size, canBeLarger);

            if (bufferDesc.buffer == null)
            {
                Interlocked.Add(ref Borrowed, size);
                Interlocked.Increment(ref ArrayAllocations);

                var result = new byte[size];

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
                lock (FreeBuffers)
                    BorrowedBuffers.Add(result, ProcessStackTrace(Environment.StackTrace));
#endif

                return result;
            }
            else
            {
#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
                lock (FreeBuffers)
                    BorrowedBuffers.Add(bufferDesc.buffer, ProcessStackTrace(Environment.StackTrace));
#endif

                Interlocked.Increment(ref GetBuffers);
            }

            Interlocked.Add(ref Borrowed, bufferDesc.buffer.Length);
            Interlocked.Add(ref PoolSize, -bufferDesc.buffer.Length);

            return bufferDesc.buffer;
        }

        /// <summary>
        /// Release back a BufferSegment's data to the pool.
        /// </summary>
        /// <param name="segment"></param>
#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Release(BufferSegment segment)
        {
            Release(segment.Data);
        }

        /// <summary>
        /// Release back a byte array to the pool.
        /// </summary>
        public static void Release(byte[] buffer)
        {
            if (!_isEnabled || buffer == null)
                return;

            int size = buffer.Length;

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
            lock (FreeBuffers)
                BorrowedBuffers.Remove(buffer);
#endif

            Interlocked.Add(ref Borrowed, -size);

            if (size == 0 || size < MinBufferSize || size > MaxBufferSize)
                return;

            using (new WriteLock(rwLock))
            {
                var ps = Interlocked.Read(ref PoolSize);
                if (ps + size > MaxPoolSize)
                    return;

                Interlocked.Add(ref PoolSize, size);

                ReleaseBuffers++;

                AddFreeBuffer(buffer);
            }
        }

        /// <summary>
        /// Resize a byte array. It will release the old one to the pool, and the new one is from the pool too.
        /// </summary>
        public static byte[] Resize(ref byte[] buffer, int newSize, bool canBeLarger, bool clear)
        {
            if (!_isEnabled)
            {
                Array.Resize<byte>(ref buffer, newSize);
                return buffer;
            }

            byte[] newBuf = BufferPool.Get(newSize, canBeLarger);
            if (buffer != null)
            {
                if (!clear)
                    Array.Copy(buffer, 0, newBuf, 0, Math.Min(newBuf.Length, buffer.Length));
                BufferPool.Release(buffer);
            }

            if (clear)
                Array.Clear(newBuf, 0, newSize);

            return buffer = newBuf;
        }


        public static KeyValuePair<byte[], string>[] GetBorrowedBuffers()
        {
#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
            lock (FreeBuffers)
                return BorrowedBuffers.ToArray();
#else
            return new KeyValuePair<byte[], string>[0];
#endif
        }

#if true //UNITY_EDITOR
        public struct BufferStats
        {
            public long Size;
            public int Count;
        }

        public struct BufferPoolStats
        {
            public long GetBuffers;
            public long ReleaseBuffers;
            public long PoolSize;
            public long MaxPoolSize;
            public long MinBufferSize;
            public long MaxBufferSize;

            public long Borrowed;
            public long ArrayAllocations;

            public int FreeBufferCount;
            public List<BufferStats> FreeBufferStats;

            public TimeSpan NextMaintenance;
        }

        public static void GetStatistics(ref BufferPoolStats stats)
        {
            using (new ReadLock(rwLock))
            {
                stats.GetBuffers = GetBuffers;
                stats.ReleaseBuffers = ReleaseBuffers;
                stats.PoolSize = PoolSize;
                stats.MinBufferSize = MinBufferSize;
                stats.MaxBufferSize = MaxBufferSize;
                stats.MaxPoolSize = MaxPoolSize;

                stats.Borrowed = Borrowed;
                stats.ArrayAllocations = ArrayAllocations;

                stats.FreeBufferCount = FreeBuffers.Count;
                if (stats.FreeBufferStats == null)
                    stats.FreeBufferStats = new List<BufferStats>(FreeBuffers.Count);
                else
                    stats.FreeBufferStats.Clear();

                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];
                    List<BufferDesc> buffers = store.buffers;

                    BufferStats bufferStats = new BufferStats();
                    bufferStats.Size = store.Size;
                    bufferStats.Count = buffers.Count;

                    stats.FreeBufferStats.Add(bufferStats);
                }

                stats.NextMaintenance = (lastMaintenance + RunMaintenanceEvery) - DateTime.UtcNow;
            }
        }
#endif

        /// <summary>
        /// Remove all stored entries instantly.
        /// </summary>
        public static void Clear()
        {
            using (new WriteLock(rwLock))
            {
                FreeBuffers.Clear();
                Interlocked.Exchange(ref PoolSize, 0);
            }
        }

        /// <summary>
        /// Internal function called by the plugin to remove old, non-used buffers.
        /// </summary>
        internal static void Maintain()
        {
            DateTime now = DateTime.UtcNow;
            if (!_isEnabled || lastMaintenance + RunMaintenanceEvery > now)
                return;
            lastMaintenance = now;

            DateTime olderThan = now - RemoveOlderThan;
            using (new WriteLock(rwLock))
            {
                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];
                    List<BufferDesc> buffers = store.buffers;

                    for (int cv = buffers.Count - 1; cv >= 0; cv--)
                    {
                        BufferDesc desc = buffers[cv];

                        if (desc.released < olderThan)
                        {
                            // buffers stores available buffers ascending by age. So, when we find an old enough, we can
                            //  delete all entries in the [0..cv] range.

                            int removeCount = cv + 1;
                            buffers.RemoveRange(0, removeCount);
                            PoolSize -= (int)(removeCount * store.Size);
                            break;
                        }
                    }

                    if (RemoveEmptyLists && buffers.Count == 0)
                        FreeBuffers.RemoveAt(i--);
                }
            }
        }

#region Private helper functions

#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool IsPowerOfTwo(long x)
        {
            return (x & (x - 1)) == 0;
        }

#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static long NextPowerOf2(long x)
        {
            long pow = 1;
            while (pow <= x)
                pow *= 2;
            return pow;
        }

#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static BufferDesc FindFreeBuffer(long size, bool canBeLarger)
        {
            // Previously it was an upgradable read lock, and later a write lock around store.buffers.RemoveAt.
            // However, checking store.buffers.Count in the if statement, and then get the last buffer and finally write lock the RemoveAt call
            //  has plenty of time for race conditions.
            //  Another thread could change store.buffers after checking count and getting the last element and before the write lock,
            //  so in theory we could return with an element and remove another one from the buffers list.
            //  A new FindFreeBuffer call could return it again causing malformed data and/or releasing it could duplicate it in the store.
            // I tried to reproduce both issues (malformed data, duble entries) with a test where creating growin number of threads getting buffers writing to them, check the buffers and finally release them
            //  would fail _only_ if i used a plain Enter/Exit ReadLock pair, or no locking at all.
            // But, because there's quite a few different platforms and unity's implementation can be different too, switching from an upgradable lock to a more stricter write lock seems safer.
            //
            // An interesting read can be found here: https://stackoverflow.com/questions/21411018/readerwriterlockslim-enterupgradeablereadlock-always-a-deadlock

            using (new WriteLock(rwLock))
            {
                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];

                    if (store.buffers.Count > 0 && (store.Size == size || (canBeLarger && store.Size > size)))
                    {
                        // Getting the last one has two desired effect:
                        //  1.) RemoveAt should be quicker as it don't have to move all the remaining entries
                        //  2.) Old, non-used buffers will age. Getting a buffer and putting it back will not keep buffers fresh.

                        BufferDesc lastFree = store.buffers[store.buffers.Count - 1];
                        store.buffers.RemoveAt(store.buffers.Count - 1);
                        
                        return lastFree;
                    }
                }
            }

            return BufferDesc.Empty;
        }

#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static void AddFreeBuffer(byte[] buffer)
        {
            int bufferLength = buffer.Length;

            for (int i = 0; i < FreeBuffers.Count; ++i)
            {
                BufferStore store = FreeBuffers[i];

                if (store.Size == bufferLength)
                {
                    // We highly assume here that every buffer will be released only once.
                    //  Checking for double-release would mean that we have to do another O(n) operation, where n is the
                    //  count of the store's elements.

                    if (IsDoubleReleaseCheckEnabled)
                        for (int cv = 0; cv < store.buffers.Count; ++cv)
                        {
                            var entry = store.buffers[cv];
                            if (System.Object.ReferenceEquals(entry.buffer, buffer))
                            {
                                HTTPManager.Logger.Error("BufferPool", string.Format("Buffer ({0}) already added to the pool!", entry.ToString()));
                                return;
                            }
                        }

                    store.buffers.Add(new BufferDesc(buffer));
                    return;
                }

                if (store.Size > bufferLength)
                {
                    FreeBuffers.Insert(i, new BufferStore(bufferLength, buffer));
                    return;
                }
            }

            // When we reach this point, there's no same sized or larger BufferStore present, so we have to add a new one
            //  to the end of our list.
            FreeBuffers.Add(new BufferStore(bufferLength, buffer));
        }

#if BESTHTTP_ENABLE_BUFFERPOOL_BORROWED_BUFFERS_COLLECTION
        private static System.Text.StringBuilder stacktraceBuilder;
        private static string ProcessStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return string.Empty;

            var lines = stackTrace.Split('\n');

            if (stacktraceBuilder == null)
                stacktraceBuilder = new System.Text.StringBuilder(lines.Length);
            else
                stacktraceBuilder.Length = 0;

            // skip top 4 lines that would show the logger.

            for (int i = 0; i < lines.Length; ++i)
                if (!lines[i].Contains(".Memory.BufferPool") &&
                    !lines[i].Contains("Environment") &&
                    !lines[i].Contains("System.Threading"))
                    stacktraceBuilder.Append(lines[i].Replace("BestHTTP.", ""));

            return stacktraceBuilder.ToString();
        }
#endif

#endregion
    }
}
