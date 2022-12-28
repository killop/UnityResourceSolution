#if UNITY_2019_3_OR_NEWER
using System.Security.Cryptography;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    public unsafe sealed class SpookyHash : HashAlgorithm
    {
        Hash128 m_Hash;

        SpookyHash()
        {
            Initialize();
        }

        public new static SpookyHash Create()
        {
            return new SpookyHash();
        }

        public override void Initialize() {}

        protected override void HashCore(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null || inputOffset < 0 || inputCount <= 0 || (inputCount > inputBuffer.Length) || (inputBuffer.Length - inputCount) < inputOffset)
                return;

#if UNITY_2020_1_OR_NEWER
            m_Hash.Append(inputBuffer, inputOffset, inputCount);
#else
            throw new System.InvalidOperationException("SpookyHash implementation was unstable and not deterministic prior to Unity 2020.1. Use MD5 or MD4 instead.");
#endif
        }

        protected override byte[] HashFinal()
        {
#if UNITY_2020_1_OR_NEWER
            byte[] results = new byte[UnsafeUtility.SizeOf<Hash128>()];
            byte* hashPtr = (byte*)UnsafeUtility.AddressOf(ref m_Hash);
            fixed(byte* d = results)
            UnsafeUtility.MemCpy(d, hashPtr, results.Length);
            return results;
#else
            throw new System.InvalidOperationException("SpookyHash implementation was unstable and not deterministic prior to Unity 2020.1. Use MD5 or MD4 instead.");
#endif
        }
    }
}
#endif
