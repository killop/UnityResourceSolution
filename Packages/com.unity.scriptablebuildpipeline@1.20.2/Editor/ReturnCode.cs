namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Options for scriptable build pipeline return codes. Int values of these return codes are standardized to 0 or greater for Success and -1 or less for Error.
    /// </summary>
    public enum ReturnCode
    {
        // Success Codes are Positive!
        /// <summary>
        /// Use to indicate that the operation suceeded.
        /// </summary>
        Success = 0,
        /// <summary>
        /// Use to indicate that the operation suceeded.
        /// </summary>
        SuccessCached = 1,
        /// <summary>
        /// Use to indicate that the operation suceeded but did not actually execute.
        /// </summary>
        SuccessNotRun = 2,
        // Error Codes are Negative!
        /// <summary>
        /// Use to indicate that the operation encountered an error.
        /// </summary>
        Error = -1,
        /// <summary>
        /// Use to indicate that the operation encountered an exception.
        /// </summary>
        Exception = -2,
        /// <summary>
        /// Use to indicate that the operation was cancelled.
        /// </summary>
        Canceled = -3,
        /// <summary>
        /// Use to indicate that the operation failed because there are unsaved scene changes.
        /// </summary>
        UnsavedChanges = -4,
        /// <summary>
        /// Use to indicate that the operation failed because it was missing the required objects.
        /// </summary>
        MissingRequiredObjects = -5
    }
}
