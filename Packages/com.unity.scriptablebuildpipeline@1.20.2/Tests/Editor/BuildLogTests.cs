using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using static UnityEditor.Build.Pipeline.Utilities.BuildLog;

namespace UnityEditor.Build.Pipeline.Tests
{
    public class BuildLogTests
    {
        [Test]
        public void WhenBeginAndEndScope_DurationIsCorrect()
        {
            BuildLog log = new BuildLog();
            using (log.ScopedStep(LogLevel.Info, "TestStep"))
                Thread.Sleep(5);
            Assert.AreEqual("TestStep", log.Root.Children[0].Name);
            Assert.Greater(log.Root.Children[0].DurationMS, 4);
        }

        [Test]
        public void WhenAddMessage_EntryIsCreated()
        {
            BuildLog log = new BuildLog();
            using (log.ScopedStep(LogLevel.Info, "TestStep"))
                log.AddEntry(LogLevel.Info, "TestEntry");
            Assert.AreEqual("TestEntry", log.Root.Children[0].Entries[0].Message);
        }

        [Test]
        public void WhenMessageAddedWithScope_EntryIsCreated()
        {
            BuildLog log = new BuildLog();
            ((IDisposable)log.ScopedStep(LogLevel.Info, "TestStep", "TestEntry")).Dispose();
            Assert.AreEqual("TestEntry", log.Root.Children[0].Entries[0].Message);
        }

        [Test]
        public void WhenScopeIsThreaded_AndThreadAddsNode_NodeEnteredInThreadedScope()
        {
            BuildLog log = new BuildLog();
            using (log.ScopedStep(LogLevel.Info, "TestStep", true))
            {
                var t = new Thread(() =>
                {
                    log.AddEntry(LogLevel.Info, "ThreadedMsg1");
                    using (log.ScopedStep(LogLevel.Info, "ThreadedStep"))
                    {
                        log.AddEntry(LogLevel.Info, "ThreadedMsg2");
                    }
                });
                t.Start();
                t.Join();
            }
            Assert.AreEqual("ThreadedMsg1", log.Root.Children[0].Entries[0].Message);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, log.Root.Children[0].Entries[0].ThreadId);
            Assert.AreEqual("ThreadedStep", log.Root.Children[0].Children[0].Name);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, log.Root.Children[0].Children[0].ThreadId);
            Assert.AreEqual("ThreadedMsg2", log.Root.Children[0].Children[0].Entries[0].Message);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, log.Root.Children[0].Children[0].Entries[0].ThreadId);
        }

        [Test]
        public void WhenBeginAndEndScopeOnThread_StartAndEndTimeAreWithinMainThreadScope()
        {
            BuildLog log = new BuildLog();
            using (log.ScopedStep(LogLevel.Info, "TestStep", true))
            {
                var t = new Thread(() =>
                {
                    Thread.Sleep(1);
                    log.AddEntry(LogLevel.Info, "ThreadedMsg1");
                    Thread.Sleep(1);
                    using (log.ScopedStep(LogLevel.Info, "ThreadedStep"))
                    {
                        Thread.Sleep(2);
                        using (log.ScopedStep(LogLevel.Info, "ThreadedStepNested"))
                            Thread.Sleep(2);
                    }
                    Thread.Sleep(1);
                });
                t.Start();
                t.Join();
            }

            double testStepStart = log.Root.Children[0].StartTime;
            double threadedMessageStart = log.Root.Children[0].Entries[0].Time;
            double threadedScopeStart = log.Root.Children[0].Children[0].StartTime;
            double threadedScopeEnd = threadedScopeStart + log.Root.Children[0].Children[0].DurationMS;
            double threadedScopeNestedStart = log.Root.Children[0].Children[0].Children[0].StartTime;
            double testStepEnd = testStepStart + log.Root.Children[0].DurationMS;

            Assert.Less(threadedScopeStart, threadedScopeNestedStart);
            Assert.Less(testStepStart, threadedMessageStart);
            Assert.Less(threadedMessageStart, threadedScopeStart);
            Assert.Less(threadedScopeStart, threadedScopeEnd);
            Assert.Less(threadedScopeEnd, testStepEnd);
        }

        [Test]
        public void WhenConvertingToTraceEventFormat_BackslashesAreEscaped()
        {
            BuildLog log = new BuildLog();
            using (log.ScopedStep(LogLevel.Info, "TestStep\\AfterSlash"))
                log.AddEntry(LogLevel.Info, "TestEntry\\AfterSlash");
            string text = log.FormatForTraceEventProfiler();
            StringAssert.Contains("TestStep\\\\AfterSlash", text);
            StringAssert.Contains("TestEntry\\\\AfterSlash", text);
        }

        [Test]
        public void WhenConvertingToTraceEventFormat_MetaDataIsAdded()
        {
            BuildLog log = new BuildLog();
            log.AddMetaData("SOMEKEY", "SOMEVALUE");
            string text = log.FormatForTraceEventProfiler();
            StringAssert.Contains("SOMEKEY", text);
            StringAssert.Contains("SOMEVALUE", text);
        }

#if UNITY_2020_2_OR_NEWER || ENABLE_DETAILED_PROFILE_CAPTURING
        [Test]
        public void WhenBeginAndEndDeferredEventsDontMatchUp_HandleDeferredEventsStream_ThrowsException()
        {
            BuildLog log = new BuildLog();
            DeferredEvent startEvent = new DeferredEvent() { Type = DeferredEventType.Begin };
            List<DeferredEvent> events = new List<DeferredEvent>() { startEvent };

            Assert.Throws<Exception>(() => log.HandleDeferredEventStreamInternal(events));
        }

        [Test]
        public void WhenBeginAndEndDeferredEventsMatchUp_HandleDeferredEventsStream_CreatesLogEvents()
        {
            BuildLog log = new BuildLog();
            DeferredEvent startEvent = new DeferredEvent() { Name = "Start", Type = DeferredEventType.Begin };
            DeferredEvent endEvent = new DeferredEvent() { Name = "End", Type = DeferredEventType.End };
            List<DeferredEvent> events = new List<DeferredEvent>() { startEvent, endEvent };

            log.HandleDeferredEventStreamInternal(events);
            Assert.AreEqual(startEvent.Name, log.Root.Children[0].Name);
        }

        [Test]
        public void WhenDeferredEventsAreOnlyInfoTypes_HandleDeferredEventsStream_CreatesLogEntry()
        {
            BuildLog log = new BuildLog();
            DeferredEvent infoEvent = new DeferredEvent() { Name = "Info", Type = DeferredEventType.Info };
            List<DeferredEvent> events = new List<DeferredEvent>() { infoEvent };

            log.HandleDeferredEventStreamInternal(events);
            Assert.AreEqual(infoEvent.Name, log.Root.Entries[0].Message);
        }
#endif
    }
}
