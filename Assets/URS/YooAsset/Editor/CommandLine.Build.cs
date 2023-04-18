
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NinjaBeats
{
    public static partial class CommandLine
    {
        public enum Enum_Build_CommandLine_Arg
        {
            IL2CPP,
            ENABLE_HE_SDK,
            ONLY_EXPORT_PROJECT,
            BUILD_CHANNEL,
            HESDK_CHANNEL,
            BUILD_VERSION,
            BUILD_NUMBER,
            APP_IDENTIFIER,
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
        public static void ExportShader()
        {
            EditorUtils.RunCommandLineAsync(async () =>
            {
                URS.Build.ExportShaderVariantCollection();
            
                await URS.Build.WaitBuildTask();

                return true;
            });
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

        private static bool PrepareBuildEnvironment(out string channel,out string buildingResourceVersion,out string buildInResourceVersion)
        {
            channel = null;
            buildingResourceVersion= null; 
            buildInResourceVersion= null;
            if (!GetArg(Enum_Build_CommandLine_Arg.APP_IDENTIFIER, out var APP_IDENTIFIER))
                return false;
            if (!GetArg(Enum_Build_CommandLine_Arg.BUILD_CHANNEL, out var BUILD_CHANNEL))
                return false;
            if (!GetArg(Enum_Build_CommandLine_Arg.HESDK_CHANNEL, out var HESDK_CHANNEL))
                return false;
            if (!GetArg(Enum_Build_CommandLine_Arg.BUILD_VERSION, out var BUILD_VERSION))
                return false;
            if (!GetArgInt(Enum_Build_CommandLine_Arg.BUILD_NUMBER, out var BUILD_NUMBER))
                return false;

            BUILD_VERSION = GenerateVersion(BUILD_CHANNEL, BUILD_VERSION);
            
            // [包]的版本号
            PlayerSettings.bundleVersion = BUILD_VERSION;
            // [包]的构建号
            PlayerSettings.Android.bundleVersionCode = BUILD_NUMBER;
#if UNITY_EDITOR_OSX || UNITY_IOS
            // [包]的构建号
            PlayerSettings.iOS.buildNumber = BUILD_NUMBER.ToString();
#endif

            // [资源]APP标识
            //URSEditorUserSettings.instance.AppId = APP_IDENTIFIER;
            // [资源]渠道
            //URSEditorUserSettings.instance.BuildChannel = BUILD_CHANNEL;
            channel= BUILD_CHANNEL;
            // [资源]的版本号
            // URSEditorUserSettings.instance.BuildVersionCode = $"{BUILD_VERSION}-{BUILD_NUMBER}";
            buildingResourceVersion= $"{BUILD_VERSION}-{BUILD_NUMBER}";
            // [资源]的版本号（进[包]的）
            // URSEditorUserSettings.instance.CopyToStreamTargetVersion = $"{BUILD_VERSION}-{BUILD_NUMBER}";
            buildInResourceVersion= $"{BUILD_VERSION}-{BUILD_NUMBER}";

            return true;
        }

        // 生成资源补丁包
        public static void BuildResourcePatch()
        {
            EditorUtils.RunCommandLineAsync(async () =>
            {
                // if (!PrepareBuildEnvironment())
                //     return false;
                //
                // URS.Build.BuildChannelRouter();
                //
                // await URS.Build.WaitBuildTask();

                return true;
            });
        }
        
        // 准备cdn的资源
        public static void BuildResource()
        {
            EditorUtils.RunCommandLineAsync(async () =>
            {
                if (!PrepareBuildEnvironment(out var channel, out var buildingResourceVersion, out var buildInResourceVersion))
                    return false;
                
                URS.Build.BuildAutoChannelVersionsAndUploadCDN(
                    buildingResourceVersion,
                    channel);
            
                await URS.Build.WaitBuildTask();

                return true;
            });
        }
        public static void UploadCdn()
        {
            EditorUtils.RunCommandLineAsync(async () =>
            {
                if (!PrepareBuildEnvironment(out var channel, out var buildingResourceVersion, out var buildInResourceVersion))
                    return false;

                URS.Build.UploadCdnSyn(channel);

                await URS.Build.WaitBuildTask();

                return true;
            });
        }
        public static void ClearBundleCache()
        {
            EditorUtils.RunCommandLineAsync(async () =>
            {
                if (!PrepareBuildEnvironment(out var channel, out var buildingResourceVersion, out var buildInResourceVersion))
                    return false;
                
                URS.Build.ClearBundleCache();
            
                await URS.Build.WaitBuildTask();

                return true;
            });    
        }
        
        public static void ReImportResource()
        {
            EditorUtils.RunCommandLine(() =>
            {
                if (!PrepareBuildEnvironment(out var channel, out var buildingResourceVersion, out var buildInResourceVersion))
                    return false;
                
                AssetDatabase.ImportAsset("Assets/GameResources/", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            
                return true;
            }); 
                
        }

        // 打资源且打包
        public static void Build()
        {
            EditorUtils.RunCommandLineAsync(async () =>
            {
                if (!PrepareBuildEnvironment(out var channel, out var buildingResourceVersion, out var buildInResourceVersion))
                    return false;
                
                bool IL2CPP = EditorUtils.CommandLineHasArg(nameof(Enum_Build_CommandLine_Arg.IL2CPP));
                bool ONLY_EXPORT_PROJECT = EditorUtils.CommandLineHasArg(nameof(Enum_Build_CommandLine_Arg.ENABLE_HE_SDK)) || EditorUtils.CommandLineHasArg(nameof(Enum_Build_CommandLine_Arg.ONLY_EXPORT_PROJECT));
            
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel30;

                bool configSuccess = false;
                if (!ONLY_EXPORT_PROJECT)
                {
                    if (IL2CPP)
                        configSuccess= URS.Build.BuildResourceAndPlayer_Standard_Config();
                    else
                        configSuccess= URS.Build.BuildResourceAndPlayer_Fast_Config();
                }
                else
                {
                    if (IL2CPP)
                        configSuccess= URS.Build.ExportAndroidProject_IL2CPP_Config();
                    else
                        configSuccess= URS.Build.ExportAndroidProject_Mono_Config();
                }
                if (configSuccess) 
                {
                    URS.Build.HybridBuild(
                        buildingResourceVersion,
                        buildInResourceVersion,
                        channel,
                        true,
                        true,
                        false,
                        false);
                    await URS.Build.WaitBuildTask();
                    URS.Build.BuildAutoChannelVersionsAndUploadCDN(
                        channel,
                        buildingResourceVersion);
                    await URS.Build.WaitBuildTask();

                    var task= URS.Build.GenUploadCdnTask(channel);
                    URS.Build.BuildPlayerWithBuildInResource(
                        buildInResourceVersion,
                        channel);
                    await URS.Build.WaitBuildTask();
                    await task;
                }
                return true;
            });
        }
    }
}