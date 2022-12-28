using System.Security.Cryptography;

namespace MHLab.Patch.Core.Octodiff
{
    internal static class SupportedAlgorithms
    {
        public static class Hashing
        {
            public static IHashAlgorithm Sha1()
            {
                return new HashAlgorithmWrapper("SHA1", SHA1.Create());
            }

            public static IHashAlgorithm Default()
            {
                return Sha1();
            }

            public static IHashAlgorithm Create(string algorithm)
            {
                if (algorithm == "SHA1")
                    return Sha1();

                throw new CompatibilityException(string.Format("The hash algorithm '{0}' is not supported in this version of Octodiff", algorithm));
            }
        }

        public static class Checksum
        {
            public static IRollingChecksum Adler32Rolling() { return new Adler32RollingChecksum();  }

            public static IRollingChecksum Default()
            {
                return Adler32Rolling();
            }

            public static IRollingChecksum Create(string algorithm)
            {
                if (algorithm == "Adler32")
                    return Adler32Rolling();
                throw new CompatibilityException(string.Format("The rolling checksum algorithm '{0}' is not supported in this version of Octodiff", algorithm));
            }
        }
    }
}