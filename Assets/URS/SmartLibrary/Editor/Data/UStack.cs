using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Represents a variable size last-in-first-out (LIFO) collection of instances of the same specified type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the stack.</typeparam>
    [Serializable]
    internal class UStack<T> : IEnumerable<T>, ICollection, IReadOnlyCollection<T>
    {
        private const int _defaultCapacity = 4;
        private static T[] _emptyArray = new T[0];

        private object _syncRoot;

        [SerializeField] private T[] _array;
        [SerializeField] private int _count;
        [SerializeField] private int _version; // Used to keep enumerator in sync w/ collection.

        /// <summary>
        /// The numver of elements contained in the <see cref="UStack{T}"/>.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                    System.Threading.Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                return _syncRoot;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UStack{T}"/> that is empty and has the default initialize capacity.
        /// </summary>
        public UStack()
        {
            _array = _emptyArray;
            _count = 0;
            _version = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UStack{T}"/> that is empty and has the specified initial capacity or default initial capacity, whichever is greater.
        /// </summary>
        /// <param name="capacity">The initial number of elements the <see cref="UStack{T}"/> can contain.</param>
        public UStack(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Non-negative number required.");

            _array = new T[capacity];
            _count = 0;
            _version = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UStack{T}"/> that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection to copy elements from.</param>
        public UStack(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> c)
            {
                _count = c.Count;
                _array = new T[_count];
                c.CopyTo(_array, 0);
            }
            else
            {
                _array = new T[_defaultCapacity];
                _count = 0;

                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Push(en.Current);
                    }
                }
            }
        }

        /// <summary>
        /// Inserts the specified element at the top of the <see cref="UStack{T}"/>.
        /// </summary>
        /// <param name="item">The element to push onto the <see cref="UStack{T}"/>. The value can be <c>null</c> for reference types.</param>
        public void Push(T item)
        {
            if (_count == _array.Length)
            {
                T[] newArray = new T[_array.Length == 0 ? _defaultCapacity : _array.Length * 2];
                Array.Copy(_array, newArray, _count);
                _array = newArray;
            }

            _array[_count] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Returns the element at the top of the <see cref="UStack{T}"/> without removing it.
        /// </summary>
        /// <returns>The element at the top of the <see cref="UStack{T}"/>.</returns>
        public T Peek()
        {
            if (_count == 0)
                throw new InvalidOperationException("Stack empty.");

            return _array[_count - 1];
        }

        /// <summary>
        /// Removes and returns the element at the top of the <see cref="UStack{T}"/>.
        /// </summary>
        /// <returns>The element removed from the top of the <see cref="UStack{T}"/>.</returns>
        public T Pop()
        {
            if (_count == 0)
                throw new InvalidOperationException("Stack empty.");

            _version++;
            _count--;
            T item = _array[_count];
            _array[_count] = default; // Free memory quicker.

            return item;
        }

        /// <summary>
        /// Determins whether the specified element is in the <see cref="UStack{T}"/>.
        /// </summary>
        /// <param name="item">The element to locate in teh <see cref="UStack{T}"/>. The value can be <c>null</c> for reference types.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is found in the <see cref="UStack{T}"/>; otherwise <c>false</c>.</returns>
        public bool Contains(T item)
        {
            int count = _count;

            EqualityComparer<T> c = EqualityComparer<T>.Default;

            while (count > 0)
            {
                count--;

                if (item == null)
                {
                    if (_array[count] == null)
                        return true;
                }
                else if (_array[count] != null && c.Equals(_array[count], item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the <see cref="UStack{T}"/>, if that number is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            int threshold = (int)(_array.Length * 0.9f);

            if (_count < threshold)
            {
                T[] newarray = new T[_count];
                Array.Copy(_array, 0, newarray, 0, _count);
                _array = newarray;
                _version++;
            }
        }

        /// <summary>
        /// Removes all elements from the <see cref="UStack{T}"/>.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_array, 0, _count);
            _count = 0;
            _version++;
        }

        /// <summary>
        /// Copies the <see cref="UStack{T}"/> to a new array.
        /// </summary>
        /// <returns>The new array containing copies of the elements of the <see cref="UStack{T}"/>.</returns>
        public T[] ToArray()
        {
            T[] _arrayCopy = new T[_count];

            for (int i = 0; i < _count; i++)
            {
                _arrayCopy[i] = _array[_count - 1 - i];
            }

            return _arrayCopy;
        }

        /// <summary>
        /// Copies the <see cref="UStack{T}"/> to an existing one-dimensional array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="UStack{T}"/>. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "Index was out of range. Must be non-negative and less than the size of the collection.");

            if (array.Length - arrayIndex < _count)
                throw new ArgumentException();

            Array.Copy(_array, 0, array, arrayIndex, _count);
            Array.Reverse(array, arrayIndex, _count);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Rank != 1)
                throw new ArgumentException("Multi-dminensional array is not supported.");

            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("Non-sero lower bound.");

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "Index was out of range. Must be non-negative and less than the size of the collection.");

            try
            {
                Array.Copy(_array, 0, array, arrayIndex, _count);
                Array.Reverse(array, arrayIndex, _count);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Invalid array type.");
            }
        }

        /// <summary>
        /// Returns an enumerator for the <see cref="UStack{T}"/>.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private UStack<T> _stack;
            private int _index;
            private int _version;
            private T _currentElement;

            public T Current
            {
                get 
                {
                    if (_index == -1)
                        throw new InvalidOperationException("Enumerator ended.");
                    if (_index == -2)
                        throw new InvalidOperationException("Enumerator has not started.");
                    return _currentElement; 
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            internal Enumerator(UStack<T> stack)
            {
                _stack = stack;
                _version = _stack._version;
                _index = -2;
                _currentElement = default;
            }

            public void Dispose()
            {
                _index = -1;
            }

            public bool MoveNext()
            {
                bool retrival;
                if (_version != _stack._version)
                    throw new InvalidOperationException("Enum failed version.");

                if (_index == -2) // First call to enumerator.
                {
                    _index = _stack._count - 1;
                    retrival = _index >= 0;
                    if (retrival)
                        _currentElement = _stack._array[_index];

                    return retrival;
                }

                if (_index == -1) // End of enumeration.
                    return false;

                _index--;
                retrival = _index >= 0;
                if (retrival)
                    _currentElement = _stack._array[_index];
                else
                    _currentElement = default;

                return retrival;
            }

            void IEnumerator.Reset()
            {
                if (_version != _stack._version)
                    throw new InvalidOperationException();

                _index = -2;
                _currentElement = default;
            }
        }
    }
}