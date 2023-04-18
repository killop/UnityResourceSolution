#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR) && BESTHTTP_WITH_BURST
using System;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.Arm.Neon;
using System.Runtime.CompilerServices;
using Unity.Burst;

// https://github.com/sschoener/burst-simd-exercises/blob/main/Assets/Examples/2-sum-small-numbers-sse3/SumSmallNumbers_SSE3.cs

namespace BestHTTP.Connections.TLS.Crypto.Impl
{
    [BurstCompile]
    internal sealed unsafe class FastGcmBlockCipherHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DecryptBlock(ReadOnlySpan<byte> input, Span<byte> output, Span<byte> ctrBlock, Span<byte> S, int BlockSize)
        {
            fixed (byte* pInput = input)
            fixed (byte* pOutput = output)
            fixed (byte* pctrBlock = ctrBlock)
            fixed (byte* pS = S) {
                DecryptBlock_Impl(pInput, input.Length, pOutput, output.Length, pctrBlock, pS, BlockSize);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private unsafe static void DecryptBlock_Impl([NoAlias] byte* pinput, int inLen, [NoAlias] byte* poutput, int outLen, [NoAlias] byte* pctrBlock, [NoAlias] byte* pS, int BlockSize)
        {
            if (IsSse2Supported)
            {
                var vInput = loadu_si128(pinput);
                var vCtrBlock = loadu_si128(pctrBlock);
                var vS = loadu_si128(pS);

                vS = xor_si128(vS, vInput);
                vCtrBlock = xor_si128(vInput, vCtrBlock);

                storeu_si128(pS, vS);
                storeu_si128(poutput, vCtrBlock);
            }
            else if (IsNeonSupported)
            {
                var vInput = vld1q_u8(pinput);
                var vCtrBlock = vld1q_u8(pctrBlock);
                var vS = vld1q_u8(pS);

                vS = veorq_u8(vS, vInput);
                vCtrBlock = veorq_u8(vInput, vCtrBlock);

                vst1q_u8(pS, vS);
                vst1q_u8(poutput, vCtrBlock);
            }
            else
            {
                Unity.Burst.CompilerServices.Hint.Assume(BlockSize == 16);

                for (int i = 0; i < BlockSize; i += 4)
                {
                    byte c0 = pinput[i + 0];
                    byte c1 = pinput[i + 1];
                    byte c2 = pinput[i + 2];
                    byte c3 = pinput[i + 3];

                    pS[i + 0] ^= c0;
                    pS[i + 1] ^= c1;
                    pS[i + 2] ^= c2;
                    pS[i + 3] ^= c3;

                    poutput[i + 0] = (byte)(c0 ^ pctrBlock[i + 0]);
                    poutput[i + 1] = (byte)(c1 ^ pctrBlock[i + 1]);
                    poutput[i + 2] = (byte)(c2 ^ pctrBlock[i + 2]);
                    poutput[i + 3] = (byte)(c3 ^ pctrBlock[i + 3]);
                }
            }
        }
    }
}
#endif
