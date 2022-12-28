using System;
using System.Collections.Generic;
using UnityEditor.Build.Content;

namespace UnityEditor.Build.Pipeline.Interfaces
{
    /// <summary>
    /// The extended data about an asset.
    /// </summary>
    [Serializable]
    public class ExtendedAssetData
    {
        /// <summary>
        /// List of object identifiers that are classified as asset representations (sub assets).
        /// </summary>
        public List<ObjectIdentifier> Representations { get; set; }

        /// <summary>
        /// Default constructor, initializes properties to defaults
        /// </summary>
        public ExtendedAssetData()
        {
            Representations = new List<ObjectIdentifier>();
        }
    }

    /// <summary>
    /// Base interface for the storing extended data about an asset.
    /// </summary>
    public interface IBuildExtendedAssetData : IContextObject
    {
        /// <summary>
        /// Map of asset to extended data about an asset.
        /// </summary>
        Dictionary<GUID, ExtendedAssetData> ExtendedData { get; }
    }
}
