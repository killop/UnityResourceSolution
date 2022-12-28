using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// This can be used to create a LinkXml for your build.  This will ensure that the desired runtime types are packed into the build.
    /// </summary>
    public class LinkXmlGenerator
    {
        Dictionary<Type, Type> m_TypeConversion = new Dictionary<Type, Type>();
        HashSet<Type> m_Types = new HashSet<Type>();
        HashSet<Assembly> m_Assemblies = new HashSet<Assembly>();

        /// <summary>
        /// Obsolete, no longer does anything.
        /// </summary>
        [Obsolete] protected Dictionary<string, HashSet<string>> serializedClassesPerAssembly = null;
        Dictionary<string, HashSet<string>> m_SerializedClassesPerAssembly = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Constructs and returns a LinkXmlGenerator object that contains default UnityEditor to UnityEngine type conversions.
        /// </summary>
        /// <returns>LinkXmlGenerator object with the default UnityEngine type conversions.</returns>
        public static LinkXmlGenerator CreateDefault()
        {
            var linker = new LinkXmlGenerator();
            var types = GetEditorTypeConversions();
            foreach (var pair in types)
                linker.SetTypeConversion(pair.Key, pair.Value);
            return linker;
        }

        /// <summary>
        /// Returns the set of UnityEditor types that have valid runtime direct mappings.
        /// </summary>
        /// <returns>Array of KeyValuePairs containing the editor type and it's equivalent runtime type.</returns>
        public static KeyValuePair<Type, Type>[] GetEditorTypeConversions()
        {
            var editor = Assembly.GetAssembly(typeof(UnityEditor.BuildPipeline));
            return new[]
            {
                new KeyValuePair<Type, Type>(typeof(UnityEditor.Animations.AnimatorController), typeof(UnityEngine.RuntimeAnimatorController)),
                new KeyValuePair<Type, Type>(editor.GetType("UnityEditor.Audio.AudioMixerController"), typeof(UnityEngine.Audio.AudioMixer)),
                new KeyValuePair<Type, Type>(editor.GetType("UnityEditor.Audio.AudioMixerGroupController"), typeof(UnityEngine.Audio.AudioMixerGroup)),
                new KeyValuePair<Type, Type>(typeof(UnityEditor.MonoScript), typeof(UnityEngine.Object)),
            };
        }

        /// <summary>
        /// Add runtime assembly to the LinkXml Generator.
        /// </summary>
        /// <param name="assemblies">The desired runtime assemblies.</param>
        public void AddAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null)
                return;
            foreach (var a in assemblies)
                AddAssemblyInternal(a);
        }

        /// <summary>
        /// Add runtime assembly to the LinkXml Generator.
        /// </summary>
        /// <param name="assemblies">The desired runtime assemblies.</param>
        public void AddAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
                return;
            foreach (var a in assemblies)
                AddAssemblyInternal(a);
        }

        /// <summary>
        /// Add runtime type to the LinkXml Generator.
        /// </summary>
        /// <param name="types">The desired runtime types.</param>
        public void AddTypes(params Type[] types)
        {
            if (types == null)
                return;
            foreach (var t in types)
                AddTypeInternal(t);
        }

        /// <summary>
        /// Add runtime type to the LinkXml Generator.
        /// </summary>
        /// <param name="types">The desired runtime types.</param>
        public void AddTypes(IEnumerable<Type> types)
        {
            if (types == null)
                return;
            foreach (var t in types)
                AddTypeInternal(t);
        }

        /// <summary>
        /// Add SerializedReference class type from fully qualified name to the Generator, those will end up in PreservedTypes.xml
        /// </summary>
        /// <param name="serializedRefTypes">The SerializeReference instance fully qualified name we want to preserve.</param>
        public void AddSerializedClass(IEnumerable<string> serializedRefTypes)
        {
            if (serializedRefTypes == null)
                return;
            foreach (var t in serializedRefTypes)
            {
                var indexOfAssembly = t.IndexOf(':');
                if (indexOfAssembly != -1)
                    AddSerializedClassInternal(t.Substring(0, indexOfAssembly), t.Substring(indexOfAssembly + 1, t.Length - (indexOfAssembly + 1)));
            }
        }

        private void AddTypeInternal(Type t)
        {
            if (t == null)
                return;

            Type convertedType;
            if (m_TypeConversion.TryGetValue(t, out convertedType))
                m_Types.Add(convertedType);
            else
                m_Types.Add(t);
        }

        private void AddSerializedClassInternal(string assemblyName, string classWithNameSpace)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return;

            if (string.IsNullOrEmpty(classWithNameSpace))
                return;

            if (!m_SerializedClassesPerAssembly.TryGetValue(assemblyName, out HashSet<string> types))
                m_SerializedClassesPerAssembly[assemblyName] = types = new HashSet<string>();

            types.Add(classWithNameSpace);
        }

        private void AddAssemblyInternal(Assembly a)
        {
            if (a == null)
                return;

            m_Assemblies.Add(a);
        }

        /// <summary>
        /// Setup runtime type conversion
        /// </summary>
        /// <param name="a">Convert from type.</param>
        /// <param name="b">Convert to type.</param>
        public void SetTypeConversion(Type a, Type b)
        {
            m_TypeConversion[a] = b;
        }

        /// <summary>
        /// Save the LinkXml to the specified path.
        /// </summary>
        /// <param name="path">The path to save the linker xml file.</param>
        public void Save(string path)
        {
            var assemblyMap = new Dictionary<Assembly, List<Type>>();
            foreach (var a in m_Assemblies)
            {
                if (!assemblyMap.TryGetValue(a, out _))
                    assemblyMap.Add(a, new List<Type>());
            }
            foreach (var t in m_Types)
            {
                var a = t.Assembly;
                List<Type> types;
                if (!assemblyMap.TryGetValue(a, out types))
                    assemblyMap.Add(a, types = new List<Type>());
                types.Add(t);
            }
            XmlDocument doc = new XmlDocument();
            var linker = doc.AppendChild(doc.CreateElement("linker"));
            foreach (var k in assemblyMap.OrderBy(a => a.Key.FullName))
            {
                var assembly = linker.AppendChild(doc.CreateElement("assembly"));
                var attr = doc.CreateAttribute("fullname");
                attr.Value = k.Key.FullName;
                if (assembly.Attributes != null)
                {
                    assembly.Attributes.Append(attr);

                    if (m_Assemblies.Contains(k.Key))
                    {
                        var preserveAssembly = doc.CreateAttribute("preserve");
                        preserveAssembly.Value = "all";
                        assembly.Attributes.Append(preserveAssembly);
                    }

                    foreach (var t in k.Value.OrderBy(t => t.FullName))
                    {
                        var typeEl = assembly.AppendChild(doc.CreateElement("type"));
                        var tattr = doc.CreateAttribute("fullname");
                        tattr.Value = t.FullName;
                        if (typeEl.Attributes != null)
                        {
                            typeEl.Attributes.Append(tattr);
                            var pattr = doc.CreateAttribute("preserve");
                            pattr.Value = "all";
                            typeEl.Attributes.Append(pattr);
                        }
                    }

                    //Add serialize reference classes which are contained in the current assembly
                    var assemblyName = k.Key.GetName().Name;
                    if (m_SerializedClassesPerAssembly.ContainsKey(assemblyName))
                    {
                        //Add content for this
                        foreach (var t in m_SerializedClassesPerAssembly[assemblyName])
                        {
                            var typeEl = assembly.AppendChild(doc.CreateElement("type"));
                            var tattr = doc.CreateAttribute("fullname");
                            tattr.Value = t;
                            if (typeEl.Attributes != null)
                            {
                                typeEl.Attributes.Append(tattr);
                                var pattr = doc.CreateAttribute("preserve");
                                pattr.Value = "nothing";
                                typeEl.Attributes.Append(pattr);
                                var sattr = doc.CreateAttribute("serialized");
                                sattr.Value = "true";
                                typeEl.Attributes.Append(sattr);
                            }
                        }
                        m_SerializedClassesPerAssembly.Remove(assemblyName);
                    }
                }
            }

            //Add serialize reference classes which are contained in other assemblies not yet removed.
            foreach (var k in m_SerializedClassesPerAssembly.OrderBy(a => a.Key))
            {
                var assembly = linker.AppendChild(doc.CreateElement("assembly"));
                var attr = doc.CreateAttribute("fullname");
                attr.Value = k.Key;
                if (assembly.Attributes != null)
                {
                    assembly.Attributes.Append(attr);
                    //Add content for this
                    foreach (var t in k.Value.OrderBy(t => t))
                    {
                        var typeEl = assembly.AppendChild(doc.CreateElement("type"));
                        var tattr = doc.CreateAttribute("fullname");
                        tattr.Value = t;
                        if (typeEl.Attributes != null)
                        {
                            typeEl.Attributes.Append(tattr);
                            var pattr = doc.CreateAttribute("preserve");
                            pattr.Value = "nothing";
                            typeEl.Attributes.Append(pattr);
                            var sattr = doc.CreateAttribute("serialized");
                            sattr.Value = "true";
                            typeEl.Attributes.Append(sattr);
                        }
                    }
                }
            }
            doc.Save(path);
        }
    }
}
