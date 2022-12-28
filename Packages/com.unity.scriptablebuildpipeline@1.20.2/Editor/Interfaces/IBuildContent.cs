using System;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Tasks;

namespace UnityEditor.Build.Pipeline.Interfaces
{
#if UNITY_2019_3_OR_NEWER
    /// <summary>
    /// Custom Content struct mapping a source asset to a processor to generate custom data for that asset.
    /// </summary>
    [Serializable]
    public struct CustomContent : IEquatable<CustomContent>
    {
        /// <summary>
        /// Input Asset for custom content
        /// </summary>
        public GUID Asset { get; set; }

        /// <summary>
        /// Processor function to run to convert the input asset to the custom content
        /// </summary>
        public Action<GUID, CalculateCustomDependencyData> Processor;

        /// <summary>
        /// IEquatable<CustomContent> Equals operator to handle generic collections
        /// </summary>
        /// <param name="other">Other CustomContent object to compare against.</param>
        /// <returns></returns>
        public bool Equals(CustomContent other)
        {
            return Asset == other.Asset && Processor == other.Processor;
        }
    }

    /// <summary>
    /// Base interface for storing the list of Custom Assets generated during the Scriptable Build Pipeline.
    /// </summary>
    public interface ICustomAssets : IContextObject
    {
        /// <summary>
        /// List of Custom Assets to include.
        /// </summary>
        List<GUID> Assets { get; }
    }
#endif

    /// <summary>
    /// Base interface for feeding Assets to the Scriptable Build Pipeline.
    /// </summary>
    public interface IBuildContent : IContextObject
    {
        /// <summary>
        /// List of Assets to include.
        /// </summary>
        List<GUID> Assets { get; }

        /// <summary>
        /// List of Scenes to include.
        /// </summary>
        List<GUID> Scenes { get; }

#if UNITY_2019_3_OR_NEWER
        /// <summary>
        /// List of custom content to be included in asset bundles.
        /// </summary>
        List<CustomContent> CustomAssets { get; }
#endif
    }

    /// <summary>
    /// Base interface for feeding Assets with explicit Asset Bundle layout to the Scriptable Build Pipeline.
    /// </summary>
    public interface IBundleBuildContent : IBuildContent
    {
        /// <summary>
        /// Specific layout of asset bundles to assets or scenes.
        /// </summary>
        Dictionary<string, List<GUID>> BundleLayout { get; }

#if UNITY_2019_3_OR_NEWER
        /// <summary>
        /// Additional list of raw files to add to an asset bundle
        /// </summary>
        Dictionary<string, List<ResourceFile>> AdditionalFiles { get; }
#endif

        /// <summary>
        /// Custom loading identifiers to use for Assets or Scenes.
        /// </summary>
        Dictionary<GUID, string> Addresses { get; }
    }
}
