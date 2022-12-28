using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using static UnityEditor.Build.Pipeline.Utilities.TaskCachingUtility;

namespace UnityEditor.Build.Pipeline.Tests
{
    public class TaskCachingUtilityTests
    {
        class ItemContext
        {
            public ItemContext(int input) { this.input = input; }
            public int input;
            public int result;
        }

        class FakeTracker : IProgressTracker
        {
            public bool shouldCancel;

            int IProgressTracker.TaskCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            float IProgressTracker.Progress => throw new NotImplementedException();


            bool IProgressTracker.UpdateInfo(string taskInfo)
            {
                return !shouldCancel;
            }

            bool IProgressTracker.UpdateTask(string taskTitle)
            {
                throw new NotImplementedException();
            }
        }

        class TestRunCachedCallbacks<T> : IRunCachedCallbacks<T>
        {
            public int CreateCachedInfoCount;
            public Func<WorkItem<T>, CachedInfo> CreateCachedInfoCB;
            public int CreateCacheEntryCount;
            public Func<WorkItem<T>, CacheEntry> CreateCacheEntryCB;
            public int PostProcessCount;
            public Action<WorkItem<T>> PostProcessCB;
            public int ProcessCachedCount;
            public Action<WorkItem<T>, CachedInfo> ProcessCachedCB;
            public int ProcessUncachedCount;
            public Action<WorkItem<T>> ProcessUncachedCB;

            public void ClearCounts()
            {
                CreateCachedInfoCount = 0;
                CreateCacheEntryCount = 0;
                PostProcessCount = 0;
                ProcessCachedCount = 0;
                ProcessUncachedCount = 0;
            }

            public CachedInfo CreateCachedInfo(WorkItem<T> item)
            {
                CreateCachedInfoCount++;
                return CreateCachedInfoCB(item);
            }

            public CacheEntry CreateCacheEntry(WorkItem<T> item)
            {
                CreateCacheEntryCount++;
                return CreateCacheEntryCB(item);
            }

            public void PostProcess(WorkItem<T> item)
            {
                PostProcessCount++;
                PostProcessCB(item);
            }

            public void ProcessCached(WorkItem<T> item, CachedInfo info)
            {
                ProcessCachedCount++;
                ProcessCachedCB(item, info);
            }

            public void ProcessUncached(WorkItem<T> item)
            {
                ProcessUncachedCount++;
                ProcessUncachedCB(item);
            }
        }

