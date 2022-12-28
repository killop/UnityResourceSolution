using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace URS
{
    class BuildLayoutPrinter
    {
        static public string GetFriendlySize(ulong byteSize)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            ulong prevOrderRemainder = 0;
            while (byteSize >= 1024 && order < sizes.Length - 1)
            {
                order++;
                prevOrderRemainder = byteSize % 1024;
                byteSize = byteSize / 1024;
            }

            double byteSizeFloat = (double)byteSize + (double)prevOrderRemainder / 1024;

            string result = String.Format("{0:0.##}{1}", byteSizeFloat, sizes[order]);
            return result;
        }

        class TabWriter
        {
            public StreamWriter Writer;
            private int Indentation;
            public TabWriter(StreamWriter writer)
            {
                Writer = writer;
            }

            class TabWriterIdentScope : IDisposable
            {
                TabWriter m_Writer;
                public TabWriterIdentScope(TabWriter writer)
                {
                    m_Writer = writer;
                    writer.Indentation++;
                }

                public void Dispose()
                {
                    m_Writer.Indentation--;
                }
            }

            public IDisposable IndentScope(string text = null)
            {
                if (text != null)
                    WriteLine(text);
                return new TabWriterIdentScope(this);
            }

            public void WriteLine(string line)
            {
                Writer.WriteLine(new String('\t', Indentation) + line);
            }
        }

        class AttrBuilder
        {
            List<Tuple<string, string>> m_Items = new List<Tuple<string, string>>();
            public void Add(string k, string v) { m_Items.Add(new Tuple<string, string>(k, v)); }
            public void AddSize(string k, ulong size) { m_Items.Add(new Tuple<string, string>(k, GetFriendlySize(size))); }
            public override string ToString()
            {
                return "(" + string.Join(", ", m_Items.Select(x => $"{x.Item1}: {x.Item2}")) + ")";
            }
        }


        static void PrintAsset(TabWriter writer, BuildLayout.ExplicitAsset asset, int fileIndex)
        {
            AttrBuilder attr = new AttrBuilder();
            attr.AddSize("Total Size", asset.SerializedSize + asset.StreamedSize);
            attr.AddSize("Size from Objects", asset.SerializedSize);
            attr.AddSize("Size from Streamed Data", asset.StreamedSize);
            attr.Add("File Index", fileIndex.ToString());
            attr.Add("Addressable Name", asset.AddressableName);
            using (writer.IndentScope($"{asset.AssetPath} {attr}"))
            {
                if (asset.ExternallyReferencedAssets.Count > 0)
                {
                    writer.WriteLine("External References: " + string.Join(", ", asset.ExternallyReferencedAssets.Select(x => x.AssetPath)));
                }
                if (asset.InternalReferencedOtherAssets.Count > 0)
                {
                    writer.WriteLine("Internal References: " + string.Join(", ", asset.InternalReferencedOtherAssets.Select(x => x.AssetPath)));
                }
            }
        }

        static void PrintDataFromOtherAsset(TabWriter writer, BuildLayout.DataFromOtherAsset asset)
        {
            AttrBuilder attr = new AttrBuilder();
            attr.AddSize("Size", asset.SerializedSize + asset.StreamedSize);
            attr.AddSize("Size from Objects", asset.SerializedSize);
            attr.AddSize("Size from Streamed Data", asset.StreamedSize);
            attr.Add("Object Count", asset.ObjectCount.ToString());
            using (writer.IndentScope($"{asset.AssetPath} {attr}"))
            {
                writer.WriteLine($"Referencing Assets: {string.Join(", ", asset.ReferencingAssets.Select(x => x.AssetPath))}");
            }
        }

        static void PrintFile(TabWriter writer, BuildLayout.File file, int i)
        {
            AttrBuilder attr = new AttrBuilder();
            if (file.PreloadInfoSize > 0)
                attr.AddSize("PreloadInfoSize", (ulong)file.PreloadInfoSize);

            attr.Add("MonoScripts", file.MonoScriptCount.ToString());
            attr.AddSize("MonoScript Size", file.MonoScriptSize);

            using (writer.IndentScope($"File {i} {attr}"))
            {
                foreach (BuildLayout.SubFile sf in file.SubFiles)
                {
                    AttrBuilder attr2 = new AttrBuilder();
                    attr2.AddSize("Size", sf.Size);
                    writer.WriteLine($"{sf.Name} {attr2}");
                }

                using (writer.IndentScope($"Data From Other Assets ({file.OtherAssets.Count})"))
                {
                    foreach (BuildLayout.DataFromOtherAsset otherData in file.OtherAssets)
                    {
                        PrintDataFromOtherAsset(writer, otherData);
                    }
                }
            }
        }

        static void PrintArchive(TabWriter writer, BuildLayout.Bundle archive)
        {
            AttrBuilder attr = new AttrBuilder();
            attr.AddSize("Size", archive.FileSize);
            attr.Add("Compression", archive.Compression);

            ulong bundleSize = 0;
            for (int i = 0; i < archive.Files.Count; i++)
            {
                var file = archive.Files[i];
                if (file.BundleObjectInfo != null)
                {
                    bundleSize = file.BundleObjectInfo.Size;
                }
            }
           // .First(x => x.BundleObjectInfo != null).BundleObjectInfo.Size;
            attr.AddSize("Asset Bundle Object Size", bundleSize);

            using (writer.IndentScope($"Archive {archive.Name} {attr}"))
            {
               // if (archive.Dependencies != null)
               //     writer.WriteLine("Bundle Dependencies: " + string.Join(", ", archive.Dependencies.Select(x => x.Name)));

                if (archive.ExpandedDependencies != null)
                    writer.WriteLine("Expanded Bundle Dependencies: " + string.Join(", ", archive.ExpandedDependencies.Select(x => x.Name)));

                using (writer.IndentScope($"Explicit Assets"))
                {
                    for (int i = 0; i < archive.Files.Count; i++)
                    {
                        BuildLayout.File f = archive.Files[i];
                        foreach (BuildLayout.ExplicitAsset asset in f.Assets)
                        {
                            PrintAsset(writer, asset, i);
                        }
                    }
                }

                using (writer.IndentScope($"Files:"))
                {
                    for (int i = 0; i < archive.Files.Count; i++)
                        PrintFile(writer, archive.Files[i], i);
                }
            }
        }
        /*
       static void PrintSchema(TabWriter writer, BuildLayout.SchemaData sd)
       {
           string text = sd.Type;
           if (sd.KvpDetails.Count > 0)
           {
               AttrBuilder attr = new AttrBuilder();
               sd.KvpDetails.ForEach(x => attr.Add(x.Item1, x.Item2));
               text += " " + attr;
           }
           writer.WriteLine(text);
       }

       static void PrintGroup(TabWriter writer, BuildLayout.Group grp)
       {
           int explicitAssetCount = grp.Bundles.Sum(x => x.Files.Sum(y => y.Assets.Count));
           AttrBuilder attr = new AttrBuilder();
           attr.Add("Bundles", grp.Bundles.Count.ToString());
           attr.AddSize("Total Size", (ulong)grp.Bundles.Sum(x => (long)x.FileSize));
           attr.Add("Explicit Asset Count", explicitAssetCount.ToString());

           using (writer.IndentScope($"Group {grp.Name} {attr}"))
           {
               using (writer.IndentScope("Schemas"))
                   grp.Schemas.ForEach(x => PrintSchema(writer, x));

               foreach (BuildLayout.Bundle archive in grp.Bundles)
                   PrintArchive(writer, archive);
           }
       }
       */

        internal static void WriteBundleLayout(Stream stream, LayoutLookupTables LayoutLookupTables)
        {
            using (StreamWriter sw = new StreamWriter(stream))
            {
                TabWriter writer = new TabWriter(sw);

               // writer.WriteLine("WARNING! The formatting in this file may change in future package versions.");
                writer.WriteLine($"Unity Version: {UnityEngine.Application.unityVersion}");
               // PackageManager.PackageInfo info = PackageManager.PackageInfo.FindForAssembly(typeof(BuildLayoutPrinter).Assembly);
               // if (info != null)
                 //   writer.WriteLine($"{info.name}: {info.version}");

                WriteSummary(writer, LayoutLookupTables);
                writer.WriteLine("");

                /*
                foreach (BuildLayout.Group grp in layout.Groups)
                {
                    PrintGroup(writer, grp);
                }
                */
                using (writer.IndentScope("BuiltIn Bundles"))
                    foreach (BuildLayout.Bundle b in LayoutLookupTables.Bundles.Values)
                        PrintArchive(writer, b);
            }
        }

        static void WriteSummary(TabWriter writer, LayoutLookupTables layout)
        {
            int ExplicitAssetCount = 0;
            int SceneBundleCount = 0;
            int AssetBundleCount = 0;
            ulong TotalBuildSize = 0;
            ulong MonoScriptSize = 0;
            ulong BundleOverheadSize = 0;

            foreach (BuildLayout.File f in BuildLayoutHelpers.EnumerateFiles(layout))
            {
                BundleOverheadSize += f.BundleObjectInfo != null ? f.BundleObjectInfo.Size : 0;
                MonoScriptSize += f.MonoScriptSize;
            }

            foreach (BuildLayout.Bundle b in BuildLayoutHelpers.EnumerateBundles(layout))
            {
                bool sceneBundle = BuildLayoutHelpers.EnumerateAssets(b).FirstOrDefault(x => x.AssetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) != null;
                SceneBundleCount += sceneBundle ? 1 : 0;
                AssetBundleCount += sceneBundle ? 0 : 1;
                TotalBuildSize += b.FileSize;
            }

            ExplicitAssetCount = BuildLayoutHelpers.EnumerateAssets(layout).Count();

            using (writer.IndentScope("Summary"))
            {
               // writer.WriteLine($"Addressable Groups: {layout.Groups.Count}");
                writer.WriteLine($"Explicit Assets Addressed: {ExplicitAssetCount}");
                writer.WriteLine($"Total Bundle: {SceneBundleCount+AssetBundleCount} ({SceneBundleCount} Scene Bundles, {AssetBundleCount} Non-Scene Bundles)");
                writer.WriteLine($"Total Build Size: {GetFriendlySize(TotalBuildSize)}");
                writer.WriteLine($"Total MonoScript Size: {GetFriendlySize(MonoScriptSize)}");
                writer.WriteLine($"Total AssetBundle Object Size: {GetFriendlySize(BundleOverheadSize)}");
            }
        }
    }
}
