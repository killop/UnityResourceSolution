using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Keeps track of the SBP build progress.
    /// </summary>
    public class ProgressTracker : IProgressTracker, IDisposable
    {
        const long k_TicksPerSecond = 10000000;

        /// <summary>
        /// Stores the number of tasks
        /// </summary>
        public int TaskCount { get; set; }

        /// <summary>
        /// Stores the amount of progress done as a decimal.
        /// </summary>
        public float Progress { get { return CurrentTask / (float)TaskCount; } }

        /// <summary>
        /// Stores the amount of updates per second.
        /// </summary>
        public uint UpdatesPerSecond
        {
            get { return (uint)(k_TicksPerSecond / UpdateFrequency); }
            set { UpdateFrequency = k_TicksPerSecond / Math.Max(value, 1); }
        }

        bool m_Disposed = false;

        /// <summary>
        /// Stores the id of currently running task.
        /// </summary>
        protected int CurrentTask { get; set; }

        /// <summary>
        /// Stores the name of the currently running task.
        /// </summary>
        protected string CurrentTaskTitle { get; set; }

        /// <summary>
        /// Stores current the time stamp.
        /// </summary>
        protected long TimeStamp { get; set; }

        /// <summary>
        /// Stores the task update frequency.
        /// </summary>
        protected long UpdateFrequency { get; set; }

        /// <summary>
        /// Stores information about the current task.
        /// </summary>
        public ProgressTracker()
        {
            CurrentTask = 0;
            CurrentTaskTitle = "";
            TimeStamp = 0;
            UpdateFrequency = k_TicksPerSecond / 100;
        }

        /// <summary>
        /// Updates the progress bar to reflect the new running task.
        /// </summary>
        /// <param name="taskTitle">The name of the new task.</param>
        /// <returns>Returns true if the progress bar is running. Returns false if the user cancels the progress bar.</returns>
        public virtual bool UpdateTask(string taskTitle)
        {
            CurrentTask++;
            CurrentTaskTitle = taskTitle;
            TimeStamp = 0;
            return !EditorUtility.DisplayCancelableProgressBar(CurrentTaskTitle, "", Progress);
        }

        /// <summary>
        /// Updates the information displayed for currently running task.
        /// </summary>
        /// <param name="taskInfo">The task information.</param>
        /// <returns>Returns true if the progress bar is running. Returns false if the user cancels the progress bar.</returns>
        public virtual bool UpdateInfo(string taskInfo)
        {
            var currentTicks = DateTime.Now.Ticks;
            if (currentTicks - TimeStamp < UpdateFrequency)
                return true;

            TimeStamp = currentTicks;
            return !EditorUtility.DisplayCancelableProgressBar(CurrentTaskTitle, taskInfo, Progress);
        }

        /// <summary>
        /// Disposes of the progress tracker instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the progress tracker instance and clears the popup progress bar.
        /// </summary>
        /// <param name="disposing">Set to true to clear the popup progress bar. Set to false to leave the progress bar as is.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
                EditorUtility.ClearProgressBar();

            m_Disposed = true;
        }
    }
}
