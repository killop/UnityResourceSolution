using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NinjaBeats.ReflectionHelper;
using Unity.Jobs;

namespace NinjaBeats
{

    public partial class ReflectionTool
    {
        private static readonly string SelfGeneratePath = Application.dataPath + "/URS/Utils/Editor/ReflectionHelper/Generated";
        private static readonly string CSharpGeneratePath = Application.dataPath + "/Editor/ReflectionHelper/Generated/";

        private static GenWrapInfo[] s_WrapInfoList => new GenWrapInfo[]
        {
           // new("TMPro.TMP_FontAsset"),
            
            new("UnityEngine.GUISkin"),
            new("UnityEngine.GUIUtility"),
            
            new("UnityEditor.AnimationClipEditor"),
            new("UnityEditor.AnimationClipInfoProperties"),
            new("UnityEditor.AnimationWindowEvent"),
            new("UnityEditor.BaseAssetImporterTabUI"),
            new("UnityEditor.BaseTextureImportPlatformSettings"),
            new("UnityEditor.Build.BuildPlatform"),
            new("UnityEditor.Build.BuildPlatforms"),
            new("UnityEditor.Editor"),
            new("UnityEditor.EditorStyles"),
            new("UnityEditor.EditorGUI"),
            new("UnityEditor.EditorGUI+RecycledTextEditor"),
            new("UnityEditor.EditorGUILayout"),
            new("UnityEditor.EditorGUIUtility"),
            new("UnityEditor.GUISlideGroup"),
            
            new("UnityEditor.EventManipulationHandler"),
            new("UnityEditor.LogEntries"),
            new("UnityEditor.LogEntry"),
            new("UnityEditor.MaterialEditor"),
            new("UnityEditor.ModelImporterClipEditor"),
            new("UnityEditor.ModelImporterClipEditor+ClipInformation"),
            new("UnityEditor.TextureImporterInspector"),
            new("UnityEditor.TextureImportPlatformSettings"),
            new("UnityEditor.TextureImportPlatformSettingsData"),
            new("UnityEngine.TextEditor"),
            new("UnityEngine.TextCore.LowLevel.FontEngine"),
            new("UnityEditor.TextCore.LowLevel.FontEngineEditorUtilities"),
            new("UnityEngine.TimeArea"),
            
            new("UnityEditor.PropertyEditor"),
            new("UnityEditor.EditorWindow"),
            
            // new("NinjaBeats.ReflectionTool+TestClass"),
        };

        private static GenHookInfo[] s_HookInfoList => new GenHookInfo[]
        {
            new("UnityEditor.EditorUtility", false, "DisplayCustomMenu", new []
            {
                typeof(Rect),
                typeof(GUIContent[]),
                typeof(Func<int, bool>),
                typeof(int),
                typeof(EditorUtility.SelectMenuItemFunction),
                typeof(object),
                typeof(bool),
            }),
            
            new("UnityEditor.LogEntries", false, "RowGotDoubleClicked", null),
            new("UnityEditor.ConsoleWindow", false, "StacktraceWithHyperlinks", null),
            new("UnityEditor.AnimationClipInfoProperties", false, "AddEvent", null),
            new("UnityEditor.AnimationClipInfoProperties", false, "SetEvent", null),
            new("UnityEditor.AnimationClipInfoProperties", false, "GetEvent", null),
            new("UnityEditor.AnimationWindowEventInspector", false, "DoEditRegularParameters", null),
            new("UnityEditor.EditorGUIExt", false, "GetIndexUnderMouse", null),
            new("UnityEditor.EventManipulationHandler", false, "CheckRectsOnMouseMove", null),
            new("UnityEditor.EventManipulationHandler", false, "Draw", null),
            new("UnityEditor.ModelImporterClipEditor", false, ".ctor", new []
            {
                typeof(UnityEditor.AssetImporters.AssetImporterEditor),
            }),
            new("UnityEditor.ModelImporterClipEditor", false, "AnimationClipGUI", null),
            
            new("UnityEditor.EditorGUIUtility", false, "ObjectContent", new[]
            {
               typeof(UnityEngine.Object),
               typeof(Type),
               typeof(int),
            }),
            
        };

        class TestClass
        {
            private delegate void FuncA(ref int a, out int b);
            
            private FuncA onFuncA;
            private int a = 0;
            private int b = 1;
            
