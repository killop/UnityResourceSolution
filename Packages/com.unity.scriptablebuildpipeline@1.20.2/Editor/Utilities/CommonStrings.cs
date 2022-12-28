namespace UnityEditor.Build.Utilities
{
    /// <summary>
    /// Static class of common strings and string formats used through out the build process
    /// </summary>
    public static class CommonStrings
    {
        /// <summary>
        /// Unity Editor Resources path
        /// </summary>
        public const string UnityEditorResourcePath = "library/unity editor resources";

        internal const string UnityEditorResourceGuid = "0000000000000000d000000000000000";

        /// <summary>
        /// Unity Default Resources path
        /// </summary>
        public const string UnityDefaultResourcePath = "library/unity default resources";

        internal const string UnityDefaultResourceGuid = "0000000000000000e000000000000000";

        /// <summary>
        /// Unity Built-In Extras path
        /// </summary>
        public const string UnityBuiltInExtraPath = "resources/unity_builtin_extra";

        internal const string UnityBuiltInExtraGuid = "0000000000000000f000000000000000";

        /// <summary>
        /// Default Asset Bundle internal file name format
        /// </summary>
        public const string AssetBundleNameFormat = "archive:/{0}/{0}";

        /// <summary>
        /// Default Scene Bundle internal file name format
        /// </summary>
        public const string SceneBundleNameFormat = "archive:/{0}/{1}.sharedAssets";
    }
}
