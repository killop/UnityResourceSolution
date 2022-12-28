using System.Collections.Generic;

namespace Bewildered.SmartLibrary.UI
{
    internal class BTreeViewItem
    {
        private BTreeViewItem _parent;
        private List<BTreeViewItem> _children;

        public int Id { get; }

        public BTreeViewItem Parent
        {
            get { return _parent; }
        }

        public IEnumerable<BTreeViewItem> Children
        {
            get { return _children; }
        }

        public bool HasChildren
        {
            get { return _children != null && _children.Count > 0; }
        }

        public int ChildCount
        {
            get { return _children != null ? _children.Count : 0; }
        }

        public BTreeViewItem(int id)
        {
            Id = id;
        }

        public void AddChild(BTreeViewItem child)
        {
            if (child == null)
                return;

            if (_children == null)
                _children = new List<BTreeViewItem>();

            _children.Add(child);
            child._parent = this;
        }

        public void AddChildren(IEnumerable<BTreeViewItem> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }

        public void RemoveChild(BTreeViewItem child)
        {
            if (child == null)
                return;

            if (_children == null)
                return;

            _children.Remove(child);
        }

        public int GetSiblingIndex()
        {
            if (_parent == null)
                return -1;

            for (int i = 0; i < _parent._children.Count; i++)
            {
                if (_parent._children[i] == this)
                    return i;
            }

            return -1;
        }
    }
}
