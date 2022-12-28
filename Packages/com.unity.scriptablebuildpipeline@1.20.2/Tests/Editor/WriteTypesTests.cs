using NUnit.Framework;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.WriteTypes;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    class WriteTypesTests
    {
        IWriteOperation[] WriteOperations;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            WriteOperations = new IWriteOperation[5];
            WriteOperations[0] = new AssetBundleWriteOperation();
#pragma warning disable CS0618 // Type or member is obsolete
            WriteOperations[1] = new RawWriteOperation();
#pragma warning restore CS0618 // Type or member is obsolete
            WriteOperations[2] = new SceneBundleWriteOperation();
            WriteOperations[3] = new SceneDataWriteOperation();
#pragma warning disable CS0618 // Type or member is obsolete
            WriteOperations[4] = new SceneRawWriteOperation();
#pragma warning restore CS0618 // Type or member is obsolete

            var command = new WriteCommand
            {
                fileName = GUID.Generate().ToString(),
                internalName = GUID.Generate().ToString()
            };
            var usageSet = new BuildUsageTagSet();
            var referenceMap = new BuildReferenceMap();

            for (int i = 0; i < WriteOperations.Length; i++)
            {
                WriteOperations[i].Command = command;
                WriteOperations[i].UsageSet = usageSet;
                WriteOperations[i].ReferenceMap = referenceMap;
            }
        }
        
        [OneTimeTearDown]
        public void OnetimeTearDown()
        {
            WriteOperations = null;
        }

        [SetUp]
        public void SetUp()
        {
            BuildInterfacesWrapper.SceneCallbackVersionHash = new Hash128();
            BuildInterfacesWrapper.ShaderCallbackVersionHash = new Hash128();
        }

        [TearDown]
        public void TearDown()
        {
            BuildInterfacesWrapper.SceneCallbackVersionHash = new Hash128();
            BuildInterfacesWrapper.ShaderCallbackVersionHash = new Hash128();
        }

        [Test]
        public void Changing_SceneCallbackVersionHash_ChangesHashesOf_SceneWriteOperations()
        {
            Hash128[] preHash = new Hash128[5];
            for (int i = 0; i < WriteOperations.Length; i++)
                preHash[i] = WriteOperations[i].GetHash128();

            BuildInterfacesWrapper.SceneCallbackVersionHash = HashingMethods.Calculate(GUID.Generate()).ToHash128();

            Hash128[] postHash = new Hash128[5];
            for (int i = 0; i < WriteOperations.Length; i++)
                postHash[i] = WriteOperations[i].GetHash128();

            // We expect AssetBundleWriteOperation [0] and RawWriteOperation [1] to not be changed by  the scene callback version hash.
            for (int i = 0; i < 2; i++)
                Assert.AreEqual(preHash[i], postHash[i], "{0} hash changed.", WriteOperations[i].GetType().Name);

            // We expect SceneBundleWriteOperation [2], SceneDataWriteOperation [3], and SceneRawWriteOperation [4] to be changed by the scene callback version hash.
            for (int i = 2; i < WriteOperations.Length; i++)
                Assert.AreNotEqual(preHash[i], postHash[i], "{0} hash unchanged. Not", WriteOperations[i].GetType().Name);
        }

        [Test]
        public void Changing_ShaderCallbackVersionHash_ChangesHashesOf_AllWriteOperations()
        {
            Hash128[] preHash = new Hash128[5];
            for (int i = 0; i < WriteOperations.Length; i++)
                preHash[i] = WriteOperations[i].GetHash128();

            BuildInterfacesWrapper.ShaderCallbackVersionHash = HashingMethods.Calculate(GUID.Generate()).ToHash128();

            Hash128[] postHash = new Hash128[5];
            for (int i = 0; i < WriteOperations.Length; i++)
                postHash[i] = WriteOperations[i].GetHash128();

            // We expect all write operation hashes to be changed by the scene callback version hash.
            for (int i = 0; i < WriteOperations.Length; i++)
                Assert.AreNotEqual(preHash[i], postHash[i], "{0} hash unchanged.", WriteOperations[i].GetType().Name);
        }

        [Test]
        public void Unchanged_CallbackVersionHashes_DoNotChangeHashesOf_AllWriteOperations()
        {
            Hash128[] preHash = new Hash128[5];
            for (int i = 0; i < WriteOperations.Length; i++)
                preHash[i] = WriteOperations[i].GetHash128();

            // Just zero out the hash again
            BuildInterfacesWrapper.SceneCallbackVersionHash = new Hash128();
            BuildInterfacesWrapper.ShaderCallbackVersionHash = new Hash128();

            Hash128[] postHash = new Hash128[5];
            for (int i = 0; i < WriteOperations.Length; i++)
                postHash[i] = WriteOperations[i].GetHash128();

            // We expect no write operation hashes to be changed by the scene callback version hash.
            for (int i = 0; i < WriteOperations.Length; i++)
                Assert.AreEqual(preHash[i], postHash[i], "{0} hash changed.", WriteOperations[i].GetType().Name);
        }
    }
}
