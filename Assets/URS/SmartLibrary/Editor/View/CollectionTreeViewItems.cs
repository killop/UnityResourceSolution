namespace Bewildered.SmartLibrary.UI
{
    /// <summary>
    /// Base <see cref="BTreeViewItem"/> used for all items in a <see cref="LibraryCollectionsView"/>.
    /// </summary>
    internal class LibraryTreeViewItem : BTreeViewItem
    {
        public virtual string Name { get; set; }
        public virtual int Count { get; }

        public LibraryTreeViewItem(int id) : base(id) { }
    }

    /// <summary>
    /// Represents a <see cref="LibraryCollection"/> for a <see cref="BTreeView"/>.
    /// </summary>
    internal class CollectionTreeViewItem : LibraryTreeViewItem
    {
        public LibraryCollection Collection { get; }

        public override string Name
        {
            get { return Collection.CollectionName; }
            set 
            {
                if (value == Collection.CollectionName)
                    return;

                Collection.PrepareChange();
                Collection.CollectionName = value;
                Collection.FinishChange();
            }
        }

        public override int Count
        {
            get { return Collection.Count; }
        }

        public CollectionTreeViewItem(LibraryCollection collection, int id) : base(id)
        {
            Collection = collection;
        }
    }

    /// <summary>
    /// Reperesents <see cref="LibraryDatabase.AllItems"/> for a <see cref="BTreeView"/>. There should only be one in the tree. ID is -1.
    /// </summary>
    internal class AllItemsTreeViewItem : LibraryTreeViewItem
    {
        public override string Name
        {
            get { return "All Library Items"; }
        }

        public override int Count
        {
            get { return LibraryDatabase.AllItems.Count; }
        }


        public AllItemsTreeViewItem() : base(-1) { }
    }
}