            public void Test()
            {
                onFuncA?.Invoke(ref a, out b);
            }
        }


        [MenuItem("Tools/类型反射生成工具/Generate Wrap")]
        static void GenerateWrap()
        {
            new GenWrapTool().Execute();

            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/类型反射生成工具/Generate Hook")]
        static void GenerateHook()
        {
            new GenHookTool().Execute();

            AssetDatabase.Refresh();
        }
        
        public abstract class GenInfoBase
        {
            public string typeName;
            public bool isCSharpAssembly;
        }

        public class GenWrapInfo : GenInfoBase
        {
            public GenWrapInfo(string typeName)
            {
                this.typeName = typeName;
                this.isCSharpAssembly = false;
            }
        }

        public class GenHookInfo : GenInfoBase
        {
            public string methodName;
            public Type[] paramTypes;
            public GenHookInfo(string typeName, bool isCSharpAssembly, string methodName, Type[] paramTypes)
            {
                this.typeName = typeName;
                this.isCSharpAssembly = isCSharpAssembly;
                this.methodName = methodName;
                this.paramTypes = paramTypes;
            }
        }
        

        public struct Member
        {
            public MemberInfo memberInfo;
            public string rawName;
            public string uniqueName;
            public FieldInfo fieldInfo => memberInfo as FieldInfo;
            public PropertyInfo propertyInfo => memberInfo as PropertyInfo;
            public MethodInfo methodInfo => memberInfo as MethodInfo;
            public ConstructorInfo constructorInfo => memberInfo as ConstructorInfo;
            public Type delegateInfo => memberInfo as Type;
        }

        public class MemberCollection
        {
            public MemberCollectionGroup ownerGroup;
            public Type type;
            public bool isCSharpAssembly;
            public List<Member> fields = new();
            public List<Member> properties = new();
            public List<Member> methods = new();
            public List<Member> constructors = new();
            public List<Member> delegates = new();
            public List<Member> fieldOrProperties = new();

            private Dictionary<string, int> nameCount = new();
            
            public MemberCollection(MemberCollectionGroup ownerGroup, Type type, bool isCSharpAssembly)
            {
                this.ownerGroup = ownerGroup;
                this.type = type;
                this.isCSharpAssembly = isCSharpAssembly;
            }

            private void AddMemberImpl(MemberInfo member)
            {
                if (member == null)
                    return;

                if (member.Name.StartsWith('<'))
                    return;
                
                Member m = new();
                m.memberInfo = member;
                m.rawName = member.Name.Replace('.', '_');

                if (!nameCount.TryGetValue(m.rawName, out var count))
                    count = 0;
                count += 1;
                nameCount[m.rawName] = count;

                if (count > 1)
                    m.uniqueName = $"{m.rawName}__{count}";
                else
                    m.uniqueName = m.rawName;

                switch (member)
                {
                    case FieldInfo fi:
                    {
                        if (fi.IsSpecialName)
                            return;
                        fields.Add(m);
                        fieldOrProperties.Add(m);
                        break;
                    }
                    case PropertyInfo:
                        properties.Add(m);
                        fieldOrProperties.Add(m);
                        break;
                    case MethodInfo mi:
                    {
                        if (mi.IsSpecialName)
                            return;
                        methods.Add(m);
                        break;   
                    }
                    case ConstructorInfo:
                        constructors.Add(m);
                        break;
                    case Type ei:
                    {
                        if (ei.Is<Delegate>())
                        {
                            var invoke = ei.GetMethod("Invoke");
                            if (invoke != null && invoke.ReturnType.IsRealPublic() && 
                                !invoke.ReturnType.ContainsGenericParameters &&
                                !invoke.GetGenericArguments().Any(x => x.ContainsGenericParameters || !x.IsRealPublic()))
                            {
                                delegates.Add(m);
                                ownerGroup.declaredDelegateTypes.Add(ei);
                            }   
                        }
                        break;
                    }
                }
            }

            public void AddMember(GenInfoBase info)
            {
                switch (info)
                {
                    case GenWrapInfo wi:
                    {
                        foreach (var m in type.GetMembers((BindingFlags)(-1)))
                        {
                            if (m.IsRealPublic())
                                continue;
                            AddMemberImpl(m);
                        }
                        break;
                    }
                    case GenHookInfo hi:
                    {
                        if (hi.paramTypes != null)
                        {
                            if (hi.methodName == ".ctor")
                            {
                                var method = type.GetConstructorInfoByParameterTypes(hi.paramTypes);
                                if (method != null)
                                    AddMemberImpl(method);
                            }
                            else
                            {
                                var method = type.GetMethodInfoByParameterTypes(hi.methodName, hi.paramTypes);
                                if (method != null)
                                    AddMemberImpl(method);   
                            }
                        }
                        else
                        {
                            foreach (var method in type.GetMember(hi.methodName, (BindingFlags)(-1)).OfType<MethodInfo>())
                                AddMemberImpl(method);
                        }
                        break;
                    }
                }
            }

        }

        public class MemberCollectionGroup
        {
            public List<MemberCollection> list = new();
            public HashSet<Type> declaredTypes = new();
            public HashSet<Type> declaredDelegateTypes = new();

            private MemberCollection GetMemberCollection(string typeName, bool CSharpAssembly)
            {
                if (string.IsNullOrWhiteSpace(typeName))
                    return null;
                var result = list.FirstOrDefault(x => x.type.FullName == typeName && x.isCSharpAssembly == CSharpAssembly);
                if (result != null)
                    return result;

                var type = EditorUtils.GetTypeByFullName(typeName);
                if (type == null)
                    return null;
                if (type.IsGenericType)
                    return null;

                result = new(this, type, CSharpAssembly);
                list.Add(result);
                declaredTypes.Add(type);
                return result;
            }

            public void AddMember(IEnumerable<GenInfoBase> genInfoList)
            {
                foreach (var info in genInfoList)
                {
                    var collection = GetMemberCollection(info.typeName, info.isCSharpAssembly);
                    if (collection == null)
                        continue;
                    collection.AddMember(info);
                }
            }
        }

        public abstract class GenToolBase
        {
            public const string k_SELF = "__self__";
            public const string k_TYPE = "__type__";
            public const string k_SUPER = "__super__";
            public const string k_VALID = "__valid__";
            public const string k_HOOK = "__hook__";
            public const string k_DELEGATE = "__delegate__";
            public const string k_REPLACE = "__replace__";
            public const string k_ORIGINAL = "__original__";
            public const string k_THIS = "__this__";
            public const string k_RESULT = "__result__";
            public const string k_PREFIX = "__";
            public const string k_ELEMENT_PREFIX = "__E";

            public enum TypeNameMode
            {
                Public,
                Wrap,
                Array,
                List,
                Dictionary,
                Object,
                Void,
                Generic,
                Delegate,
                WrapDelegate,
            }

            public enum RefMode
            {
                None,
                In,
                Out,
                Ref,
            }

            public struct TypeName
            {
                public string name;
                public TypeNameMode mode;

                public string Cast(string value) => mode switch
                {
                    TypeNameMode.Wrap => $"new {name}({value})",
                    TypeNameMode.WrapDelegate => $"({value} as Delegate)?.Cast<{name}>()",
                    TypeNameMode.Void => $"{value}",
                    _ => $"({name})({value})"
                };

                public string CastTo(string value, string type) => mode switch
                {
                    TypeNameMode.Wrap => $"{value}.{k_SELF}",
                    TypeNameMode.WrapDelegate => $"{value}?.Cast({type})",
                    TypeNameMode.Void => "",
                    _ => value
                };
            }

            
            public struct ParameterTypeName
            {
                public TypeName typeName;
                public ParameterInfo parameter;
                public RefMode refMode;

                public string ParameterModifier => parameter.GetParameterModifierStr();

                public string Cast(string value) => typeName.Cast(value);

                public string GetValue(string value) => typeName.CastTo(value, "null");
            }

            public abstract class GenJobBase
            {
                private GenToolBase _genTool;
                public MemberCollection collection;

                public Type type;
                public string typeFullName;
                public string typeFlatName;
                public bool typeIsStatic;
                public bool typeIsUnityObject;
                public string objectTypeName;

                public int indent;
                public StringBuilder sb;
                public string fileName;

                public GenJobBase(GenToolBase genTool, MemberCollection collection)
                {
                    this._genTool = genTool;
                    this.collection = collection;

                    this.type = collection.type;
                    this.typeFullName = type.FullName;
                    this.typeFlatName = type.GetTypeDisplayName(true);
                    this.typeIsStatic = type.IsRealStatic();
                    this.typeIsUnityObject = type.Is<UnityEngine.Object>();
                    this.objectTypeName = this.typeIsUnityObject ? "UnityEngine.Object" : "object";

                    this.indent = 0;
                    this.sb = new();
                }
                
                public bool HasWrapper(Type type) => _genTool.group.declaredTypes.Contains(type);
                public bool HasWrapperDelegate(Type type) => _genTool.group.declaredDelegateTypes.Contains(type);

                public ParameterTypeName GetTypeName(ParameterInfo p)
                {
                    ParameterTypeName result = new();
                    result.typeName = GetTypeName(p.ParameterType);
                    result.parameter = p;
                    if (p.IsOut)

                        result.refMode = RefMode.Out;
                    else if (p.IsIn)
                        result.refMode = RefMode.In;
                    else if (p.ParameterType.IsByRef)
                        result.refMode = RefMode.Ref;
                    else
                        result.refMode = RefMode.None;
                    return result;
                }
                public TypeName GetTypeName(Type t)
                {
                    TypeName result = new();
                    if (t.IsByRef)
                        t = t.GetElementType();

                    if (t == EditorUtils.typeof_Void)
                    {
                        result.name = "void";
                        result.mode = TypeNameMode.Void;
                    }
                    else if (t.IsGenericParameter)
                    {
                        result.name = t.Name;
                        result.mode = TypeNameMode.Generic;
                    }
                    else if (t.IsRealPublic())
                    {
                        result.name = t.GetTypeDisplayName(false);
                        result.mode = TypeNameMode.Public;
                    }
                    else if (HasWrapper(t))
                    {
                        result.name = t.GetTypeDisplayName(true);
                        result.mode = TypeNameMode.Wrap;
                    }
                    else if (HasWrapperDelegate(t))
                    {
                        result.name = t.DeclaringType != type ? $"{t.DeclaringType.GetTypeDisplayName(true)}.{t.Name}" : t.Name;
                        result.mode = TypeNameMode.WrapDelegate;
                    }
                    else if (t.IsArray)
                    {
                        result.name = "Array";
                        result.mode = TypeNameMode.Array;
                    }
                    else if (t.Is(typeof(IList)))
                    {
                        result.name = "System.Collections.IList";
                        result.mode = TypeNameMode.List;
                    }
                    else if (t.Is(typeof(IDictionary)))
                    {
                        result.name = "System.Collections.IDictionary";
                        result.mode = TypeNameMode.Dictionary;
                    }
                    else if (t.Is<Delegate>())
                    {
                        result.name = "Delegate";
                        result.mode = TypeNameMode.Delegate;
                    }
                    else
                    {
                        result.name = "object";
                        result.mode = TypeNameMode.Object;
                    }

                    return result;
                }

                private Type TestSuperType(Type t)
                {
                    if (t == null)
                        return null;
                    if (t == EditorUtils.typeof_Object || t == EditorUtils.typeof_UnityObject)
                        return null;
                    if (t.IsRealPublic())
                        return t;
                    if (HasWrapper(t))
                        return t;
                    return t.BaseType;
                }

                public Type GetSuperType(Type t) => t.IsRealPublic() ? t : TestSuperType(t.BaseType);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void line(string text = "") => this.sb?.AppendIndentLine(text, indent);
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void line_begin(string text) => this.sb?.AppendIndent(text, indent);
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void line_end(string text = "") => this.sb?.AppendLine(text);
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void append(string text) => this.sb?.Append(text);
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void comment(string text) => this.sb?.AppendIndentComment(text, indent);
                
                public void Save()
                {
                    var path = Path.Combine(collection.isCSharpAssembly ? CSharpGeneratePath : SelfGeneratePath, fileName);
                    EditorUtils.WriteToFile(path, sb.ToString());
                }

                public abstract void Execute();
            }

            public abstract GenInfoBase[] genInfoList { get; }
            public MemberCollectionGroup group = new();


            public abstract GenJobBase CreateJob(MemberCollection collection);
            public void Execute()
            {
                group.AddMember(genInfoList);

                List<GenJobBase> resultList = new();
                foreach (var collection in group.list)
                    resultList.Add(CreateJob(collection));

                var handleList = resultList.Select(x => Task.Run(x.Execute));
                foreach (var handle in handleList)
                {
                    handle.Wait();
                }
            }
        }

    }
}