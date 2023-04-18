

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NinjaBeats
{
    
    public class AssemblyCache
    {
        private Assembly m_Assembly;
        private Type[] m_Types;
        private Type[] m_ExportedTypes;
        private Module m_ManifestModule;

        public Assembly Assembly => m_Assembly;

        private string m_FullName;
        public string FullName
        {
            get
            {
                if (m_FullName == null)
                    m_FullName = m_Assembly?.FullName;
                return m_FullName;
            }
        }

        public Type[] GetTypes()
        {
            if (m_Types == null)
                m_Types = m_Assembly?.GetTypes();
            return m_Types;
        }

        public Type[] GetExportedTypes()
        {
            if (m_ExportedTypes == null)
                m_ExportedTypes = m_Assembly?.GetExportedTypes();
            return m_ExportedTypes;
        }

        public Module ManifestModule
        {
            get
            {
                if (m_ManifestModule == null)
                    m_ManifestModule = m_Assembly?.ManifestModule;
                return m_ManifestModule;
            }
        }
        
        public AssemblyCache(Assembly assembly)
        {
            m_Assembly = assembly;
        }
    }
    
    
    public class TypeAttrPairs<T> where T : Attribute
    {
        public Type type;
        public T attr;

        public TypeAttrPairs()
        {
            type = null;
            attr = null;
        }

        public TypeAttrPairs(Type type, T attr)
        {
            this.type = type;
            this.attr = attr;
        }
    }

    public class MethodAttrPair<T> where T : Attribute
    {
        public MethodInfo method;
        public T attr;

        public MethodAttrPair()
        {
            method = null;
            attr = null;
        }

        public MethodAttrPair(MethodInfo method, T attr)
        {
            this.method = method;
            this.attr = attr;
        }
    }

    
    public partial class EditorUtils
    {

        private static AssemblyCache[] s_Assemblies = null;
        public static AssemblyCache[] Assemblies
        {
            get
            {
                if (s_Assemblies == null)
                {
#if !UNITY_EDITOR
                    Debug.LogWarning("Init AssemblyCache in runtime");
#endif
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    s_Assemblies = new AssemblyCache[assemblies.Length];
                    for (int i = 0; i < assemblies.Length; ++i)
                        s_Assemblies[i] = new(assemblies[i]);
                }

                return s_Assemblies;
            }
        }

        private static Dictionary<string, AssemblyCache> s_AssemblyByFullName = null;

        public static Dictionary<string, AssemblyCache> AssemblyByFullName
        {
            get
            {
                if (s_AssemblyByFullName == null)
                {
                    s_AssemblyByFullName = new();
                    foreach (var a in Assemblies)
                        s_AssemblyByFullName[a.FullName] = a;
                }

                return s_AssemblyByFullName;
            }
        }

        private static Dictionary<string, Type> s_TypeByFullName = null;

        public static Dictionary<string, Type> TypeByFullName
        {
            get
            {
                if (s_TypeByFullName == null)
                {
                    s_TypeByFullName = new(65536);
                    foreach (var a in Assemblies)
                    {
                        foreach (var t in a.GetTypes())
                        {
                            var fullname = t.FullName;
                            if (!string.IsNullOrWhiteSpace(fullname))
                                s_TypeByFullName[fullname] = t;
                        }
                    }
                }
                return s_TypeByFullName;
            }
        }

        private static Dictionary<string, List<Type>> s_TypesByNamespace = null;

        public static Dictionary<string, List<Type>> TypesByNamespace
        {
            get
            {
                if (s_TypesByNamespace == null)
                {
                    s_TypesByNamespace = new(2048);
                    string cacheNamespace = null;
                    List<Type> cacheList = null;
                    foreach (var a in Assemblies)
                    {
                        foreach (var t in a.GetTypes())
                        {
                            var Namespace = t.Namespace;
                            if (string.IsNullOrWhiteSpace(Namespace))
                                continue;

                            if (cacheNamespace != Namespace)
                            {
                                cacheNamespace = Namespace;
                                if (!s_TypesByNamespace.TryGetValue(Namespace, out cacheList))
                                {
                                    cacheList = new();
                                    s_TypesByNamespace.Add(Namespace, cacheList);
                                }
                            }

                            cacheList.Add(t);
                        }
                    }
                }

                return s_TypesByNamespace;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssemblyCache[] GetAssemblies() => Assemblies;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssemblyCache GetAssemblyByFullName(string fullName)
        {
            if (AssemblyByFullName.TryGetValue(fullName, out var value))
                return value;
            return null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetTypeByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;
            if (TypeByFullName.TryGetValue(fullName, out var value))
                return value;
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<Type> GetTypesByNamespace(string Namespace)
        {
            if (TypesByNamespace.TryGetValue(Namespace, out var value))
                return value;
            return null;
        }
        
        public static readonly Type typeof_UnityObject = typeof(UnityEngine.Object);
        
        public static readonly Type typeof_Object = typeof(object);
        public static readonly Type typeof_String = typeof(string);
        public static readonly Type typeof_Byte = typeof(byte);
        public static readonly Type typeof_Char = typeof(char);
        public static readonly Type typeof_Int16 = typeof(short);
        public static readonly Type typeof_Int32 = typeof(int);
        public static readonly Type typeof_Int64 = typeof(long);
        public static readonly Type typeof_UInt16 = typeof(ushort);
        public static readonly Type typeof_UInt32 = typeof(uint);
        public static readonly Type typeof_UInt64 = typeof(ulong);
        public static readonly Type typeof_Single = typeof(float);
        public static readonly Type typeof_Double = typeof(double);
        public static readonly Type typeof_Boolean = typeof(bool);
        public static readonly Type typeof_Void = typeof(void);

        private static readonly Dictionary<Type, string> s_InternalTypeNameMap = new()
        {
            { typeof_Object, "object" },
            { typeof_String, "string" },
            { typeof_Byte, "byte" },
            { typeof_Char, "char" },
            { typeof_Int16, "short" },
            { typeof_Int32, "int" },
            { typeof_Int64, "long" },
            { typeof_UInt16, "ushort" },
            { typeof_UInt32, "uint" },
            { typeof_UInt64, "ulong" },
            { typeof_Single, "float" },
            { typeof_Double, "double" },
            { typeof_Boolean, "bool" },
            { typeof_Void, "void" },
        };

        private static string GetTypeFullName(this Type type)
        {
            string fullName = type.Name;
            var it = type;
            while (it.IsNested)
            {
                it = it.DeclaringType;
                fullName = $"{it.Name}.{fullName}";
            }
            if (!string.IsNullOrEmpty(type.Namespace))
                fullName = $"{type.Namespace}.{fullName}";
            return fullName;
        }
        
        public static string GetTypeDisplayName(this Type type, bool flat = false)
        {
            if (type == null)
                return "";

            if (s_InternalTypeNameMap.TryGetValue(type, out var internalName))
                return internalName;

            string fullName = "";
            if (type.IsArray)
            {
                var elementTypeName = type.GetElementType().GetTypeDisplayName(flat);
                var rank = type.GetArrayRank();
                if (flat)
                    fullName = $"Array{(rank > 1 ? $"_{rank}" : "")}_{elementTypeName}";
                else
                    fullName = $"{elementTypeName}[{string.Join(',', Enumerable.Repeat("", rank - 1))}]";
            }
            else if (type.IsByRef)
            {
                fullName = type.GetElementType().GetTypeDisplayName(flat);
            }
            else if (type.IsGenericParameter)
            {
                fullName = type.Name;
            }
            else
            {
                fullName = type.GetTypeFullName();
                
                if (type.IsGenericType)
                {
                    var idx = fullName.IndexOf('`');
                    if (idx != -1)
                        fullName = fullName.Substring(0, idx);
                    if (type.IsNested)
                        fullName = fullName.Replace('+', '.');
                
                    if (!flat)
                    {
                        fullName += "<";
                        fullName += string.Join(", ", type.GetGenericArguments().Select(x=>x.GetTypeDisplayName(flat)));
                        fullName += ">";    
                    }
                    else
                    {
                        fullName += "_";
                        fullName += string.Join("_", type.GetGenericArguments().Select(x=>x.GetTypeDisplayName(flat)));
                    }
                }
            }

            if (flat)
                fullName = fullName.Replace('.', '_');
            return fullName;
        }
        
        public static string GetParameterModifierStr(this ParameterInfo parameter, bool space = true)
        {
            if (parameter.IsOut)
                return space ? "out " : "out";
            if (parameter.IsIn)
                return space ? "in " : "in";
            if (parameter.ParameterType.IsByRef)
                return space ? "ref " : "ref";
            return "";
        }
        
        public static List<MethodInfo> ScanAllMethodsInNamespace(string Namespace, BindingFlags? bindingFlags)
        {
            List<MethodInfo> returnList = new List<MethodInfo>();
            foreach (var pair in TypeByFullName)
            {
                var t = pair.Value;
                if (string.IsNullOrEmpty(Namespace) || Namespace == t.Namespace)
                    returnList.AddRange(bindingFlags != null
                        ? t.GetMethods(bindingFlags.Value)
                        : t.GetMethods());
            }
            return returnList;
        }


        public static List<TypeAttrPairs<T>> ScanAllTypeWithAttributeMark<T>() where T : Attribute
        {
            List<TypeAttrPairs<T>> returnList = new List<TypeAttrPairs<T>>();
            foreach (var type in UnityEditor.TypeCache.GetTypesWithAttribute<T>())
            {
                T attribute = type.GetCustomAttribute<T>(true);
                if (attribute != null)
                {
                    returnList.Add(new TypeAttrPairs<T>(type, attribute));
                }
            }

            return returnList;
        }

        public static List<MethodAttrPair<T>> ScanAllMethodsWithAttributeMark<T>(string Namespace,
            BindingFlags? bindFlag) where T : Attribute
        {
            List<MethodAttrPair<T>> returnList = new List<MethodAttrPair<T>>();
            
            List<MethodInfo> methodList = ScanAllMethodsInNamespace(Namespace, bindFlag);
            for (int i = 0; i < methodList.Count; i++)
            {
                MethodInfo method = methodList[i];
                T attribute = method.GetCustomAttribute<T>(true);
                if (attribute != null)
                {
                    returnList.Add(new MethodAttrPair<T>(method, attribute));
                }
            }

            return returnList;
        }
        
    }
}