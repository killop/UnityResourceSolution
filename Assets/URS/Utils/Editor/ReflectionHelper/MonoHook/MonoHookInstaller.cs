using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace NinjaBeats.ReflectionHelper
{
    public enum MonoHookType
    {
        Method,
        Property,
        Constructor,
        PropertyGet,
        PropertySet,
    }

    public class MonoHookAttribute : Attribute
    {

        public MonoHookType HookType;
        public Type[] Parameters;

        public MonoHookAttribute()
        {
            this.HookType = MonoHookType.Method;
            this.Parameters = null;
        }

        public MonoHookAttribute(MonoHookType hookType, params Type[] parameters)
        {
            this.HookType = hookType;
            this.Parameters = parameters;
        }
    }

    public class MonoHookInstallerV2
    {
        static readonly string __type__Str = "__type__";
        static readonly string __original__Str = "__original__";
        static readonly string __replace__Str = "__replace__";

        private MethodHook hook = null;

        private delegate MethodBase GetTargetMethodDelegate(Type originType, Type hookType, MethodInfo __replace__);
        
        void InitMethod(Type targetType, Type hookType, GetTargetMethodDelegate getter)
        {
            if (hook != null)
                return;

            // 找到被替换成的新方法
            MethodInfo miReplacement = hookType.GetMethod(__replace__Str, (BindingFlags)(-1));

            // 这个方法是用来调用原始方法的
            MethodInfo miProxy = hookType.GetMethod(__original__Str, (BindingFlags)(-1));

            // 找到需要 Hook 的方法
            MethodBase miTarget = getter(targetType, hookType, miReplacement);
            
            if (miTarget == null)
            {
                Debug.LogErrorFormat("{0} hook failed, method {1} is invalid", targetType?.FullName ?? "?", hookType?.Name ?? "?");
                return;
            }

            if (miReplacement.IsStatic != miTarget.IsStatic || miReplacement.IsStatic != miProxy.IsStatic)
            {
                Debug.LogError($"{targetType.FullName ?? "?"} hook failed, method static is not match");
                return;
            }

            // 创建一个 Hook 并 Install 就OK啦, 之后无论哪个代码再调用原始方法都会重定向到
            //  我们写的方法ヾ(ﾟ∀ﾟゞ)
            hook = new MethodHook(miTarget, miReplacement, miProxy);
            hook.Install();
        }

        private static List<MonoHookInstallerV2> _MonoHookInstallerList = new();
        
        static MethodBase GetTargetMethod(Type originType, Type hookType, MethodInfo __replace__)
        {
            return originType.GetMethodInfoByParameterTypes(hookType.Name, __replace__.GetParameters().Select(x => x.ParameterType).ToArray());
        }
        
        static MethodBase GetTargetConstructor(Type originType, Type hookType, MethodInfo __replace__)
        {
            return originType.GetConstructorInfoByParameterTypes(__replace__.GetParameters().Select(x => x.ParameterType).ToArray());
        }
        
        static MethodBase GetTargetPropertyGet(Type originType, Type hookType, MethodInfo __replace__)
        {
            return originType.GetProperty(hookType.Name, (BindingFlags)(-1))?.GetMethod;
        }
        
        static MethodBase GetTargetPropertySet(Type originType, Type hookType, MethodInfo __replace__)
        {
            return originType.GetProperty(hookType.Name, (BindingFlags)(-1))?.SetMethod;
        }

        [InitializeOnLoadMethod]
        static void Initalize()
        {
#if UNITY_EDITOR_OSX
        return;
#endif
            EditorPrefs.SetBool("ScriptDebugInfoEnabled", true);
            UnityEditor.Compilation.CompilationPipeline.codeOptimization =
                UnityEditor.Compilation.CodeOptimization.Debug;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            if (UnityEngine_GUISkin.current == null)
                return;

            EditorApplication.update -= OnEditorUpdate;

            var list = EditorUtils.ScanAllTypeWithAttributeMark<MonoHookAttribute>();
            foreach (var info in list)
            {
                try
                {
                    var installer = new MonoHookInstallerV2();
                    
                    var typeFullName = info.type.GetField(__type__Str, (BindingFlags)(-1))?.GetValue(null) as string;
                    var targetType = EditorUtils.GetTypeByFullName(typeFullName);
                    if (targetType == null)
                        continue;
                    
                    switch (info.attr.HookType)
                    {
                        case MonoHookType.Method:
                            installer.InitMethod(targetType, info.type, GetTargetMethod);
                            break;
                        case MonoHookType.Constructor:
                            installer.InitMethod(targetType, info.type, GetTargetConstructor);
                            break;
                        case MonoHookType.PropertyGet:
                            installer.InitMethod(targetType, info.type, GetTargetPropertyGet);
                            break;
                        case MonoHookType.PropertySet:
                            installer.InitMethod(targetType, info.type, GetTargetPropertySet);
                            break;
                    }

                    _MonoHookInstallerList.Add(installer);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + "\n" + e.StackTrace);
                }
            }
        }
    }

    public class MonoHookInstaller
    {
        static readonly string PropNameTarget = "__Target";
        static readonly string FuncNameOriginal = "__Original";
        static readonly string FuncNameReplace = "__Replace";
        MethodHook hook = null;
        MethodHook hookExtra = null;

        bool CheckSame(MethodInfo method, string name, ParameterInfo[] parameters, Type returnType)
        {
            if (method.Name != name)
                return false;
            if (method.ReturnType != returnType)
                return false;

            var methodParameters = method.GetParameters();
            if (parameters.Length != methodParameters.Length)
                return false;

            for (int i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i].ParameterType != methodParameters[i].ParameterType)
                    return false;
            }

            return true;
        }

        void InitMethod(Type originType, Type hookType)
        {
            if (hook != null)
                return;

            // 找到被替换成的新方法
            MethodInfo miReplacement = hookType.GetMethod(FuncNameReplace, (BindingFlags)(-1));
            var miReplacementParameters = miReplacement.GetParameters();

            // 这个方法是用来调用原始方法的
            MethodInfo miProxy = hookType.GetMethod(FuncNameOriginal, (BindingFlags)(-1));

            // 找到需要 Hook 的方法
            MethodInfo miTarget = null;
            foreach (var method in originType.GetMethods((BindingFlags)(-1)))
            {
                if (CheckSame(method, hookType.Name, miReplacementParameters, miReplacement.ReturnType))
                {
                    miTarget = method;
                    break;
                }
            }

            if (miTarget == null)
            {
                Debug.LogErrorFormat("{0} hook failed, method {1} is invalid", originType?.FullName ?? "?",
                    hookType?.Name ?? "?");
                return;
            }

            // 创建一个 Hook 并 Install 就OK啦, 之后无论哪个代码再调用原始方法都会重定向到
            //  我们写的方法ヾ(ﾟ∀ﾟゞ)
            hook = new MethodHook(miTarget, miReplacement, miProxy);
            hook.Install();
        }

        void InitProperty(Type originType, Type hookType)
        {
            if (hook != null || hookExtra != null)
                return;

            // 找到需要 Hook 的属性
            PropertyInfo piTarget = originType.GetProperty(hookType.Name, (BindingFlags)(-1));
            if (piTarget == null)
            {
                Debug.LogErrorFormat("{0} hook failed, property {1} is invalid", originType?.FullName ?? "?",
                    hookType?.Name ?? "?");
                return;
            }

            var getMethod = piTarget.GetMethod;
            if (getMethod != null)
            {
                // 找到被替换成的新方法
                MethodInfo miReplacement = hookType.GetMethod(FuncNameReplace + "_Get", (BindingFlags)(-1));

                // 这个方法是用来调用原始方法的
                MethodInfo miProxy = hookType.GetMethod(FuncNameOriginal + "_Get", (BindingFlags)(-1));

                if (miReplacement != null && miProxy != null)
                {
                    // 创建一个 Hook 并 Install 就OK啦, 之后无论哪个代码再调用原始方法都会重定向到
                    //  我们写的方法ヾ(ﾟ∀ﾟゞ)
                    hook = new MethodHook(getMethod, miReplacement, miProxy);
                    hook.Install();
                }
            }

            var setMethod = piTarget.SetMethod;
            if (setMethod != null)
            {
                // 找到被替换成的新方法
                MethodInfo miReplacement = hookType.GetMethod(FuncNameReplace + "_Set", (BindingFlags)(-1));

                // 这个方法是用来调用原始方法的
                MethodInfo miProxy = hookType.GetMethod(FuncNameOriginal + "_Set", (BindingFlags)(-1));

                if (miReplacement != null && miProxy != null)
                {
                    // 创建一个 Hook 并 Install 就OK啦, 之后无论哪个代码再调用原始方法都会重定向到
                    //  我们写的方法ヾ(ﾟ∀ﾟゞ)
                    hookExtra = new MethodHook(setMethod, miReplacement, miProxy);
                    hookExtra.Install();
                }
            }
        }

        void InitConstructor(Type originType, Type hookType, Type[] parameters)
        {
            if (hook != null)
                return;

            // 找到需要 Hook 的方法
            ConstructorInfo miTarget = originType.GetConstructor(parameters ?? new Type[] { });
            if (miTarget == null)
            {
                Debug.LogErrorFormat("{0} hook failed, method CheckRectsOnMouseMove is invalid",
                    originType?.FullName ?? "?");
                return;
            }

            // 找到被替换成的新方法
            MethodInfo miReplacement = hookType.GetMethod(FuncNameReplace, (BindingFlags)(-1));

            // 这个方法是用来调用原始方法的
            MethodInfo miProxy = hookType.GetMethod(FuncNameOriginal, (BindingFlags)(-1));

            // 创建一个 Hook 并 Install 就OK啦, 之后无论哪个代码再调用原始方法都会重定向到
            //  我们写的方法ヾ(ﾟ∀ﾟゞ)
            hook = new MethodHook(miTarget, miReplacement, miProxy);
            hook.Install();
        }

        static List<MonoHookInstaller> _MonoHookInstallerList = new List<MonoHookInstaller>();

        [InitializeOnLoadMethod]
        static void Initalize()
        {
#if UNITY_EDITOR_OSX
        return;
#endif
            EditorPrefs.SetBool("ScriptDebugInfoEnabled", true);
            UnityEditor.Compilation.CompilationPipeline.codeOptimization =
                UnityEditor.Compilation.CodeOptimization.Debug;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            if (UnityEngine_GUISkin.current == null)
                return;

            EditorApplication.update -= OnEditorUpdate;

            var list = EditorUtils.ScanAllTypeWithAttributeMark<MonoHookAttribute>();
            foreach (var info in list)
            {
                try
                {
                    var installer = new MonoHookInstaller();
                    Type targetType = info.type.GetProperty(PropNameTarget, (BindingFlags)(-1))?.GetValue(null) as Type;
                    if (targetType == null)
                        continue;
                    switch (info.attr.HookType)
                    {
                        case MonoHookType.Method:
                            installer.InitMethod(targetType, info.type);
                            break;
                        case MonoHookType.Property:
                            installer.InitProperty(targetType, info.type);
                            break;
                        case MonoHookType.Constructor:
                            installer.InitConstructor(targetType, info.type, info.attr.Parameters);
                            break;
                    }

                    _MonoHookInstallerList.Add(installer);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + "\n" + e.StackTrace);
                }
            }
        }
    }
}