        [Test]
        public void RunCachedOperation_WhenSomeItemsCached_UncachedItemsAreProcessed()
        {
            Func<int, int> transform = (x) => x * 27;
            const int kFirstPassCount = 5;
            const int kSecondPassCount = 12;

            Dictionary<int, int> indexToResult = new Dictionary<int, int>();

            Action<int> AssertResults = (count) =>
            {
                Assert.AreEqual(count, indexToResult.Count);
                indexToResult.Keys.ToList().ForEach(x => Assert.AreEqual(transform(x), indexToResult[x]));
            };

            List<int> pass1Inputs = Enumerable.Range(0, kFirstPassCount).Select(x => x * 2).ToList();
            List<int> pass2Inputs = Enumerable.Range(0, kSecondPassCount).ToList();

            List<WorkItem<ItemContext>> workerInput1 =
                pass1Inputs.Select(i => new WorkItem<ItemContext>(new ItemContext(i))).ToList();

            List<WorkItem<ItemContext>> workerInput2 =
                pass2Inputs.Select(i => new WorkItem<ItemContext>(new ItemContext(i))).ToList();

            TestRunCachedCallbacks<ItemContext> callbacks = new TestRunCachedCallbacks<ItemContext>();
            callbacks.CreateCacheEntryCB = (item) =>
            {
                return new CacheEntry()
                {
                    Guid = HashingMethods.Calculate("Test").ToGUID(),
                    Hash = HashingMethods.Calculate(item.Context.input).ToHash128(),
                    Type = CacheEntry.EntryType.Data
                };
            };
            callbacks.ProcessUncachedCB = (item) => item.Context.result = transform(item.Context.input);
            callbacks.ProcessCachedCB = (item, info) => item.Context.result = (int)info.Data[0];
            callbacks.PostProcessCB = (item) => indexToResult.Add(item.Context.input, item.Context.result);
            callbacks.CreateCachedInfoCB = (item) =>
            {
                return new CachedInfo() { Data = new object[] { item.Context.result }, Dependencies = new CacheEntry[0], Asset = item.entry };
            };

            BuildCache.PurgeCache(false);
            using (BuildCache cache = new BuildCache())
            {
                TaskCachingUtility.RunCachedOperation<ItemContext>(cache, null, null, workerInput1, callbacks);
                AssertResults(workerInput1.Count);
                indexToResult.Clear();
                Assert.AreEqual(kFirstPassCount, callbacks.CreateCacheEntryCount);
                Assert.AreEqual(kFirstPassCount, callbacks.PostProcessCount);
                Assert.AreEqual(0, callbacks.ProcessCachedCount);
                Assert.AreEqual(kFirstPassCount, callbacks.ProcessUncachedCount);
                Assert.AreEqual(kFirstPassCount, callbacks.CreateCachedInfoCount);
                callbacks.ClearCounts();
                cache.SyncPendingSaves();

                TaskCachingUtility.RunCachedOperation<ItemContext>(cache, null, null, workerInput2, callbacks);
                AssertResults(workerInput2.Count);
                Assert.AreEqual(kSecondPassCount, callbacks.CreateCacheEntryCount);
                Assert.AreEqual(kSecondPassCount, callbacks.PostProcessCount);
                Assert.AreEqual(kFirstPassCount, callbacks.ProcessCachedCount);
                Assert.AreEqual(kSecondPassCount - kFirstPassCount, callbacks.ProcessUncachedCount);
                Assert.AreEqual(kSecondPassCount - kFirstPassCount, callbacks.CreateCachedInfoCount);
            }
        }

        [Test]
        public void RunCachedOperation_WhenCancelled_DoesNotContinueProcessing()
        {
            TestRunCachedCallbacks<int> callbacks = new TestRunCachedCallbacks<int>();
            List<WorkItem<int>> list = Enumerable.Range(0, 2).
                Select(i => new WorkItem<int>(i)).ToList();
            FakeTracker tracker = new FakeTracker();

            callbacks.ProcessUncachedCB = (item) => { tracker.shouldCancel = true; };
            callbacks.PostProcessCB = (item) => {};
            ReturnCode code = TaskCachingUtility.RunCachedOperation<int>(null, null, tracker, list, callbacks);
            Assert.AreEqual(1, callbacks.ProcessUncachedCount);
            Assert.AreEqual(code, ReturnCode.Canceled);
        }

        [Test]
        public void RunCachedOperation_WhenNoCache_ProcessesAllItems()
        {
            const int kIterations = 5;
            TestRunCachedCallbacks<ItemContext> callbacks = new TestRunCachedCallbacks<ItemContext>();
            Dictionary<int, int> indexToResult = new Dictionary<int, int>();
            List<WorkItem<ItemContext>> list = Enumerable.Range(0, kIterations).
                Select(i => new WorkItem<ItemContext>(new ItemContext(i * 10))).ToList();

            callbacks.ProcessUncachedCB = (item) => item.Context.result = item.Context.input * 100;
            callbacks.PostProcessCB = (item) => indexToResult.Add(item.Context.input, item.Context.result);
            TaskCachingUtility.RunCachedOperation<ItemContext>(null, null, null, list, callbacks);
            Assert.AreEqual(5, callbacks.PostProcessCount);
            Assert.AreEqual(5, callbacks.ProcessUncachedCount);
            foreach (WorkItem<ItemContext> item in list)
                Assert.AreEqual(item.Context.result, indexToResult[item.Context.input]);
        }
    }
}
