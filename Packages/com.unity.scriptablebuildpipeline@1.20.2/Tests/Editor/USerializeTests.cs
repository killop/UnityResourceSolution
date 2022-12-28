// Define this to cause the before and after serialization DumpToText output for objects being tested to be echoed to the console.  
// Slows down tests (some considerably) but allows visual inspection of the data which can be helpful for debugging issues
// #define DUMP_DATA_TEXT_TO_CONSOLE   

using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEngine;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.Build.Pipeline;
using UnityEditor.Build.Pipeline.Tests;
using System.Reflection;

namespace UnityEditor.Build.Pipeline.Utilities.USerialize.Tests
{
    // Tests for the USerialize serialization code
    [TestFixture]
    class USerializeTests
    {
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Test Cases
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        // Test 'bool' is serialized/deserialized correctly
        [TestCase(true)]
        [TestCase(false)]
        public void TestBoolSerializes(bool testValue) => TestSerializeData(new PrimitiveValue<bool>() { m_Value = testValue });

        // Test 'Int32' is serialized/deserialized correctly
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        [TestCase(0)]
        [TestCase(1234)]
        public void TestInt32Serializes(int testValue) => TestSerializeData(new PrimitiveValue<int>() { m_Value = testValue });

        // Test 'UInt32' is serialized/deserialized correctly
        [TestCase(uint.MinValue)]
        [TestCase(uint.MaxValue)]
        [TestCase(0u)]
        [TestCase(1234u)]
        public void TestUInt32Serializes(uint testValue) => TestSerializeData(new PrimitiveValue<uint>() { m_Value = testValue });

        // Test 'Int64' is serialized/deserialized correctly
        [TestCase(long.MinValue)]
        [TestCase(long.MaxValue)]
        [TestCase(0)]
        [TestCase(1234)]
        public void TestInt64Serializes(long testValue) => TestSerializeData(new PrimitiveValue<long>() { m_Value = testValue });

        // Test 'UInt64' is serialized/deserialized correctly
        [TestCase(ulong.MinValue)]
        [TestCase(ulong.MaxValue)]
        [TestCase(0u)]
        [TestCase(1234u)]
        public void TestUInt64Serializes(ulong testValue) => TestSerializeData(new PrimitiveValue<ulong>() { m_Value = testValue });

        // Test trying to serialize an unsupported primitive type throws
        [Test]
        public void TestSerializingUnsupportedPrimitiveTypeThrows()
        {
            Assert.Throws(typeof(InvalidDataException), () => TestSerializeData(new PrimitiveValue<float>() { m_Value = 123.4f }));
        }

        // Test trying to serialize an unsupported primitive array type throws
        [Test]
        public void TestSerializingUnsupportedArrayPrimitiveTypeThrows()
        {
            Assert.Throws(typeof(InvalidDataException), () => TestSerializeData(new PrimitiveValue<ClassWithUnsupportedPrimitiveArrayType>() { m_Value = new ClassWithUnsupportedPrimitiveArrayType() }));
        }

        // Test trying to serialize an array of rank greater than one throws
        [Test]
        public void TestSerializingUnsupportedArrayRankThrows()
        {
            Assert.Throws(typeof(InvalidDataException), () => TestSerializeData(new PrimitiveValue<int[,]>() { m_Value = new int[1, 1] }));
        }

        // Test writing string indices gives us back the values we expect and uses the expected number of bytes
        [TestCase(0, 2)]
        [TestCase(0x7FFF, 2)]
        [TestCase(0x8000, 4)]
        [TestCase(0x8001, 4)]
        [TestCase(0x12345678, 4)]
        [TestCase(USerialize.InvalidStringIndex, 4)]
        public void TestWriteReadStringIndices(int stringIndex, int expectedSizeBytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // Write string index checking we wrote the expected number of bytes
                Serializer serializer = new Serializer();
                serializer.StartWritingToStream(stream);
                long streamPosBeforeWriting = stream.Position;
                serializer.WriteStringIndex(stringIndex);
                serializer.FinishWritingToStream();
                int numBytesWritten = (int)(stream.Position - streamPosBeforeWriting);
                Assert.AreEqual(expectedSizeBytes, numBytesWritten);

                // Read back the string index checking we got back what we expected and read the expected number of bytes
                stream.Position = 0;
                DeSerializer deserializer = new DeSerializer();
                deserializer.StartReadingFromStream(stream);
                long streamPosBeforeReading = stream.Position;
                int readStringIndex = deserializer.ReadStringIndex();
                int numBytesRead = (int)(stream.Position - streamPosBeforeReading);
                Assert.AreEqual(expectedSizeBytes, numBytesRead);

                Assert.AreEqual(stringIndex, readStringIndex);
            }
        }

