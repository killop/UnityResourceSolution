using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor;

namespace URS
{
    /// <summary>
    /// The BuildTask used to generate the bundle layout.
    /// </summary>
    public class BuildLayoutGenerationTask : IBuildTask
    {
        const int k_Version = 1;

        internal static Action<LayoutLookupTables> s_LayoutCompleteCallback;

        /// <summary>
        /// The GenerateLocationListsTask version.
        /// </summary>
        public int Version { get { return k_Version; } }

        /// <summary>
        /// The mapping of the old to new bundle names. 
        /// </summary>
        public Dictionary<string, string> BundleNameRemap { get { return m_BundleNameRemap; } set { m_BundleNameRemap = value; }}


        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;

        [InjectContext]
        IBuildResults m_Results;

        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.In)]
        IBundleBuildResults m_BuildBundleResults;
#pragma warning restore 649

        internal Dictionary<string, string> m_BundleNameRemap;

      //  internal static string m_LayoutTextFile = Addressables.LibraryPath + "/buildlayout.txt";

        static AssetBucket GetOrCreate(Dictionary<string, AssetBucket> buckets, string asset)
        {
            if (!buckets.TryGetValue(asset, out AssetBucket bucket))
            {
                bucket = new AssetBucket();
                bucket.guid = asset;
                buckets.Add(asset, bucket);
            }
            return bucket;
        }

        class AssetBucket
        {
            public string guid;
            public bool isFilePathBucket;
            public List<ObjectSerializedInfo> objs = new List<ObjectSerializedInfo>();
            public BuildLayout.ExplicitAsset ExplictAsset;

            public ulong CalcObjectSize() { return (ulong)objs.Sum(x => (long)x.header.size); }
            public ulong CalcStreamedSize() { return (ulong)objs.Sum(x => (long)x.rawData.size); }
        }

        private LayoutLookupTables CreateBuildLayout()
        {
            LayoutLookupTables lookup = new LayoutLookupTables();

            foreach (string bundleName in m_WriteData.FileToBundle.Values.Distinct())
            {
                BuildLayout.Bundle bundle = new BuildLayout.Bundle();
                bundle.Name = bundleName;
                UnityEngine.BuildCompression compression = m_Parameters.GetCompressionForIdentifier(bundle.Name);
                bundle.Compression = compression.compression.ToString();
                lookup.Bundles.Add(bundle.Name, bundle);
            }

            // create files
            foreach (KeyValuePair<string, string> fileBundle in m_WriteData.FileToBundle)
            {
                BuildLayout.Bundle bundle = lookup.Bundles[fileBundle.Value];
                BuildLayout.File f = new BuildLayout.File();
                f.Name = fileBundle.Key;

                WriteResult result = m_Results.WriteResults[f.Name];
                foreach (ResourceFile rf in result.resourceFiles)
                {
                    var sf = new BuildLayout.SubFile();
                    sf.IsSerializedFile = rf.serializedFile;
                    sf.Name = rf.fileName+" alias:  "+rf.fileAlias;
                    sf.Size = (ulong)new FileInfo(rf.fileName).Length;
                    f.SubFiles.Add(sf);
                }

                bundle.Files.Add(f);
                lookup.Files.Add(f.Name, f);
            }

            // create assets
            foreach (KeyValuePair<GUID, List<string>> assetFile in m_WriteData.AssetToFiles)
            {
                BuildLayout.File file = lookup.Files[assetFile.Value[0]];
                BuildLayout.ExplicitAsset a = new BuildLayout.ExplicitAsset();
                a.Guid = assetFile.Key.ToString();
                a.AssetPath = AssetDatabase.GUIDToAssetPath(a.Guid);
                file.Assets.Add(a);
                lookup.GuidToExplicitAsset.Add(a.Guid, a);
            }

            Dictionary<string, List<BuildLayout.DataFromOtherAsset>> guidToPulledInBuckets = new Dictionary<string, List<BuildLayout.DataFromOtherAsset>>();

            foreach (BuildLayout.File file in lookup.Files.Values)
            {
                Dictionary<string, AssetBucket> buckets = new Dictionary<string, AssetBucket>();
                WriteResult writeResult = m_Results.WriteResults[file.Name];
                List<ObjectSerializedInfo> sceneObjects = new List<ObjectSerializedInfo>();

                foreach (ObjectSerializedInfo info in writeResult.serializedObjects)
                {
                    string sourceGuid = string.Empty;
                    if (info.serializedObject.guid.Empty())
                    {
                        if (info.serializedObject.filePath.Equals("temp:/assetbundle", StringComparison.OrdinalIgnoreCase))
                        {
                            file.BundleObjectInfo = new BuildLayout.AssetBundleObjectInfo();
                            file.BundleObjectInfo.Size = info.header.size;
                            continue;
                        }
                        else if (info.serializedObject.filePath.StartsWith("temp:/preloaddata", StringComparison.OrdinalIgnoreCase))
                        {
                            file.PreloadInfoSize = (int)info.header.size;
                            continue;
                        }
                        else if (info.serializedObject.filePath.StartsWith("temp:/", StringComparison.OrdinalIgnoreCase))
                        {
                            sceneObjects.Add(info);
                            continue;
                        }
                        else if (!string.IsNullOrEmpty(info.serializedObject.filePath))
                        {
                            AssetBucket pathBucket = GetOrCreate(buckets, info.serializedObject.filePath.ToString());
                            pathBucket.isFilePathBucket = true;
                            pathBucket.objs.Add(info);
                            continue;
                        }
                    }

                    AssetBucket bucket = GetOrCreate(buckets, info.serializedObject.guid.ToString());
                    bucket.objs.Add(info);
                }

                if (sceneObjects.Count > 0)
                {
                    BuildLayout.ExplicitAsset sceneAsset = file.Assets.First(x => x.AssetPath.EndsWith(".unity"));
                    AssetBucket bucket = GetOrCreate(buckets, sceneAsset.Guid);
                    bucket.objs.AddRange(sceneObjects);
                }

                // Update buckets with a reference to their explicit asset
                file.Assets.ForEach(eAsset =>
                {
                    if (!buckets.TryGetValue(eAsset.Guid, out AssetBucket b))
                        b = GetOrCreate(buckets, eAsset.Guid); // some assets might not pull in any objects
                    b.ExplictAsset = eAsset;
                });

                // Create entries for buckets that are implicitly pulled in
                Dictionary<string, BuildLayout.DataFromOtherAsset> guidToOtherData = new Dictionary<string, BuildLayout.DataFromOtherAsset>();
                foreach (AssetBucket bucket in buckets.Values.Where(x => x.ExplictAsset == null))
                {
                    string assetPath = bucket.isFilePathBucket ? bucket.guid : AssetDatabase.GUIDToAssetPath(bucket.guid);
                    if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || assetPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        file.MonoScriptCount++;
                        file.MonoScriptSize += bucket.CalcObjectSize();
                        continue;
                    }
                    var otherData = new BuildLayout.DataFromOtherAsset();
                    otherData.AssetPath = assetPath;
                    otherData.AssetGuid = bucket.guid;
                    otherData.SerializedSize = bucket.CalcObjectSize();
                    otherData.StreamedSize = bucket.CalcStreamedSize();
                    otherData.ObjectCount = bucket.objs.Count;
                    file.OtherAssets.Add(otherData);
                    guidToOtherData[otherData.AssetGuid] = otherData;

                    if (!guidToPulledInBuckets.TryGetValue(otherData.AssetGuid, out List<BuildLayout.DataFromOtherAsset> bucketList))
                        bucketList = guidToPulledInBuckets[otherData.AssetGuid] = new List<BuildLayout.DataFromOtherAsset>();
                    bucketList.Add(otherData);
                }

                // Add references
                foreach (BuildLayout.ExplicitAsset asset in file.Assets)
                {
                    AssetBucket bucket = buckets[asset.Guid];
                    asset.SerializedSize = bucket.CalcObjectSize();
                    asset.StreamedSize = bucket.CalcStreamedSize();

                    IEnumerable<ObjectIdentifier> refs = null;
                    if (m_DependencyData.AssetInfo.TryGetValue(new GUID(asset.Guid), out AssetLoadInfo info))
                        refs = info.referencedObjects;
                    else
                        refs = m_DependencyData.SceneInfo[new GUID(asset.Guid)].referencedObjects;
                    foreach (string refGUID in refs.Select(x => x.guid.Empty() ? x.filePath : x.guid.ToString()).Distinct())
                    {
                        if (guidToOtherData.TryGetValue(refGUID, out BuildLayout.DataFromOtherAsset dfoa))
                        {
                            dfoa.ReferencingAssets.Add(asset);
                            asset.InternalReferencedOtherAssets.Add(dfoa);
                        }
                        else if (buckets.TryGetValue(refGUID, out AssetBucket refBucket))
                        {
                            asset.InternalReferencedExplicitAssets.Add(refBucket.ExplictAsset);
                        }
                        else if (lookup.GuidToExplicitAsset.TryGetValue(refGUID, out BuildLayout.ExplicitAsset refAsset))
                        {
                            asset.ExternallyReferencedAssets.Add(refAsset);
                        }
                    }
                }
            }
            var outerFolderPath = (m_Parameters as BuildParameters).OutputFolder;
            var bbResult = m_Results as BundleBuildResults;
                // go through all the bundles and put them in groups
            foreach (BuildLayout.Bundle b in lookup.Bundles.Values)
            {
                 b.ExpandedDependencies = new List<BuildLayout.Bundle>();
                 var dependencys= bbResult.BundleInfos[b.Name].Dependencies;
                 for (int i = 0; i < dependencys.Length; i++)
                 {
                    var dp = dependencys[i];
                    if (lookup.Bundles.ContainsKey(dp))
                    {
                        b.ExpandedDependencies.Add(lookup.Bundles[dp]);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("没有收集到dp的依赖"+ dp);
                    }
                 }
                b.FileSize = (ulong)new FileInfo(Path.Combine(outerFolderPath, b.Name)).Length;
            }
            return lookup;
        }

        /// <summary>
        /// Runs the build task with the injected context.
        /// </summary>
        /// <returns>The success or failure ReturnCode</returns>
        public ReturnCode Run()
        {
            LayoutLookupTables LayoutLookupTables = CreateBuildLayout();

            var path = (m_Parameters as BuildParameters).OutputFolder + "/buildlayout.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (FileStream s = File.Open(path, FileMode.Create))
                BuildLayoutPrinter.WriteBundleLayout(s, LayoutLookupTables);

            UnityEngine.Debug.Log($"Build layout written to {path}");

            s_LayoutCompleteCallback?.Invoke(LayoutLookupTables);

            return ReturnCode.Success;
        }
    }
}
