namespace Bewildered.SmartLibrary
{
    public enum HierarchyChangeType { Added, Removed, Moved }

    public readonly struct LibraryHierarchyChangedEventArgs
    {
        /// <summary>
        /// The <see cref="LibraryCollection"/> that was changed
        /// </summary>
        public readonly LibraryCollection subcollection;

        /// <summary>
        /// The parent <see cref="LibraryCollection"/> of the <see cref="LibraryCollection"/> that was changed.
        /// </summary>
        public readonly LibraryCollection collection;

        /// <summary>
        /// Returns the type of change the happened.
        /// </summary>
        public readonly HierarchyChangeType type;

        /// <summary>
        /// Returns the index of the subcollection that changed. Returns the <see cref="LibraryCollection"/>'s previus index if <see cref="HierarchyChangeType.Moved"/>, and -1 if <see cref="HierarchyChangeType.Removed"/>.
        /// </summary>
        public readonly int index;

        public LibraryHierarchyChangedEventArgs(LibraryCollection subcollection, LibraryCollection collection, int index, HierarchyChangeType changeType)
        {
            this.subcollection = subcollection;
            this.collection = collection;
            type = changeType;
            this.index = index;
        }
    }
}
