using System.Collections.Generic;

namespace Bewildered.SmartLibrary
{
    public enum LibraryItemsChangeType { Added, Removed }

    public readonly struct LibraryItemsChangedEventArgs
    {
        /// <summary>
        /// The <see cref="LibraryItem"/>s that were changed.
        /// </summary>
        public readonly IReadOnlyCollection<LibraryItem> items;

        /// <summary>
        /// The type of change the occurred.
        /// </summary>
        public readonly LibraryItemsChangeType type;

        /// <summary>
        /// The <see cref="LibraryCollection"/> where the items changed.
        /// </summary>
        public readonly LibraryCollection collection;

        public LibraryItemsChangedEventArgs(IReadOnlyCollection<LibraryItem> items, LibraryCollection collection, LibraryItemsChangeType type)
        {
            this.items = items;
            this.collection = collection;
            this.type = type;
        }
    }
}