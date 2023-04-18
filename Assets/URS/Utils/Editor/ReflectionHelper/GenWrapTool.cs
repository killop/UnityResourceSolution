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
using Unity.Jobs;

namespace NinjaBeats
{

    public partial class ReflectionTool
    {

        public class GenWrapTool : GenToolBase
        {
            public override GenInfoBase[] genInfoList => s_WrapInfoList;

            public class GenWrapJob : GenJobBase
            {
                public GenWrapJob(GenToolBase genTool, MemberCollection collection) : base(genTool, collection)
                {
                    this.fileName = $"Wrap_{this.typeFlatName}.cs";
                }

                void DoType()
                {
                    line();
                    comment($"<see cref=\"{typeFullName}\"/>");
                    line($"public static Type {k_TYPE} {{ get; }} = EditorUtils.GetTypeByFullName(\"{typeFullName}\");");
                }

                void DoFieldElementTypeReflection(Member m)
                {
                    var memberType = m.memberInfo.GetRealMemberType();
                    var memberTypeName = GetTypeName(memberType);

                    switch (memberTypeName.mode)
                    {
                        case TypeNameMode.Array:
                        case TypeNameMode.List:
                        {
                            if (memberType.GetArrayOrListElementType(out var elementType))
                            {
                                line();
                                line($"private static Type _{k_ELEMENT_PREFIX}{k_PREFIX}{m.uniqueName};");
                                line($"public static Type {k_ELEMENT_PREFIX}{k_PREFIX}{m.uniqueName}");
                                line("{");
                                indent++;
                                {
                                    line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                                    line($"get => _{k_ELEMENT_PREFIX}{k_PREFIX}{m.uniqueName} ??= EditorUtils.GetTypeByFullName(\"{elementType.FullName}\");");
                                }
                                indent--;
                                line("}");
                            }
                            break;
                        }
                    }
                }

                void DoFieldReflection()
                {
                    foreach (var m in collection.fields)
                    {
                        line();
                        line($"private static FieldInfo _{k_PREFIX}{m.uniqueName};");
                        line($"private static FieldInfo {k_PREFIX}{m.uniqueName}");
                        line("{");
                        indent++;
                        line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        line($"get => _{k_PREFIX}{m.uniqueName} ??= {k_TYPE}?.GetField(\"{m.rawName}\", (BindingFlags)(-1));");
                        indent--;
                        line("}");

                        DoFieldElementTypeReflection(m);
                    }

                    foreach (var m in collection.properties)
                    {
                        line();
                        line($"private static PropertyInfo _{k_PREFIX}{m.uniqueName};");
                        line($"private static PropertyInfo {k_PREFIX}{m.uniqueName}");
                        line("{");
                        indent++;
                        line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        line($"get => _{k_PREFIX}{m.uniqueName} ??= {k_TYPE}?.GetProperty(\"{m.rawName}\", (BindingFlags)(-1));");
                        indent--;
                        line("}");
                        
                        DoFieldElementTypeReflection(m);
                    }
                }

                void DoMethodReflection()
                {
                    foreach (var m in collection.methods)
                    {
                        var parameters = m.methodInfo.GetParameters();
                        line();
                        line($"private static MethodInfo _{k_PREFIX}{m.uniqueName};");
                        line($"private static MethodInfo {k_PREFIX}{m.uniqueName}");
                        line("{");
                        indent++;
                        line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        if (parameters.Length == 0)
                            line($"get => _{k_PREFIX}{m.uniqueName} ??= {k_TYPE}?.GetMethodInfoByParameterTypeNames(\"{m.rawName}\");");
                        else
                            line($"get => _{k_PREFIX}{m.uniqueName} ??= {k_TYPE}?.GetMethodInfoByParameterTypeNames(\"{m.rawName}\", {string.Join(", ", parameters.Select(p => $"\"{p.ParameterType.FullName}\""))});");
                        indent--;
                        line("}");
                    }
                }

