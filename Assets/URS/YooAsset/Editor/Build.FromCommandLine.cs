
using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using TagInfo = System.Collections.Generic.Dictionary<Bewildered.SmartLibrary.TagRule.TagRuleType, string>;
using Context = System.Collections.Generic.Dictionary<string, object>;
using Debug = UnityEngine.Debug;

public enum Enum_Build_CommandLine_Arg
{
    IL2CPP,
    ENABLE_HE_SDK,
    ONLY_EXPORT_PROJECT,
    CHANNEL,
    VERSION,
    BUILD_NUMBER,
    APP_IDENTIFIER,
}

namespace URS
{
    public static partial class Build
    {
        public class BuildTaskAwaitable : System.Runtime.CompilerServices.INotifyCompletion
        {
            protected Action Continuation;
            public object Result { get; protected set; }
            public bool IsCompleted { get; protected set; }
            
            public void OnCompleted(Action continuation)
            {
                if (IsCompleted)
                {
                    continuation?.Invoke();
                }
                else
                {
                    if (continuation != null)
                        Continuation += continuation;
                }
            }

            public void SetDone()
            {
                if (IsCompleted)
                    return;
                Result = null;
                IsCompleted = true;
                Continuation?.Invoke();
            }

            public object GetResult() => Result;

            public BuildTaskAwaitable GetAwaiter() => this;
        }

        private static BuildTaskAwaitable WaitBuildTask()
        {
            BuildTaskAwaitable r = new();
            EditorApplication.CallbackFunction func = null;
            func = () =>
            {
                if (hasTask)
                {
                    if (_buildTaskWorkSpaces[0].hasException)
                        EditorApplication.Exit(1);
                    return;   
                }

                EditorApplication.update -= func;    
                r.SetDone();
            };
            EditorApplication.update += func;
            return r;
        }

        private static bool GetArg(Enum_Build_CommandLine_Arg type, out string arg)
        {
            arg = EditorUtils.CommandLineGetArgValue(type.ToString());
            if (string.IsNullOrWhiteSpace(arg))
            {
                Debug.LogError($"{type.ToString()} is invalid");
                EditorApplication.Exit(1);
                return false;
            }
            return true;
        }
        
        private static bool GetArgInt(Enum_Build_CommandLine_Arg type, out int arg)
        {
            arg = EditorUtils.CommandLineGetArgValueInt(type.ToString());
            if (arg == 0)
            {
                Debug.LogError($"{type.ToString()} is invalid");
                EditorApplication.Exit(1);
                return false;
            }
            return true;
        }

        // 收集Shader变体
        public static async void ExportShaderFromCommandLine()
        {
            ExportShaderVariantCollection();
           
            await WaitBuildTask(); 
           
            EditorApplication.Exit(0);     
        }

        private static string GenerateVersion(string channel, string ver)
        {
            if (!System.Version.TryParse(ver, out var version))
                return ver;

            var build = version.Build;
            
            var stored_version_path = Path.Combine(Application.dataPath, $"../BuildInfo/{channel}/stored_version.txt");
            if (build == 0)
            {
                var str = EditorUtils.ReadAllText(stored_version_path);
                if (!string.IsNullOrWhiteSpace(str) && System.Version.TryParse(str, out var stored_version))
                {
                    if (version.Major == stored_version.Major && version.Minor == stored_version.Minor)
                    {
                        build = stored_version.Build + 1;
                    }
                }
            }

            var r = $"{version.Major}.{version.Minor}.{build}";
            EditorUtils.WriteAllText(stored_version_path, r);
            return r;
        }

        private static bool PrepareBuildEnvironment()
        {
            if (!GetArg(Enum_Build_CommandLine_Arg.APP_IDENTIFIER, out var APP_IDENTIFIER))
                return false;
            if (!GetArg(Enum_Build_CommandLine_Arg.CHANNEL, out var CHANNEL))
                return false;
            if (!GetArg(Enum_Build_CommandLine_Arg.VERSION, out var VERSION))
                return false;
            if (!GetArgInt(Enum_Build_CommandLine_Arg.BUILD_NUMBER, out var BUILD_NUMBER))
                return false;

            VERSION = GenerateVersion(CHANNEL, VERSION);
            
            // [包]的版本号
            PlayerSettings.bundleVersion = VERSION;
            // [包]的构建号
            PlayerSettings.Android.bundleVersionCode = BUILD_NUMBER;
#if UNITY_EDITOR_OSX || UNITY_IOS
            // [包]的构建号
            PlayerSettings.iOS.buildNumber = BUILD_NUMBER.ToString();
#endif    
            
            // [资源]APP标识
            URSEditorUserSettings.instance.AppId = APP_IDENTIFIER;
            // [资源]渠道
            URSEditorUserSettings.instance.BuildChannel = CHANNEL;
            // [资源]的版本号
            URSEditorUserSettings.instance.BuildVersionCode = $"{VERSION}-{BUILD_NUMBER}";
            // [资源]的版本号（进[包]的）
            URSEditorUserSettings.instance.CopyToStreamTargetVersion = $"{VERSION}-{BUILD_NUMBER}";

            return true;
        }

        // 生成资源补丁包
        public static async void BuildResourcePatch()
        {
            EditorUtils.CommandLineListenError();
            // if (!PrepareBuildEnvironment())
            //     return;
            //
            // BuildChannelRouter();
            //
            // await WaitBuildTask();
            
            EditorUtils.CommandLineSaveError();
            EditorApplication.Exit(0);  
        }
        
        // 只打资源不打包
        public static async void BuildResourceFromCommandLine()
        {
            EditorUtils.CommandLineListenError();
            if (!PrepareBuildEnvironment())
                return;
            
            BuildBundleAndRaw();
            
            await WaitBuildTask();
           
            EditorUtils.CommandLineSaveError();
            EditorApplication.Exit(0);    
        }
        
        public static async void ClearBundleCacheFromCommandLine()
        {
            EditorUtils.CommandLineListenError();
            if (!PrepareBuildEnvironment())
                return;
            
            ClearBundleCache();
            
            await WaitBuildTask();
           
            EditorUtils.CommandLineSaveError();
            EditorApplication.Exit(0);    
        }

        // 打资源且打包
        public static async void BuildFromCommandLine()
        {
            EditorUtils.CommandLineListenError();
            if (!PrepareBuildEnvironment())
                return;
            
            bool IL2CPP = EditorUtils.CommandLineHasArg(nameof(Enum_Build_CommandLine_Arg.IL2CPP));
            bool ONLY_EXPORT_PROJECT = EditorUtils.CommandLineHasArg(nameof(Enum_Build_CommandLine_Arg.ENABLE_HE_SDK)) || EditorUtils.CommandLineHasArg(nameof(Enum_Build_CommandLine_Arg.ONLY_EXPORT_PROJECT));

            if (IL2CPP)
                BuildResourceAndPlayer_Standard();
            else
                BuildResourceAndPlayer_Fast();

            await WaitBuildTask();
           
            EditorUtils.CommandLineSaveError();
            EditorApplication.Exit(0);    
        }
 
    }
}
   
