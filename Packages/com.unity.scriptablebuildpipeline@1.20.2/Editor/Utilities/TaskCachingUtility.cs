using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;


namespace UnityEditor.Build.Pipeline.Utilities
{
    internal static class TaskCachingUtility
    {
        public class WorkItem<T>
        {
            public T Context;
            public int Index;
            public CacheEntry entry;
            public string StatusText;
            public WorkItem(T context, string statusText = "")
            {
                this.Context = context;
                this.StatusText = statusText;
            }
        }

        public interface IRunCachedCallbacks<T>
        {
            /// <summary>
            /// Creates a cache entry for the specified work item.
            /// </summary>
            /// <param name="item">The work item.</param>
            /// <returns>Returns the created entry.</returns>
            CacheEntry CreateCacheEntry(WorkItem<T> item);

            /// <summary>
            /// Process the uncached work item.
            /// </summary>
            /// <param name="item">The work item.</param>
            void ProcessUncached(WorkItem<T> item);

            /// <summary>
            /// Process the cached work item.
            /// </summary>
            /// <param name="item">The work item.</param>
            /// <param name="info">The cached information for the work item.</param>
            void ProcessCached(WorkItem<T> item, CachedInfo info);

            /// <summary>
            /// Post processes the work item.
            /// </summary>
            /// <param name="item">The work item.</param>
            void PostProcess(WorkItem<T> item);

            /// <summary>
            /// Creates cached information for the specified work item.
            /// </summary>
            /// <param name="item">The work item.</param>
            /// <returns>Returns the cached information created.</returns>
            CachedInfo CreateCachedInfo(WorkItem<T> item);
        }

        public static ReturnCode RunCachedOperation<T>(IBuildCache cache, IBuildLogger log, IProgressTracker tracker, List<WorkItem<T>> workItems,
            IRunCachedCallbacks<T> cbs
        )
        {
            using (log.ScopedStep(LogLevel.Info, "RunCachedOperation"))
            {
                List<CacheEntry> cacheEntries = null;
                List<WorkItem<T>> nonCachedItems = workItems;
                var cachedItems = new List<WorkItem<T>>();

                for (int i = 0; i < workItems.Count; i++)
                {
                    workItems[i].Index = i;
                }

                IList<CachedInfo> cachedInfo = null;

                if (cache != null)
                {
                    using (log.ScopedStep(LogLevel.Info, "Creating Cache Entries"))
                        for (int i = 0; i < workItems.Count; i++)
                        {
                            workItems[i].entry = cbs.CreateCacheEntry(workItems[i]);
                        }

                    cacheEntries = workItems.Select(i => i.entry).ToList();

                    using (log.ScopedStep(LogLevel.Info, "Load Cached Data"))
                        cache.LoadCachedData(cacheEntries, out cachedInfo);

                    cachedItems = workItems.Where(x => cachedInfo[x.Index] != null).ToList();
                    nonCachedItems = workItems.Where(x => cachedInfo[x.Index] == null).ToList();
                }

                using (log.ScopedStep(LogLevel.Info, "Process Entries"))
                    foreach (WorkItem<T> item in nonCachedItems)
                    {
                        if (!tracker.UpdateInfoUnchecked(item.StatusText))
                            return ReturnCode.Canceled;
                        cbs.ProcessUncached(item);
                    }

                using (log.ScopedStep(LogLevel.Info, "Process Cached Entries"))
                    foreach (WorkItem<T> item in cachedItems)
                        cbs.ProcessCached(item, cachedInfo[item.Index]);

                foreach (WorkItem<T> item in workItems)
                    cbs.PostProcess(item);

                if (cache != null)
                {
                    List<CachedInfo> uncachedInfo;
                    using (log.ScopedStep(LogLevel.Info, "Saving to Cache"))
                    {
                        using (log.ScopedStep(LogLevel.Info, "Creating Cached Infos"))
                            uncachedInfo = nonCachedItems.Select((item) => cbs.CreateCachedInfo(item)).ToList();
                        cache.SaveCachedData(uncachedInfo);
                    }
                }

                log.AddEntrySafe(LogLevel.Info, $"Total Entries: {workItems.Count}, Processed: {nonCachedItems.Count}, Cached: {cachedItems.Count}");
                return ReturnCode.Success;
            }
        }
    }
}
