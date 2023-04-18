#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Collections
{
    /// <summary>Interface for matching objects in an <see cref="IStore{T}"/>.</summary>
    /// <typeparam name="T">The contravariant type of selectable objects.</typeparam>
    public interface ISelector<in T>
        : ICloneable
    {
        /// <summary>Match the passed in object, returning true if it would be selected by this selector, false
        /// otherwise.</summary>
        /// <param name="candidate">The object to be matched.</param>
        /// <returns><code>true</code> if the objects is matched by this selector, false otherwise.</returns>
        bool Match(T candidate);
    }
}
#pragma warning restore
#endif