        // Test 'Hash128' is serialized/deserialized correctly
        [TestCase(0x0123456789ABCDEFU, 0x94F8A3B87F327E2CU)]
        public void TestHash128Serializes(ulong hashPart1, ulong hashPart2)
        {
            Hash128 testValue = new Hash128(hashPart1, hashPart2);
            TestSerializeData(new PrimitiveValue<Hash128>() { m_Value = testValue });
        }

        // Test 'GUID' is serialized/deserialized correctly
        [TestCase("ABD386FE297853ADF2397542BC3EA875")]
        public void TestGUIDSerializes(string guidString)
        {
            GUID testValue = new GUID(guidString);
            TestSerializeData(new PrimitiveValue<GUID>() { m_Value = testValue });
        }

        static readonly string SampleUnicodeString = "A Unicode string containing characters outside the  ASCII code range, Pi (\u03a0) and Sigma (\u03a3) and a surrogate pair (\ud83e\udd70)";

        // "The world is a better place with more creators in it" in various languages to exercise international characters. (done with Google translate, apologies for errors)
        static readonly string BetterPlaceArabic = "\u0627\u0644\u0639\u0627\u0644\u0645\u0020\u0645\u0643\u0627\u0646\u0020\u0623\u0641\u0636\u0644\u0020\u0628\u0647\u0020\u0639\u062f\u062f\u0020\u0623\u0643\u0628\u0631\u0020\u0645\u0646\u0020\u0627\u0644\u0645\u0628\u062f\u0639\u064a\u0646";
        static readonly string BetterPlaceHindi = "\u0907\u0938\u092e\u0947\u0902\u0020\u0905\u0927\u093f\u0915\u0020\u0930\u091a\u0928\u093e\u0915\u093e\u0930\u094b\u0902\u0020\u0915\u0947\u0020\u0938\u093e\u0925\u0020\u0926\u0941\u0928\u093f\u092f\u093e\u0020\u090f\u0915\u0020\u092c\u0947\u0939\u0924\u0930\u0020\u0938\u094d\u0925\u093e\u0928\u0020\u0939\u0948";
        static readonly string BetterPlaceRussian = "\u043c\u0438\u0440\u0020\u0441\u0442\u0430\u043b\u0020\u043b\u0443\u0447\u0448\u0435\u002c\u0020\u0432\u0020\u043d\u0435\u043c\u0020\u0431\u043e\u043b\u044c\u0448\u0435\u0020\u0442\u0432\u043e\u0440\u0446\u043e\u0432";
        static readonly string BetterPlaceIcelandic = "\u0068\u0065\u0069\u006d\u0075\u0072\u0069\u006e\u006e\u0020\u0065\u0072\u0020\u0062\u0065\u0074\u0072\u0069\u0020\u0073\u0074\u0061\u00f0\u0075\u0072\u0020\u006d\u0065\u00f0\u0020\u0066\u006c\u0065\u0069\u0072\u0069\u0020\u0068\u00f6\u0066\u0075\u006e\u0064\u0075\u006d\u0020\u00ed\u0020\u0068\u006f\u006e\u0075\u006d";
        static readonly string BetterPlaceJapanese = "\u4e16\u754c\u306f\u3088\u308a\u591a\u304f\u306e\u30af\u30ea\u30a8\u30a4\u30bf\u30fc\u304c\u3044\u308b\u3088\u308a\u826f\u3044\u5834\u6240\u3067\u3059";

        // Test strings of various forms are serialized/deserialized correctly
        public static IEnumerable<string> TestStringsSerialize_TestCases()
        {
            yield return null;
            yield return "";
            yield return "\n";
            yield return "FooBar";
            yield return "\"FooBarQuotes\"";
            yield return "\\FooBarBackslash\\";
            yield return "Multi\nLine\nString";
            yield return SampleUnicodeString;
            yield return BetterPlaceArabic;
            yield return BetterPlaceHindi;
            yield return BetterPlaceRussian;
            yield return BetterPlaceIcelandic;
            yield return BetterPlaceJapanese;
        }
        [TestCaseSource("TestStringsSerialize_TestCases")]
        public void TestStringsSerialize(string testValue) => TestSerializeData(new PrimitiveValue<string>() { m_Value = testValue });

        // Test string arrays of various forms are serialized/deserialized correctly
        public static IEnumerable<string[]> TestStringArraysSerialize_TestCases()
        {
            yield return null;
            yield return new string[] { "" };
            yield return new string[] { "foo" };
            yield return new string[] { "foo", "bar", "multi\nline", "table\\backslash" };
            yield return TestStringsSerialize_TestCases().ToArray();
        }
        [TestCaseSource("TestStringArraysSerialize_TestCases")]
        public void TestStringArraysSerialize(string[] stringArray)
        {
            TestSerializeData(new PrimitiveArray<string>() { m_ArrayElements = stringArray });
        }

