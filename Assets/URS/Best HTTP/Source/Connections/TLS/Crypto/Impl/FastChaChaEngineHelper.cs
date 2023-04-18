#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Runtime.CompilerServices;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;

#if BESTHTTP_WITH_BURST
using Unity.Burst;
#endif

namespace BestHTTP.Connections.TLS.Crypto.Impl
{
#if BESTHTTP_WITH_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal static class FastChaChaEngineHelper
    {
        internal unsafe static void ChachaCore(int rounds, uint[] input, byte[] output)
        {
            fixed (uint* pinput = input)
            fixed (byte* poutput = output)
                ChachaCoreImpl(rounds, pinput, poutput);
        }

#if BESTHTTP_WITH_BURST
        [Unity.Burst.BurstCompile]
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        internal unsafe static void ChachaCoreImpl(int rounds,
#if BESTHTTP_WITH_BURST
            [NoAlias]
#endif
            uint* input,
#if BESTHTTP_WITH_BURST
            [NoAlias]
#endif
            byte* output)
        {
            uint* x = stackalloc uint[16];

            for (int i = 0; i < 16; i++)
                x[i] = input[i];

            uint tmp = 0;
            for (int i = rounds; i > 0; i -= 2)
            {
                x[00] += x[04]; tmp = x[12] ^ x[00]; x[12] = (tmp << 16) | (tmp >> -16); // Integers.RotateLeft(x[12] ^ x[00], 16);
                x[01] += x[05]; tmp = x[13] ^ x[01]; x[13] = (tmp << 16) | (tmp >> -16); // Integers.RotateLeft(x[13] ^ x[01], 16);
                x[02] += x[06]; tmp = x[14] ^ x[02]; x[14] = (tmp << 16) | (tmp >> -16); // Integers.RotateLeft(x[14] ^ x[02], 16);
                x[03] += x[07]; tmp = x[15] ^ x[03]; x[15] = (tmp << 16) | (tmp >> -16); // Integers.RotateLeft(x[15] ^ x[03], 16);

                x[08] += x[12]; tmp = x[04] ^ x[08]; x[04] = (tmp << 12) | (tmp >> -12); // Integers.RotateLeft(x[04] ^ x[08], 12);
                x[09] += x[13]; tmp = x[05] ^ x[09]; x[05] = (tmp << 12) | (tmp >> -12); // Integers.RotateLeft(x[05] ^ x[09], 12);
                x[10] += x[14]; tmp = x[06] ^ x[10]; x[06] = (tmp << 12) | (tmp >> -12); // Integers.RotateLeft(x[06] ^ x[10], 12);
                x[11] += x[15]; tmp = x[07] ^ x[11]; x[07] = (tmp << 12) | (tmp >> -12); // Integers.RotateLeft(x[07] ^ x[11], 12);

                x[00] += x[04]; tmp = x[12] ^ x[00]; x[12] = (tmp << 8) | (tmp >> -8); // Integers.RotateLeft(x[12] ^ x[00], 8);
                x[01] += x[05]; tmp = x[13] ^ x[01]; x[13] = (tmp << 8) | (tmp >> -8); // Integers.RotateLeft(x[13] ^ x[01], 8);
                x[02] += x[06]; tmp = x[14] ^ x[02]; x[14] = (tmp << 8) | (tmp >> -8); // Integers.RotateLeft(x[14] ^ x[02], 8);
                x[03] += x[07]; tmp = x[15] ^ x[03]; x[15] = (tmp << 8) | (tmp >> -8); // Integers.RotateLeft(x[15] ^ x[03], 8);

                x[08] += x[12]; tmp = x[04] ^ x[08]; x[04] = (tmp << 7) | (tmp >> -7); // Integers.RotateLeft(x[04] ^ x[08], 7);
                x[09] += x[13]; tmp = x[05] ^ x[09]; x[05] = (tmp << 7) | (tmp >> -7); // Integers.RotateLeft(x[05] ^ x[09], 7);
                x[10] += x[14]; tmp = x[06] ^ x[10]; x[06] = (tmp << 7) | (tmp >> -7); // Integers.RotateLeft(x[06] ^ x[10], 7);
                x[11] += x[15]; tmp = x[07] ^ x[11]; x[07] = (tmp << 7) | (tmp >> -7); // Integers.RotateLeft(x[07] ^ x[11], 7);
                x[00] += x[05]; tmp = x[15] ^ x[00]; x[15] = (tmp << 16) | (tmp >> -16); // Integers.RotateLeft(x[15] ^ x[00], 16);
                x[01] += x[06]; tmp = x[12] ^ x[01]; x[12] = (tmp << 16) | (tmp >> -16); // Integers.RotateLeft(x[12] ^ x[01], 16);
                x[02] += x[07]; tmp = x[13] ^ x[02]; x[13] = (tmp << 16) | (tmp >> -16); // Integers.RotateLeft(x[13] ^ x[02], 16);
                x[03] += x[04]; tmp = x[14] ^ x[03]; x[14] = (tmp << 16) | (tmp >> -16); // Integers.RotateLeft(x[14] ^ x[03], 16);

                x[10] += x[15]; tmp = x[05] ^ x[10]; x[05] = (tmp << 12) | (tmp >> -12); // Integers.RotateLeft(x[05] ^ x[10], 12);
                x[11] += x[12]; tmp = x[06] ^ x[11]; x[06] = (tmp << 12) | (tmp >> -12); // Integers.RotateLeft(x[06] ^ x[11], 12);
                x[08] += x[13]; tmp = x[07] ^ x[08]; x[07] = (tmp << 12) | (tmp >> -12); // Integers.RotateLeft(x[07] ^ x[08], 12);
                x[09] += x[14]; tmp = x[04] ^ x[09]; x[04] = (tmp << 12) | (tmp >> -12); // Integers.RotateLeft(x[04] ^ x[09], 12);

                x[00] += x[05]; tmp = x[15] ^ x[00]; x[15] = (tmp << 8) | (tmp >> -8); // Integers.RotateLeft(x[15] ^ x[00], 8);
                x[01] += x[06]; tmp = x[12] ^ x[01]; x[12] = (tmp << 8) | (tmp >> -8); // Integers.RotateLeft(x[12] ^ x[01], 8);
                x[02] += x[07]; tmp = x[13] ^ x[02]; x[13] = (tmp << 8) | (tmp >> -8); // Integers.RotateLeft(x[13] ^ x[02], 8);
                x[03] += x[04]; tmp = x[14] ^ x[03]; x[14] = (tmp << 8) | (tmp >> -8); // Integers.RotateLeft(x[14] ^ x[03], 8);

                x[10] += x[15]; tmp = x[05] ^ x[10]; x[05] = (tmp << 7) | (tmp >> -7); // Integers.RotateLeft(x[05] ^ x[10], 7);
                x[11] += x[12]; tmp = x[06] ^ x[11]; x[06] = (tmp << 7) | (tmp >> -7); // Integers.RotateLeft(x[06] ^ x[11], 7);
                x[08] += x[13]; tmp = x[07] ^ x[08]; x[07] = (tmp << 7) | (tmp >> -7); // Integers.RotateLeft(x[07] ^ x[08], 7);
                x[09] += x[14]; tmp = x[04] ^ x[09]; x[04] = (tmp << 7) | (tmp >> -7); // Integers.RotateLeft(x[04] ^ x[09], 7);
            }

            for (int i = 0; i < 16; i++)
            {
                uint n = x[i] + input[i];

                output[(i * 4)] = (byte)n;
                output[(i * 4) + 1] = (byte)(n >> 8);
                output[(i * 4) + 2] = (byte)(n >> 16);
                output[(i * 4) + 3] = (byte)(n >> 24);
            }
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || _UNITY_2021_2_OR_NEWER_
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ImplProcessBlock(ReadOnlySpan<byte> input, Span<byte> output, byte[] keyStream)
        {
            fixed (byte* pinput = input)
            fixed (byte* poutput = output)
            fixed (byte* pkeyStream = keyStream)
            {
                ulong* pulinput = (ulong*)pinput;
                ulong* puloutput = (ulong*)poutput;
                ulong* pulkeyStream = (ulong*)pkeyStream;

                puloutput[0] = pulkeyStream[0] ^ pulinput[0];
                puloutput[1] = pulkeyStream[1] ^ pulinput[1];
                puloutput[2] = pulkeyStream[2] ^ pulinput[2];
                puloutput[3] = pulkeyStream[3] ^ pulinput[3];

                puloutput[4] = pulkeyStream[4] ^ pulinput[4];
                puloutput[5] = pulkeyStream[5] ^ pulinput[5];
                puloutput[6] = pulkeyStream[6] ^ pulinput[6];
                puloutput[7] = pulkeyStream[7] ^ pulinput[7];
            }
        }
#endif
    }
}
#endif
