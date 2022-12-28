using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Gerneral purpose <see cref="LibraryCollection"/> that allows manually adding, and removing <see cref="LibraryItem"/>s.
    /// </summary>
    [System.Serializable]
    public class StandardCollection : LibraryCollection, ILibrarySet
    {
        /// <inheritdoc/>
        public override void UpdateItems(bool syn= false)
        {
            List<LibraryItem> invalidItems = new List<LibraryItem>();
            foreach (var item in this)
            {
                if (!Rules.Evaluate(item))
                {
                    invalidItems.Add(item);
                }
                else if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(item.GUID)))
                {
                    Debug.LogWarning("invalid guid:"+ item.GUID);
                    invalidItems.Add(item);
                }
            }

            ExceptWith(invalidItems);
        }

        /// <inheritdoc/>
        public bool Add(LibraryItem item)
        {
            return Add(item, false);
        }

        protected virtual bool Add(LibraryItem item, bool partOfCollection)
        {
            if (!IsAddable(item))
                return false;

            if (!partOfCollection)
                PrepareItemChange();

            // If item is a folder, add it's contents instead of itself.
            if (!Path.HasExtension(item.AssetPath))
                UnionWith(AssetUtility.LoadAllAtPath(item.AssetPath));
            else
                AddItem(item, !partOfCollection);
            
            if (!partOfCollection)
                FinishChange();

            return true;
        }

        /// <inheritdoc/>
        public bool Remove(LibraryItem item)
        {
            return Remove(item, false);
        }

        protected bool Remove(LibraryItem item, bool partOfCollection)
        {
            if (Contains(item))
            {
                if (!partOfCollection)
                    PrepareItemChange();

                RemoveItem(item, !partOfCollection);
                
                if (!partOfCollection)
                    FinishChange();
                
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public void UnionWith(IEnumerable<Object> other)
        {
            if (other == null)
                throw new System.ArgumentNullException(nameof(other));

            int undoGroup = PrepareItemChange();

            var addedItems = new List<LibraryItem>();
            foreach (var obj in other)
            {
                var item = LibraryItem.GetItemInstance(obj);
                if (Add(item, true) && Path.HasExtension(item.AssetPath))
                    addedItems.Add(item);
            }
            FinishChange(undoGroup);

            if (addedItems.Count > 0)
                NotifyItemsChanged(addedItems, LibraryItemsChangeType.Added);
        }

        /// <inheritdoc/>
        public void UnionWith(IEnumerable<LibraryItem> other)
        {
            if (other == null)
                throw new System.ArgumentNullException(nameof(other));

            int undoGroup = PrepareItemChange();
            

            var addedItems = new List<LibraryItem>();
            foreach (var item in other)
            {
                if (Add(item, true) && Path.HasExtension(item.AssetPath))
                    addedItems.Add(item);
            }
            FinishChange(undoGroup);

            if (addedItems.Count > 0)
                NotifyItemsChanged(addedItems, LibraryItemsChangeType.Added);
        }

        /// <inheritdoc/>
        public void ExceptWith(IEnumerable<LibraryItem> other)
        {
            if (other == null)
                throw new System.ArgumentNullException(nameof(other));

            // This is already empty; return.
            if (Count == 0)
            {
                return;
            }

            // Special case if other is 'this', a 'Set' minus itself is just empty.
            if (other == (IEnumerable<LibraryItem>)this)
            {
                Clear();
                return;
            }

            PrepareItemChange();
            // Remove every item in 'other' from 'this'.
            var removedItems = new List<LibraryItem>();
            foreach (var item in other)
            {
                if (Remove(item, true))
                    removedItems.Add(item);
            }
            FinishChange();

            if (removedItems.Count > 0)
                NotifyItemsChanged(removedItems, LibraryItemsChangeType.Removed);
        }

        /// <summary>
        /// Removes all <see cref="LibraryItem"/>s from the <see cref="StandardCollection"/>.
        /// </summary>
        public void Clear()
        {
            if (Count > 0)
                PrepareItemChange();

            ClearItems();
            // TODO: This needs to be called differently otherwise anything that changes because of the event in ClearItems
            // will end up being group together.
            FinishChange();
        }
    } 
}
