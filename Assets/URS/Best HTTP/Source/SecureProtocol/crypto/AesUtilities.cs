#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto
{
    public static class AesUtilities
    {
        public static IBlockCipher CreateEngine()
        {
#if NETCOREAPP3_0_OR_GREATER
            if (AesEngine_X86.IsSupported)
                return new AesEngine_X86();
#endif

            return new AesEngine();
        }

#if NETCOREAPP3_0_OR_GREATER
        public static bool IsHardwareAccelerated => AesEngine_X86.IsSupported;
#else
        public static bool IsHardwareAccelerated => false;
#endif
    }
}
#pragma warning restore
#endif
