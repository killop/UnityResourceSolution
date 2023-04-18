using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaBeats
{

    public static class _PoolExtension
    {
        public static void ReleaseToPool<T>(this List<T> self) => UnityEngine.Pool.ListPool<T>.Release(self);
        public static void ReleaseToPool<T>(this HashSet<T> self) => UnityEngine.Pool.HashSetPool<T>.Release(self);

        public static void ReturnToFixedArrayPool<T>(this T[] self) =>
            FixedArrayPool<T>.Shared(self.Length).Return(self);
    }
}