        // Test serializing a string array with more than 32,767 unique entries to ensure the standard 15bit string table index 
        // is insufficient and the fallback 31bit stringtable index encoding has to be used for some of the strings
        [Test]
        public void TestStringIndicesLargerThan15bit()
        {
            string[] testStrings = new string[40000];
            for (int elementNum = 0; elementNum < testStrings.Length; elementNum++)
                testStrings[elementNum] = "Test_" + elementNum;
            TestSerializeData(new PrimitiveArray<string>() { m_ArrayElements = testStrings });
        }

        // Test byte arrays of various forms are serialized/deserialized correctly
        [TestCase(null)]
        [TestCase(new byte[0])]
        [TestCase(new byte[] { 1, 2, 3, 4, 5 })]
        public void TestByteArraysSerialize(byte[] testValue) => TestSerializeData(new PrimitiveArray<byte>() { m_ArrayElements = testValue });

        // Test ulong arrays of various forms are serialized/deserialized correctly
        [TestCase(null)]
        [TestCase(new ulong[0])]
        [TestCase(new ulong[] { 1, 2, 3, 4, 5 })]
        [TestCase(new ulong[] { ulong.MinValue, 0, 1, ulong.MaxValue })]
        public void TestULongArraysSerialize(ulong[] testValue) => TestSerializeData(new PrimitiveArray<ulong>() { m_ArrayElements = testValue });

        // Test Type arrays of various forms are serialized/deserialized correctly
        public static IEnumerable<Type[]> TestTypeArraysSerialize_TestCases()
        {
            yield return null;
            yield return new Type[0];
            yield return new Type[] { typeof(int), typeof(uint), typeof(float), typeof(CachedInfo), typeof(MonoBehaviour) };
            yield return new Type[] { typeof(int), null, typeof(float), null, typeof(MonoBehaviour) };
        }
        [TestCaseSource("TestTypeArraysSerialize_TestCases")]
        public void TestTypeArraysSerialize(Type[] testValue) => TestSerializeData(new PrimitiveArray<Type>() { m_ArrayElements = testValue });

        // Test structs are serialized/deserialized correctly
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public void TestLinearStructsSerialize(int scalar)
        {
            LinearStruct value = new LinearStruct(scalar);
            if (scalar != -1)
                value.m_LinearClass = new LinearClass(scalar * 2);
            TestSerializeData(value);
        }

        // Test classes are serialized/deserialized correctly
        [Test]
        public void TestLinearClassSerializes() => TestSerializeData(new PrimitiveValue<LinearClass>() { m_Value = new LinearClass(1) });

        // Test the DeSerializer.ObjectVersion property returns the correct value after deserialization 
        [Test]
        public void TestClientObjectVersionPropertyReturnedCorrectly()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // Our data
                PrimitiveValue<int> data = new PrimitiveValue<int>() { m_Value = 3149123 };
                const int objectVersion = 12345678;

                // Write to stream
                Serializer serializer = new Serializer();
                serializer.Serialize(stream, data, objectVersion);

                // Read from stream
                stream.Position = 0;
                DeSerializer deserializer = new DeSerializer();
                PrimitiveValue<int> deserializedData = deserializer.DeSerialize<PrimitiveValue<int>>(stream);

