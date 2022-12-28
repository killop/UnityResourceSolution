using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Represents a set of asset GUIDs.
    /// </summary>
    [Serializable]
    public class AssetGUIDHashSet : ISet<string>, ICollection<string>, IReadOnlyCollection<string>, IEnumerable<string>, IEnumerable, ISerializationCallbackReceiver
    {
        [NonSerialized] private HashSet<string> _hashSet = new HashSet<string>();
        [SerializeField] private List<string> _hashSetList = new List<string>();

        public int Count
        {
            get { return _hashSet.Count; }
        }

        bool ICollection<string>.IsReadOnly
        {
            get { return ((ICollection<string>)_hashSet).IsReadOnly; }
        }

        public bool Add(string item)
        {
            return _hashSet.Add(item);
        }

        public void ExceptWith(IEnumerable<string> other)
        {
            _hashSet.ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<string> other)
        {
            _hashSet.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<string> other)
        {
            return _hashSet.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<string> other)
        {
            return _hashSet.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<string> other)
        {
            return _hashSet.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<string> other)
        {
            return _hashSet.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<string> other)
        {
            return _hashSet.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<string> other)
        {
            return _hashSet.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<string> other)
        {
            _hashSet.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<string> other)
        {
            _hashSet.UnionWith(other);
        }

        void ICollection<string>.Add(string item)
        {
            _hashSet.Add(item);
        }

        public void Clear()
        {
            _hashSet.Clear();
        }

        public bool Contains(string item)
        {
            return _hashSet.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _hashSet.CopyTo(array, arrayIndex);
        }

        public bool Remove(string item)
        {
            return _hashSet.Remove(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _hashSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_hashSet).GetEnumerator();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _hashSetList.Clear();
            _hashSetList.AddRange(_hashSet);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _hashSet.Clear();
            foreach (var item in _hashSetList)
                _hashSet.Add(item);
        }
    }
}
