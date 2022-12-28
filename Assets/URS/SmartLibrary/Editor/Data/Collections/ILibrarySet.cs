using System.Collections.Generic;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Defines methods to manipulate the items of a <see cref="LibraryCollection"/>.
    /// </summary>
    public interface ILibrarySet
    {
        /// <summary>
        /// The number of <see cref="LibraryItem"/>s that the <see cref="ILibrarySet"/> contains.
        /// </summary>
        public int Count { get; }
        
        /// <summary>
        /// Adds the specified <see cref="LibraryItem"/> to the set and returns a value to indicate if the <see cref="LibraryItem"/> was successfully added.
        /// </summary>
        /// <param name="item">The <see cref="LibraryItem"/> to add to the set.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is added to the set; otherwise <c>false</c>.</returns>
        bool Add(LibraryItem item);

        /// <summary>
        /// Removes the specified item from the set.
        /// </summary>
        /// <param name="item">The <see cref="LibraryItem"/> to remove.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is successfully found and removed; otherwise, <c>false</c>.</returns>
        bool Remove(LibraryItem item);

        /// <summary>
        /// Adds all the objects from the specified collection to the set.
        /// </summary>
        /// <param name="other">The collection to compare to the set.</param>
        void UnionWith(IEnumerable<Object> other);

        void UnionWith(IEnumerable<LibraryItem> other);

        /// <summary>
        /// Removes all items in the specified collection from the set.
        /// </summary>
        /// <param name="other">The collection of items to remove from the set.</param>
        void ExceptWith(IEnumerable<LibraryItem> other);
    } 
}
