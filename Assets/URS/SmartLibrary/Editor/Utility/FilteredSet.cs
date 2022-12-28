using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    internal struct FilterResult
    {
        public bool doesMatch;
        public long score;
        public string itemName;

        public FilterResult(bool doesMatch, long score, string itemName)
        {
            this.doesMatch = doesMatch;
            this.score = score;
            this.itemName = itemName;
        }
    }

    internal struct FilterEntry<T>
    {
        public T Item { get; }
        public FilterResult Result { get; }

        public FilterEntry(T item, FilterResult result)
        {
            Item = item;
            Result = result;
        }
    }

    /// <summary>
    /// Represents a collection of items that can be filtered by a string value.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the <see cref="FilteredSet{T}"/>.</typeparam>
    internal class FilteredSet<T> : List<FilterEntry<T>>
    {
        private string _filter;
        private IComparer<FilterEntry<T>> _comparer = new EntryComparer();
        private IEnumerable<T> _itemsSource;

        /// <summary>
        /// The unfiltered items.
        /// </summary>
        public IEnumerable<T> ItemsSource
        {
            get { return _itemsSource; }
            set
            {
                if (_itemsSource == value)
                    return;

                _itemsSource = value;
                FilterItems();
            }
        }

        /// <summary>
        /// Handles the per item filtering. Item, Filter.
        /// </summary>
        public Func<T, string, FilterResult> ItemFilter { get; set; }

        public IComparer<FilterEntry<T>> Comparer
        {
            get { return _comparer; }
            set
            {
                _comparer = value;
                SortEntries();
            }
        }

        /// <summary>
        /// Determines whether to sort the items after filtering them.
        /// </summary>
        public bool SortOnFilter { get; set; } = true;

        /// <summary>
        /// The string used to filter items by.
        /// </summary>
        public string Filter
        {
            get { return _filter; }
            set
            {
                var previusFilter = _filter;
                _filter = value;
                if (_filter != previusFilter)
                    FilterItems();
            }
        }

        public FilteredSet()
        {

        }

        public FilteredSet(IEnumerable<T> items)
        {
            ItemsSource = items;
        }

        public FilteredSet(IEnumerable<T> items, Func<T, string, FilterResult> itemFilter)
        {
            ItemsSource = items;
            ItemFilter = itemFilter;
        }

        public void FilterItems()
        {
            Clear();
            foreach (var item in ItemsSource)
            {
                FilterItem(item);
            }

            if (SortOnFilter)
                Sort(Comparer);
        }

        /// <summary>
        /// Filter the specified item, adding it to the list if it matches the filter.
        /// </summary>
        /// <param name="item"></param>
        /// <returns><c>true</c> if <paramref name="item"/> matches the filter and was added; otherwise, <c>false</c>.</returns>
        public bool FilterItem(T item)
        {
            var result = ItemFilter(item, _filter);
            if (result.doesMatch)
            {
                Add(new FilterEntry<T>(item, result));
            }
            
            return result.doesMatch;
        }

        /// <summary>
        /// Sort the entries using the provided comparer.
        /// </summary>
        public void SortEntries()
        {
            Sort(Comparer);
        }

        public class EntryComparer : IComparer<FilterEntry<T>>
        {
            public int Compare(FilterEntry<T> x, FilterEntry<T> y)
            {
                // Sorts by score then by alphabetical.
                int scoreResult = y.Result.score.CompareTo(x.Result.score);
                return scoreResult != 0 ? scoreResult : string.CompareOrdinal(x.Result.itemName, y.Result.itemName);
            }
        }
    } 
}
