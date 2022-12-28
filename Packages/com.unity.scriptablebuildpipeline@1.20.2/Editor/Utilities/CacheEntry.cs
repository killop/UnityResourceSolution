using System;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    interface ICachedData {}

    /// <summary>
    /// Stores asset information for the cache.
    /// </summary>
    [Serializable]
    public class CachedInfo : ICachedData
    {
        /// <summary>
        /// Stores the asset.
        /// </summary>
        public CacheEntry Asset { get; set; }

        /// <summary>
        /// Stores the asset dependencies.
        /// </summary>
        public CacheEntry[] Dependencies { get; set; }

        /// <summary>
        /// Stores extra data related to the asset.
        /// </summary>
        public object[] Data { get; set; }
    }

    /// <summary>
    /// Creates a container to store data in build cache.
    /// </summary>
    [Serializable]
    public struct CacheEntry : IEquatable<CacheEntry>
    {
        /// <summary>
        /// Options for the cache entry type.
        /// </summary>
        public enum EntryType
        {
            /// <summary>
            /// Indicates that the entry is an asset.
            /// </summary>
            Asset,
            /// <summary>
            /// Indicates that the entry is a file.
            /// </summary>
            File,
            /// <summary>
            /// Indicates that the entry holds general data.
            /// </summary>
            Data,
            /// <summary>
            /// Indicates that the entry is a type.
            /// </summary>
            ScriptType
        }

        /// <summary>
        /// Stores the entry hash.
        /// </summary>
        public Hash128 Hash { get; internal set; }

        /// <summary>
        /// Stores the entry guid.
        /// </summary>
        public GUID Guid { get; internal set; }

        /// <summary>
        /// Stores the entry version.
        /// </summary>
        public int Version { get; internal set; }

        /// <summary>
        /// Stores the entry type.
        /// </summary>
        public EntryType Type { get; internal set; }

        /// <summary>
        /// Stores the entry file name.
        /// </summary>
        public string File { get; internal set; }

        /// <summary>
        /// Stores the entry scripting type.
        /// </summary>
        public string ScriptType { get; internal set; }

        /// <summary>
        /// Determines if the entry is valid.
        /// </summary>
        /// <returns>Returns true if the entry is valid. Returns false otherwise.</returns>
        public bool IsValid()
        {
            return Hash.isValid && !Guid.Empty();
        }

        /// <summary>
        /// Determines if the current entry instance is equal to the specified entry.
        /// </summary>
        /// <param name="obj">The entry to compare.</param>
        /// <returns>Returns true if the entries are equivalent. Returns false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is CacheEntry && Equals((CacheEntry)obj);
        }

        /// <inheritdoc/>
        public static bool operator==(CacheEntry x, CacheEntry y)
        {
            return x.Equals(y);
        }

        /// <inheritdoc/>
        public static bool operator!=(CacheEntry x, CacheEntry y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Creates the hash code for the cache entry.
        /// </summary>
        /// <returns>Returns the hash code for the cache entry.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Hash.GetHashCode();
                hashCode = (hashCode * 397) ^ Guid.GetHashCode();
                hashCode = (hashCode * 397) ^ Version;
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (File != null ? File.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ScriptType != null ? ScriptType.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the information about the cache entry to a formatted string.
        /// </summary>
        /// <returns>Returns information about the cache entry.</returns>
        public override string ToString()
        {
            if (Type == EntryType.File)
                return string.Format("({0}, {1})", File, Hash);
            if (Type == EntryType.ScriptType)
                return string.Format("({0}, {1})", ScriptType, Hash);
            return string.Format("({0}, {1})", Guid, Hash);
        }

        /// <summary>
        /// Determines if the current entry instance is equal to the specified entry.
        /// </summary>
        /// <param name="other">The entry to compare.</param>
        /// <returns>Returns true if the entries are equivalent. Returns false otherwise.</returns>
        public bool Equals(CacheEntry other)
        {
            return Hash.Equals(other.Hash) && Guid.Equals(other.Guid) && Version == other.Version && Type == other.Type && string.Equals(File, other.File) && string.Equals(ScriptType, other.ScriptType);
        }
    }
}
