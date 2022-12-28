using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Basic implementation of IBuildSpriteData. Stores the sprite importer data for a sprite asset in the build.
    /// <seealso cref="IBuildSpriteData"/>
    /// </summary>
    [Serializable]
    public class BuildSpriteData : IBuildSpriteData
    {
        /// <inheritdoc />
        public Dictionary<GUID, SpriteImporterData> ImporterData { get; set; }

        /// <summary>
        /// Default constructor, initializes properties to defaults
        /// </summary>
        public BuildSpriteData()
        {
            ImporterData = new Dictionary<GUID, SpriteImporterData>();
        }
    }
}
