#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Generates a deterministic identifier using a MD5 hash algorithm and does not require object ordering to be deterministic.
    /// This algorithm ensures objects coming from the same asset are packed closer together and can improve loading performance under certain situations.
    /// Sorts MonoScript types to the top of the file and is required when building ContentFiles.
    /// </summary>
    public class ContentFileIdentifiers : PrefabPackedIdentifiers, IDeterministicIdentifiers
    {
        /// <inheritdoc />
        public override long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            long result = base.SerializationIndexFromObjectIdentifier(objectID);
            long mask = ((long)1 << 63);
            if (BuildCacheUtility.GetMainTypeForObject(objectID) == typeof(MonoScript))
                result |= mask;
            else
                result &= ~mask;

            return result;
        }
    }
}
#endif
