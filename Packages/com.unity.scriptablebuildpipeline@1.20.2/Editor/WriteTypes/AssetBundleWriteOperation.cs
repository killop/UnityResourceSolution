using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using System.Reflection;

namespace UnityEditor.Build.Pipeline.WriteTypes
{
    /// <summary>
    /// Explicit implementation for writing a serialized file that can be used with the Asset Bundle systems.
    /// </summary>
    [Serializable]
    public class AssetBundleWriteOperation : IWriteOperation
    {
        static MethodInfo BuildReferenceMapSerializeToJson = null;
         static AssetBundleWriteOperation() {

            BuildReferenceMap BM= new BuildReferenceMap();
            var type= BM.GetType();
           BuildReferenceMapSerializeToJson = type.GetMethod("SerializeToJson",BindingFlags.Instance| BindingFlags.NonPublic | BindingFlags.Public);
        }
        /// <inheritdoc />
        public WriteCommand Command { get; set; }
        /// <inheritdoc />
        public BuildUsageTagSet UsageSet { get; set; }
        /// <inheritdoc />
        public BuildReferenceMap ReferenceMap { get; set; }
        /// <inheritdoc />
        public Hash128 DependencyHash { get; set; }

        /// <summary>
        /// Information needed for generating the Asset Bundle object to be included in the serialized file.
        /// <see cref="AssetBundleInfo"/>
        /// </summary>
        public AssetBundleInfo Info { get; set; }

        /// <inheritdoc />
        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
#if UNITY_2019_3_OR_NEWER
            return ContentBuildInterface.WriteSerializedFile(outputFolder, new WriteParameters
            {
                writeCommand = Command,
                settings = settings,
                globalUsage = globalUsage,
                usageSet = UsageSet,
                referenceMap = ReferenceMap,
                bundleInfo = Info
            });
#else
            return ContentBuildInterface.WriteSerializedFile(outputFolder, Command, settings, globalUsage, UsageSet, ReferenceMap, Info);
#endif
        }

        /// <inheritdoc />
        public Hash128 GetHash128(IBuildLogger log)
        {
            HashSet<CacheEntry> hashObjects = new HashSet<CacheEntry>();
            var bundleNameAndFileName = $"{Info.bundleName} ,fileName {Command.fileName}";
            using (log.ScopedStep(LogLevel.Verbose, $"Gather Objects {GetType().Name}", bundleNameAndFileName)) {
                Command.GatherSerializedObjectCacheEntries(hashObjects);
            }
            List<Hash128> hashes = new List<Hash128>();
            using (log.ScopedStep(LogLevel.Verbose, $"Hashing Command", bundleNameAndFileName)) 
            {
                var hash = Command.GetHash128();
                log.AddEntry(LogLevel.Verbose, $"Hashing Command hash {hash}");
                hashes.Add(hash);
            }

            using (log.ScopedStep(LogLevel.Verbose, $"Hashing UsageSet", bundleNameAndFileName)) 
            {
                var hash = UsageSet.GetHash128();
                log.AddEntry(LogLevel.Verbose, $" Hashing UsageSet {hash}");
                hashes.Add(hash);
            }

           using (log.ScopedStep(LogLevel.Verbose, $"Hashing ReferenceMap", bundleNameAndFileName)) {
           
               var hash = ReferenceMap.GetHash128();
               var json = (string)(BuildReferenceMapSerializeToJson.Invoke(ReferenceMap,new object[] { }));
               log.AddEntry(LogLevel.Verbose, $" Hashing ReferenceMap {hash} json: {json}");
               hashes.Add(hash);
           }

            using (log.ScopedStep(LogLevel.Verbose, $"Hashing Info", bundleNameAndFileName))
            {
                var hash = Info.GetHash128();
                log.AddEntry(LogLevel.Verbose, $" Hashing Info {hash}");
                hashes.Add(hash);
            }

            using (log.ScopedStep(LogLevel.Verbose, $"Hashing Objects", bundleNameAndFileName)) 
            {
                var hash = HashingMethods.Calculate(hashObjects).ToHash128();
                log.AddEntry(LogLevel.Verbose, $" Hashing Objects {hash}");
                hashes.Add(hash);
            }
               
            hashes.Add(DependencyHash);
            hashes.Add(BuildInterfacesWrapper.ShaderCallbackVersionHash);

            var finalHash = HashingMethods.Calculate(hashes).ToHash128();
            using (log.ScopedStep(LogLevel.Verbose, $" Hashing {GetType().Name} bundleName {bundleNameAndFileName},finalHash {finalHash}"))
            {
                return finalHash;
            }
        
        }

        /// <inheritdoc />
        public Hash128 GetHash128()
        {
            return GetHash128(null);
        }
    }
}
