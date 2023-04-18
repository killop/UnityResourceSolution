using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace NinjaBeats
{

    public class SharedObjectPool<T> where T : class, new()
    {
        private static ObjectPool<T> _Shared = new ObjectPool<T>(() => new T());

        public static T Get() => _Shared.Get();
        public static PooledObject<T> Get(out T o) => _Shared.Get(out o);
        public static void Release(T o) => _Shared.Release(o);
    }
}