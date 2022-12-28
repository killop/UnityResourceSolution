using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    [TestFixture]
    class HashingMethodsTests
    {
        [Test]
        public void CsMD4_Calcualte_FileNames_IdenticalTo_CppMD4()
        {
            // Test ensures our C# implementation of MD4 matches our expected C++ MD4 for compatibility
            // Until which time we no longer care about compatibility with BuildPipeline.BuildAssetBundles
            var sourceNames = new[]
            {
                "basic_sprite",
                "audio",
                "prefabs",
                "shaderwithcollection",
                "multi_sprite_packed"
            };

            var expectedNames = new[]
            {
                "a67a7313ceb7840411094318a4aa7055",
                "d5ae2b5aa3edc0f73b4bb6b1ae125a53",
                "6fee5e41c4939eed80f81beb3e5e6ebc",
                "dc5d7d3a7d9efcf91a0314cdcc3af3c8",
                "ddc8dcea83a5ff418d94c6a1623e81ad"
            };

            for (var i = 0; i < 5; i++)
                Assert.AreEqual(expectedNames[i], HashingMethods.Calculate<MD4>(sourceNames[i]).ToString());
        }

        [Test]
        public void CsMD4_Calcualte_FileIDs_IdenticalTo_CppMD4()
        {
            // Test ensures our C# implementation of MD4 matches our expected C++ MD4 for compatibility
            // Until which time we no longer care about compatibility with BuildPipeline.BuildAssetBundles
            Assert.AreEqual(-7588530676450950513, BitConverter.ToInt64(HashingMethods.Calculate<MD4>("fb3a9882e5510684697de78116693750", FileType.MetaAssetType, (long)21300000).ToBytes(), 0));
            Assert.AreEqual(-8666180608703991793, BitConverter.ToInt64(HashingMethods.Calculate<MD4>("library/atlascache/27/2799803afb660251e3b3049ba37cb15a", (long)2).ToBytes(), 0));
        }

#if UNITY_2020_1_OR_NEWER
        [TestCase(false)]   // Use old hasher (MD5)
        [TestCase(true)]    // Use V2 hasher (Spooky)
        public void HashingMethods_Has128x2Fast_SameAsGeneric(bool useV2Hasher)
        {
            // Test ensures the HashingMethods.Calculate(Hash128, Hash128) fast path produces the same results as the general HashingMethods.Calculate(params object[] objects) one does
            Hash128 hash1 = new Hash128(0x1122334455667788, 0x99AABBCCDDEEFF00);
            Hash128 hash2 = new Hash128(0x123456789ABCDEF0, 0x1967AbC487Df2F12);

            bool prevUseV2Hasher = ScriptableBuildPipeline.useV2Hasher;
            ScriptableBuildPipeline.useV2Hasher = useV2Hasher;

            Assert.AreEqual(HashingMethods.Calculate(hash1, hash2), HashingMethods.Calculate(new object[] { hash1, hash2 }));

            ScriptableBuildPipeline.useV2Hasher = prevUseV2Hasher;
        }
#else
        [Test]
        public void HashingMethods_Has128x2Fast_SameAsGeneric()
        {
            // Test ensures the HashingMethods.Calculate(Hash128, Hash128) fast path produces the same results as the general HashingMethods.Calculate(params object[] objects) one does
            Hash128 hash1 = new Hash128(0x1122334455667788, 0x99AABBCCDDEEFF00);
            Hash128 hash2 = new Hash128(0x123456789ABCDEF0, 0x1967AbC487Df2F12);

            Assert.AreEqual(HashingMethods.Calculate(hash1, hash2), HashingMethods.Calculate(new object[] { hash1, hash2 }));
        }
#endif

        // Struct that is binary compatible with Hash128 but is not Hash128
        struct Hash128Proxy
        {
            public uint m_Value0;
            public uint m_Value1;
            public uint m_Value2;
            public uint m_Value3;
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_Hash128RawBytes_SameAsGeneric(IHasher hashFunc)
        {
            // Test ensures the HashingMethods.GetRawBytes() function's fast-path for Hash128 type produces the same results as generic but slower reflection based path it replaces
            Hash128 hash128 = new Hash128(0x11223344, 0x55667788, 0x99AABBCC, 0xDDEEFF00);

            Hash128Proxy hash128Proxy = new Hash128Proxy() { m_Value0 = 0x11223344, m_Value1 = 0x55667788, m_Value2 = 0x99AABBCC, m_Value3 = 0xDDEEFF00 };

            Assert.AreEqual(hashFunc.Calculate(hash128), hashFunc.Calculate(hash128Proxy));
        }

        // Struct that is binary compatible with GUID but is not GUID
        struct GUIDProxy
        {
            public uint m_Value0;
            public uint m_Value1;
            public uint m_Value2;
            public uint m_Value3;
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_GuidRawBytes_SameAsGeneric(IHasher hashFunc)
        {
            // Test ensures the HashingMethods.GetRawBytes() function's fast-path for GUID type produces the same results as generic but slower reflection based path it replaces
            GUID guid = new GUID("4433221188776655CCBBAA9900FFEEDD");

            GUIDProxy guidProxy = new GUIDProxy() { m_Value0 = 0x11223344, m_Value1 = 0x55667788, m_Value2 = 0x99AABBCC, m_Value3 = 0xDDEEFF00 };

            Assert.AreEqual(hashFunc.Calculate(guid), hashFunc.Calculate(guidProxy));
        }

        public interface IHasher
        {
            Type HashType();
            RawHash Calculate(object obj);
            RawHash Calculate(params object[] objects);
            RawHash CalculateStream(Stream stream);
        }

        public class HashTester<T> : IHasher where T : HashAlgorithm
        {
            public Type HashType() => typeof(T);

            public RawHash Calculate(object obj) => HashingMethods.Calculate<T>(obj);

            public RawHash Calculate(params object[] objects) => HashingMethods.Calculate<T>(objects);

            public RawHash CalculateStream(Stream stream) => HashingMethods.CalculateStream<T>(stream);

            public override string ToString() => $"{typeof(T).Name}";
        }

        public enum TestIndex
        {
            Array,
            List,
            HashSet,
            Dictionary,
            Offset,
            Unicode,
            Identical
        }

        public static Dictionary<Type, string[]> TestResults = new Dictionary<Type, string[]>
        {
            // TestResults format:
            // { Hashing Type,
            //  new[] { "Array Hash     ", "List Hash  ", "HashSet Hash",
            //          "Dictionary Hash", "Offset Hash", "Unicode Hash",
            //          "Identical Hash " } }
            { typeof(MD4),
              new[] { "99944412d5093e431ba7ccdaf48f44f3", "99944412d5093e431ba7ccdaf48f44f3", "99944412d5093e431ba7ccdaf48f44f3",
                      "34392e04ec079d34cd861df956db2099", "086143d6671971fcdd40d96af36c0e92", "452f83421f98bf3831b7fd4217af3f92",
                      "65de0f26e6502fa975d8ea0b79806517" } },
            { typeof(MD5),
              new[] { "6d489a02294c1a5ce775050cfa2cd363", "6d489a02294c1a5ce775050cfa2cd363", "6d489a02294c1a5ce775050cfa2cd363",
                      "2844dc4c3aa734b2cae0f4a670d5346e", "384a563d6a14deb7553f7efc95d1b67f", "c434697711429f8205b09c47bcd87d85",
                      "5e5335125abe521354a9f9a7c302d690" } }
#if UNITY_2020_1_OR_NEWER
            , { typeof(SpookyHash),
                new[] { "6e59a12bc07db93b5f9e6a0a4acecbd1", "6e59a12bc07db93b5f9e6a0a4acecbd1", "6e59a12bc07db93b5f9e6a0a4acecbd1",
                        "bca5ae54cb78244bd544d06111694efb", "0fa373cc7984b0e05e7052fd7a7e51eb", "2e6baf5f29327f5ea5d668c988307232",
                        "5d212d4d612906870257cb183932d1a7" } }
#endif
        };

        public static IEnumerable<IHasher> TestCases()
        {
            yield return new HashTester<MD4>();
            yield return new HashTester<MD5>();
#if UNITY_2020_1_OR_NEWER
            yield return new HashTester<SpookyHash>();
#endif
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_ProducesValidHashFor_Array(IHasher hashFunc)
        {
            var sourceNames = new[]
            {
                "basic_sprite",
                "audio",
                "prefabs",
                "shaderwithcollection",
                "multi_sprite_packed"
            };

            // Use (object) cast so Calculate doesn't expand the array and use params object[] objects case
            Assert.AreEqual(TestResults[hashFunc.HashType()][(int)TestIndex.Array], hashFunc.Calculate((object)sourceNames).ToString());
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_ProducesValidHashFor_List(IHasher hashFunc)
        {
            var sourceNames = new List<string>
            {
                "basic_sprite",
                "audio",
                "prefabs",
                "shaderwithcollection",
                "multi_sprite_packed"
            };

            Assert.AreEqual(TestResults[hashFunc.HashType()][(int)TestIndex.List], hashFunc.Calculate(sourceNames).ToString());
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_ProducesValidHashFor_HashSet(IHasher hashFunc)
        {
            var sourceNames = new HashSet<string>
            {
                "basic_sprite",
                "audio",
                "prefabs",
                "shaderwithcollection",
                "multi_sprite_packed"
            };

            Assert.AreEqual(TestResults[hashFunc.HashType()][(int)TestIndex.HashSet], hashFunc.Calculate(sourceNames).ToString());
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_ProducesValidHashFor_Dictionary(IHasher hashFunc)
        {
            var sourceNames = new Dictionary<string, string>
            {
                { "basic_sprite", "audio" },
                { "prefabs", "shaderwithcollection" }
            };

            Assert.AreEqual(TestResults[hashFunc.HashType()][(int)TestIndex.Dictionary], hashFunc.Calculate(sourceNames).ToString());
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_ProducesValidHashFor_OffsetStreams(IHasher hashFunc)
        {
            byte[] bytes = { 0xe1, 0x43, 0x2f, 0x83, 0xdf, 0xeb, 0xa8, 0x86, 0xfb, 0xfe, 0xc9, 0x97, 0x20, 0xfb, 0x53, 0x45,
                             0x24, 0x5d, 0x92, 0x8b, 0xa2, 0xc4, 0xe1, 0xe2, 0x48, 0x4a, 0xbb, 0x66, 0x43, 0x9a, 0xbc, 0x84 };

            using (var stream = new MemoryStream(bytes))
            {
                stream.Position = 16;
                RawHash hash1 = hashFunc.CalculateStream(stream);

                stream.Position = 0;
                RawHash hash2 = hashFunc.CalculateStream(stream);

                Assert.AreNotEqual(hash1.ToString(), hash2.ToString());
                Assert.AreEqual(TestResults[hashFunc.HashType()][(int)TestIndex.Offset], hash1.ToString());
            }
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_ProducesValidHashFor_UnicodeStrings(IHasher hashFunc)
        {
            // It might not look it at first glance, but the below 2 strings are indeed different!
            // The ASCII byte representation of both of these strings is identical, which was causing
            // hashing methods to return identical hashes as we had used ASCII for everything.
            string str1 = "[기본]양손무기";
            string str2 = "[기본]한손무기";
            RawHash hash1 = hashFunc.Calculate(str1);
            RawHash hash2 = hashFunc.Calculate(str2);

            Assert.AreNotEqual(str1, str2);
            Assert.AreNotEqual(hash1, hash2);
            Assert.AreEqual(TestResults[hashFunc.HashType()][(int)TestIndex.Unicode], hash1.ToString());
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void HashingMethods_ProducesValidHashFor_IdenticalCalculateCalls(IHasher hashFunc)
        {
            // This test seems silly, but has exposed issues with HashingMethods not being deterministic internally when run back to back (SpookyHash)
            var hash1 = hashFunc.Calculate("HashingMethods_ProducesValidHashFor_IdenticalCalculateCalls");
            var hash2 = hashFunc.Calculate("HashingMethods_ProducesValidHashFor_IdenticalCalculateCalls");
            Assert.IsTrue(hash1.Equals(hash2));
            Assert.AreEqual(TestResults[hashFunc.HashType()][(int)TestIndex.Identical], hash1.ToString());
        }
    }
}
