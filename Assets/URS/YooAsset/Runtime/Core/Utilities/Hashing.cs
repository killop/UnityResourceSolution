using System;
using System.IO;
using System.Security.Cryptography;
using MHLab.Patch.Core.IO;
using Standart.Hash.xxHash;

namespace MHLab.Patch.Core.Utilities
{
    public static class Hashing
    {
        private const long IOBufferSize = 1024 * 1024 * 8;

        public static string GetFileHash(string filePath, IFileSystem fileSystem)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
                return GetHash(filePath, sha1, fileSystem);
        }

        public static uint GetFileXXhash(string filePath, IFileSystem fileSystem) 
        {
            using (var fs = fileSystem.GetFileStream((FilePath)filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return xxHash32.ComputeHash(fs);
            }
        }
        public static uint GetFileXXhash(string filePath)
        {
            using (var fs = new  FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return xxHash32.ComputeHash(fs);
            }
        }
        private static string GetHash(string filePath, HashAlgorithm hasher, IFileSystem fileSystem)
        {
            using (var fs = fileSystem.GetFileStream((FilePath)filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var bufferSize = (int)Math.Max(1, Math.Min(fs.Length, IOBufferSize));
                using (var bs = new BufferedStream(fs, bufferSize))
                    return GetHash(bs, hasher);
            }
        }

        private static string GetHash(Stream s, HashAlgorithm hasher)
        {
            var hash    = hasher.ComputeHash(s);
            var hashStr = Convert.ToBase64String(hash);
            return hashStr.TrimEnd('=');
        }
    }
}