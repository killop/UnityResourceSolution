using System;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Build.Pipeline;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Additional MetaData that is associated with a serialized file write result
    /// <seealso cref="IBuildResults"/>
    /// </summary>
    [Serializable]
    public class SerializedFileMetaData
    {
        /// <summary>
        /// A hash of all the serialized files
        /// </summary>
        public Hash128 RawFileHash;
        /// <summary>
        /// Hash of file contents. Some resource files may choose to exclude sections of their content from this hash. For example,
        /// serialized files exclude the header of their content which allows this hash not to change with new Unity versions.
        /// </summary>
        public Hash128 ContentHash;
    }

    /// <summary>
    /// Basic implementation of IBuildResults. Stores the results for script compilation and content building.
    /// <seealso cref="IBuildResults"/>
    /// </summary>
    [Serializable]
    public class BuildResults : IBuildResults
    {
        /// <inheritdoc />
        public ScriptCompilationResult ScriptResults { get; set; }
        /// <inheritdoc />
        public Dictionary<string, WriteResult> WriteResults { get; private set; }
        /// <inheritdoc />
        public Dictionary<string, SerializedFileMetaData> WriteResultsMetaData { get; private set; }

        /// <summary>
        /// Default constructor, initializes properties to defaults
        /// </summary>
        public BuildResults()
        {
            WriteResults = new Dictionary<string, WriteResult>();
        }
    }

    /// <summary>
    /// Basic implementation of IBundleBuildResults. Stores the results for script compilation and asset bundle building.
    /// <seealso cref="IBundleBuildResults"/>
    /// </summary>
    [Serializable]
    public class BundleBuildResults : IBundleBuildResults
    {
        /// <inheritdoc />
        public ScriptCompilationResult ScriptResults { get; set; }
        /// <inheritdoc />
        public Dictionary<string, BundleDetails> BundleInfos { get; private set; }
        /// <inheritdoc />
        public Dictionary<string, WriteResult> WriteResults { get; private set; }
        /// <inheritdoc />
        public Dictionary<string, SerializedFileMetaData> WriteResultsMetaData { get; private set; }

        /// <summary>
        /// Default constructor, initializes properties to defaults
        /// </summary>
        public BundleBuildResults()
        {
            BundleInfos = new Dictionary<string, BundleDetails>();
            WriteResults = new Dictionary<string, WriteResult>();
            WriteResultsMetaData = new Dictionary<string, SerializedFileMetaData>();
        }
    }
}
