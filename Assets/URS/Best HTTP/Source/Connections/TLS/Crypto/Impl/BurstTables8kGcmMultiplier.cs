#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Runtime.CompilerServices;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes.Gcm;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

#if BESTHTTP_WITH_BURST
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace BestHTTP.Connections.TLS.Crypto.Impl
{
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
#if BESTHTTP_WITH_BURST
    [BurstCompile]
#endif
    public sealed class BurstTables8kGcmMultiplier //: IGcmMultiplier
    {
        private byte[] H;
        private GcmUtilities.FieldElement[][] T;

        public void Init(byte[] H)
        {
            if (T == null)
            {
                T = new GcmUtilities.FieldElement[2][];
            }
            else if (Arrays.AreEqual(this.H, H))
            {
                return;
            }

            if (this.H == null)
                this.H = Arrays.Clone(H);
            else
            {
                if (this.H.Length != H.Length)
                    Array.Resize(ref this.H, H.Length);

                Array.Copy(H, this.H, H.Length);
            }

            for (int i = 0; i < 2; ++i)
            {
                if (T[i] == null)
                    T[i] = new GcmUtilities.FieldElement[256];

                GcmUtilities.FieldElement[] t = T[i];

                // t[0] = 0

                if (i == 0)
                {
                    // t[1] = H.p^7
                    GcmUtilities.AsFieldElement(this.H, out t[1]);
                    GcmUtilities.MultiplyP7(ref t[1]);
                }
                else
                {
                    // t[1] = T[i-1][1].p^8
                    GcmUtilities.MultiplyP8(ref T[i - 1][1], out t[1]);
                }

                for (int n = 1; n < 128; ++n)
                {
                    // t[2.n] = t[n].p^-1
                    GcmUtilities.DivideP(ref t[n], out t[n << 1]);

                    // t[2.n + 1] = t[2.n] + t[1]
                    GcmUtilities.Xor(ref t[n << 1], ref t[1], out t[(n << 1) + 1]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void MultiplyH(byte[] x)
        {
            fixed (byte* px = x)
            fixed (GcmUtilities.FieldElement* pT0 = this.T[0])
            fixed (GcmUtilities.FieldElement* pT1 = this.T[1])
                MultiplyHImpl(px, pT0, pT1);
        }

#if BESTHTTP_WITH_BURST
        [BurstCompile]
#endif
        private static unsafe void MultiplyHImpl(
#if BESTHTTP_WITH_BURST
            [NoAlias]
#endif
            byte* px,
#if BESTHTTP_WITH_BURST
            [NoAlias]
#endif
            GcmUtilities.FieldElement* pT0,
#if BESTHTTP_WITH_BURST
            [NoAlias]
#endif
            GcmUtilities.FieldElement* pT1)
        {
            int vPos = px[15];
            int uPos = px[14];
            ulong z1 = pT0[uPos].n1 ^ pT1[vPos].n1;
            ulong z0 = pT0[uPos].n0 ^ pT1[vPos].n0;

            for (int i = 12; i >= 0; i -= 2)
            {
                vPos = px[i + 1];
                uPos = px[i];

                ulong c = z1 << 48;
                z1 = pT0[uPos].n1 ^ pT1[vPos].n1 ^ ((z1 >> 16) | (z0 << 48));
                z0 = pT0[uPos].n0 ^ pT1[vPos].n0 ^ (z0 >> 16) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            }

            //GcmUtilities.AsBytes(z0, z1, x);

            //UInt32_To_BE((uint)(n >> 32), bs, off);
            uint n = (uint)(z0 >> 32);
            px[0] = (byte)(n >> 24);
            px[1] = (byte)(n >> 16);
            px[2] = (byte)(n >> 8);
            px[3] = (byte)(n);
            //UInt32_To_BE((uint)(n), bs, off + 4);
            n = (uint)(z0);
            px[4] = (byte)(n >> 24);
            px[5] = (byte)(n >> 16);
            px[6] = (byte)(n >> 8);
            px[7] = (byte)(n);
            
            n = (uint)(z1 >> 32);
            px[8] = (byte)(n >> 24);
            px[9] = (byte)(n >> 16);
            px[10] = (byte)(n >> 8);
            px[11] = (byte)(n);
            //UInt32_To_BE((uint)(n), bs, off + 4);
            n = (uint)(z1);
            px[12] = (byte)(n >> 24);
            px[13] = (byte)(n >> 16);
            px[14] = (byte)(n >> 8);
            px[15] = (byte)(n);
        }
    }
}
#pragma warning restore
#endif
