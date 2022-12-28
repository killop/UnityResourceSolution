using System.Diagnostics;

namespace UnityEditor.Build.Pipeline.Utilities
{
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Logging overrides for SBP build logs.
    /// </summary>
    public static class BuildLogger
    {
        /// <summary>
        /// Logs build cache information.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        /// <param name="attrs">The objects formatted in the message.</param>
        [Conditional("BUILD_CACHE_DEBUG")]
        public static void LogCache(string msg, params object[] attrs)
        {
            Log(msg, attrs);
        }

        /// <summary>
        /// Logs a warning about the build cache.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        /// <param name="attrs">The objects formatted in the message.</param>
        [Conditional("BUILD_CACHE_DEBUG")]
        public static void LogCacheWarning(string msg, params object[] attrs)
        {
            LogWarning(msg, attrs);
        }

        /// <summary>
        /// Logs general information.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        [Conditional("DEBUG")]
        public static void Log(string msg)
        {
            Debug.Log(msg);
        }

        /// <summary>
        /// Logs general information.
        /// </summary>
        /// <param name="msg">The message object to display.</param>
        [Conditional("DEBUG")]
        public static void Log(object msg)
        {
            Debug.Log(msg);
        }

        /// <summary>
        /// Logs general information.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        /// <param name="attrs">The objects formatted in the message.</param>
        [Conditional("DEBUG")]
        public static void Log(string msg, params object[] attrs)
        {
            Debug.Log(string.Format(msg, attrs));
        }

        /// <summary>
        /// Logs a general warning.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        [Conditional("DEBUG")]
        public static void LogWarning(string msg)
        {
            Debug.LogWarning(msg);
        }

        /// <summary>
        /// Logs a general warning.
        /// </summary>
        /// <param name="msg">The message object to display.</param>
        [Conditional("DEBUG")]
        public static void LogWarning(object msg)
        {
            Debug.LogWarning(msg);
        }

        /// <summary>
        /// Logs a general warning.
        /// </summary>
        /// <param name="msg">The message object to display.</param>
        /// <param name="attrs">The objects formatted in the message.</param>
        [Conditional("DEBUG")]
        public static void LogWarning(string msg, params object[] attrs)
        {
            Debug.LogWarning(string.Format(msg, attrs));
        }

        /// <summary>
        /// Logs a general error.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        [Conditional("DEBUG")]
        public static void LogError(string msg)
        {
            Debug.LogError(msg);
        }

        /// <summary>
        /// Logs a general error.
        /// </summary>
        /// <param name="msg">The message object to display.</param>
        [Conditional("DEBUG")]
        public static void LogError(object msg)
        {
            Debug.LogError(msg);
        }

        /// <summary>
        /// Logs a general error.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        /// <param name="attrs">The objects formatted in the message.</param>
        [Conditional("DEBUG")]
        public static void LogError(string msg, params object[] attrs)
        {
            Debug.LogError(string.Format(msg, attrs));
        }

        /// <summary>
        /// Logs a general exception.
        /// </summary>
        /// <param name="e">The exception to display.</param>
        [Conditional("DEBUG")]
        public static void LogException(System.Exception e)
        {
            Debug.LogException(e);
        }
    }
}
