using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tests
{
    [TestFixture]
    public class LinkXMLGeneratorTests
    {
        const string k_LinkFile = "link.xml";

        [TearDown]
        public void OnTearDown()
        {
            if (File.Exists(k_LinkFile))
                File.Delete(k_LinkFile);
        }

        public static string ReadLinkXML(string linkFile, out int assemblyCount, out int typeCount)
        {
            FileAssert.Exists(linkFile);
            var fileText = File.ReadAllText(linkFile);
            assemblyCount = Regex.Matches(fileText, "<assembly").Count;
            typeCount = Regex.Matches(fileText, "<type").Count;
            return fileText;
        }

        public static void AssertTypePreserved(string input, Type t)
        {
            StringAssert.IsMatch($"type.*?{t.FullName}.*?preserve=\"all\"", input);
        }

        public static void AssertTypeWithAttributePreserved(string input, string fullName)
        {
            StringAssert.IsMatch($"type.*?{fullName}.*? preserve=\"nothing\" serialized=\"true\"", input);
        }

        public static void AssertAssemblyPreserved(string input, Assembly a)
        {
            StringAssert.IsMatch($"assembly.*?{a.FullName}.*?preserve=\"all\"", input);
        }

        public static void AssertAssemblyFullName(string input, string assemblyName)
        {
            StringAssert.IsMatch($"assembly.*?{assemblyName}", input);
        }

        [Test]
        public void CreateDefault_Converts_ExpectedUnityEditorTypes()
        {
            var types = LinkXmlGenerator.GetEditorTypeConversions();
            var editorTypes = types.Select(x => x.Key).ToArray();
            var runtimeTypes = types.Select(x => x.Value).ToArray();
            var assemblies = runtimeTypes.Select(x => x.Assembly).Distinct().ToArray();

            var link = LinkXmlGenerator.CreateDefault();
            link.AddTypes(editorTypes);
            link.Save(k_LinkFile);

            var xml = ReadLinkXML(k_LinkFile, out int assemblyCount, out int typeCount);
            Assert.AreEqual(assemblyCount, assemblies.Length);
            Assert.AreEqual(typeCount, runtimeTypes.Length);
            foreach (var t in runtimeTypes)
                AssertTypePreserved(xml, t);
        }

        [Test]
        public void CreateDefault_DoesNotConvert_UnexpectedUnityEditorTypes()
        {
            var unexpectedType = typeof(UnityEditor.BuildPipeline);

            var link = LinkXmlGenerator.CreateDefault();
            link.AddTypes(new[] { unexpectedType });
            link.Save(k_LinkFile);

            var xml = ReadLinkXML(k_LinkFile, out int assemblyCount, out int typeCount);
            Assert.AreEqual(assemblyCount, 1);
            Assert.AreEqual(typeCount, 1);
            AssertTypePreserved(xml, unexpectedType);
        }

        [Test]
        public void LinkXML_Preserves_MultipleTypes_FromMultipleAssemblies()
        {
            var types = new[] { typeof(UnityEngine.MonoBehaviour), typeof(UnityEngine.Build.Pipeline.CompatibilityAssetBundleManifest) };

            var link = new LinkXmlGenerator();
            link.AddTypes(types);
            link.Save(k_LinkFile);

            var xml = ReadLinkXML(k_LinkFile, out int assemblyCount, out int typeCount);
            Assert.AreEqual(assemblyCount, 2);
            Assert.AreEqual(typeCount, types.Length);
            foreach (var t in types)
                AssertTypePreserved(xml, t);
        }

        [Test]
        public void LinkXML_Preserves_Assemblies()
        {
            var assemblies = new[] { typeof(UnityEngine.MonoBehaviour).Assembly, typeof(UnityEngine.Build.Pipeline.CompatibilityAssetBundleManifest).Assembly };

            var link = new LinkXmlGenerator();
            link.AddAssemblies(assemblies);
            link.Save(k_LinkFile);

            var xml = ReadLinkXML(k_LinkFile, out int assemblyCount, out int typeCount);
            Assert.AreEqual(assemblyCount, assemblies.Length);
            Assert.AreEqual(typeCount, 0);
            foreach (var a in assemblies)
                AssertAssemblyPreserved(xml, a);
        }


        [Test]
        public void LinkXML_Preserves_SerializeClasses()
        {
            var serializedRefClasses = new[] { "FantasticAssembly:AwesomeNS.Foo", "FantasticAssembly:AwesomeNS.Bar", "SuperFantasticAssembly:SuperAwesomeNS.Bar"};

            var link = new LinkXmlGenerator();
            link.AddSerializedClass(serializedRefClasses);
            link.Save(k_LinkFile);

            var xml = ReadLinkXML(k_LinkFile, out int assemblyCount, out int typeCount);
            Assert.AreEqual(assemblyCount, 2);
            AssertAssemblyFullName(xml, "FantasticAssembly");
            AssertAssemblyFullName(xml, "SuperFantasticAssembly");
            Assert.AreEqual(typeCount,3);
            AssertTypeWithAttributePreserved(xml, "AwesomeNS.Foo");
            AssertTypeWithAttributePreserved(xml, "AwesomeNS.Bar");
            AssertTypeWithAttributePreserved(xml, "SuperAwesomeNS.Bar");
        }
        
        [Test]
        public void LinkXML_Is_Sorted()
        {
            // For incremental builds to perform well, we need link xml files
            // to be deterministic - if contents don't change, they should be bit-wise
            // identical. So we sort contents, since HashSets/Dictionary have no deterministic order.
            // This test verfies that assemblies and classes are sorted alphabetically.
            // Note that types added via AddSerializedClass will always come after types and assemblies
            // added via AddType/AddAssembly, regardless of alphabetical order.
            var serializedRefClasses = new[]
            {
                "AssemblyC:NamespaceA.ClassA", 
                "AssemblyB:NamespaceC.ClassB",
                "AssemblyA:NamespaceB.ClassC",
                "AssemblyC:NamespaceC.ClassC",
                "AssemblyB:NamespaceB.ClassA",
                "AssemblyA:NamespaceA.ClassB",
                "AssemblyA:NamespaceA.ClassC",
                "AssemblyC:NamespaceB.ClassA",
                "AssemblyB:NamespaceC.ClassA",
            };
            var types = new[] { typeof(UnityEngine.Texture), typeof(UnityEngine.MonoBehaviour) , typeof(UnityEngine.Build.Pipeline.CompatibilityAssetBundleManifest) };

            var link = new LinkXmlGenerator();
            link.AddSerializedClass(serializedRefClasses);
            link.AddTypes(types);
            link.Save(k_LinkFile);

            var xml = ReadLinkXML(k_LinkFile, out int assemblyCount, out int typeCount)
                .Replace(" ", "")
                .Replace("\n", "")
                .Replace("\r", "");
            Console.WriteLine(xml);
            var sorted = @"
<linker>
<assembly fullname=""Unity.ScriptableBuildPipeline,Version=0.0.0.0,Culture=neutral,PublicKeyToken=null"">
<type fullname=""UnityEngine.Build.Pipeline.CompatibilityAssetBundleManifest"" preserve=""all"" />
</assembly>
<assembly fullname=""UnityEngine.CoreModule,Version=0.0.0.0,Culture=neutral,PublicKeyToken=null"">
<type fullname=""UnityEngine.MonoBehaviour"" preserve=""all"" />
<type fullname=""UnityEngine.Texture"" preserve=""all"" />
</assembly>
<assembly fullname=""AssemblyA"">
<type fullname=""NamespaceA.ClassB"" preserve=""nothing"" serialized=""true"" />
<type fullname=""NamespaceA.ClassC"" preserve=""nothing"" serialized=""true"" />
<type fullname=""NamespaceB.ClassC"" preserve=""nothing"" serialized=""true"" />
</assembly>
<assembly fullname=""AssemblyB"">
<type fullname=""NamespaceB.ClassA"" preserve=""nothing"" serialized=""true"" />
<type fullname=""NamespaceC.ClassA"" preserve=""nothing"" serialized=""true"" />
<type fullname=""NamespaceC.ClassB"" preserve=""nothing"" serialized=""true"" />
</assembly>
<assembly fullname=""AssemblyC"">
<type fullname=""NamespaceA.ClassA"" preserve=""nothing"" serialized=""true"" />
<type fullname=""NamespaceB.ClassA"" preserve=""nothing"" serialized=""true"" />
<type fullname=""NamespaceC.ClassC"" preserve=""nothing"" serialized=""true"" />
</assembly>
</linker>
".Replace(" ", "").Replace("\n", "").Replace("\r", "");
            Assert.AreEqual(sorted, xml);

        }        
    }
}
