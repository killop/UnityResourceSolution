using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Basic implementation of IBuildLogger. Stores events in memory and can dump them to the trace event format.
    /// <seealso cref="IBuildLogger"/>
    /// </summary>
    [Serializable]
    public class BuildLog : IBuildLogger, IDeferredBuildLogger
    {
        [Serializable]
        internal struct LogEntry
        {
            public int ThreadId { get; set; }
            public double Time { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; }
        }

        [Serializable]
        internal class LogStep
        {
            List<LogStep> m_Children;
            List<LogEntry> m_Entries;

            public string Name { get; set; }
            public LogLevel Level { get; set; }
            public List<LogStep> Children { get { if (m_Children == null) m_Children = new List<LogStep>(); return m_Children; } }
            public List<LogEntry> Entries { get { if (m_Entries == null) m_Entries = new List<LogEntry>(); return m_Entries; } }
            public double DurationMS { get; private set; }
            public int ThreadId { get; set; }
            public double StartTime { get; set; }
            internal bool isThreaded;

            public bool HasChildren { get { return Children != null && Children.Count > 0; } }
            public bool HasEntries { get { return Entries != null && Entries.Count > 0; } }

            internal void Complete(double time)
            {
                DurationMS = time - StartTime;
            }
        }

        LogStep m_Root;
        [NonSerialized]
        Stack<LogStep> m_Stack;
        [NonSerialized]
        ThreadLocal<BuildLog> m_ThreadedLogs;
        [NonSerialized]
        Stopwatch m_WallTimer;

        bool m_ShouldOverrideWallTimer;
        double m_WallTimerOverride;

        double GetWallTime()
        {
            return m_ShouldOverrideWallTimer ? m_WallTimerOverride : m_WallTimer.Elapsed.TotalMilliseconds;
        }

        void Init(bool onThread)
        {
            m_WallTimer = Stopwatch.StartNew();
            m_Root = new LogStep();
            m_Stack = new Stack<LogStep>();
            m_Stack.Push(m_Root);

            AddMetaData("Date", DateTime.Now.ToString());

            if (!onThread)
            {
                AddMetaData("UnityVersion", UnityEngine.Application.unityVersion);
#if UNITY_2019_2_OR_NEWER // PackageManager package inspection APIs didn't exist until 2019.2
                PackageManager.PackageInfo info = PackageManager.PackageInfo.FindForAssembly(typeof(BuildLog).Assembly);
                if (info != null)
                    AddMetaData(info.name, info.version);
#endif
            }
        }

        /// <summary>
        /// Creates a new build log object.
        /// </summary>
        public BuildLog()
        {
            Init(false);
        }

        internal BuildLog(bool onThread)
        {
            Init(onThread);
        }

        private BuildLog GetThreadSafeLog()
        {
            if (m_ThreadedLogs != null)
            {
                if (!m_ThreadedLogs.IsValueCreated)
                    m_ThreadedLogs.Value = new BuildLog(true);
                return m_ThreadedLogs.Value;
            }
            return this;
        }

        /// <inheritdoc />
        public void BeginBuildStep(LogLevel level, string stepName, bool multiThreaded)
        {
            BuildLog log = GetThreadSafeLog();
            BeginBuildStepInternal(log, level, stepName, multiThreaded);
        }

        private static void BeginBuildStepInternal(BuildLog log, LogLevel level, string stepName, bool multiThreaded)
        {
            LogStep node = new LogStep();
            node.Level = level;
            node.Name = stepName;
            node.StartTime = log.GetWallTime();
            node.ThreadId = Thread.CurrentThread.ManagedThreadId;
            log.m_Stack.Peek().Children.Add(node);
            log.m_Stack.Push(node);
            if (multiThreaded)
            {
                Debug.Assert(log.m_ThreadedLogs == null);
                log.m_ThreadedLogs = new ThreadLocal<BuildLog>(true);
                log.m_ThreadedLogs.Value = log;
                node.isThreaded = true;
            }
        }

        /// <inheritdoc />
        public void EndBuildStep()
        {
            EndBuildStepInternal(GetThreadSafeLog());
        }

        private static void OffsetTimesR(LogStep step, double offset)
        {
            step.StartTime += offset;
            if (step.HasEntries)
            {
                for (int i = 0; i < step.Entries.Count; i++)
                {
                    LogEntry e = step.Entries[i];
                    e.Time = e.Time + offset;
                    step.Entries[i] = e;
                }
            }
            if (step.HasChildren)
                foreach (LogStep subStep in step.Children)
                    OffsetTimesR(subStep, offset);
        }

        private static void EndBuildStepInternal(BuildLog log)
        {
            Debug.Assert(log.m_Stack.Count > 1);
            LogStep node = log.m_Stack.Pop();
            node.Complete(log.GetWallTime());

            if (node.isThreaded)
            {
                foreach (BuildLog subLog in log.m_ThreadedLogs.Values)
                {
                    if (subLog != log)
                    {
                        OffsetTimesR(subLog.Root, node.StartTime);
                        if (subLog.Root.HasChildren)
                            node.Children.AddRange(subLog.Root.Children);

                        if (subLog.Root.HasEntries)
                            node.Entries.AddRange(subLog.Root.Entries);
                    }
                }
                log.m_ThreadedLogs.Dispose();
                log.m_ThreadedLogs = null;
            }
        }

        internal LogStep Root { get { return m_Root; } }

        /// <inheritdoc />
        public void AddEntry(LogLevel level, string msg)
        {
            BuildLog log = GetThreadSafeLog();
            log.m_Stack.Peek().Entries.Add(new LogEntry() { Level = level, Message = msg, Time = log.GetWallTime(), ThreadId = Thread.CurrentThread.ManagedThreadId });
        }

        /// <summary>
        /// Internal use only.
        /// <seealso cref="IBuildLogger"/>
        /// </summary>
        /// <param name="events">Event collection to handle</param>
        void IDeferredBuildLogger.HandleDeferredEventStream(IEnumerable<DeferredEvent> events)
        {
            HandleDeferredEventStreamInternal(events);
        }

        internal void HandleDeferredEventStreamInternal(IEnumerable<DeferredEvent> events)
        {
            // now make all those times relative to the active event
            LogStep startStep = m_Stack.Peek();

            m_ShouldOverrideWallTimer = true;
            foreach (DeferredEvent e in events)
            {
                m_WallTimerOverride = e.Time + startStep.StartTime;
                if (e.Type == DeferredEventType.Begin)
                {
                    BeginBuildStep(e.Level, e.Name, false);
                    if (!string.IsNullOrEmpty(e.Context))
                        AddEntry(e.Level, e.Context);
                }
                else if (e.Type == DeferredEventType.End)
                    EndBuildStep();
                else
                    AddEntry(e.Level, e.Name);
            }
            m_ShouldOverrideWallTimer = false;

            LogStep stopStep = m_Stack.Peek();
            if (stopStep != startStep)
                throw new Exception("Deferred events did not line up as expected");
        }

        static void AppendLineIndented(StringBuilder builder, int indentCount, string text)
        {
            for (int i = 0; i < indentCount; i++)
                builder.Append(" ");
            builder.AppendLine(text);
        }

        static void PrintNodeR(bool includeSelf, StringBuilder builder, int indentCount, BuildLog.LogStep node)
        {
            if (includeSelf)
                AppendLineIndented(builder, indentCount, $"[{node.Name}] {node.DurationMS * 1000}us");
            foreach (var msg in node.Entries)
            {
                string line = (msg.Level == LogLevel.Warning || msg.Level == LogLevel.Error) ? $"{msg.Level}: {msg.Message}" : msg.Message;
                AppendLineIndented(builder, indentCount + 1, line);
            }
            foreach (var child in node.Children)
                PrintNodeR(true, builder, indentCount + 1, child);
        }

        internal string FormatAsText()
        {
            using (new CultureScope())
            {
                StringBuilder builder = new StringBuilder();
                PrintNodeR(false, builder, -1, Root);
                return builder.ToString();
            }
        }

        static string CleanJSONText(string message)
        {
            return message.Replace("\\", "\\\\");
        }

        static IEnumerable<string> IterateTEPLines(bool includeSelf, BuildLog.LogStep node)
        {
            ulong us = (ulong)(node.StartTime * 1000);

            string argText = string.Empty;
            if (node.Entries.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(", \"args\": {");
                for (int i = 0; i < node.Entries.Count; i++)
                {
                    string line = (node.Entries[i].Level == LogLevel.Warning || node.Entries[i].Level == LogLevel.Error) ? $"{node.Entries[i].Level}: {node.Entries[i].Message}" : node.Entries[i].Message;
                    builder.Append($"\"{i}\":\"{CleanJSONText(line)}\"");
                    if (i < (node.Entries.Count - 1))
                        builder.Append(", ");
                }
                builder.Append("}");
                argText = builder.ToString();
            }

            if (includeSelf)
                yield return "{" + $"\"name\": \"{CleanJSONText(node.Name)}\", \"ph\": \"X\", \"dur\": {node.DurationMS * 1000}, \"tid\": {node.ThreadId}, \"ts\": {us}, \"pid\": 1" + argText + "}";

            foreach (var child in node.Children)
                foreach (var r in IterateTEPLines(true, child))
                    yield return r;
        }

        class CultureScope : IDisposable
        {
            CultureInfo m_Prev;
            public CultureScope()
            {
                m_Prev = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            public void Dispose()
            {
                Thread.CurrentThread.CurrentCulture = m_Prev;
            }
        }

        private List<Tuple<string, string>> m_MetaData = new List<Tuple<string, string>>();

        /// <summary>
        /// Adds a key value pair to the MetaData list. This can be used to store things like package version numbers.
        /// </summary>
        /// <param name="key">The key for the MetaData.</param>
        /// <param name="value">The value of the MetaData.</param>
        public void AddMetaData(string key, string value)
        {
            m_MetaData.Add(new Tuple<string, string>(key, value));
        }

        /// <summary>
        /// Converts the captured build log events into the text Trace Event Profiler format
        /// </summary>
        /// <returns>Profile data.</returns>
        public string FormatForTraceEventProfiler()
        {
            using (new CultureScope())
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("{");

                foreach (Tuple<string, string> tuple in m_MetaData)
                    builder.AppendLine($"\"{tuple.Item1}\": \"{tuple.Item2}\",");

                builder.AppendLine("\"traceEvents\": [");
                int i = 0;
                foreach (string line in IterateTEPLines(false, Root))
                {
                    if (i != 0)
                        builder.Append(",");
                    builder.AppendLine(line);
                    i++;
                }
                builder.AppendLine("]");
                builder.AppendLine("}");
                return builder.ToString();
            }
        }
    }
}
