#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR) && BESTHTTP_WITH_BURST
using System;
using System.Runtime.CompilerServices;

using Unity.Burst;

using Unity.Burst.Intrinsics;

using static Unity.Burst.Intrinsics.X86;
using static Unity.Burst.Intrinsics.Arm;

// https://github.com/sschoener/burst-simd-exercises/blob/main/Assets/Examples/2-sum-small-numbers-sse3/SumSmallNumbers_SSE3.cs
// https://github.com/jratcliff63367/sse2neon/blob/master/SSE2NEON.h#L789

namespace BestHTTP.Connections.TLS.Crypto.Impl
{
    [BurstCompile]
    public unsafe static class FastChaCha7539EngineHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessBlocks2(ReadOnlySpan<byte> input, Span<byte> output, uint[] state, int rounds, byte[] keyStream)
        {
            fixed (byte* pinput = input)
            fixed (byte* poutput = output)
            fixed (uint* pstate = state)
            fixed(byte* pkeyStream = keyStream)
                ProcessBlocks2Impl(pinput, input.Length, poutput, output.Length, pstate, state.Length, rounds, pkeyStream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile(CompileSynchronously = true)]
        private static void ProcessBlocks2Impl([NoAlias] byte* input, int inputLen, [NoAlias] byte* output, int outLen, [NoAlias] uint* state, int stateLen, int rounds, [NoAlias] byte* keyStream)
        {
            if (Avx2.IsAvx2Supported)
            {
                var t0 = new v128(state[0], state[1], state[2], state[3]); //Load128_UInt32(state.AsSpan());
                var t1 = new v128(state[4], state[5], state[6], state[7]); //Load128_UInt32(state.AsSpan(4));
                var t2 = new v128(state[8], state[9], state[10], state[11]); //Load128_UInt32(state.AsSpan(8));
                var t3 = new v128(state[12], state[13], state[14], state[15]); //Load128_UInt32(state.AsSpan(12));
                ++state[12];
                var t4 = new v128(state[12], state[13], state[14], state[15]); //Load128_UInt32(state.AsSpan(12));
                ++state[12];
            
                var x0 = new v256(t0, t0); //Vector256.Create(t0, t0);
                var x1 = new v256(t1, t1); //Vector256.Create(t1, t1);
                var x2 = new v256(t2, t2); //Vector256.Create(t2, t2);
                var x3 = new v256(t3, t4); //Vector256.Create(t3, t4);
                
                var v0 = x0;
                var v1 = x1;
                var v2 = x2;
                var v3 = x3;
                
                for (int i = rounds; i > 0; i -= 2)
                {
                    v0 = Avx2.mm256_add_epi32(v0, v1);
                    v3 = Avx2.mm256_xor_si256(v3, v0);
                    v3 = Avx2.mm256_xor_si256(Avx2.mm256_slli_epi32(v3, 16), Avx2.mm256_srli_epi32(v3, 16));
                    v2 = Avx2.mm256_add_epi32(v2, v3);
                    v1 = Avx2.mm256_xor_si256(v1, v2);
                    v1 = Avx2.mm256_xor_si256(Avx2.mm256_slli_epi32(v1, 12), Avx2.mm256_srli_epi32(v1, 20));
                    v0 = Avx2.mm256_add_epi32(v0, v1);
                    v3 = Avx2.mm256_xor_si256(v3, v0);
                    v3 = Avx2.mm256_xor_si256(Avx2.mm256_slli_epi32(v3, 8), Avx2.mm256_srli_epi32(v3, 24));
                    v2 = Avx2.mm256_add_epi32(v2, v3);
                    v1 = Avx2.mm256_xor_si256(v1, v2);
                    v1 = Avx2.mm256_xor_si256(Avx2.mm256_slli_epi32(v1, 7), Avx2.mm256_srli_epi32(v1, 25));
                
                    v1 = Avx2.mm256_shuffle_epi32(v1, 0x39);
                    v2 = Avx2.mm256_shuffle_epi32(v2, 0x4E);
                    v3 = Avx2.mm256_shuffle_epi32(v3, 0x93);
                
                    v0 = Avx2.mm256_add_epi32(v0, v1);
                    v3 = Avx2.mm256_xor_si256(v3, v0);
                    v3 = Avx2.mm256_xor_si256(Avx2.mm256_slli_epi32(v3, 16), Avx2.mm256_srli_epi32(v3, 16));
                    v2 = Avx2.mm256_add_epi32(v2, v3);
                    v1 = Avx2.mm256_xor_si256(v1, v2);
                    v1 = Avx2.mm256_xor_si256(Avx2.mm256_slli_epi32(v1, 12), Avx2.mm256_srli_epi32(v1, 20));
                    v0 = Avx2.mm256_add_epi32(v0, v1);
                    v3 = Avx2.mm256_xor_si256(v3, v0);
                    v3 = Avx2.mm256_xor_si256(Avx2.mm256_slli_epi32(v3, 8), Avx2.mm256_srli_epi32(v3, 24));
                    v2 = Avx2.mm256_add_epi32(v2, v3);
                    v1 = Avx2.mm256_xor_si256(v1, v2);
                    v1 = Avx2.mm256_xor_si256(Avx2.mm256_slli_epi32(v1, 7), Avx2.mm256_srli_epi32(v1, 25));
                
                    v1 = Avx2.mm256_shuffle_epi32(v1, 0x93);
                    v2 = Avx2.mm256_shuffle_epi32(v2, 0x4E);
                    v3 = Avx2.mm256_shuffle_epi32(v3, 0x39);
                }
                
                v0 = Avx2.mm256_add_epi32(v0, x0);
                v1 = Avx2.mm256_add_epi32(v1, x1);
                v2 = Avx2.mm256_add_epi32(v2, x2);
                v3 = Avx2.mm256_add_epi32(v3, x3);
                
                var n0 = Avx2.mm256_permute2x128_si256(v0, v1, 0x20);
                var n1 = Avx2.mm256_permute2x128_si256(v2, v3, 0x20);
                var n2 = Avx2.mm256_permute2x128_si256(v0, v1, 0x31);
                var n3 = Avx2.mm256_permute2x128_si256(v2, v3, 0x31);
            
                ulong* uInput = (ulong*)input;
                n0 = Avx2.mm256_xor_si256(n0, new v256(uInput[0], uInput[1], uInput[2], uInput[3])); // Load256_Byte(input)
                n1 = Avx2.mm256_xor_si256(n1, new v256(uInput[4], uInput[5], uInput[6], uInput[7])); // Load256_Byte(input[0x20..])
                n2 = Avx2.mm256_xor_si256(n2, new v256(uInput[8], uInput[9], uInput[10], uInput[11])); // Load256_Byte(input[0x40..])
                n3 = Avx2.mm256_xor_si256(n3, new v256(uInput[12], uInput[13], uInput[14], uInput[15])); // Load256_Byte(input[0x60..])
            
                ulong* uOutput = (ulong*)output;
                uOutput[0] = n0.ULong0; uOutput[1] = n0.ULong1; uOutput[2] = n0.ULong2; uOutput[3] = n0.ULong3; //Store256_Byte(n0, output);
                uOutput[4] = n1.ULong0; uOutput[5] = n1.ULong1; uOutput[6] = n1.ULong2; uOutput[7] = n1.ULong3; //Store256_Byte(n1, output[0x20..]);
                uOutput[8] = n2.ULong0; uOutput[9] = n2.ULong1; uOutput[10] = n2.ULong2; uOutput[11] = n2.ULong3; //Store256_Byte(n2, output[0x40..]);
                uOutput[12] = n3.ULong0; uOutput[13] = n3.ULong1; uOutput[14] = n3.ULong2; uOutput[15] = n3.ULong3; //Store256_Byte(n3, output[0x60..]);
            }
            else if (Sse2.IsSse2Supported)
            {
                var x0 = Sse2.loadu_si128(state); //new v128(state[0], state[1], state[2], state[3]); //Load128_UInt32(state.AsSpan());
                var x1 = Sse2.loadu_si128(state + 4); //new v128(state[4], state[5], state[6], state[7]); //Load128_UInt32(state.AsSpan(4));
                var x2 = Sse2.loadu_si128(state + 8); //new v128(state[8], state[9], state[10], state[11]); //Load128_UInt32(state.AsSpan(8));
                var x3 = Sse2.loadu_si128(state + 12); //new v128(state[12], state[13], state[14], state[15]); //Load128_UInt32(state.AsSpan(12));
                ++state[12];
                
                var v0 = x0;
                var v1 = x1;
                var v2 = x2;
                var v3 = x3;
                
                for (int i = rounds; i > 0; i -= 2)
                {
                    v0 = Sse2.add_epi32(v0, v1);
                    v3 = Sse2.xor_si128(v3, v0);
                    v3 = Sse2.xor_si128(Sse2.slli_epi32(v3, 16), Sse2.srli_epi32(v3, 16));
                    v2 = Sse2.add_epi32(v2, v3);
                    v1 = Sse2.xor_si128(v1, v2);
                    v1 = Sse2.xor_si128(Sse2.slli_epi32(v1, 12), Sse2.srli_epi32(v1, 20));
                    v0 = Sse2.add_epi32(v0, v1);
                    v3 = Sse2.xor_si128(v3, v0);
                    v3 = Sse2.xor_si128(Sse2.slli_epi32(v3, 8), Sse2.srli_epi32(v3, 24));
                    v2 = Sse2.add_epi32(v2, v3);
                    v1 = Sse2.xor_si128(v1, v2);
                    v1 = Sse2.xor_si128(Sse2.slli_epi32(v1, 7), Sse2.srli_epi32(v1, 25));
                
                    v1 = Sse2.shuffle_epi32(v1, 0x39);
                    v2 = Sse2.shuffle_epi32(v2, 0x4E);
                    v3 = Sse2.shuffle_epi32(v3, 0x93);
                
                    v0 = Sse2.add_epi32(v0, v1);
                    v3 = Sse2.xor_si128(v3, v0);
                    v3 = Sse2.xor_si128(Sse2.slli_epi32(v3, 16), Sse2.srli_epi32(v3, 16));
                    v2 = Sse2.add_epi32(v2, v3);
                    v1 = Sse2.xor_si128(v1, v2);
                    v1 = Sse2.xor_si128(Sse2.slli_epi32(v1, 12), Sse2.srli_epi32(v1, 20));
                    v0 = Sse2.add_epi32(v0, v1);
                    v3 = Sse2.xor_si128(v3, v0);
                    v3 = Sse2.xor_si128(Sse2.slli_epi32(v3, 8), Sse2.srli_epi32(v3, 24));
                    v2 = Sse2.add_epi32(v2, v3);
                    v1 = Sse2.xor_si128(v1, v2);
                    v1 = Sse2.xor_si128(Sse2.slli_epi32(v1, 7), Sse2.srli_epi32(v1, 25));
                
                    v1 = Sse2.shuffle_epi32(v1, 0x93);
                    v2 = Sse2.shuffle_epi32(v2, 0x4E);
                    v3 = Sse2.shuffle_epi32(v3, 0x39);
                }
                
                v0 = Sse2.add_epi32(v0, x0);
                v1 = Sse2.add_epi32(v1, x1);
                v2 = Sse2.add_epi32(v2, x2);
                v3 = Sse2.add_epi32(v3, x3);
                
                var n0 = Sse2.loadu_si128(input + 0x00); //Load128_Byte(input);
                var n1 = Sse2.loadu_si128(input + 0x10); //Load128_Byte(input[0x10..]);
                var n2 = Sse2.loadu_si128(input + 0x20); //Load128_Byte(input[0x20..]);
                var n3 = Sse2.loadu_si128(input + 0x30); //Load128_Byte(input[0x30..]);
                
                n0 = Sse2.xor_si128(n0, v0);
                n1 = Sse2.xor_si128(n1, v1);
                n2 = Sse2.xor_si128(n2, v2);
                n3 = Sse2.xor_si128(n3, v3);
                
                Sse2.storeu_si128(output + 0x00, n0); //Store128_Byte(n0, output);
                Sse2.storeu_si128(output + 0x10, n1); //Store128_Byte(n1, output[0x10..]);
                Sse2.storeu_si128(output + 0x20, n2); //Store128_Byte(n2, output[0x20..]);
                Sse2.storeu_si128(output + 0x30, n3); //Store128_Byte(n3, output[0x30..]);
            
            
                x3 = Sse2.loadu_si128(state + 12); // Load128_UInt32(state.AsSpan(12));
                ++state[12];
                
                v0 = x0;
                v1 = x1;
                v2 = x2;
                v3 = x3;
                
                for (int i = rounds; i > 0; i -= 2)
                {
                    v0 = Sse2.add_epi32(v0, v1);
                    v3 = Sse2.xor_si128(v3, v0);
                    v3 = Sse2.xor_si128(Sse2.slli_epi32(v3, 16), Sse2.srli_epi32(v3, 16));
                    v2 = Sse2.add_epi32(v2, v3);
                    v1 = Sse2.xor_si128(v1, v2);
                    v1 = Sse2.xor_si128(Sse2.slli_epi32(v1, 12), Sse2.srli_epi32(v1, 20));
                    v0 = Sse2.add_epi32(v0, v1);
                    v3 = Sse2.xor_si128(v3, v0);
                    v3 = Sse2.xor_si128(Sse2.slli_epi32(v3, 8), Sse2.srli_epi32(v3, 24));
                    v2 = Sse2.add_epi32(v2, v3);
                    v1 = Sse2.xor_si128(v1, v2);
                    v1 = Sse2.xor_si128(Sse2.slli_epi32(v1, 7), Sse2.srli_epi32(v1, 25));
                
                    v1 = Sse2.shuffle_epi32(v1, 0x39);
                    v2 = Sse2.shuffle_epi32(v2, 0x4E);
                    v3 = Sse2.shuffle_epi32(v3, 0x93);
                
                    v0 = Sse2.add_epi32(v0, v1);
                    v3 = Sse2.xor_si128(v3, v0);
                    v3 = Sse2.xor_si128(Sse2.slli_epi32(v3, 16), Sse2.srli_epi32(v3, 16));
                    v2 = Sse2.add_epi32(v2, v3);
                    v1 = Sse2.xor_si128(v1, v2);
                    v1 = Sse2.xor_si128(Sse2.slli_epi32(v1, 12), Sse2.srli_epi32(v1, 20));
                    v0 = Sse2.add_epi32(v0, v1);
                    v3 = Sse2.xor_si128(v3, v0);
                    v3 = Sse2.xor_si128(Sse2.slli_epi32(v3, 8), Sse2.srli_epi32(v3, 24));
                    v2 = Sse2.add_epi32(v2, v3);
                    v1 = Sse2.xor_si128(v1, v2);
                    v1 = Sse2.xor_si128(Sse2.slli_epi32(v1, 7), Sse2.srli_epi32(v1, 25));
                
                    v1 = Sse2.shuffle_epi32(v1, 0x93);
                    v2 = Sse2.shuffle_epi32(v2, 0x4E);
                    v3 = Sse2.shuffle_epi32(v3, 0x39);
                }
                
                v0 = Sse2.add_epi32(v0, x0);
                v1 = Sse2.add_epi32(v1, x1);
                v2 = Sse2.add_epi32(v2, x2);
                v3 = Sse2.add_epi32(v3, x3);
                
                n0 = Sse2.loadu_si128(input + 0x40); //Load128_Byte(input[0x40..]);
                n1 = Sse2.loadu_si128(input + 0x50); //Load128_Byte(input[0x50..]);
                n2 = Sse2.loadu_si128(input + 0x60); //Load128_Byte(input[0x60..]);
                n3 = Sse2.loadu_si128(input + 0x70); //Load128_Byte(input[0x70..]);
                
                n0 = Sse2.xor_si128(n0, v0);
                n1 = Sse2.xor_si128(n1, v1);
                n2 = Sse2.xor_si128(n2, v2);
                n3 = Sse2.xor_si128(n3, v3);
                
                Sse2.storeu_si128(output + 0x40, n0); //Store128_Byte(n0, output[0x40..]);
                Sse2.storeu_si128(output + 0x50, n1); //Store128_Byte(n1, output[0x50..]);
                Sse2.storeu_si128(output + 0x60, n2); //Store128_Byte(n2, output[0x60..]);
                Sse2.storeu_si128(output + 0x70, n3); //Store128_Byte(n3, output[0x70..]);
            }
            else if (Neon.IsNeonSupported)
            {
                var x0 = Neon.vld1q_u32(state); //new v128(state[0], state[1], state[2], state[3]); //Load128_UInt32(state.AsSpan());
                var x1 = Neon.vld1q_u32(state + 4); //new v128(state[4], state[5], state[6], state[7]); //Load128_UInt32(state.AsSpan(4));
                var x2 = Neon.vld1q_u32(state + 8); //new v128(state[8], state[9], state[10], state[11]); //Load128_UInt32(state.AsSpan(8));
                var x3 = Neon.vld1q_u32(state + 12);
                ++state[12];

                var v0 = x0;
                var v1 = x1;
                var v2 = x2;
                var v3 = x3;

                for (int i = rounds; i > 0; i -= 2)
                {
                    v0 = Neon.vaddq_u32(v0, v1);
                    v3 = Neon.veorq_u32(v3, v0);
                    v3 = Neon.veorq_u32(Neon.vshlq_n_u32(v3, 16), Neon.vshrq_n_u32(v3, 16));
                    v2 = Neon.vaddq_u32(v2, v3);
                    v1 = Neon.veorq_u32(v1, v2);
                    v1 = Neon.veorq_u32(Neon.vshlq_n_u32(v1, 12), Neon.vshrq_n_u32(v1, 20));
                    v0 = Neon.vaddq_u32(v0, v1);
                    v3 = Neon.veorq_u32(v3, v0);
                    v3 = Neon.veorq_u32(Neon.vshlq_n_u32(v3, 8), Neon.vshrq_n_u32(v3, 24));
                    v2 = Neon.vaddq_u32(v2, v3);
                    v1 = Neon.veorq_u32(v1, v2);
                    v1 = Neon.veorq_u32(Neon.vshlq_n_u32(v1, 7), Neon.vshrq_n_u32(v1, 25));

                    ///*v1 = */Neon_shuffle_epi32(v1, 0x39, out v1);
                    v128 ret;
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v1, (0x39) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x39) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x39) >> 4) & 0x3), ret, 2);
                    v1 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x39) >> 6) & 0x3), ret, 3);
                    
                    ///*v2 = */Neon_shuffle_epi32(v2, 0x4E, out v2);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v2,  (0x4E) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 4) & 0x3), ret, 2);
                    v2 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 6) & 0x3), ret, 3);
                    
                    ///*v3 = */Neon_shuffle_epi32(v3, 0x93, out v3);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v3, (0x93) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x93) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x93) >> 4) & 0x3), ret, 2);
                    v3 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x93) >> 6) & 0x3), ret, 3);
                    
                    v0 = Neon.vaddq_u32(v0, v1);
                    v3 = Neon.veorq_u32(v3, v0);
                    v3 = Neon.veorq_u32(Neon.vshlq_n_u32(v3, 16), Neon.vshrq_n_u32(v3, 16));
                    v2 = Neon.vaddq_u32(v2, v3);
                    v1 = Neon.veorq_u32(v1, v2);
                    v1 = Neon.veorq_u32(Neon.vshlq_n_u32(v1, 12), Neon.vshrq_n_u32(v1, 20));
                    v0 = Neon.vaddq_u32(v0, v1);
                    v3 = Neon.veorq_u32(v3, v0);
                    v3 = Neon.veorq_u32(Neon.vshlq_n_u32(v3, 8), Neon.vshrq_n_u32(v3, 24));
                    v2 = Neon.vaddq_u32(v2, v3);
                    v1 = Neon.veorq_u32(v1, v2);
                    v1 = Neon.veorq_u32(Neon.vshlq_n_u32(v1, 7), Neon.vshrq_n_u32(v1, 25));

                    ///*v1 = */Neon_shuffle_epi32(v1, 0x93, out v1);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v1, (0x93) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x93) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x93) >> 4) & 0x3), ret, 2);
                    v1 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x93) >> 6) & 0x3), ret, 3);
                    
                    ///*v2 = */Neon_shuffle_epi32(v2, 0x4E, out v2);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v2, (0x4E) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 4) & 0x3), ret, 2);
                    v2 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 6) & 0x3), ret, 3);
                    
                    ///*v3 = */Neon_shuffle_epi32(v3, 0x39, out v3);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v3, (0x39) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x39) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x39) >> 4) & 0x3), ret, 2);
                    v3 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x39) >> 6) & 0x3), ret, 3);
                }

                v0 = Neon.vaddq_u32(v0, x0);
                v1 = Neon.vaddq_u32(v1, x1);
                v2 = Neon.vaddq_u32(v2, x2);
                v3 = Neon.vaddq_u32(v3, x3);

                var n0 = Neon.vld1q_u32((uint*)(input + 0x00)); //Load128_Byte(input);
                var n1 = Neon.vld1q_u32((uint*)(input + 0x10)); //Load128_Byte(input[0x10..]);
                var n2 = Neon.vld1q_u32((uint*)(input + 0x20)); //Load128_Byte(input[0x20..]);
                var n3 = Neon.vld1q_u32((uint*)(input + 0x30)); //Load128_Byte(input[0x30..]);

                n0 = Neon.veorq_u32(n0, v0);
                n1 = Neon.veorq_u32(n1, v1);
                n2 = Neon.veorq_u32(n2, v2);
                n3 = Neon.veorq_u32(n3, v3);

                Neon.vst1q_u32((uint*)(output + 0x00), n0); //Store128_Byte(n0, output);
                Neon.vst1q_u32((uint*)(output + 0x10), n1); //Store128_Byte(n1, output[0x10..]);
                Neon.vst1q_u32((uint*)(output + 0x20), n2); //Store128_Byte(n2, output[0x20..]);
                Neon.vst1q_u32((uint*)(output + 0x30), n3); //Store128_Byte(n3, output[0x30..]);


                x3 = Neon.vld1q_u32(state + 12); // Load128_UInt32(state.AsSpan(12));
                ++state[12];

                v0 = x0;
                v1 = x1;
                v2 = x2;
                v3 = x3;

                for (int i = rounds; i > 0; i -= 2)
                {
                    v0 = Neon.vaddq_u32(v0, v1);
                    v3 = Neon.veorq_u32(v3, v0);
                    v3 = Neon.veorq_u32(Neon.vshlq_n_u32(v3, 16), Neon.vshrq_n_u32(v3, 16));
                    v2 = Neon.vaddq_u32(v2, v3);
                    v1 = Neon.veorq_u32(v1, v2);
                    v1 = Neon.veorq_u32(Neon.vshlq_n_u32(v1, 12), Neon.vshrq_n_u32(v1, 20));
                    v0 = Neon.vaddq_u32(v0, v1);
                    v3 = Neon.veorq_u32(v3, v0);
                    v3 = Neon.veorq_u32(Neon.vshlq_n_u32(v3, 8), Neon.vshrq_n_u32(v3, 24));
                    v2 = Neon.vaddq_u32(v2, v3);
                    v1 = Neon.veorq_u32(v1, v2);
                    v1 = Neon.veorq_u32(Neon.vshlq_n_u32(v1, 7), Neon.vshrq_n_u32(v1, 25));

                    ///*v1 = */Neon_shuffle_epi32(v1, 0x39, out v1);
                    v128 ret;
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v1, (0x39) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x39) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x39) >> 4) & 0x3), ret, 2);
                    v1 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x39) >> 6) & 0x3), ret, 3);
                    
                    ///*v2 = */Neon_shuffle_epi32(v2, 0x4E, out v2);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v2, (0x4E) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 4) & 0x3), ret, 2);
                    v2 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 6) & 0x3), ret, 3);
                    
                    ///*v3 = */Neon_shuffle_epi32(v3, 0x93, out v3);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v3, (0x93) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x93) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x93) >> 4) & 0x3), ret, 2);
                    v3 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x93) >> 6) & 0x3), ret, 3);
                    
                    v0 = Neon.vaddq_u32(v0, v1);
                    v3 = Neon.veorq_u32(v3, v0);
                    v3 = Neon.veorq_u32(Neon.vshlq_n_u32(v3, 16), Neon.vshrq_n_u32(v3, 16));
                    v2 = Neon.vaddq_u32(v2, v3);
                    v1 = Neon.veorq_u32(v1, v2);
                    v1 = Neon.veorq_u32(Neon.vshlq_n_u32(v1, 12), Neon.vshrq_n_u32(v1, 20));
                    v0 = Neon.vaddq_u32(v0, v1);
                    v3 = Neon.veorq_u32(v3, v0);
                    v3 = Neon.veorq_u32(Neon.vshlq_n_u32(v3, 8), Neon.vshrq_n_u32(v3, 24));
                    v2 = Neon.vaddq_u32(v2, v3);
                    v1 = Neon.veorq_u32(v1, v2);
                    v1 = Neon.veorq_u32(Neon.vshlq_n_u32(v1, 7), Neon.vshrq_n_u32(v1, 25));

                    ///*v1 = */Neon_shuffle_epi32(v1, 0x93, out v1);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v1, (0x93) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x93) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x93) >> 4) & 0x3), ret, 2);
                    v1 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v1, ((0x93) >> 6) & 0x3), ret, 3);
                    
                    ///*v2 = */Neon_shuffle_epi32(v2, 0x4E, out v2);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v2, (0x4E) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 4) & 0x3), ret, 2);
                    v2 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v2, ((0x4E) >> 6) & 0x3), ret, 3);
                    
                    ///*v3 = */Neon_shuffle_epi32(v3, 0x39, out v3);
                    ret = Neon.vmovq_n_u32(Neon.vgetq_lane_u32(v3, (0x39) & 0x3));
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x39) >> 2) & 0x3), ret, 1);
                    ret = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x39) >> 4) & 0x3), ret, 2);
                    v3 = Neon.vsetq_lane_u32(Neon.vgetq_lane_u32(v3, ((0x39) >> 6) & 0x3), ret, 3);
                }

                v0 = Neon.vaddq_u32(v0, x0);
                v1 = Neon.vaddq_u32(v1, x1);
                v2 = Neon.vaddq_u32(v2, x2);
                v3 = Neon.vaddq_u32(v3, x3);

                n0 = Neon.vld1q_u32((uint*)(input + 0x40)); //Load128_Byte(input[0x40..]);
                n1 = Neon.vld1q_u32((uint*)(input + 0x50)); //Load128_Byte(input[0x50..]);
                n2 = Neon.vld1q_u32((uint*)(input + 0x60)); //Load128_Byte(input[0x60..]);
                n3 = Neon.vld1q_u32((uint*)(input + 0x70)); //Load128_Byte(input[0x70..]);

                n0 = Neon.veorq_u32(n0, v0);
                n1 = Neon.veorq_u32(n1, v1);
                n2 = Neon.veorq_u32(n2, v2);
                n3 = Neon.veorq_u32(n3, v3);

                Neon.vst1q_u32((uint*)(output + 0x40), n0); //Store128_Byte(n0, output[0x40..]);
                Neon.vst1q_u32((uint*)(output + 0x50), n1); //Store128_Byte(n1, output[0x50..]);
                Neon.vst1q_u32((uint*)(output + 0x60), n2); //Store128_Byte(n2, output[0x60..]);
                Neon.vst1q_u32((uint*)(output + 0x70), n3); //Store128_Byte(n3, output[0x70..]);
            }
            else
            {
                // Inlined to two ImplProcessBlock calls:
                //ImplProcessBlock(input, output);
                //ImplProcessBlock(input[64..], output[64..]);

                FastChaChaEngineHelper.ChachaCoreImpl(rounds, state, keyStream);
                ++state[12];
                ulong* pulinput = (ulong*)input;
                ulong* puloutput = (ulong*)output;
                ulong* pulkeyStream = (ulong*)keyStream;

                puloutput[0] = pulkeyStream[0] ^ pulinput[0];
                puloutput[1] = pulkeyStream[1] ^ pulinput[1];
                puloutput[2] = pulkeyStream[2] ^ pulinput[2];
                puloutput[3] = pulkeyStream[3] ^ pulinput[3];

                puloutput[4] = pulkeyStream[4] ^ pulinput[4];
                puloutput[5] = pulkeyStream[5] ^ pulinput[5];
                puloutput[6] = pulkeyStream[6] ^ pulinput[6];
                puloutput[7] = pulkeyStream[7] ^ pulinput[7];

                FastChaChaEngineHelper.ChachaCoreImpl(rounds, state, keyStream);
                ++state[12];

                pulinput = (ulong*)&input[64];
                puloutput = (ulong*)&output[64];

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
    }
}
#endif
