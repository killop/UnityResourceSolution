#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;

namespace BestHTTP.Connections.TLS.Crypto.Impl
{
    internal static class FastAesEngineHelper
    {
        public unsafe static void EncryptBlock(byte[] input, int inOff, byte[] output, int outOff, uint[][] KW, int ROUNDS, uint[] T0, byte[] S, byte[] s)
        {
            uint C0 = Pack.LE_To_UInt32(input, inOff + 0);
            uint C1 = Pack.LE_To_UInt32(input, inOff + 4);
            uint C2 = Pack.LE_To_UInt32(input, inOff + 8);
            uint C3 = Pack.LE_To_UInt32(input, inOff + 12);

            uint[] kw = KW[0];
            uint t0 = C0 ^ kw[0];
            uint t1 = C1 ^ kw[1];
            uint t2 = C2 ^ kw[2];

            uint r0, r1, r2, r3 = C3 ^ kw[3];
            int r = 1;

            byte idx;
            uint tmp1, tmp2, tmp3;

            fixed (uint* pT0 = T0)
            {
                while (r < ROUNDS - 1)
                {
                    kw = KW[r++];

                    fixed (uint* pkw = kw)
                    {
                        idx = (byte)(t1 >> 8);
                        tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                        idx = (byte)(t2 >> 16);
                        tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                        idx = (byte)(r3 >> 24);
                        tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);
                        r0 = pT0[t0 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[0];

                        idx = (byte)(t2 >> 8);
                        tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                        idx = (byte)(r3 >> 16);
                        tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                        idx = (byte)(t0 >> 24);
                        tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);
                        r1 = pT0[t1 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[1];

                        idx = (byte)(r3 >> 8);
                        tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                        idx = (byte)(t0 >> 16);
                        tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                        idx = (byte)(t1 >> 24);
                        tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);

                        r2 = pT0[t2 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[2];

                        idx = (byte)(t0 >> 8);
                        tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                        idx = (byte)(t1 >> 16);
                        tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                        idx = (byte)(t2 >> 24);
                        tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);

                        r3 = pT0[r3 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[3];
                    }

                    kw = KW[r++];

                    fixed (uint* pkw = kw)
                    {
                        idx = (byte)(r1 >> 8);
                        tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                        idx = (byte)(r2 >> 16);
                        tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                        idx = (byte)(r3 >> 24);
                        tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);
                        t0 = pT0[r0 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[0];

                        idx = (byte)(r2 >> 8);
                        tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                        idx = (byte)(r3 >> 16);
                        tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                        idx = (byte)(r0 >> 24);
                        tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);
                        t1 = pT0[r1 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[1];

                        idx = (byte)(r3 >> 8);
                        tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                        idx = (byte)(r0 >> 16);
                        tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                        idx = (byte)(r1 >> 24);
                        tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);

                        t2 = pT0[r2 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[2];

                        idx = (byte)(r0 >> 8);
                        tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                        idx = (byte)(r1 >> 16);
                        tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                        idx = (byte)(r2 >> 24);
                        tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);
                        r3 = pT0[r3 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[3];
                    }
                }

                kw = KW[r++];

                fixed (uint* pkw = kw)
                {
                    idx = (byte)(t1 >> 8);
                    tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                    idx = (byte)(t2 >> 16);
                    tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                    idx = (byte)(r3 >> 24);
                    tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);
                    r0 = pT0[t0 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[0];

                    idx = (byte)(t2 >> 8);
                    tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                    idx = (byte)(r3 >> 16);
                    tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                    idx = (byte)(t0 >> 24);
                    tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);

                    r1 = pT0[t1 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[1];

                    idx = (byte)(r3 >> 8);
                    tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                    idx = (byte)(t0 >> 16);
                    tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                    idx = (byte)(t1 >> 24);
                    tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);

                    r2 = pT0[t2 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[2];

                    idx = (byte)(t0 >> 8);
                    tmp1 = (pT0[idx] >> 24) | (pT0[idx] << 8);

                    idx = (byte)(t1 >> 16);
                    tmp2 = (pT0[idx] >> 16) | (pT0[idx] << 16);

                    idx = (byte)(t2 >> 24);
                    tmp3 = (pT0[idx] >> 8) | (pT0[idx] << 24);

                    r3 = pT0[r3 & 255] ^ tmp1 ^ tmp2 ^ tmp3 ^ pkw[3];
                }

                // the final round's table is a simple function of S so we don't use a whole other four tables for it

                kw = KW[r];

                fixed (byte* pS = S, ps = s)
                fixed (uint* pkw = kw)
                {
                    C0 = (uint)pS[(byte)r0] ^ (((uint)pS[(byte)(r1 >> 8)]) << 8) ^ (((uint)ps[(byte)(r2 >> 16)]) << 16) ^ (((uint)ps[(byte)(r3 >> 24)]) << 24) ^ pkw[0];
                    C1 = (uint)ps[(byte)r1] ^ (((uint)pS[(byte)(r2 >> 8)]) << 8) ^ (((uint)pS[(byte)(r3 >> 16)]) << 16) ^ (((uint)ps[(byte)(r0 >> 24)]) << 24) ^ pkw[1];
                    C2 = (uint)ps[(byte)r2] ^ (((uint)pS[(byte)(r3 >> 8)]) << 8) ^ (((uint)pS[(byte)(r0 >> 16)]) << 16) ^ (((uint)pS[(byte)(r1 >> 24)]) << 24) ^ pkw[2];
                    C3 = (uint)ps[(byte)r3] ^ (((uint)ps[(byte)(r0 >> 8)]) << 8) ^ (((uint)ps[(byte)(r1 >> 16)]) << 16) ^ (((uint)pS[(byte)(r2 >> 24)]) << 24) ^ pkw[3];
                }
            }

            Pack.UInt32_To_LE(C0, output, outOff + 0);
            Pack.UInt32_To_LE(C1, output, outOff + 4);
            Pack.UInt32_To_LE(C2, output, outOff + 8);
            Pack.UInt32_To_LE(C3, output, outOff + 12);
        }
    }
}
#endif
