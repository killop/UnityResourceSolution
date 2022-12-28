using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    [TestFixture]
    class PrefabPackedStressTest
    {
        static ObjectIdentifier MakeObjectId(GUID guid, long localIdentifierInFile, FileType fileType, string filePath)
        {
            var objectId = new ObjectIdentifier();
            var boxed = (object)objectId;
            var type = typeof(ObjectIdentifier);
            type.GetField("m_GUID", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, guid);
            type.GetField("m_LocalIdentifierInFile", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, localIdentifierInFile);
            type.GetField("m_FileType", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, fileType);
            type.GetField("m_FilePath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, filePath);
            return (ObjectIdentifier)boxed;
        }

        static long LongRandom(System.Random rand)
        {
            byte[] buffer = new byte[8];
            rand.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        static unsafe GUID GuidRandom(System.Random rand)
        {
            GUID guid = new GUID();
            byte[] bytes = new byte[16];
            rand.NextBytes(bytes);
            fixed (byte* ptr = &bytes[0])
                UnsafeUtility.MemCpy(&guid, ptr, bytes.Length);
            return guid;
        }

        void SetupSBP(out bool prevV2Hasher, out int prevSeed, out int prevHeader, bool v2Hasher = false, int seed = 0, int header = 2)
        {
#if UNITY_2020_1_OR_NEWER
            prevV2Hasher = ScriptableBuildPipeline.useV2Hasher;
            ScriptableBuildPipeline.useV2Hasher = v2Hasher;
#else
            prevV2Hasher = false;
#endif
            prevSeed = ScriptableBuildPipeline.fileIDHashSeed;
            ScriptableBuildPipeline.fileIDHashSeed = seed;
            prevHeader = ScriptableBuildPipeline.prefabPackedHeaderSize;
            ScriptableBuildPipeline.prefabPackedHeaderSize = header;
        }

        void ResetSBP(bool prevV2Hasher, int prevSeed, int prevHeader)
        {
#if UNITY_2020_1_OR_NEWER
            ScriptableBuildPipeline.useV2Hasher = prevV2Hasher;
#endif
            ScriptableBuildPipeline.fileIDHashSeed = prevSeed;
            ScriptableBuildPipeline.prefabPackedHeaderSize = prevHeader;
        }

        // We want to ensure 1 million objects in a single asset is collision free with the default implementations we provide users.
        static int LfidStressRunCount = 1000000;
        // We want to ensure 1 object in a 1 million assets is collision free with the default implementations we provide users.
        static int GuidStressRunCount = 1000000;
        // We want to ensure that we have a low number of cluster collisions in a bundle as this ensures better loading performance.
        // Ideally we want 0 collisions, but working with 64 bits in the way we do for build, that might be impossible without getting more collisions
        // in the full lfid generation which will break a build, vs just slow downloading slightly.
        static int BatchStressRunCount_Large = 10000;
        static int BatchStressRunCount_Small = 100;

        static int RandomSeed = 131072;

        static ScriptableBuildPipeline.Settings DefaultSettings = new ScriptableBuildPipeline.Settings();
        static object[] LfidStressCases =
        {
            new object[] { new PrefabPackedIdentifiers(), false, DefaultSettings.fileIDHashSeed, DefaultSettings.prefabPackedHeaderSize },
            new object[] { new PrefabPackedIdentifiers(), true, DefaultSettings.fileIDHashSeed, DefaultSettings.prefabPackedHeaderSize }
        };

        [Test]
        [TestCaseSource(nameof(LfidStressCases))]
        public void SerializationIndexFromObjectIdentifier_Lfid_CollisionFreeStressTest(IDeterministicIdentifiers packingMethod, bool useV2Hasher, int seed, int headerSize)
        {
            SetupSBP(out bool prevV2Hasher, out int prevSeed, out int prevHeader, useV2Hasher, seed, headerSize);

            System.Random rand = new System.Random(RandomSeed);
            ObjectIdentifier objId = MakeObjectId(GuidRandom(rand), 3867712242362628071, FileType.MetaAssetType, "");
            Dictionary<long, ObjectIdentifier> consumedIds = new Dictionary<long, ObjectIdentifier>(LfidStressRunCount);
            for (int i = 0; i < LfidStressRunCount; i++)
            {
                objId.SetLocalIdentifierInFile(LongRandom(rand));
                long lfid = packingMethod.SerializationIndexFromObjectIdentifier(objId);
                if (consumedIds.TryGetValue(lfid, out var prevObjId))
                {
                    if (objId == prevObjId)
                        continue;
                    else
                        Assert.Fail($"{objId} with {prevObjId} at {lfid}");
                }

                consumedIds.Add(lfid, objId);
            }

            ResetSBP(prevV2Hasher, prevSeed, prevHeader);
        }

        [Test]
        [TestCaseSource(nameof(LfidStressCases))]
        public void SerializationIndexFromObjectIdentifier_GUID_CollisionFreeStressTest(IDeterministicIdentifiers packingMethod, bool useV2Hasher, int seed, int headerSize)
        {
            SetupSBP(out bool prevV2Hasher, out int prevSeed, out int prevHeader, useV2Hasher, seed, headerSize);
            
            System.Random rand = new System.Random(RandomSeed);
            ObjectIdentifier objId = MakeObjectId(GuidRandom(rand), 3867712242362628071, FileType.MetaAssetType, "");
            Dictionary<long, ObjectIdentifier> consumedIds = new Dictionary<long, ObjectIdentifier>(GuidStressRunCount);
            for (int i = 0; i < GuidStressRunCount; i++)
            {
                objId.SetGuid(GuidRandom(rand));
                long lfid = packingMethod.SerializationIndexFromObjectIdentifier(objId);
                if (consumedIds.TryGetValue(lfid, out var prevObjId))
                {
                    if (objId == prevObjId)
                        continue;
                    else
                        Assert.Fail($"{objId} with {prevObjId} at {lfid}");
                }

                consumedIds.Add(lfid, objId);
            }
            
            ResetSBP(prevV2Hasher, prevSeed, prevHeader);
        }

        [Test]
        [TestCaseSource(nameof(LfidStressCases))]
        public void SerializationIndexFromObjectIdentifier_BatchingQualityStressTest_Large(IDeterministicIdentifiers packingMethod, bool useV2Hasher, int seed, int headerSize)
        {
            BatchingQualityStressTest(packingMethod, useV2Hasher, seed, headerSize, BatchStressRunCount_Large);
        }

        [Test]
        [TestCaseSource(nameof(LfidStressCases))]
        public void SerializationIndexFromObjectIdentifier_BatchingQualityStressTest_Small(IDeterministicIdentifiers packingMethod, bool useV2Hasher, int seed, int headerSize)
        {
            BatchingQualityStressTest(packingMethod, useV2Hasher, seed, headerSize, BatchStressRunCount_Small);
        }

        void BatchingQualityStressTest(IDeterministicIdentifiers packingMethod, bool useV2Hasher, int seed, int headerSize, int runCount)
        {
            // This test is to check the quality of default clustering per source asset falls within certain guidelines
            // For 10,000 unique assets and 100 unique assets, we want to ensure that at most we see <0.1% (10) 
            // assets generate the same cluster and <10% collision of all clusters.
            SetupSBP(out bool prevV2Hasher, out int prevSeed, out int prevHeader, useV2Hasher, seed, headerSize);
            
            System.Random rand = new System.Random(RandomSeed);
            ObjectIdentifier objId = MakeObjectId(GuidRandom(rand), 3867712242362628071, FileType.MetaAssetType, "");
            Dictionary<int, int> clusters = new Dictionary<int, int>();
            for (int i = 0; i < runCount; i++)
            {
                objId.SetGuid(GuidRandom(rand));
                long lfid = packingMethod.SerializationIndexFromObjectIdentifier(objId);
                byte[] bytes = BitConverter.GetBytes(lfid);
                byte[] header = new byte[4];
                for (int j = 0; j < ScriptableBuildPipeline.prefabPackedHeaderSize; j++)
                    header[4 - ScriptableBuildPipeline.prefabPackedHeaderSize + j] = bytes[j];

                int cluster = BitConverter.ToInt32(header, 0);
                clusters.TryGetValue(cluster, out int count);
                clusters[cluster] = count + 1;
            }

            int[] collisionValues = clusters.Values.Where(x => x > 1).ToArray();
            Array.Sort(collisionValues);
            int collisions = collisionValues.Length;
            // Maximum assets per cluster with multiple assets, lower is better
            int maxCollisions = collisionValues.Length > 0 ? collisionValues.Last() : 0;
            // Median assets per cluster with multiple assets, lower is better
            int medCollisions = collisionValues.Length > 0 ? collisionValues[collisionValues.Length / 2] : 0;
            Debug.Log($"Reused Clusters {collisions} ({(float)collisions/runCount*100:n2}%), Max {maxCollisions} ({(float)maxCollisions/runCount*100}%), Med {medCollisions} ({(float)medCollisions/runCount*100}%)");
            Assert.IsTrue(runCount * 0.1f > collisions, "Reused cluster count > 10%");
            Assert.IsTrue(runCount * 0.001f > maxCollisions, "Max per cluster reuse > 0.1%");
            
            ResetSBP(prevV2Hasher, prevSeed, prevHeader);
        }

        [Test]
        public void CreateWriteCommand_ThrowsBuildException_OnCollision()
        {
            SetupSBP(out bool prevV2Hasher, out int prevSeed, out int prevHeader, false, 0, 4);

            List<ObjectIdentifier> objectIds = new List<ObjectIdentifier>();
            objectIds.Add(MakeObjectId(new GUID("066ce95d52fe15041854096a2145195e"), 3867712242362628071, FileType.MetaAssetType, ""));
            objectIds.Add(MakeObjectId(new GUID("066ce95d52fe15041854096a2145195e"), 7498449973661844796, FileType.MetaAssetType, ""));
            
            Assert.Throws(typeof(BuildFailedException), () => GenerateBundleCommands.CreateWriteCommand("InternalName", objectIds, new PrefabPackedIdentifiers()));

            ResetSBP(prevV2Hasher, prevSeed, prevHeader);
        }
    }
}
