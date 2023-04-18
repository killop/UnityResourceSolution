#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;

namespace BestHTTP.Connections.TLS.Crypto.Impl
{
#if BESTHTTP_WITH_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal static class FastSalsa20EngineHelper
    {
#if BESTHTTP_WITH_BURST
        [Unity.Burst.BurstCompile]
        public unsafe static void ProcessBytes([Unity.Burst.NoAlias] byte* outBytes, int outOff, [Unity.Burst.NoAlias] byte* inBytes, int inOff, [Unity.Burst.NoAlias] byte* keyStream)
        {
            //for (int i = 0; i < 64; ++i)
            //    outBytes[idx + i + outOff] = (byte)(keyStream[i] ^ inBytes[idx + i + inOff]);

            ulong* pulOut = (ulong*)&outBytes[outOff];
            ulong* pulIn = (ulong*)&inBytes[inOff];
            ulong* pulKeyStream = (ulong*)keyStream;

            pulOut[0] = pulKeyStream[0] ^ pulIn[0];
            pulOut[1] = pulKeyStream[1] ^ pulIn[1];
            pulOut[2] = pulKeyStream[2] ^ pulIn[2];
            pulOut[3] = pulKeyStream[3] ^ pulIn[3];
        }
#endif

    }
}
#endif