                // Test object contents and version is as expected
                Assert.AreEqual(objectVersion, deserializer.ObjectVersion);
                Assert.AreEqual(data.m_Value, deserializedData.m_Value);
            }
        }

        // Test that CachedInfo instances serialize/deserialize correctly
        [Test]
        public void TestCachedInfoSerialises()
        {
            CachedInfo cachedInfo = CreateSyntheticCachedInfo(12478324);

            TestSerializeData(new PrimitiveValue<CachedInfo>() { m_Value = cachedInfo },
                BuildCache.CustomSerializers,
                BuildCache.ObjectFactories,
                new DumpToText.ICustomDumper[] { new CustomDumper_BuildUsageTagSet() },
                false); // full Equality not implemented for CachedInfo so we rely on the dumper text comparison
        }


        // --------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Utilities & Supporting Code
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        // Wrapper for a value that provides an IEquatable interface so it can be compared post-serialization-deserialization to the original value
        class PrimitiveValue<ValueType> : IEquatable<PrimitiveValue<ValueType>>
        {
            internal ValueType m_Value;

            public bool Equals(PrimitiveValue<ValueType> other)
            {
                return (other == null) ? false : (EqualityComparer<ValueType>.Default.Equals(m_Value, other.m_Value));
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals(obj as PrimitiveValue<ValueType>);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        // Wrapper for an array of primitive values that provides an IEquatable interface so it can be compared post-serialization-deserialization to the original value
        class PrimitiveArray<ValueType> : IEquatable<PrimitiveArray<ValueType>>
        {
            internal ValueType[] m_ArrayElements;

            public bool Equals(PrimitiveArray<ValueType> other)
            {
                if (other == null)
                    return false;

                return (m_ArrayElements == null) ? (other.m_ArrayElements == null) : m_ArrayElements.SequenceEqual(other.m_ArrayElements);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals(obj as PrimitiveArray<ValueType>);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        // A struct of various primitive and reference types that provides an IEquatable interface so it can be compared post-serialization-deserialization to the original value
        internal struct LinearStruct : IEquatable<LinearStruct>
        {
            internal bool m_Bool;
            internal byte m_Byte;
            internal int m_Int;
            internal uint m_UInt;
            internal long m_Long;
            internal ulong m_ULong;
            internal string m_String;

            internal LinearClass m_LinearClass;

            internal LinearStruct(int scalar)
            {
                m_Bool = ((scalar & 1) == 0);
                m_Byte = (byte)scalar;
                m_Int = 2 * scalar;
                m_UInt = (uint)(3 * scalar);
                m_Long = (long)(4 * scalar);
                m_ULong = (ulong)(5 * scalar);
                m_String = "(" + scalar.ToString() + ")";

                m_LinearClass = null;
            }

            public bool Equals(LinearStruct other)
            {
                return (m_Bool == other.m_Bool) && (m_Byte == other.m_Byte) && (m_Int == other.m_Int) && (m_UInt == other.m_UInt) && (m_Long == other.m_Long) && (m_ULong == other.m_ULong) && (m_String == other.m_String) && (System.Object.Equals(m_LinearClass, other.m_LinearClass));
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        // A class of various primitive and reference types that provides an IEquatable interface so it can be compared post-serialization-deserialization to the original value
        internal class LinearClass : IEquatable<LinearClass>
        {
            internal bool m_Bool;
            internal byte m_Byte;
            internal int m_Int;
            internal uint m_UInt;
            internal long m_Long;
            internal ulong m_ULong;
            internal string m_String;

            internal LinearClass m_LinearClass;
            internal LinearStruct m_LinearStruct;

            public LinearClass()
            {
            }

            internal LinearClass(int scalar)
            {
                m_Bool = ((scalar & 1) == 0);
                m_Byte = (byte)scalar;
                m_Int = 2 * scalar;
                m_UInt = (uint)(3 * scalar);
                m_Long = (long)(4 * scalar);
                m_ULong = (ulong)(5 * scalar);
                m_String = "(" + scalar.ToString() + ")";

                m_LinearStruct = new LinearStruct(scalar * 10);
            }

            public bool Equals(LinearClass other)
            {
                return (m_Bool == other.m_Bool) && (m_Byte == other.m_Byte) && (m_Int == other.m_Int) && (m_UInt == other.m_UInt) && (m_Long == other.m_Long) && (m_ULong == other.m_ULong) && (m_String == other.m_String) && (m_LinearClass == other.m_LinearClass) && m_LinearStruct.Equals(other.m_LinearStruct);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals(obj as LinearClass);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        // Class containing a field of an unsupported primitive array type
        internal class ClassWithUnsupportedPrimitiveArrayType
        {
            internal object[] m_InvalidArray = new object[1] { 123.4f };    // Only primitive type supported in object arrays is Int32
        }

        // Utility to create a GUID from a random number generator
        static GUID CreateGuid(System.Random rnd)
        {
            string guidString = "";
            for (int charNum = 0; charNum < 32; charNum++)
                guidString = guidString + rnd.Next(10);
            return new GUID(guidString);
        }

        // Utility to create a Hash128 from a random number generator
        static Hash128 CreateHash128(System.Random rnd)
        {
            return new Hash128((uint)rnd.Next(), (uint)rnd.Next(), (uint)rnd.Next(), (uint)rnd.Next());
        }

        // Utility to create a CacheEntry instance from a random number generator
        static CacheEntry CreateSyntheticCacheEntry(System.Random rnd)
        {
            CacheEntry cacheEntry = new CacheEntry();
            cacheEntry.Hash = CreateHash128(rnd);
            cacheEntry.Guid = CreateGuid(rnd);
            cacheEntry.Version = rnd.Next(100);
            cacheEntry.Type = (CacheEntry.EntryType)rnd.Next(4);
            cacheEntry.File = "File_" + rnd.Next();
            cacheEntry.ScriptType = "ScriptType_" + rnd.Next();
            return cacheEntry;
        }

        // Utility to create an ObjectIdentifier instance from a random number generator
        static ObjectIdentifier CreateObjectIdentifier(System.Random rnd)
        {
            ObjectIdentifier objectIdentifier = new ObjectIdentifier();
            objectIdentifier.SetObjectIdentifier(CreateGuid(rnd), rnd.Next(), (FileType)rnd.Next(4), "FilePath_" + rnd.Next());
            return objectIdentifier;
        }

        // Create a CachedInfo instance with every field populated from a given random number seed
        // The data doesn't have to make sense, but everything has to have a value of some sort so we can check everything is being serialized/deserialized
        static CachedInfo CreateSyntheticCachedInfo(int seed)
        {
            System.Random rnd = new System.Random(seed);

            CachedInfo cachedData = new CachedInfo();

            cachedData.Asset = CreateSyntheticCacheEntry(rnd);

            cachedData.Dependencies = new CacheEntry[2];
            cachedData.Dependencies[0] = CreateSyntheticCacheEntry(rnd);
            cachedData.Dependencies[1] = CreateSyntheticCacheEntry(rnd);

            cachedData.Data = new object[]
              {
                CreateSyntheticAssetLoadInfo(rnd),
                CreateSyntheticBuildUsageTagSet(),
                CreateSyntheticSpriteImporterData(rnd),
                CreateSyntheticExtendedAssetData(rnd),
                CreateSyntheticObjectTypes(rnd),
                CreateSyntheticSceneDependencyInfo(rnd),
                CreateHash128(rnd),
                CreateSyntheticWriteResult(rnd),
                CreateSyntheticSerializedFileMetaData(rnd),
                CreateSyntheticBundleDetails(rnd)
              };

            return cachedData;
        }

        static AssetLoadInfo CreateSyntheticAssetLoadInfo(System.Random rnd)
        {
            AssetLoadInfo assetInfo = new AssetLoadInfo();
            assetInfo.asset = CreateGuid(rnd);
            assetInfo.address = "AnyAddress";
            assetInfo.includedObjects = new List<ObjectIdentifier>();
            assetInfo.includedObjects.Add(CreateObjectIdentifier(rnd));
            assetInfo.includedObjects.Add(CreateObjectIdentifier(rnd));
            assetInfo.referencedObjects = new List<ObjectIdentifier>();
            assetInfo.referencedObjects.Add(CreateObjectIdentifier(rnd));
            assetInfo.referencedObjects.Add(CreateObjectIdentifier(rnd));
            return assetInfo;
        }

        static BuildUsageTagSet CreateSyntheticBuildUsageTagSet()
        {
            // JSON for a BuildUsageTagSet instance.  There is no API to create this programatically so this was captured from a real project to be used in these tests
            string buildUsageTagSetJsonData = "{\"m_objToUsage\":[{\"first\":{\"filePath\":\"\",\"fileType\":3,\"guid\":\"822642a2b47082c49966f0c54db535a4\",\"localIdentifierInFile\":-7728386467694932768},\"second\":{\"forceTextureReadable\":false,\"maxBonesPerVertex\":4,\"meshSupportedChannels\":12799,\"meshUsageFlags\":1,\"shaderIncludeInstancingVariants\":false,\"shaderUsageKeywordNames\":[],\"strippedPrefabObject\":false}},{\"first\":{\"filePath\":\"\",\"fileType\":3,\"guid\":\"90b695013db7b334cbcf925848031399\",\"localIdentifierInFile\":-1848259448780025149},\"second\":{\"forceTextureReadable\":false,\"maxBonesPerVertex\":0,\"meshSupportedChannels\":383,\"meshUsageFlags\":0,\"shaderIncludeInstancingVariants\":false,\"shaderUsageKeywordNames\":[],\"strippedPrefabObject\":false}},{\"first\":{\"filePath\":\"\",\"fileType\":3,\"guid\":\"db9aadf200fd84e4591cc30ea4d1358c\",\"localIdentifierInFile\":3883874861523733925},\"second\":{\"forceTextureReadable\":false,\"maxBonesPerVertex\":0,\"meshSupportedChannels\":383,\"meshUsageFlags\":0,\"shaderIncludeInstancingVariants\":false,\"shaderUsageKeywordNames\":[],\"strippedPrefabObject\":false}}]}";

            BuildUsageTagSet buildUsageTagSet = new BuildUsageTagSet();

#if UNITY_2019_4_OR_NEWER
            buildUsageTagSet.DeserializeFromJson(buildUsageTagSetJsonData);
#else
            typeof(BuildUsageTagSet).GetMethod("DeserializeFromJson", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(buildUsageTagSet, new object[] { buildUsageTagSetJsonData });
#endif

            return buildUsageTagSet;

        }

        static SpriteImporterData CreateSyntheticSpriteImporterData(System.Random rnd)
        {
            SpriteImporterData spriteImporterData = new SpriteImporterData();
            spriteImporterData.PackedSprite = true;
            spriteImporterData.SourceTexture = CreateObjectIdentifier(rnd);
            return spriteImporterData;
        }

        static ExtendedAssetData CreateSyntheticExtendedAssetData(System.Random rnd)
        {
            ExtendedAssetData extendedAssetData = new ExtendedAssetData();
            extendedAssetData.Representations = new List<ObjectIdentifier>();
            extendedAssetData.Representations.Add(CreateObjectIdentifier(rnd));
            extendedAssetData.Representations.Add(CreateObjectIdentifier(rnd));
            return extendedAssetData;
        }

        static List<ObjectTypes> CreateSyntheticObjectTypes(System.Random rnd)
        {
            List<ObjectTypes> objectTypes = new List<ObjectTypes>();
            objectTypes.Add(new ObjectTypes(CreateObjectIdentifier(rnd), new Type[] { typeof(int), typeof(CachedInfo) }));
            objectTypes.Add(new ObjectTypes(CreateObjectIdentifier(rnd), new Type[] { typeof(MonoBehaviour), typeof(ScriptableObject) }));
            return objectTypes;
        }

#if !UNITY_2019_4_OR_NEWER
        // Set the value of a named field on an instance using the reflection API.  Required only for 2018.4 where we don't have direct access to internal fields
        static void SetFieldValue<ObjectType>(ObjectType instance, string fieldName, object value)
        {
            typeof(ObjectType).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
        }
#endif

        // Create a populated 'SceneDependencyInfo' instance.  Can set fields directly for 2019.4+ as internals are available but have to use reflection for 2018.4
        static SceneDependencyInfo CreateSceneDependencyInfo(string scene, ObjectIdentifier[] objectIdentifiers, BuildUsageTagGlobal globalUsage, Type[] includedTypes)
        {
            SceneDependencyInfo sceneDependencyInfo = new SceneDependencyInfo();

#if UNITY_2019_4_OR_NEWER
            sceneDependencyInfo.m_Scene = scene;
            sceneDependencyInfo.m_ReferencedObjects = objectIdentifiers;
            sceneDependencyInfo.m_GlobalUsage = globalUsage;

#if UNITY_2020_1_OR_NEWER
            sceneDependencyInfo.m_IncludedTypes = includedTypes;
#endif
#else
            sceneDependencyInfo.SetScene(scene);
            sceneDependencyInfo.SetReferencedObjects(objectIdentifiers);
            SetFieldValue(sceneDependencyInfo, "m_GlobalUsage", globalUsage);
#endif

            return sceneDependencyInfo;
        }

        // Create a populated 'BuildUsageTagGlobal' instance.  Can set fields directly for 2019.4+ as internals are available but have to use reflection for 2018.4
        static BuildUsageTagGlobal CreateBuildUsageTagGlobal(uint lightmapModesUsed, uint legacyLightmapModesUsed, uint dynamicLightmapsUsed, uint fogModesUsed, bool forceInstancingStrip, bool forceInstancingKeep, bool shadowMasksUsed, bool subtractiveUsed, bool hybridRendererPackageUsed)
        {
            BuildUsageTagGlobal buildUsageTagGlobal = new BuildUsageTagGlobal();

#if UNITY_2019_4_OR_NEWER
            buildUsageTagGlobal.m_LightmapModesUsed = lightmapModesUsed;
            buildUsageTagGlobal.m_LegacyLightmapModesUsed = legacyLightmapModesUsed;
            buildUsageTagGlobal.m_DynamicLightmapsUsed = dynamicLightmapsUsed;
            buildUsageTagGlobal.m_FogModesUsed = fogModesUsed;
            buildUsageTagGlobal.m_ForceInstancingStrip = forceInstancingStrip;
            buildUsageTagGlobal.m_ForceInstancingKeep = forceInstancingKeep;
            buildUsageTagGlobal.m_ShadowMasksUsed = shadowMasksUsed;
            buildUsageTagGlobal.m_SubtractiveUsed = subtractiveUsed;
#if UNITY_2020_1_OR_NEWER
            buildUsageTagGlobal.m_HybridRendererPackageUsed = hybridRendererPackageUsed;
#endif
#else
            SetFieldValue(buildUsageTagGlobal, "m_LightmapModesUsed", lightmapModesUsed);
            SetFieldValue(buildUsageTagGlobal, "m_LegacyLightmapModesUsed", legacyLightmapModesUsed);
            SetFieldValue(buildUsageTagGlobal, "m_DynamicLightmapsUsed", dynamicLightmapsUsed);
            SetFieldValue(buildUsageTagGlobal, "m_FogModesUsed", fogModesUsed);
            SetFieldValue(buildUsageTagGlobal, "m_ForceInstancingStrip", forceInstancingStrip);
            SetFieldValue(buildUsageTagGlobal, "m_ForceInstancingKeep", forceInstancingKeep);
            SetFieldValue(buildUsageTagGlobal, "m_ShadowMasksUsed", shadowMasksUsed);
            SetFieldValue(buildUsageTagGlobal, "m_SubtractiveUsed", subtractiveUsed);
#endif

            return buildUsageTagGlobal;
        }

        static SceneDependencyInfo CreateSyntheticSceneDependencyInfo(System.Random rnd)
        {
            BuildUsageTagGlobal buildUsageTagGlobal = CreateBuildUsageTagGlobal((uint)rnd.Next(100), (uint)rnd.Next(100), (uint)rnd.Next(100), (uint)rnd.Next(100), ((rnd.Next() & 1) != 0), ((rnd.Next() & 1) != 0), ((rnd.Next() & 1) != 0), ((rnd.Next() & 1) != 0), ((rnd.Next() & 1) != 0));

            return CreateSceneDependencyInfo("SceneName", new ObjectIdentifier[2] { CreateObjectIdentifier(rnd), CreateObjectIdentifier(rnd) }, buildUsageTagGlobal, new Type[] { typeof(MonoBehaviour), typeof(ScriptableObject) });
        }

        // Create a populated 'WriteResult' instance.  Can set fields directly for 2019.4+ as internals are available but have to use reflection for 2018.4
        static WriteResult CreateWriteResult(ObjectSerializedInfo[] serializedObjects, ResourceFile[] resourceFiles, Type[] includedTypes, String[] includedSerializeReferenceFQN)
        {
            WriteResult writeResult = new WriteResult();

#if UNITY_2019_4_OR_NEWER
            writeResult.m_SerializedObjects = serializedObjects;
            writeResult.m_ResourceFiles = resourceFiles;
            writeResult.m_IncludedTypes = includedTypes;
            writeResult.m_IncludedSerializeReferenceFQN = includedSerializeReferenceFQN;
#else
            SetFieldValue(writeResult, "m_SerializedObjects", serializedObjects);
            SetFieldValue(writeResult, "m_ResourceFiles", resourceFiles);
            SetFieldValue(writeResult, "m_IncludedTypes", includedTypes);
#endif

            return writeResult;
        }

        // Create a populated 'ObjectSerializedInfo' instance.  Can set fields directly for 2019.4+ as internals are available but have to use reflection for 2018.4
        static ObjectSerializedInfo CreateObjectSerializedInfo(ObjectIdentifier serializedObject, SerializedLocation header, SerializedLocation rawData)
        {
            ObjectSerializedInfo objectSerializedInfo = new ObjectSerializedInfo();

#if UNITY_2019_4_OR_NEWER
            objectSerializedInfo.m_SerializedObject = serializedObject;
            objectSerializedInfo.m_Header = header;
            objectSerializedInfo.m_RawData = rawData;
#else
            SetFieldValue(objectSerializedInfo, "m_SerializedObject", serializedObject);
            SetFieldValue(objectSerializedInfo, "m_Header", header);
            SetFieldValue(objectSerializedInfo, "m_RawData", rawData);
#endif

            return objectSerializedInfo;
        }

        // Create a populated 'ResourceFile' instance.  Can set fields directly for 2019.4+ as internals are available but have to use reflection for 2018.4
        static ResourceFile CreateResourceFile(string filename, string fileAlias, bool serializedFile)
        {
            ResourceFile resourceFile = new ResourceFile();

#if UNITY_2019_4_OR_NEWER
            resourceFile.m_FileName = filename;
            resourceFile.m_FileAlias = fileAlias;
            resourceFile.m_SerializedFile = serializedFile;
#else
            SetFieldValue(resourceFile, "m_FileName", filename);
            SetFieldValue(resourceFile, "m_FileAlias", fileAlias);
            SetFieldValue(resourceFile, "m_SerializedFile", serializedFile);
#endif

            return resourceFile;
        }

        // Create a populated 'SerializedLocation' instance.  Can set fields directly for 2019.4+ as internals are available but have to use reflection for 2018.4
        static SerializedLocation CreateSerializedLocation(string filename, uint offset, uint size)
        {
            SerializedLocation serializedLocation = new SerializedLocation();

#if UNITY_2019_4_OR_NEWER
            serializedLocation.m_FileName = filename;
            serializedLocation.m_Offset = offset;
            serializedLocation.m_Size = size;
#else
            SetFieldValue(serializedLocation, "m_FileName", filename);
            SetFieldValue(serializedLocation, "m_Offset", offset);
            SetFieldValue(serializedLocation, "m_Size", size);
#endif

            return serializedLocation;
        }

        static WriteResult CreateSyntheticWriteResult(System.Random rnd)
        {
            ObjectSerializedInfo[] serializedObjects = new ObjectSerializedInfo[]
            {
                CreateObjectSerializedInfo(CreateObjectIdentifier(rnd), CreateSerializedLocation("Header_" + rnd.Next(), (uint)rnd.Next(), (uint)rnd.Next()), CreateSerializedLocation("RawData_" + rnd.Next(), (uint)rnd.Next(), (uint)rnd.Next())),
                CreateObjectSerializedInfo(CreateObjectIdentifier(rnd), CreateSerializedLocation("Header_" + rnd.Next(), (uint)rnd.Next(), (uint)rnd.Next()), CreateSerializedLocation("RawData_" + rnd.Next(), (uint)rnd.Next(), (uint)rnd.Next()))
            };

            ResourceFile[] resourceFiles = new ResourceFile[]
            {
                CreateResourceFile("Filename_" + rnd.Next(), "FileAlias_" + rnd.Next(), ((rnd.Next() & 1) != 0)),
                CreateResourceFile("Filename_" + rnd.Next(), "FileAlias_" + rnd.Next(), ((rnd.Next() & 1) != 0))
            };

            return CreateWriteResult(serializedObjects, resourceFiles, new Type[] { typeof(ScriptableObject), typeof(Vector2) }, new String[] { "IncludedSerializeReferenceFQN_" + rnd.Next(), "IncludedSerializeReferenceFQN_" + rnd.Next() });
        }

        static BundleDetails CreateSyntheticBundleDetails(System.Random rnd)
        {
            BundleDetails bundleDetails = new BundleDetails();
            bundleDetails.FileName = "FileName_" + rnd.Next();
            bundleDetails.Crc = (uint)rnd.Next();
            bundleDetails.Hash = CreateHash128(rnd);
            bundleDetails.Dependencies = new string[] { "Dependencies_" + rnd.Next(), "Dependencies_" + rnd.Next(), "Dependencies_" + rnd.Next() };
            return bundleDetails;
        }

        static SerializedFileMetaData CreateSyntheticSerializedFileMetaData(System.Random rnd)
        {
            SerializedFileMetaData serializedFileMetaData = new SerializedFileMetaData();
            serializedFileMetaData.RawFileHash = CreateHash128(rnd);
            serializedFileMetaData.ContentHash = CreateHash128(rnd);
            return serializedFileMetaData;
        }

        // Test some data serializes/deserializes correctly
        // The 'data' instance is serialized to a MemoryStream then deserialized back again. The original and deserialized instances are then converted to text with the DumpToText utility class and the resultant text compared for equality
        // The deserialized object is then compared directly to the original.
        void TestSerializeData<DataType>(DataType data) where DataType : new()
        {
            TestSerializeData(data, null, null, null, true);
        }

        // Test some data serializes/deserializes correctly optionally using a collection of custom serializers, deserializers, object factories and custom object->text dumpers
        // The 'data' instance is serialized to a MemoryStream then deserialized back again. The original and deserialized instances are then converted to text with the DumpToText utility class and the resultant text compared for equality
        // If the 'testEquality' flag is true then the deserialized object is compared directly to the original.  CachedInfo is the only type that doesn't do this as the full equality operation is not supported there
        void TestSerializeData<DataType>(DataType data,
            ICustomSerializer[] customSerializers,
            (Type, DeSerializer.ObjectFactory)[] objectFactories,
            DumpToText.ICustomDumper[] customDumpers,
            bool testEquality) where DataType : new()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // Write to stream
                Serializer serializer = new Serializer(customSerializers);
                serializer.Serialize(stream, data, 1);

                // Read from stream
                stream.Position = 0;
                DeSerializer deserializer = new DeSerializer(customSerializers, objectFactories);
                DataType deserializedData = deserializer.DeSerialize<DataType>(stream);

                // Dump initial object to text
                DumpToText dumper = new DumpToText(customDumpers);
                string originalText = dumper.Dump("Data", data).ToString();

#if DUMP_DATA_TEXT_TO_CONSOLE
                Debug.Log("Original:");
                Array.ForEach(originalText.Split('\n'), (textLine) => Debug.Log(textLine));
#endif

                // Dump deserialized object to text
                dumper.Clear();
                string deserializedText = dumper.Dump("Data", deserializedData).ToString();

#if DUMP_DATA_TEXT_TO_CONSOLE
                Debug.Log("Deserialized:");
                Array.ForEach(deserializedText.Split('\n'), (textLine) => Debug.Log(textLine));
#endif

                Assert.AreEqual(originalText, deserializedText);

                if (testEquality)
                    Assert.AreEqual(data, deserializedData);
            }
        }
    }
}

