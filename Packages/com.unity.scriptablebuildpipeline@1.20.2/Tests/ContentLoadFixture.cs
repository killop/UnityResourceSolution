#if UNITY_2022_2_OR_NEWER
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Content;
using Unity.Loading;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
#endif

namespace UnityEditor.Build.Pipeline.Tests.ContentLoad
{
    public abstract class ContentFileFixture : IPrebuildSetup
    {
        protected string m_ContentRoot;

        protected string ContentRoot
        {
            get { return m_ContentRoot; }
        }

        public virtual string ContentDir
        {
            get { return Path.Combine(ContentRoot, this.GetType().Name); }
        }

        protected Catalog m_Catalog;
        protected string m_CatalogDir;
        protected ContentNamespace m_NS;
        Dictionary<string, ContentFile> m_LoadedFiles = new Dictionary<string, ContentFile>();
        List<ContentFile> m_ToUnload = new List<ContentFile>();

        public ContentFileFixture()
        {
            m_ContentRoot = Application.streamingAssetsPath;
        }

        [SetUp]
        public void SetUp()
        {
            m_NS = ContentNamespace.GetOrCreateNamespace("Test");
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_ToUnload.Count - 1; i >= 0; i--)
                m_ToUnload[i].UnloadAsync().WaitForCompletion(0);
            m_ToUnload.Clear();

            if (TestContext.CurrentContext.Result.Outcome != ResultState.Failure)
            {
                Assert.AreEqual(0, ContentLoadInterface.GetContentFiles(m_NS).Length);
                Assert.AreEqual(0, ContentLoadInterface.GetSceneFiles(m_NS).Length);
            }

            m_NS.Delete();
        }

        public void LoadCatalog(string name, bool mountAllArchives = true)
        {
            m_CatalogDir = Path.Combine(ContentDir, name);
            string path = Path.Combine(m_CatalogDir, "catalog.json");
            m_Catalog = Catalog.LoadFromFile(path);
        }

        public string GetVFSFilename(string fn)
        {
            return Path.Combine(m_CatalogDir, fn);
        }

#if UNITY_EDITOR
        protected abstract void PrepareBuildLayout();

        Dictionary<string, List<AssetBundleBuild>> m_CurrentBuildLayouts =
            new Dictionary<string, List<AssetBundleBuild>>();

        protected virtual void DefaultBuild()
        {
            PrepareBuildLayout();
            foreach (var kvp in m_CurrentBuildLayouts)
            {
                string outDir = Path.Combine(ContentDir, kvp.Key);
                BuildContentLoadsAndCatalog(outDir, kvp.Value.ToArray());
            }

            // Create a file manifest
            List<string> allFiles = Directory.EnumerateFiles(ContentDir, "*", SearchOption.AllDirectories)
                .Select(x => x.Substring(ContentDir.Length + 1))
                .Where(x => !x.EndsWith(".meta") && x != "FileManifest.txt").ToList();
            string manifestDst = Path.Combine(ContentDir, "FileManifest.txt");
            File.WriteAllText(manifestDst, string.Join('\n', allFiles));

        }

        public class ContentBuildCatalog : IDisposable
        {
            List<AssetBundleBuild> m_Items = new List<AssetBundleBuild>();
            ContentFileFixture m_Fixture;
            string m_Name;

            public ContentBuildCatalog(string name, ContentFileFixture fixture)
            {
                m_Name = name;
                m_Fixture = fixture;
            }

            public void Add(AssetBundleBuild build)
            {
                m_Items.Add(build);
            }

            public void Dispose()
            {
                m_Fixture.m_CurrentBuildLayouts.Add(m_Name, m_Items);
            }
        }

        public ContentBuildCatalog CreateCatalog(string name)
        {
            return new ContentBuildCatalog(name, this);
        }

        protected void BuildContentLoadsAndCatalog(string outdir, AssetBundleBuild[] bundles)
        {
            Directory.CreateDirectory(outdir);
            for (int i = 0; i < bundles.Length; i++)
                bundles[i].assetBundleName = "";
            var content = new BundleBuildContent(bundles);
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var parameters = new BundleBuildParameters(target, group, outdir);
            parameters.UseCache = false;
            parameters.BundleCompression = UnityEngine.BuildCompression.Uncompressed;
            parameters.NonRecursiveDependencies = false;
            parameters.WriteLinkXML = true;
            var taskList = DefaultBuildTasks.ContentFileCompatible();
            ClusterOutput cOutput = new ClusterOutput();
            IBundleBuildResults results;
            ContentPipeline.BuildAssetBundles(parameters, content, out results, taskList,
                    new ContentFileIdentifiers(), cOutput);

            Catalog catalog = new Catalog();
            foreach (var result in results.WriteResults)
            {
                var fi = new Catalog.ContentFileInfo();
                fi.Filename = result.Key;
                fi.Dependencies = new List<string>();
                foreach (var extRef in result.Value.externalFileReferences)
                    fi.Dependencies.Add(extRef.filePath);
                catalog.ContentFiles.Add(fi);
            }

            foreach (AssetBundleBuild ab in bundles)
            {
                for (int i = 0; i < ab.assetNames.Length; i++)
                {
                    string assetName = ab.assetNames[i];
                    GUID guid = AssetDatabase.GUIDFromAssetPath(assetName);
                    var loc = new Catalog.AddressableLocation();
                    loc.AddressableName = ab.addressableNames[i];
                    if (assetName.EndsWith(".unity"))
                    {
                        loc.Filename = guid.ToString();
                        loc.LFID = 0;
                    }
                    else
                    {
                        ObjectIdentifier oi =
                            ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(guid,
                                EditorUserBuildSettings.activeBuildTarget)[0];
                        loc.Filename = cOutput.ObjectToCluster[oi].ToString();
                        loc.LFID = (ulong) cOutput.ObjectToLocalID[oi];
                    }

                    catalog.Locations.Add(loc);
                }
            }

            File.WriteAllText(Path.Combine(outdir, "catalog.json"), JsonUtility.ToJson(catalog));
        }
#endif

        public void Setup()
        {
#if UNITY_EDITOR
            DefaultBuild();
#endif
        }
    }
}
#endif