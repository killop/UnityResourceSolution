using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Stores hash information as an array of bytes.
    /// </summary>
    [Serializable]
    public struct RawHash : IEquatable<RawHash>
    {
        readonly byte[] m_Hash;

        internal RawHash(byte[] hash)
        {
            m_Hash = hash;
        }

        internal static RawHash Zero()
        {
            return new RawHash(new byte[16]);
        }

        /// <summary>
        /// Converts the hash to bytes.
        /// </summary>
        /// <returns>Returns the converted hash as an array of bytes.</returns>
        public byte[] ToBytes()
        {
            return m_Hash;
        }

        /// <summary>
        /// Converts the hash to <see cref="Hash128"/> format.
        /// </summary>
        /// <returns>Returns the converted hash.</returns>
        public Hash128 ToHash128()
        {
            if (m_Hash == null || m_Hash.Length != 16)
                return new Hash128();

            return new Hash128(BitConverter.ToUInt32(m_Hash, 0), BitConverter.ToUInt32(m_Hash, 4),
                BitConverter.ToUInt32(m_Hash, 8), BitConverter.ToUInt32(m_Hash, 12));
        }

        /// <summary>
        /// Converts the hash to a guid.
        /// </summary>
        /// <returns>Returns the converted hash as a guid.</returns>
        public GUID ToGUID()
        {
            if (m_Hash == null || m_Hash.Length != 16)
                return new GUID();

            return new GUID(ToString());
        }

        /// <summary>
        /// Converts the hash to a formatted string.
        /// </summary>
        /// <returns>Returns the hash as a string.</returns>
        public override string ToString()
        {
            if (m_Hash == null || m_Hash.Length != 16)
                return "00000000000000000000000000000000";

            return BitConverter.ToString(m_Hash).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Determines if the current hash instance is equivalent to the specified hash.
        /// </summary>
        /// <param name="other">The hash to compare to.</param>
        /// <returns>Returns true if the hashes are equivalent. Returns false otherwise.</returns>
        public bool Equals(RawHash other)
        {
            return m_Hash.SequenceEqual(other.m_Hash);
        }

        /// <summary>
        /// Determines if the current hash instance is equivalent to the specified hash.
        /// </summary>
        /// <param name="obj">The hash to compare to.</param>
        /// <returns>Returns true if the hashes are equivalent. Returns false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is RawHash && Equals((RawHash)obj);
        }

        /// <summary>
        /// Creates the hash code for the cache entry.
        /// </summary>
        /// <returns>Returns the hash code for the cache entry.</returns>
        public override int GetHashCode()
        {
            return (m_Hash != null ? m_Hash.GetHashCode() : 0);
        }

        /// <summary>
        /// Determines if the left hash instance is equivalent to the right hash.
        /// </summary>
        /// <param name="left">The hash to compare against.</param>
        /// <param name="right">The hash to compare to.</param>
        /// <returns>Returns true if the hashes are equivalent. Returns false otherwise.</returns>
        public static bool operator==(RawHash left, RawHash right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines if the left hash instance is not equivalent to the right hash.
        /// </summary>
        /// <param name="left">The hash to compare against.</param>
        /// <param name="right">The hash to compare to.</param>
        /// <returns>Returns true if the hashes are not equivalent. Returns false otherwise.</returns>
        public static bool operator!=(RawHash left, RawHash right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Creates the <see cref="RawHash"/> for an object.
    /// </summary>
    public static class HashingMethods
    {
        // TODO: Make this even faster!
        // Maybe use unsafe code to access the raw bytes and pass them directly into the stream?
        // Maybe pass the bytes into the HashAlgorithm directly
        // TODO: Does this handle arrays?
        static void GetRawBytes(Stack<object> state, Stream stream)
        {
            if (state.Count == 0)
                return;

            object currObj = state.Pop();
            if (currObj == null)
                return;

            // Handle basic types
            if (currObj is bool)
            {
                var bytes = BitConverter.GetBytes((bool)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is char)
            {
                var bytes = BitConverter.GetBytes((char)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is double)
            {
                var bytes = BitConverter.GetBytes((double)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is short)
            {
                var bytes = BitConverter.GetBytes((short)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is int)
            {
                var bytes = BitConverter.GetBytes((int)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is long)
            {
                var bytes = BitConverter.GetBytes((long)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is float)
            {
                var bytes = BitConverter.GetBytes((float)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is ushort)
            {
                var bytes = BitConverter.GetBytes((ushort)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is uint)
            {
                var bytes = BitConverter.GetBytes((uint)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is ulong)
            {
                var bytes = BitConverter.GetBytes((ulong)currObj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is byte[])
            {
                var bytes = (byte[])currObj;
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is string)
            {
                byte[] bytes;
                var str = (string)currObj;
                if (str.Any(c => c > 127))
                    bytes = Encoding.Unicode.GetBytes(str);
                else
                    bytes = Encoding.ASCII.GetBytes(str);
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (currObj is GUID)
            {
                byte[] hashBytes = new byte[16];
                IntPtr ptr = Marshal.AllocHGlobal(16);
                Marshal.StructureToPtr((GUID)currObj, ptr, false);
                Marshal.Copy(ptr, hashBytes, 0, 16);
                Marshal.FreeHGlobal(ptr);
                PassBytesToStreamInBlocks(stream, hashBytes, 4);
            }
            else if (currObj is Hash128)
            {
                byte[] hashBytes = new byte[16];
                IntPtr ptr = Marshal.AllocHGlobal(16);
                Marshal.StructureToPtr((Hash128)currObj, ptr, false);
                Marshal.Copy(ptr, hashBytes, 0, 16);
                Marshal.FreeHGlobal(ptr);
                PassBytesToStreamInBlocks(stream, hashBytes, 4);
            }
            else if (currObj.GetType().IsEnum)
            {
                // Handle enums
                var type = Enum.GetUnderlyingType(currObj.GetType());
                var newObj = Convert.ChangeType(currObj, type);
                state.Push(newObj);
            }
            else if (currObj.GetType().IsArray)
            {
                // Handle arrays
                var array = (Array)currObj;
                for (int i = array.Length - 1; i >= 0; i--)
                    state.Push(array.GetValue(i));
            }
            else if (currObj is System.Collections.IEnumerable)
            {
                var iterator = (System.Collections.IEnumerable)currObj;
                var reverseOrder = new Stack<object>();
                foreach (var newObj in iterator)
                    reverseOrder.Push(newObj);
                foreach (var newObj in reverseOrder)
                    state.Push(newObj);
            }
            else
            {
                // Use reflection for remainder
                var fields = currObj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (var index = fields.Length - 1; index >= 0; index--)
                {
                    var field = fields[index];
                    var newObj = field.GetValue(currObj);
                    state.Push(newObj);
                }
            }
        }

        static void GetRawBytes(Stream stream, object obj)
        {
            var objStack = new Stack<object>();
            objStack.Push(obj);
            while (objStack.Count > 0)
                GetRawBytes(objStack, stream);
        }

        static void GetRawBytes(Stream stream, params object[] objects)
        {
            var objStack = new Stack<object>();
            for (var index = objects.Length - 1; index >= 0; index--)
                objStack.Push(objects[index]);
            while (objStack.Count > 0)
                GetRawBytes(objStack, stream);
        }

        internal static HashAlgorithm GetHashAlgorithm()
        {
#if UNITY_2020_1_OR_NEWER
            // New projects on 2021.1 will default useSpookyHash to true
            // Upgraded projects will remain false until they choose to switch
            return ScriptableBuildPipeline.useV2Hasher ? (HashAlgorithm)SpookyHash.Create() : new MD5CryptoServiceProvider();
#else
            return new MD5CryptoServiceProvider();
#endif
        }

        internal static HashAlgorithm GetHashAlgorithm(Type type)
        {
            if (type == null)
            {
#if UNITY_2020_1_OR_NEWER
                // New projects on 2021.1 will default useSpookyHash to true
                // Upgraded projects will remain false until they choose to switch
                type = ScriptableBuildPipeline.useV2Hasher ? typeof(SpookyHash) : typeof(MD5);
#else
                type = typeof(MD5);
#endif
            }

            if (type == typeof(MD4))
                return MD4.Create();
#if UNITY_2019_3_OR_NEWER
            if (type == typeof(SpookyHash))
                return SpookyHash.Create();
#endif

            // TODO: allow user created HashAlgorithms?
            var alggorithm = HashAlgorithm.Create(type.FullName);
            if (alggorithm == null)
                throw new NotImplementedException("Unable to create hash algorithm: '" + type.FullName + "'.");
            return alggorithm;
        }

        /// <summary>
        /// Creates the hash for a stream of data.
        /// </summary>
        /// <param name="stream">The stream of data.</param>
        /// <returns>Returns the hash of the stream.</returns>
        public static RawHash CalculateStream(Stream stream)
        {
            if (stream == null)
                return RawHash.Zero();
            if (stream is HashStream hs)
                return hs.GetHash();

            byte[] hash;
            using (var hashAlgorithm = GetHashAlgorithm())
                hash = hashAlgorithm.ComputeHash(stream);
            return new RawHash(hash);
        }

        /// <summary>
        /// Creates the hash for a stream of data.
        /// </summary>
        /// <typeparam name="T">The hash algorithm type.</typeparam>
        /// <param name="stream">The stream of data.</param>
        /// <returns>Returns the hash of the stream.</returns>
        public static RawHash CalculateStream<T>(Stream stream) where T : HashAlgorithm
        {
            if (stream == null)
                return RawHash.Zero();
            if (stream is HashStream hs)
                return hs.GetHash();

            byte[] hash;
            using (var hashAlgorithm = GetHashAlgorithm(typeof(T)))
                hash = hashAlgorithm.ComputeHash(stream);
            return new RawHash(hash);
        }

        /// <summary>
        /// Creates the hash for an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>Returns the hash of the object.</returns>
        public static RawHash Calculate(object obj)
        {
            RawHash rawHash;
            using (var stream = new HashStream(GetHashAlgorithm()))
            {
                GetRawBytes(stream, obj);
                rawHash = stream.GetHash();
            }
            return rawHash;
        }

        /// <summary>
        /// Creates the hash for a set of objects.
        /// </summary>
        /// <param name="objects">The objects.</param>
        /// <returns>Returns the hash of the set of objects.</returns>
        public static RawHash Calculate(params object[] objects)
        {
            if (objects == null)
                return RawHash.Zero();

            RawHash rawHash;
            using (var stream = new HashStream(GetHashAlgorithm()))
            {
                GetRawBytes(stream, objects);
                rawHash = stream.GetHash();
            }
            return rawHash;
        }

        /// <summary>
        /// Creates the hash for a pair of Hash128 objects.  Optimized specialization of the generic Calculate() methods that has been shown to be ~3x faster
        /// The generic function uses reflection to obtain the four 32bit fields in the Hash128 which is slow, this function uses more direct byte access
        /// </summary>
        /// <param name="hash1">The first hash to combine</param>
        /// <param name="hash2">The second hash to combine</param>
        /// <returns>Returns the combined hash of the two hashes.</returns>
        public static RawHash Calculate(Hash128 hash1, Hash128 hash2)
        {
            byte[] hashBytes = new byte[32];
            IntPtr ptr = Marshal.AllocHGlobal(16);

            Marshal.StructureToPtr(hash1, ptr, false);
            Marshal.Copy(ptr, hashBytes, 0, 16);

            Marshal.StructureToPtr(hash2, ptr, true);
            Marshal.Copy(ptr, hashBytes, 16, 16);

            Marshal.FreeHGlobal(ptr);

            HashStream hashStream = new HashStream(GetHashAlgorithm());
            PassBytesToStreamInBlocks(hashStream, hashBytes, 4);
            return hashStream.GetHash();
        }

        /// <summary>
        /// Send bytes from an array to a stream as a sequence of blocks
        /// Not all hash algorithms behave the same passing data as one large block instead of multiple smaller ones so produce different hashes if
        /// we pass a GUID or Hash128 as a single 16 byte block for example instead of the 4x4 byte blocks the generic hashing code produces for these types 
        /// This function passes bytes from a buffer in chunks of a specified size to ensure we get the same hash from the same data in such cases
        /// Our SpookyHash implementation suffers from this for example (see SpookyHash::Short()) while MD5 does not
        /// </summary>
        /// <param name="stream">Stream to write bytes to</param>
        /// <param name="byteBlock">Array of bytes to pass to the stream, must be multiple of blockSizeBytes in size or an InvalidOperationException will be thrown</param>
        /// <param name="blockSizeBytes">Number of bytes to write to the stream each time</param>
        /// <returns></returns>
        static void PassBytesToStreamInBlocks(Stream stream, byte[] bytesToWrite, int blockSizeBytes)
        {
            if ((bytesToWrite.Length % blockSizeBytes) != 0)
                throw new InvalidOperationException($"PassBytesToStreamInBlocks() byte array size of {bytesToWrite.Length} is required to be a multiple of {blockSizeBytes} which it's not");

            for (int offset = 0; offset < bytesToWrite.Length; offset += blockSizeBytes)
            {
                stream.Write(bytesToWrite, offset, blockSizeBytes);
            }
        }

        /// <summary>
        /// Creates the hash for an object.
        /// </summary>
        /// <typeparam name="T">The hash algorithm type.</typeparam>
        /// <param name="obj">The object.</param>
        /// <returns>Returns the hash of the object.</returns>
        public static RawHash Calculate<T>(object obj) where T : HashAlgorithm
        {
            RawHash rawHash;
            using (var stream = new HashStream(GetHashAlgorithm(typeof(T))))
            {
                GetRawBytes(stream, obj);
                rawHash = stream.GetHash();
            }
            return rawHash;
        }

        /// <summary>
        /// Creates the hash for a set of objects.
        /// </summary>
        /// <typeparam name="T">The hash algorithm type.</typeparam>
        /// <param name="objects">The objects.</param>
        /// <returns>Returns the hash of the set of objects.</returns>
        public static RawHash Calculate<T>(params object[] objects) where T : HashAlgorithm
        {
            if (objects == null)
                return RawHash.Zero();

            RawHash rawHash;
            using (var stream = new HashStream(GetHashAlgorithm(typeof(T))))
            {
                GetRawBytes(stream, objects);
                rawHash = stream.GetHash();
            }
            return rawHash;
        }

        /// <summary>
        /// Creates the hash for a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>Returns the hash of the file.</returns>
        public static RawHash CalculateFile(string filePath)
        {
            RawHash rawHash;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                rawHash = CalculateStream(stream);
            return rawHash;
        }

        /// <summary>
        /// Creates the hash for a file.
        /// </summary>
        /// <typeparam name="T">The hash algorithm type.</typeparam>
        /// <param name="filePath">The file path.</param>
        /// <returns>Returns the hash of the file.</returns>
        public static RawHash CalculateFile<T>(string filePath) where T : HashAlgorithm
        {
            RawHash rawHash;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                rawHash = CalculateStream<T>(stream);
            return rawHash;
        }
    }
}