                void DoCtor()
                {
                    if (typeIsStatic)
                        return;

                    line();
                    line($"public {typeFlatName}(object {k_SELF}) => this.{k_SELF} = {k_SELF} as {objectTypeName};");
                    line($"public {objectTypeName} {k_SELF};");
                    line($"public bool {k_VALID}");
                    line("{");
                    indent++;
                    {
                        line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        line($"get => {k_SELF} != null && {k_TYPE} != null;");
                    }
                    indent--;
                    line("}");

                    var superType = GetSuperType(type);
                    if (superType == null)
                        return;

                    var superTypeName = GetTypeName(superType);
                    line($"public {superTypeName.name} {k_SUPER}");
                    line("{");
                    indent++;
                    {
                        line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        line($"get => {superTypeName.Cast(k_SELF)};");
                    }
                    indent--;
                    line("}");
                }

                void DoField()
                {
                    foreach (var m in collection.fieldOrProperties)
                    {
                        var memberInfo = m.memberInfo;
                        var isStatic = memberInfo.IsRealStatic();
                        var canRead = memberInfo.IsRealCanRead();
                        var canWrite = memberInfo.IsRealCanWrite();
                        var memberType = memberInfo.GetRealMemberType();
                        var memberTypeName = GetTypeName(memberType);

                        var strAccess = isStatic ? " static " : " ";
                        var strType = $"{k_PREFIX}{m.uniqueName}.{(m.fieldInfo != null ? "FieldType" : "PropertyType")}";
                        var strObj = isStatic ? "null" : k_SELF;
                        var strMember = m.rawName;
                        line();
                        line($"public{strAccess}{memberTypeName.name} {strMember}");
                        line("{");
                        indent++;
                        {
                            if (canRead)
                            {
                                line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                                line($"get => {memberTypeName.Cast($"{k_PREFIX}{m.uniqueName}?.GetValue({strObj})")};");
                            }

                            if (canWrite)
                            {
                                line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                                line($"set => {k_PREFIX}{m.uniqueName}?.SetValue({strObj}, {memberTypeName.CastTo("value", strType)});");
                            }
                        }
                        indent--;
                        line("}");

                        if (canRead)
                        {
                            switch (memberTypeName.mode)
                            {
                                case TypeNameMode.Array:
                                {
                                    var elementType = memberType.GetElementType();
                                    var elementTypeName = GetTypeName(elementType);
                                    line();
                                    line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                                    line($"public{strAccess}{elementTypeName.name} {strMember}{k_PREFIX}GetItem(int i) => {elementTypeName.Cast($"{strMember}?.GetValue(i)")};");
                                    line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                                    line($"public{strAccess}void {strMember}{k_PREFIX}SetItem(int i, {elementTypeName.name} value) => {strMember}?.SetValue({elementTypeName.CastTo("value", $"{k_ELEMENT_PREFIX}{k_PREFIX}{m.uniqueName}")}, i);");
                                    break;
                                }
                                case TypeNameMode.List:
                                {
                                    if (memberType.GetArrayOrListElementType(out var elementType))
                                    {
                                        var elementTypeName = GetTypeName(elementType);
                                        line();
                                        line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                                        line($"public{strAccess}{elementTypeName.name} {strMember}{k_PREFIX}GetItem(int i) => {elementTypeName.Cast($"{strMember}?[i]")};");
                                        line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                                        line($"public{strAccess}void {strMember}{k_PREFIX}SetItem(int i, {elementTypeName.name} value)");
                                        line("{");
                                        indent++;
                                        {
                                            line($"var __list__ = {strMember};");
                                            line($"if (__list__ == null) return;");
                                            line($"__list__[i] = {elementTypeName.CastTo("value", $"{k_ELEMENT_PREFIX}{k_PREFIX}{m.uniqueName}")};");
                                        }
                                        indent--;
                                        line("}");
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                void DoMethod()
                {
                    foreach (var m in collection.methods)
                    {
                        var methodInfo = m.methodInfo;
                        var isStatic = methodInfo.IsRealStatic();
                        var strAccess = isStatic ? " static " : " ";
                        var strObj = isStatic ? "null" : k_SELF;
                        var strMethod = m.rawName;
                        var strGenericDecl = methodInfo.IsGenericMethod ? $"<{string.Join(", ", methodInfo.GetGenericArguments().Select(x => x.Name))}>" : "";
                        var parameters = methodInfo.GetParameters();
                        var parameterTypeNames = parameters.Select(GetTypeName).ToArray();
                        var strParamDecl = string.Join(", ", parameterTypeNames.Select(x => $"{x.ParameterModifier}{x.typeName.name} {x.parameter.Name}"));
                        var returnTypeName = GetTypeName(methodInfo.ReturnType);
                        line();
                        line($"public{strAccess}{returnTypeName.name} {strMethod}{strGenericDecl}({strParamDecl})");
                        line("{");
                        indent++;
                        {
                            string strParam;
                            if (parameterTypeNames.Length > 0)
                            {
                                strParam = "__params__";
                                
                                line($"var __pool__ = FixedArrayPool<object>.Shared({parameters.Length});");
                                line($"var __params__ = __pool__.Rent();");
                                for (var i = 0; i < parameterTypeNames.Length; i++)
                                {
                                    var p = parameterTypeNames[i];
                                    if (p.refMode == RefMode.Out)
                                    {
                                        var flags = methodInfo.MethodImplementationFlags;
                                        if ((flags & MethodImplAttributes.InternalCall) != 0 ||
                                            (flags & MethodImplAttributes.Native) != 0 ||
                                            (flags & MethodImplAttributes.PreserveSig) != 0)
                                        {
                                            if (p.typeName.mode == TypeNameMode.Wrap)
                                                line($"__params__[{i}] = Activator.CreateInstance({p.typeName.name}.{k_TYPE});");
                                            else
                                                line($"__params__[{i}] = Activator.CreateInstance(typeof({p.typeName.name}));");
                                        }
                                        else
                                        {
                                            line($"__params__[{i}] = null;");
                                        }
                                    }
                                    else
                                    {
                                        line($"__params__[{i}] = {p.GetValue(p.parameter.Name)};");
                                    }
                                }
                            }
                            else
                            {
                                strParam = "System.Array.Empty<object>()";
                            }

                            if (returnTypeName.mode == TypeNameMode.Void)
                                line($"{k_PREFIX}{m.uniqueName}?.Invoke({strObj}, {strParam});");
                            else
                                line($"var __result__ = {k_PREFIX}{m.uniqueName}?.Invoke({strObj}, {strParam});");

                            if (parameterTypeNames.Length > 0)
                            {
                                for (var i = 0; i < parameterTypeNames.Length; i++)
                                {
                                    var p = parameterTypeNames[i];
                                    if (p.refMode == RefMode.Out || p.refMode == RefMode.Ref)
                                        line($"{p.parameter.Name} = {p.Cast($"__params__[{i}]")};");
                                }

                                line($"__pool__.Return(__params__);");
                            }

                            if (returnTypeName.mode != TypeNameMode.Void)
                                line($"return __result__ != null ? {returnTypeName.Cast("__result__")} : default;");
                        }
                        indent--;
                        line("}");
                    }
                }

                void DoDelegate()
                {
                    line();
                    foreach (var m in collection.delegates)
                    {
                        var delegateInfo = m.delegateInfo;
                        var methodInfo = delegateInfo.GetMethod("Invoke");
                        var returnTypeName = GetTypeName(methodInfo.ReturnType);
                        var parameters = methodInfo.GetParameters();
                        var parameterTypeNames = parameters.Select(GetTypeName).ToArray();

                        line($"public delegate {returnTypeName.name} {m.rawName}({string.Join(", ", parameterTypeNames.Select(x => $"{x.ParameterModifier}{x.typeName.name} {x.parameter.Name}"))});");
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
                            DoType();
                            DoDelegate();
                            DoField();
                            DoMethod();
                            DoCtor();
                            DoFieldReflection();
                            DoMethodReflection();
                        }
                        indent--;
                        line("}");

                        if (type.IsRealPublic() && !typeIsStatic)
                        {
                            line($"public static class {typeFlatName}_Extension");
                            line("{");
                            indent++;
                            {
                                line($"public static {typeFlatName} ReflectionHelper(this {typeFullName} self) => new(self);");
                            }
                            indent--;
                            line("}");
                        }
                    }
                    indent--;
                    line("}");
                    
                    Save();
                }
            }

            public override GenJobBase CreateJob(MemberCollection collection) => new GenWrapJob(this, collection);
        }


    }
}