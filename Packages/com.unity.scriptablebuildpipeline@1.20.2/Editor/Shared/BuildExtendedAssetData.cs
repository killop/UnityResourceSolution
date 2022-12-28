using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Basic implementation of IBuildExtendedAssetData. Stores the extended data about an asset in the build.
    /// <seealso cref="IBuildExtendedAssetData"/>
    /// </summary>
    [Serializable]
    public class BuildExtendedAssetData : IBuildExtendedAssetData
    {
        /// <inheritdoc />
        public Dictionary<GUID, ExtendedAssetData> ExtendedData { get; private set; }

        /// <summary>
        /// Default constructor, initializes properties to defaults
        /// </summary>
        public BuildExtendedAssetData()
        {
            ExtendedData = new Dictionary<GUID, ExtendedAssetData>();
        }
    }
}
