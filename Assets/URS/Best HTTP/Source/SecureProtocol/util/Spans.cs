#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
using System;
using System.Runtime.CompilerServices;

//#nullable enable

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities
{
    internal static class Spans
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyFrom<T>(this Span<T> output, ReadOnlySpan<T> input)
        {
            input[..output.Length].CopyTo(output);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> FromNullable<T>(T[]? array)
        {
            return array == null ? Span<T>.Empty : array.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> FromNullable<T>(T[]? array, int start)
        {
            return array == null ? Span<T>.Empty : array.AsSpan(start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<T> FromNullableReadOnly<T>(T[]? array)
        {
            return array == null ? Span<T>.Empty : array.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<T> FromNullableReadOnly<T>(T[]? array, int start)
        {
            return array == null ? Span<T>.Empty : array.AsSpan(start);
        }
    }
}
#endif
#pragma warning restore
#endif
