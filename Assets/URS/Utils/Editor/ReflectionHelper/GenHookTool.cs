using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NinjaBeats.ReflectionHelper;
using Unity.Jobs;

namespace NinjaBeats
{

    public partial class ReflectionTool
    {
        public class GenHookTool : GenToolBase
        {
            public override GenInfoBase[] genInfoList => s_HookInfoList;

            public class GenHookJob : GenJobBase
            {
                public GenHookJob(GenToolBase genTool, MemberCollection collection) : base(genTool, collection)
                {
                    this.fileName = $"Hook_{this.typeFlatName}.cs";
                }

                void DoMethodImpl(Member m, MonoHookType hookType, bool isStatic, TypeName returnTypeName, ParameterTypeName[] parameterTypeNames)
                {
                    var strAccess = isStatic ? "static " : "";
                    line();
                    line($"[MonoHook{(hookType != MonoHookType.Method ? $"(MonoHookType.{hookType})" : "")}]");
                    line($"public class {m.uniqueName}");
                    line("{");
                    indent++;
                    {
                        var parameterDecl = string.Join(", ", parameterTypeNames.Select(x => $"{x.ParameterModifier}{x.typeName.name} {x.parameter.Name}"));

                        comment($"<see cref=\"{typeFullName}\"/>");
                        line($"public static string {k_TYPE} = \"{typeFullName}\";");
                        line($"public delegate {returnTypeName.name} {k_DELEGATE}({parameterDecl});");

                        line();
                        line($"public struct Param");
                        line("{");
                        indent++;
                        {
                            foreach (var p in parameterTypeNames)
                            {
                                line($"public {p.typeName.name} {p.parameter.Name};");
                            }

                            line();
                            if (!isStatic)
                                line($"public object {k_THIS};");
                            if (returnTypeName.mode != TypeNameMode.Void)
                                line($"public {returnTypeName.name} {k_RESULT};");
                            line($"public {k_DELEGATE} {k_ORIGINAL};");
                        }
                        indent--;
                        line("}");

                        line();
                        line($"public delegate bool BeginDelegate(ref Param param);");
                        line($"public delegate void EndDelegate(ref Param param);");
                        line($"public static BeginDelegate OnBegin = null;");
                        line($"public static EndDelegate OnEnd = null;");

                        line();
                        line($"[MethodImpl(MethodImplOptions.NoOptimization)]");
                        line($"public {strAccess}{returnTypeName.name} {k_REPLACE}({parameterDecl})");
                        line("{");
                        indent++;
                        {
                            line($"Param param = new();");
                            foreach (var p in parameterTypeNames)
                                line($"param.{p.parameter.Name} = {p.parameter.Name};");
                            if (!isStatic)
                                line($"param.{k_THIS} = this;");
                            if (returnTypeName.mode != TypeNameMode.Void)
                                line($"param.{k_RESULT} = default;");
                            line($"param.{k_ORIGINAL} = {k_ORIGINAL};");

                            line();
                            line($"if (OnBegin?.Invoke(ref param) == true)");
                            indent++;
                            {
                                if (returnTypeName.mode != TypeNameMode.Void)
                                    line($"return param.{k_RESULT};");
                                else
                                    line($"return;");
                            }
                            indent--;

                            line();
                            line($"{(returnTypeName.mode != TypeNameMode.Void ? $"param.{k_RESULT} = " : "")}{k_ORIGINAL}({string.Join(", ", parameterTypeNames.Select(x => $"{x.ParameterModifier}param.{x.parameter.Name}"))});");
                            line();
                            line($"OnEnd?.Invoke(ref param);");

                            if (returnTypeName.mode != TypeNameMode.Void)
                                line($"return param.{k_RESULT};");
                        }
                        indent--;
                        line("}");

                        line();
                        line($"[MethodImpl(MethodImplOptions.NoOptimization)]");
                        line($"public {strAccess}{returnTypeName.name} {k_ORIGINAL}({parameterDecl})");
                        line("{");
                        indent++;
                        {
                            if (returnTypeName.mode != TypeNameMode.Void)
                                line($"return default;");
                        }
                        indent--;
                        line("}");
                    }
                    indent--;
                    line("}");
                }

                void DoMethod()
                {
                    foreach (var m in collection.methods)
                    {
                        var methodInfo = m.methodInfo;
                        var returnTypeName = GetTypeName(methodInfo.ReturnType);
                        var parameterTypeNames = methodInfo.GetParameters().Select(GetTypeName).ToArray();
                        DoMethodImpl(m, MonoHookType.Method, methodInfo.IsStatic, returnTypeName, parameterTypeNames);
                    }
                }

                void DoConstructor()
                {
                    foreach (var m in collection.constructors)
                    {
                        var constructorInfo = m.constructorInfo;
                        var returnTypeName = GetTypeName(typeof(void));
                        var parameterTypeNames = constructorInfo.GetParameters().Select(GetTypeName).ToArray();
                        DoMethodImpl(m, MonoHookType.Constructor, false, returnTypeName, parameterTypeNames);
                    }
                }

                public override void Execute()
                {
                    line("//This file was automatically generated by kuroneko.");
                    line("using System;");
                    line("using System.Reflection;");
                    line("using System.Runtime.CompilerServices;");
                    line();
                    line($"namespace NinjaBeats.ReflectionHelper");
                    line("{");
                    indent++;
                    {
                        line($"public partial struct {typeFlatName}");
                        line("{");
                        indent++;
                        {
                            line($"public static class {k_HOOK}");
                            line("{");
                            indent++;
                            {
                                DoMethod();
                                DoConstructor();
                            }
                            indent--;
                            line("}");
                        }
                        indent--;
                        line("}");
                    }
                    indent--;
                    line("}");

                    Save();
                }
            }

            public override GenJobBase CreateJob(MemberCollection collection) => new GenHookJob(this, collection);
        }

    }
}