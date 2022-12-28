using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Generates a deterministic identifier using a MD4 hash algorithm and does not require object ordering to be deterministic.
    /// This algorithm generates identical results to what is used internally in <c>BuildPipeline.BuildAssetbundles</c>.
    /// </summary>
    public class Unity5PackedIdentifiers : IDeterministicIdentifiers
    {
        /// <inheritdoc />
        public virtual string GenerateInternalFileName(string name)
        {
            return "CAB-" + HashingMethods.Calculate<MD4>(name);
        }

        /// <inheritdoc />
        public virtual long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            RawHash hash;
            bool extraArtifact = objectID.filePath.StartsWith("VirtualArtifacts/Extra/", StringComparison.Ordinal);
            int hashSeed = ScriptableBuildPipeline.fileIDHashSeed;
            if (extraArtifact && hashSeed != 0)
            {
                RawHash fileHash = HashingMethods.CalculateFile(objectID.filePath);
                hash = HashingMethods.Calculate(hashSeed, fileHash, objectID.localIdentifierInFile);
            }
            else if (extraArtifact)
            {
                RawHash fileHash = HashingMethods.CalculateFile(objectID.filePath);
                hash = HashingMethods.Calculate(fileHash, objectID.localIdentifierInFile);
            }
            else if (hashSeed != 0)
            {
                if (objectID.fileType == FileType.MetaAssetType || objectID.fileType == FileType.SerializedAssetType)
                    hash = HashingMethods.Calculate<MD4>(hashSeed, objectID.guid.ToString(), objectID.fileType, objectID.localIdentifierInFile);
                else
                    hash = HashingMethods.Calculate<MD4>(hashSeed, objectID.filePath, objectID.localIdentifierInFile);
            }
            else
            {
                if (objectID.fileType == FileType.MetaAssetType || objectID.fileType == FileType.SerializedAssetType)
                    hash = HashingMethods.Calculate<MD4>(objectID.guid.ToString(), objectID.fileType, objectID.localIdentifierInFile);
                else
                    hash = HashingMethods.Calculate<MD4>(objectID.filePath, objectID.localIdentifierInFile);
            }
            
            return BitConverter.ToInt64(hash.ToBytes(), 0);
        }
    }
}
