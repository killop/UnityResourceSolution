using System.Collections.Concurrent;
using System.Threading;
using NUnit.Framework;

namespace UnityEditor.Build.Pipeline.Tests
{
    [TestFixture]
    public class ThreadingManagerTasks
    {
        [TestCase(ThreadingManager.ThreadQueues.SaveQueue)]
        [TestCase(ThreadingManager.ThreadQueues.UploadQueue)]
        [TestCase(ThreadingManager.ThreadQueues.PruneQueue)]
        public void ThreadingTasks_SameQueues_ExecuteSequentially(object queue)
        {
            var testQueue = (ThreadingManager.ThreadQueues)queue;

            SemaphoreSlim s1 = new SemaphoreSlim(0);
            SemaphoreSlim s2 = new SemaphoreSlim(0);
            ConcurrentQueue<int> callOrder = new ConcurrentQueue<int>();
            ThreadingManager.QueueTask(testQueue, (_) =>
            {
                s1.Wait();
                callOrder.Enqueue(1);
                Thread.Sleep(50);
                callOrder.Enqueue(2);
            }, null);

            ThreadingManager.QueueTask(testQueue, (_) =>
            {
                callOrder.Enqueue(3);
            }, null);

            Thread.Sleep(100);
            s1.Release();

            ThreadingManager.WaitForOutstandingTasks();

            Assert.AreEqual(3, callOrder.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, callOrder.ToArray());
        }

        [Test]
        public void ThreadingTasks_SaveQueue_RunsInParallelWith_UploadQueue()
        {
            SemaphoreSlim s1 = new SemaphoreSlim(0);
            SemaphoreSlim s2 = new SemaphoreSlim(0);
            SemaphoreSlim s3 = new SemaphoreSlim(0);
            ConcurrentQueue<int> callOrder = new ConcurrentQueue<int>();
            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.SaveQueue, (_) =>
            {
                s1.Wait();
                callOrder.Enqueue(1);
                s2.Release();
                s3.Wait();
                callOrder.Enqueue(2);
            }, null);

            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.UploadQueue, (_) =>
            {
                s2.Wait();
                callOrder.Enqueue(3);
                s3.Release();
            }, null);

            Thread.Sleep(100);
            s1.Release();

            ThreadingManager.WaitForOutstandingTasks();

            Assert.AreEqual(3, callOrder.Count);
            CollectionAssert.AreEqual(new[] { 1, 3, 2 }, callOrder.ToArray());
        }

        [Test]
        public void ThreadingTasks_PruneQueue_RunsAfter_SaveAndUploadQueue()
        {
            SemaphoreSlim s1 = new SemaphoreSlim(0);
            SemaphoreSlim s2 = new SemaphoreSlim(0);
            SemaphoreSlim s3 = new SemaphoreSlim(0);
            ConcurrentQueue<int> callOrder = new ConcurrentQueue<int>();
            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.SaveQueue, (_) =>
            {
                s1.Wait();
                callOrder.Enqueue(1);
                s2.Release();
                s3.Wait();
                callOrder.Enqueue(2);
            }, null);

            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.UploadQueue, (_) =>
            {
                s2.Wait();
                callOrder.Enqueue(3);
                s3.Release();
            }, null);

            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.PruneQueue, (_) =>
            {
                callOrder.Enqueue(4);
            }, null);

            Thread.Sleep(100);
            s1.Release();

            ThreadingManager.WaitForOutstandingTasks();

            Assert.AreEqual(4, callOrder.Count);
            CollectionAssert.AreEqual(new[] { 1, 3, 2, 4 }, callOrder.ToArray());
        }

        [Test]
        public void WhenPruneTaskActive_SaveAndUploadTasksWaitForPruneCompletion()
        {
            SemaphoreSlim s1 = new SemaphoreSlim(0);
            ConcurrentQueue<int> callOrder = new ConcurrentQueue<int>();
            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.PruneQueue, (_) =>
            {
                s1.Wait();
                callOrder.Enqueue(1);
            }, null);

            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.SaveQueue, (_) =>
            {
                callOrder.Enqueue(2);
            }, null);

            ThreadingManager.QueueTask(ThreadingManager.ThreadQueues.UploadQueue, (_) =>
            {
                callOrder.Enqueue(3);
            }, null);

            Thread.Sleep(100);
            s1.Release();

            ThreadingManager.WaitForOutstandingTasks();

            Assert.AreEqual(3, callOrder.Count);
            Assert.IsTrue(callOrder.TryDequeue(out int result));
            Assert.AreEqual(1, result);
            var results = callOrder.ToArray();
            CollectionAssert.AreEquivalent(new[] { 2, 3 }, results);
        }
    }
}
