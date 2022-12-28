using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Generates a deterministic identifier using a MD5 hash algorithm and does not require object ordering to be deterministic.
    /// This algorithm ensures objects coming from the same asset are packed closer together and can improve loading performance under certain situations.
    /// </summary>
    public class PrefabPackedIdentifiers : IDeterministicIdentifiers
    {
        /// <inheritdoc />
        public virtual string GenerateInternalFileName(string name)
        {
            return "CAB-" + HashingMethods.Calculate(name);
        }

        /// <inheritdoc />
        public virtual long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            byte[] assetHash;
            byte[] objectHash;
            bool extraArtifact = objectID.filePath.StartsWith("VirtualArtifacts/Extra/", StringComparison.Ordinal);
            int hashSeed = ScriptableBuildPipeline.fileIDHashSeed;
            if (extraArtifact && hashSeed != 0)
            {
                RawHash fileHash = HashingMethods.CalculateFile(objectID.filePath);
                assetHash = HashingMethods.Calculate(hashSeed, fileHash).ToBytes();
                objectHash = HashingMethods.Calculate(hashSeed, fileHash, objectID.localIdentifierInFile).ToBytes();
            }
            else if (extraArtifact)
            {
                RawHash fileHash = HashingMethods.CalculateFile(objectID.filePath);
                assetHash = fileHash.ToBytes();
                objectHash = HashingMethods.Calculate(fileHash, objectID.localIdentifierInFile).ToBytes();
            }
            else if (hashSeed != 0)
            {
                assetHash = HashingMethods.Calculate(hashSeed, objectID.guid, objectID.filePath).ToBytes();
                objectHash = HashingMethods.Calculate(hashSeed, objectID).ToBytes();
            }
            else
            {
                assetHash = HashingMethods.Calculate(objectID.guid, objectID.filePath).ToBytes();
                objectHash = HashingMethods.Calculate(objectID).ToBytes();
            }

            int headerSize = ScriptableBuildPipeline.prefabPackedHeaderSize;
            if (headerSize < 4)
            {
                for (int i = 0; i < headerSize; i++)
                    objectHash[i] = assetHash[i];
                return BitConverter.ToInt64(objectHash, 0);
            }
            else
            {
                var assetVal = BitConverter.ToUInt64(assetHash, 0);
                var objectVal = BitConverter.ToUInt64(objectHash, 0);
                return (long)((0xFFFFFFFF00000000 & assetVal) | (0x00000000FFFFFFFF & (objectVal ^ assetVal)));
            }
        }
    }
}
