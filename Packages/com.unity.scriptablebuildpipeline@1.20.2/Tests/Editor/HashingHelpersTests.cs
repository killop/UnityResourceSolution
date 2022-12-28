using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    class HashingHelpersTests
    {
        class AssertExpectedLayout : Attribute
        {
            public Type CompareType;
            public AssertExpectedLayout(Type compareType)
            {
                CompareType = compareType;
            }
        }

        [AssertExpectedLayout(typeof(PreloadInfo))]
        class ExpectedPreloadInfo
        {
            public List<ObjectIdentifier> preloadObjects { get; set; }
        }

        [AssertExpectedLayout(typeof(AssetBundleInfo))]
        class ExpectedAssetBundleInfo
        {
            public string bundleName { get; set; }
            public List<AssetLoadInfo> bundleAssets { get; set; }
        }

        [AssertExpectedLayout(typeof(WriteCommand))]
        class ExpectedWriteCommand
        {
            public string fileName { get; set; }
            public string internalName { get; set; }
            public List<SerializationInfo> serializeObjects { get; set; }
        }

        [AssertExpectedLayout(typeof(SerializationInfo))]
        class ExpectedSerializationInfo
        {
            public ObjectIdentifier serializationObject { get; set; }
            public long serializationIndex { get; set; }
        }

        [AssertExpectedLayout(typeof(ObjectIdentifier))]
        class ExpectedObjectIdentifier
        {
            public GUID guid { get; }
            public long localIdentifierInFile { get; }
            public FileType fileType { get; }
            public string filePath { get; }
        }

        public static IEnumerable CustomHashTypes
        {
            get
            {
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
                    foreach (AssertExpectedLayout attr in type.GetCustomAttributes<AssertExpectedLayout>(true))
                    {
                        yield return new TestCaseData(type, attr.CompareType).SetName($"CompareLayout_{type.Name}_{attr.CompareType.Name}");
                    }
            }
        }

        // README! If this test fails, update the associated hashing function in HashingHelpers.cs and then
        // update the expected type above to reflect the new layout.
        // The purpose of this test is to make sure our custom hashing function is updated if the associate engine type changes.
        [Test, TestCaseSource(typeof(HashingHelpersTests), "CustomHashTypes")]
        public static void CompareLayout(Type expected, Type actual)
        {
            {
                PropertyInfo[] eInfo = expected.GetProperties();
                PropertyInfo[] aInfo = actual.GetProperties();
                Assert.AreEqual(eInfo.Length, aInfo.Length);
                for (int i = 0; i < eInfo.Length; i++)
                    Assert.AreEqual(eInfo[i].PropertyType, aInfo[i].PropertyType);
            }

            {
                FieldInfo[] eInfo = expected.GetFields();
                FieldInfo[] aInfo = actual.GetFields();
                Assert.AreEqual(eInfo.Length, aInfo.Length);
                for (int i = 0; i < eInfo.Length; i++)
                    Assert.AreEqual(eInfo[i].FieldType, aInfo[i].FieldType);
            }
        }

        [Test]
        public static void PreloadInfo_WhenValueChanges_HashesChange()
        {
            ObjectIdentifier obj1 = new ObjectIdentifier();
            obj1.SetFilePath("TestPath");
            PreloadInfo[] infos = new PreloadInfo[]
            {
                new PreloadInfo() { preloadObjects = new List<ObjectIdentifier>() },
                new PreloadInfo() { preloadObjects = new List<ObjectIdentifier>() { new ObjectIdentifier() } },
                new PreloadInfo() { preloadObjects = new List<ObjectIdentifier>() { obj1 } },
            };
            HashSet<Hash128> set = new HashSet<Hash128>(infos.Select(x => HashingHelpers.GetHash128(x)));
            Assert.AreEqual(infos.Length, set.Count);
        }

        [Test]
        public static void SerializationInfo_WhenValueChanges_HashesChange()
        {
            ObjectIdentifier obj1 = new ObjectIdentifier();
            obj1.SetFilePath("TestPath");
            SerializationInfo[] infos = new SerializationInfo[]
            {
                new SerializationInfo() { serializationIndex = 0 },
                new SerializationInfo() { serializationIndex = 1 },
                new SerializationInfo() { serializationIndex = 0, serializationObject = obj1  }
            };
            HashSet<Hash128> set = new HashSet<Hash128>(infos.Select(x => HashingHelpers.GetHash128(x)));
            Assert.AreEqual(infos.Length, set.Count);
        }

        [Test]
        public static void AssetBundleInfo_WhenValueChanges_HashesChange()
        {
            AssetBundleInfo[] infos = new AssetBundleInfo[]
            {
                new AssetBundleInfo() { bundleName = "Test" },
                new AssetBundleInfo() { bundleName = "Test2" },
                new AssetBundleInfo() { bundleAssets = new List<AssetLoadInfo>() { new AssetLoadInfo() { address = "a1" } } },
                new AssetBundleInfo() { bundleAssets = new List<AssetLoadInfo>() { new AssetLoadInfo() { address = "a2" } } }
            };
            HashSet<Hash128> set = new HashSet<Hash128>(infos.Select(x => HashingHelpers.GetHash128(x)));
            Assert.AreEqual(infos.Length, set.Count);
        }

        [Test]
        public static void AssetLoadInfo_WhenValueChanges_HashesChange()
        {
            AssetLoadInfo[] infos = new AssetLoadInfo[]
            {
                new AssetLoadInfo() { address = "Test" },
                new AssetLoadInfo() { asset = GUID.Generate()},
                new AssetLoadInfo() { includedObjects = new List<ObjectIdentifier>(){ new ObjectIdentifier()} },
                new AssetLoadInfo() { referencedObjects = new List<ObjectIdentifier>() { new ObjectIdentifier() } }
            };
            HashSet<Hash128> set = new HashSet<Hash128>(infos.Select(x => HashingHelpers.GetHash128(x)));
            Assert.AreEqual(infos.Length, set.Count);
        }

        [Test]
        public static void WriteCommand_WhenValueChanges_HashesChange()
        {
            WriteCommand[] infos = new WriteCommand[]
            {
                new WriteCommand() { fileName = "Test" },
                new WriteCommand() { internalName = "Test2" },
                new WriteCommand() { serializeObjects = new List<SerializationInfo>() { new SerializationInfo() }},
                new WriteCommand() { serializeObjects = new List<SerializationInfo>() { new SerializationInfo() { serializationIndex = 2 } }}
            };
            HashSet<Hash128> set = new HashSet<Hash128>(infos.Select(x => HashingHelpers.GetHash128(x)));
            Assert.AreEqual(infos.Length, set.Count);
        }
    }
}
