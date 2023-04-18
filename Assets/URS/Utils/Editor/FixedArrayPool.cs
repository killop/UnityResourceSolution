using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace NinjaBeats
{
    public class FixedArrayPool<T>
    {
        static Dictionary<int, FixedArrayPool<T>> _PoolDict = new Dictionary<int, FixedArrayPool<T>>();

        public static FixedArrayPool<T> Shared(int size)
        {
            if (!_PoolDict.TryGetValue(size, out var r))
            {
                r = new FixedArrayPool<T>(size);
                _PoolDict.Add(size, r);
            }

            return r;
        }

        public Queue<T[]> Objects { get; protected set; }
        public int ArraySize { get; protected set; }

        public int Count
        {
            get { return Objects == null ? 0 : Objects.Count; }
        }

        public FixedArrayPool(int arraySize)
        {
            Objects = new Queue<T[]>();
            ArraySize = arraySize;
        }

        virtual public T[] Rent()
        {
            if (Objects.Count > 0)
            {
                T[] item = Objects.Dequeue();
                if (item != null)
                {
                    return item;
                }
            }

            T[] ret = new T[ArraySize];
            return ret;
        }

        virtual public bool Return(T[] item)
        {
            if (item == null)
                return false;
            if (item.Length != ArraySize)
                return false;

            System.Array.Clear(item, 0, ArraySize);
            Objects.Enqueue(item);
            return true;
        }

        public T[][] ToArray()
        {
            return Objects.ToArray();
        }
    }
}