using System;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Logs information about the progress tracker.
    /// </summary>
    public class ProgressLoggingTracker : ProgressTracker
    {
        /// <summary>
        /// Creates a new progress tracking object.
        /// </summary>
        public ProgressLoggingTracker()
        {
            BuildLogger.Log(string.Format("[{0}] Progress Tracker Started.", DateTime.Now.ToString()));
        }

        /// <inheritdoc/>
        public override bool UpdateTask(string taskTitle)
        {
            BuildLogger.Log(string.Format("[{0}] {1:P2} Running Task: '{2}'", DateTime.Now.ToString(), Progress.ToString(), taskTitle));
            return base.UpdateTask(taskTitle);
        }

        /// <inheritdoc/>
        public override bool UpdateInfo(string taskInfo)
        {
            BuildLogger.Log(string.Format("[{0}] {1:P2} Running Task: '{2}' Information: '{3}'", DateTime.Now.ToString(), Progress.ToString(), CurrentTaskTitle, taskInfo));
            return base.UpdateInfo(taskInfo);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            BuildLogger.Log(string.Format("[{0}] Progress Tracker Completed.", DateTime.Now.ToString()));
            base.Dispose(disposing);
        }
    }
}
