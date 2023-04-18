using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using BestHTTP.PlatformSupport.Threading;

namespace BestHTTP.PlatformSupport.Text
{
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public static class StringBuilderPool
    {
        /// <summary>
        /// Setting this property to false the pooling mechanism can be disabled.
        /// </summary>
        public static bool IsEnabled
        {
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
        public static TimeSpan RemoveOlderThan = TimeSpan.FromSeconds(10);

        /// <summary>
        /// How often pool maintenance must run.
        /// </summary>
        public static TimeSpan RunMaintenanceEvery = TimeSpan.FromSeconds(5);

        private static DateTime lastMaintenance = DateTime.MinValue;

        private readonly static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        struct BuilderShelf
        {
            public StringBuilder builder;
            public DateTime released;

            public BuilderShelf(StringBuilder sb)
            {
                this.builder = sb;
                this.released = DateTime.UtcNow;
            }
        }

        private static List<BuilderShelf> pooledBuilders = new List<BuilderShelf>();

        public static StringBuilder Get(int lengthHint)
        {
            if (!_isEnabled)
                return new StringBuilder(lengthHint);

            using (new WriteLock(rwLock))
            {
                for (int i = pooledBuilders.Count - 1; i >= 0; i--)
                {
                    BuilderShelf shelf = pooledBuilders[i];
                    if (shelf.builder.Capacity >= lengthHint)
                    {
                        pooledBuilders.RemoveAt(i);
                        return shelf.builder;
                    }
                }

                // no builder found with lengthHint, take the first available
                if (pooledBuilders.Count > 0)
                {
                    BuilderShelf shelf = pooledBuilders[pooledBuilders.Count - 1];
                    pooledBuilders.RemoveAt(pooledBuilders.Count - 1);
                    return shelf.builder;
                }
            }

            return new StringBuilder(lengthHint);
        }

        public static void Release(StringBuilder builder)
        {
            if (builder == null)
                return;

            if (!_isEnabled)
                return;

            builder.Clear();

            using (new WriteLock(rwLock))
                pooledBuilders.Add(new BuilderShelf(builder));
        }

        public static string ReleaseAndGrab(StringBuilder builder)
        {
            if (builder == null)
                return null;

            var result = builder.ToString();
            if (!_isEnabled)
                return result;

            builder.Clear();

            using (new WriteLock(rwLock))
                pooledBuilders.Add(new BuilderShelf(builder));

            return result;
        }

        internal static void Maintain()
        {
            DateTime now = DateTime.UtcNow;
            if (!_isEnabled || lastMaintenance + RunMaintenanceEvery > now)
                return;
            lastMaintenance = now;

            DateTime olderThan = now - RemoveOlderThan;
            using (new WriteLock(rwLock))
            {
                for (int i = 0; i < pooledBuilders.Count; i++)
                {
                    BuilderShelf shelf = pooledBuilders[i];

                    if (shelf.released < olderThan)
                        pooledBuilders.RemoveAt(i--);
                }
            }
        }

        public static void Clear()
        {
            using (new WriteLock(rwLock))
                pooledBuilders.Clear();
        }
    }
}
