using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace NinjaBeats
{
    public static partial class Utils
    {
        class ListDummy<T>
        {
            public T[] _items;
            public int _size;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<T>(this List<T> self, int size)
        {
            if (self.Capacity < size)
                self.Capacity = size > 1024 ? (size + 256) : Mathf.NextPowerOfTwo(size + 1);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AddBlock<T>(this List<T> self, int blockSize)
        {
            if (self == null)
                return Span<T>.Empty;
            self.EnsureCapacity(self.Count + blockSize);
            var dummy = Unsafe.As<ListDummy<T>>(self);
            var old = dummy._size;
            dummy._size += blockSize;
            return dummy._items.AsSpan(old, blockSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this List<T> self)
        {
            if (self == null)
                return Span<T>.Empty;
            var dummy = Unsafe.As<ListDummy<T>>(self);
            return dummy._items.AsSpan(0, dummy._size);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindIndex<T>(this IList<T> self, Predicate<T> func)
        {
            for (int i = 0; i < self.Count; ++i)
            {
                var value = self[i];
                if (func(value))
                    return i;
            }

            return -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Find<T>(this IList<T> self, Predicate<T> func)
        {
            for (int i = 0; i < self.Count; ++i)
            {
                var value = self[i];
                if (func(value))
                    return value;
            }

            return default;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Empty<T>(this IList<T> self) => self.Count == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotEmpty<T>(this IList<T> self) => self.Count > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTimes<T>(this IList<T> self, T item, int count)
        {
            if (count <= 0)
                return;
            for (int i = 0; i < count; ++i)
                self.Add(item);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTimes<T>(this List<T> self, T item, int count)
        {
            if (count <= 0)
                return;
            self.AddBlock(count).Fill(item);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange(this IList self, IEnumerable list)
        {
            if (self == null || list == null)
                return;
            foreach (var v in list)
                self.Add(v);
        }
        
        // foreach 接口会 GC Alloc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<T>(this ISet<T> self, IList<T> items)
        {
            if (self == null || items == null)
                return;
            for (int i = 0; i < items.Count; ++ i)
                self.Add(items[i]);
        }
        // foreach 接口会 GC Alloc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<T>(this ISet<T> self, HashSet<T> items)
        {
            if (self == null || items == null)
                return;
            foreach (var item in items)
                self.Add(item);
        }
        // foreach 接口会 GC Alloc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRangeNonAlloc<T>(this IList<T> self, HashSet<T> items)
        {
            if (self == null || items == null)
                return;
            foreach (var item in items)
                self.Add(item);
        }
        // foreach 接口会 GC Alloc
        public static void AddValues<K, V>(this IList<V> self, Dictionary<K, V> dict)
        {
            if (self == null || dict == null)
                return;
            foreach (var pair in dict)
                self.Add(pair.Value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddUnique<T>(this IList<T> self, T item, Func<T, T, bool> equalFunc)
        {
            if (self.FindIndex(x => equalFunc.Invoke(item, x)) != -1)
                return;
            self.Add(item);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddUnique<T>(this IList<T> self, T item) where T : IEquatable<T>
        {
            if (self.FindIndex(x => x.Equals(item)) != -1)
                return;
            self.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this IList<T> self, Predicate<T> func) => self.FindIndex(func) != -1;

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static void AddUnique(this IList<Vector2> self, Vector2 item) =>
        //     self.AddUnique(item, (x, y) => x == y);

        public delegate int RandomRangeDelegate(int min, int max);

        /// <summary>
        /// 从[min,max]中选择count个数字
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="count"></param>
        /// <param name="allowDuplicate"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static T RandomSelect<T>(int min, int max, int count, bool allowDuplicate, T result, RandomRangeDelegate randomInclusiveMax) where T : IList<int>
        {
            result.Clear();
            if (allowDuplicate)
            {
                for (int i = 0; i < count; ++i)
                    result.Add(randomInclusiveMax(min, max));
            }
            else
            {
                if (max < min)
                    (min, max) = (max, min);

                int total = max - min + 1;
                if (count > total)
                    throw new Exception($"not enough number:{count} in [{min}, {max}]");

                if (count >= total / 2)
                {
                    var temp = new List<int>();
                    {
                        for (int i = min; i <= max; ++i)
                            temp.Add(i);
                        temp.RandomSelect(count, allowDuplicate, result, randomInclusiveMax);
                    }   
                }
                else
                {
                    var temp = new HashSet<int>();
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            while (true)
                            {
                                var randomValue = randomInclusiveMax(min, max);
                                if (!temp.Contains(randomValue))
                                {
                                    temp.Add(randomValue);
                                    result.Add(randomValue);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RandomIdx<T>(this IList<T> self, RandomRangeDelegate randomInclusiveMax) => self.Count <= 0 ? -1 : randomInclusiveMax(0, self.Count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Random<T>(this IList<T> self, RandomRangeDelegate randomInclusiveMax) => self[self.RandomIdx(randomInclusiveMax)];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IdxValid<T>(this IList<T> self, int idx) => idx >= 0 && idx < self.Count;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IndexValid<T>(this IList<T> self, int idx) => self.IdxValid(idx);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfIdxInvalid<T>(this IList<T> self, int idx)
        {
            if (!self.IdxValid(idx))
                throw new Exception($"index:{idx} of list:{self.Count} is out of range");
        }
        public static TList RandomSelect<TValue, TList>(this IList<TValue> self, int count, bool allowDuplicate, TList result, RandomRangeDelegate randomInclusiveMax) where TList : IList<TValue>
        {
            result.Clear();
            int size = self.Count;
            if (count > 0 && size > 0)
            {
                //Span<int> idxArr = stackalloc int[count];
                if (allowDuplicate)
                {
                    for (int i = 0; i < count; ++i)
                    {
                        var randomIdx = self.RandomIdx(randomInclusiveMax);
                        if (self.IdxValid(randomIdx))
                        {
                            result.Add(self[randomIdx]);
                        }
                        else
                        {
                            throw new Exception($"{nameof(RandomSelect)} not enough valid item, size:{size}, count:{count}");
                        }
                    }
                }
                else
                {
                    if (count > size)
                        throw new Exception($"{nameof(RandomSelect)} out of range, size:{size}, count:{count}");

                    var temp = new List<TValue>();
                    {
                        foreach (var v in self)
                            temp.Add(v);

                        for (int i = 0; i < count; ++i)
                        {
                            var randomIdx = temp.RandomIdx(randomInclusiveMax);
                            if (temp.IdxValid(randomIdx))
                            {
                                result.Add(temp[randomIdx]);
                                temp.RemoveAt(randomIdx);
                            }
                            else
                            {
                                throw new Exception($"{nameof(RandomSelect)} not enough valid item, size:{size}, count:{count}");
                            }
                        }
                    }                        
                }
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> RandomSelect<T>(this IList<T> self, int count, bool allowDuplicate, RandomRangeDelegate randomInclusiveMax) => self.RandomSelect(count, allowDuplicate, new List<T>(), randomInclusiveMax);
        
        public static int BinaryLowerBound<TValue, TKey>(this IList<TValue> self, int index, int count, TKey item, Func<TValue, TKey, int> comparer)
        {
            int end = index + count;
            int first = index;
            int mid, count2;

            while (0 < count)
            {
                count2 = count / 2;
                mid = first + count2;
                if (comparer(self[mid], item) < 0)
                {
                    first = mid + 1;
                    count = count - count2 - 1;
                }
                else
                {
                    count = count2;
                }
            }
            if (first >= end || first >= self.Count)
                return -1;
            return first;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryLowerBound<TValue, TKey>(this IList<TValue> self, int index, TKey item, Func<TValue, TKey, int> comparer) => self.BinaryLowerBound(index, self.Count - index, item, comparer);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryLowerBound<TValue, TKey>(this IList<TValue> self, TKey item, Func<TValue, TKey, int> comparer) => self.BinaryLowerBound(0, self.Count, item, comparer);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryLowerBound<T>(this IList<T> self, int index, int count, T item, IComparer<T> comparer = null) => self.BinaryLowerBound(index, count, item, comparer.Compare);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryLowerBound<T>(this IList<T> self, int index, T item, IComparer<T> comparer = null) => self.BinaryLowerBound(index, self.Count - index, item, comparer.Compare);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryLowerBound<T>(this IList<T> self, T item, IComparer<T> comparer = null) => self.BinaryLowerBound(0, self.Count, item, comparer.Compare);
        public static int BinaryUpperBound<TValue, TKey>(this IList<TValue> self, int index, int count, TKey item, Func<TValue, TKey, int> comparer)
        {
            int end = index + count;
            int first = index;
            int mid, count2;

            while (0 < count)
            {
                count2 = count / 2;
                mid = first + count2;
                if (comparer(self[mid], item) <= 0)
                {
                    first = mid + 1;
                    count = count - count2 - 1;
                }
                else
                {
                    count = count2;
                }
            }
            if (first >= end || first >= self.Count)
                return -1;
            return first;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryUpperBound<TValue, TKey>(this IList<TValue> self, int index, TKey item, Func<TValue, TKey, int> comparer) => self.BinaryUpperBound(index, self.Count - index, item, comparer);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryUpperBound<TValue, TKey>(this IList<TValue> self, TKey item, Func<TValue, TKey, int> comparer) => self.BinaryUpperBound(0, self.Count, item, comparer);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryUpperBound<T>(this IList<T> self, int index, int count, T item, IComparer<T> comparer = null) => self.BinaryUpperBound(index, count, item, (comparer ?? Comparer<T>.Default).Compare);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryUpperBound<T>(this IList<T> self, int index, T item, IComparer<T> comparer = null) => self.BinaryUpperBound(index, self.Count - index, item, (comparer ?? Comparer<T>.Default).Compare);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinaryUpperBound<T>(this IList<T> self, T item, IComparer<T> comparer = null) => self.BinaryUpperBound(0, self.Count, item, (comparer ?? Comparer<T>.Default).Compare);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InsertAscending<TValue, TKey>(this IList<TValue> self, TKey key, TValue item, Func<TValue, TKey, int> comparer)
        {
            var it = self.BinaryLowerBound(0, self.Count, key, comparer);
            if (!self.IdxValid(it))
                self.Add(item);
            else
                self.Insert(it, item);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InsertAscending<T>(this IList<T> self, T item, IComparer<T> comparer = null) => self.InsertAscending(item, item, (comparer ?? Comparer<T>.Default).Compare);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InsertDescending<TValue, TKey>(this IList<TValue> self, TKey key, TValue item, Func<TValue, TKey, int> comparer)
        {
            var it = self.BinaryLowerBound(0, self.Count, key, (v, k) => -1 * comparer(v, k));
            if (!self.IdxValid(it))
                self.Add(item);
            else
                self.Insert(it, item);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InsertDescending<T>(this IList<T> self, T item, IComparer<T> comparer = null) => self.InsertDescending(item, item, (comparer ?? Comparer<T>.Default).Compare);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsEquals(byte[] buffer1, byte* buffer2)
        {
            for (int i = 0; i < buffer1.Length; ++i)
            {
                if (buffer1[i] != buffer2[i])
                    return false;
            }

            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsEquals(byte* buffer1, byte* buffer2, int length)
        {
            for (int i = 0; i < length; ++i)
            {
                if (buffer1[i] != buffer2[i])
                    return false;
            }

            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsEquals(byte[] buffer1, int offset1, byte* buffer2, int length)
        {
            fixed (byte* ptr1 = buffer1)
                return IsEquals(ptr1 + offset1, buffer2, length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsEquals(byte* buffer1, byte[] buffer2, int offset2, int length)
        {
            fixed (byte* ptr2 = buffer2)
                return IsEquals(buffer1, ptr2 + offset2, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsEquals(byte[] buffer1, int offset1, byte[] buffer2, int offset2, int length)
        {
            fixed (byte* ptr1 = buffer1)
            fixed (byte* ptr2 = buffer2)
                return IsEquals(ptr1 + offset1, ptr2 + offset2, length);
        }
        
        public struct _ConvertScopeImpl<T, R> : IDisposable where T : class
        {
            public IList<T> self;
            public List<R> list;
            public Action<T, R> setter;

            public _ConvertScopeImpl(IList<T> self, Func<T, R> getter, Action<T, R> setter)
            {
                this.self = self;
                this.list = ListPool<R>.Get();
                this.setter = setter;
                if (getter != null && self != null)
                {
                    foreach (var t in self)
                        this.list.Add(getter(t));   
                }
            }

            public void Dispose()
            {
                if (list != null)
                {
                    if (setter != null && self != null && self.Count == list.Count)
                    {
                        for (int i = 0; i < list.Count; ++i)
                            setter(self[i], list[i]);
                    }
                    ListPool<R>.Release(list);
                }
            }
        }

        public static _ConvertScopeImpl<T, R> ConvertScope<T, R>(this IList<T> self, out List<R> list, Func<T, R> getter, Action<T, R> setter) where T : class
        {
            _ConvertScopeImpl<T, R> r = new(self, getter, setter);
            list = r.list;
            return r;
        }
        
        public static List<R> ConvertToList<T, R>(this IList<T> self, Func<T, R> func, List<R> r = null)
        {
            if (self == null)
                return null;

            r ??= new();
            foreach (var v in self)
                r.Add(func(v));
            return r;
        }

        public static List<T> ConvertToList<T>(this IList self, Func<object, T> func)
        {
            if (self == null)
                return null;

            List<T> r = new();
            foreach (var v in self)
                r.Add(func(v));
            return r;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ConvertArray<T>(Array fromArray)
        {
            return (T[])ConvertArray(fromArray, typeof(T[]));
        }

        public static Array ConvertArray(Array fromArray, Type toType)
        {
            if (fromArray == null)
                return null;
            var ret = Array.CreateInstance(toType.GetElementType(), fromArray.Length);
            for (int i = 0; i < fromArray.Length; ++i)
                ret.SetValue(fromArray.GetValue(i), i);
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutput[] ConvertArray<TInput, TOutput>(TInput[] fromArray, Converter<TInput, TOutput> converter)
        {
            if (fromArray == null)
                return null;
            return Array.ConvertAll(fromArray, converter);
        }

        public static bool IsEquals<T>(this ICollection<T> a, ICollection<T> b)
        {
            if ((a == null) != (b == null))
                return false;
            if (a == null)
                return true;
            if (a.Count != b.Count)
                return false;
            foreach (var v in a)
            {
                if (!b.Contains(v))
                    return false;
            }

            return true;
        }

        public static bool IsEquals<T1, T2>(this IList<T1> a, IList<T2> b, Func<T1, T2, bool> f)
        {
            if ((a == null) != (b == null))
                return false;
            if (a == null)
                return true;
            int count = a.Count;
            if (count != b.Count)
                return false;
            for (int i = 0; i < count; ++i)
            {
                if (!f(a[i], b[i]))
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this T[] self, Predicate<T> match)
        {
            for (int i = 0; i < self.Length; ++i)
            {
                if (match(self[i]))
                    return i;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddIfValid<T>(this IList<T> self, T value) where T : class
        {
            if (value != null)
                self.Add(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddIfValid<T>(this IProducerConsumerCollection<T> self, T value) where T : class
        {
            if (value != null)
                return self.TryAdd(value);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PopFirst<T>(this IList<T> self, out T first)
        {
            if (self.Count == 0)
            {
                first = default;
                return false;
            }

            first = self[0];
            self.RemoveAt(0);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PopLast<T>(this IList<T> self, out T last)
        {
            if (self.Count == 0)
            {
                last = default;
                return false;
            }

            last = self[self.Count - 1];
            self.RemoveAt(self.Count - 1);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V SafeGetValue<K, V>(this Dictionary<K, V> self, K key)
        {
            V value = default(V);
            if (self.TryGetValue(key, out value))
                return value;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V GetOrAdd<K, V>(this Dictionary<K, V> self, K key, Func<V> newFunc)
        {
            if (self.TryGetValue(key, out var value))
                return value;
            value = newFunc();
            self.Add(key, value);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V GetOrAdd<K, V>(this IDictionary<K, V> self, K key) where V : new()
        {
            if (self.TryGetValue(key, out var value))
                return value;
            value = new V();
            self.Add(key, value);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetOrAdd<T>(this IList<T> self, Predicate<T> match, Func<T> func)
        {
            var index = self.FindIndex(match);
            if (index == -1)
            {
                var value = func();
                self.Add(value);
                return value;
            }

            return self[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveIf<T>(this List<T> self, Predicate<T> match)
        {
            var idx = self.FindIndex(match);
            if (idx != -1)
                self.RemoveAt(idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T IdxOrDefault<T>(this IList<T> self, int idx)
        {
            if (idx >= 0 && idx < self.Count)
                return self[idx];
            return default;
        }
        
        public static bool IsSimilar<K, V>(IDictionary<K, V> a, IDictionary<K, V> b, Func<V, V, bool> func)
        {
            if (object.ReferenceEquals(a, b))
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            foreach (var pair in a)
            {
                if (!b.TryGetValue(pair.Key, out var vb))
                    return false;

                if (!func(pair.Value, vb))
                    return false;
            }

            return true;
        }

        public static bool IsSimilar<V>(IList<V> a, IList<V> b, Func<V, V, bool> func)
        {
            if (object.ReferenceEquals(a, b))
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            for (int ia = 0; ia < a.Count; ++ ia)
            {
                var va = a[ia];
                bool exist = false;

                for (int ib = 0; ib < b.Count; ++ ib)
                {
                    var vb = b[ib];
                    if (func(va, vb))
                    {
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] NewArray<T>(int size) where T : new()
        {
            var arr = new T[size];
            for (int i = 0; i < size; ++i)
                arr[i] = new();
            return arr;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitTask(this IList<Task> self)
        {
            for (int i = 0; i < self.Count; ++i)
                self[i].Wait();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitTask<T>(this IList<Task<T>> self)
        {
            for (int i = 0; i < self.Count; ++i)
                self[i].Wait();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitTask(this IEnumerable<Task> self)
        {
            foreach (var item in self)
                item.Wait();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitTask<T>(this IEnumerable<Task<T>> self)
        {
            foreach (var item in self)
                item.Wait();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update<K, V>(this IDictionary<K, V> self, K k, V v, bool enable)
        {
            if (enable)
                self.TryAdd(k, v);
            else
                self.Remove(k);
        }
    }
}