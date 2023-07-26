using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;
using NinjaBeats;

public class BuildTaskBuildPlayer : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        EditorUtility.DisplayProgressBar("BuildTaskBuildPlayer", "最终打包", 0);
        bool debug = GetArgValue("debug", "false").Equals("true");
        BuildPlayer(debug);
        EditorUtility.ClearProgressBar();
        this.FinishTask();
    }
    private static string GetArgValue(string argName, string defaultValue)
    {
        string[] args = Environment.GetCommandLineArgs();
        argName = string.Format("-{0}", argName);
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(argName) && IsArgValueValid(args[i + 1]))
                return args[i + 1];
        return defaultValue;
    }
    private static bool IsArgValueValid(string argValue)
    {
        return argValue != null && !string.IsNullOrEmpty(argValue) && !argValue.StartsWith("-");
    }
    private static void BuildPlayer(bool debug)
    {
     
        var path = Path.Combine(Environment.CurrentDirectory, "Build");
        if (path.Length == 0)
        {
            return;
        }

        var levels = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
        if (levels.Length == 0)
        {
            Debug.LogError("在Settings里面没有配置任何的启动场景");
            return;
        }


        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        GetBuildTargetName(buildTarget, out var middleTargetName, out var finalTargetName);
        if (string.IsNullOrEmpty(middleTargetName) || string.IsNullOrEmpty(finalTargetName))
            return;

        if (!CleanupTempData(buildTarget))
            return;

        BuildOptions options = BuildOptions.None;
        if (EditorUserBuildSettings.development)
        {
            debug = true;
        }
        if (debug)
        {
            options |= BuildOptions.Development;
            options |= BuildOptions.AllowDebugging;
        }
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = levels,
            locationPathName = path + middleTargetName,
            target = buildTarget,
            options = options
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        //  string fullPath = Path.Combine(outputPath, binName);
        //	BuildTarget buildTarget = GetBuildTarget(buildTargetParam);

        //	BuildReport report = BuildPipeline.BuildPlayer(buildScenes, fullPath, buildTarget, options);
        switch (report.summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log("Build success!");
                break;
            case BuildResult.Failed:
                Debug.Log("Build fail!");
                break;
            case BuildResult.Cancelled:
                Debug.Log("Build cancel!");
                break;
        }
        
        BuildFinalTarget(buildTarget, finalTargetName);
    }

    private static bool CleanupTempData(BuildTarget target)
    {
        if (EditorUserBuildSettings.exportAsGoogleAndroidProject && target == BuildTarget.Android)
        {
            try
            {
                var androidProjectPath = Path.Combine(Application.dataPath, "../Build/AndroidProject");
                if (Directory.Exists(androidProjectPath))
                    Directory.Delete(androidProjectPath, true);
            }
            catch (Exception e)
            {
                Debug.LogError("清理安卓工程失败：" + e.Message + "\n" + e.StackTrace);
                if (Application.isBatchMode)
                    throw;
                return false;
            }
        }
        else if (target == BuildTarget.iOS)
        {
            try
            {
                var iosProjectPath = Path.Combine(Application.dataPath, "../Build/iOSProject");
                if (Directory.Exists(iosProjectPath))
                    Directory.Delete(iosProjectPath, true);
            }
            catch (Exception e)
            {
                Debug.LogError("清理苹果工程失败：" + e.Message + "\n" + e.StackTrace);
                if (Application.isBatchMode)
                    throw;
                return false;
            }
        }

        return true;
    }


    [PostProcessBuild(39)]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            var HESDK_CHANNEL = EditorUtils.CommandLineGetArgValue(nameof(CommandLine.Enum_Build_CommandLine_Arg.HESDK_CHANNEL));
            if (string.IsNullOrEmpty(HESDK_CHANNEL))
                HESDK_CHANNEL = "dev_ios";

            var productName = PlayerSettings.productName;
            var APP_IDENTIFIER = EditorUtils.CommandLineGetArgValue(nameof(CommandLine.Enum_Build_CommandLine_Arg.APP_IDENTIFIER));
            if (!string.IsNullOrEmpty(APP_IDENTIFIER) && APP_IDENTIFIER.IndexOf('.') != -1)
                productName = APP_IDENTIFIER.Substring(APP_IDENTIFIER.LastIndexOf('.') + 1);

#if UNITY_EDITOR_OSX || UNITY_IOS
            var projPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(pathToBuiltProject);
            UnityEditor.iOS.Xcode.PBXProject proj = new();
            proj.ReadFromFile(projPath);

            string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();
            proj.SetBuildProperty(frameworkTargetGuid, "SWIFT_VERSION", "5.0");
            
            proj.WriteToFile(projPath);

            var MMNViOSCoreHapticsInterfacePath = Path.Combine(pathToBuiltProject, "Libraries/Packages/NiceVibrations/Common/Plugins/iOS/Swift/MMNViOSCoreHapticsInterface.mm");
            var MMNViOSCoreHapticsInterface = File.ReadAllText(MMNViOSCoreHapticsInterfacePath);
            MMNViOSCoreHapticsInterface = MMNViOSCoreHapticsInterface.Replace("\"UnityFramework/UnityFramework-Swift.h\"",
                $"<{productName}_UnityFramework_{HESDK_CHANNEL}/{productName}_UnityFramework_{HESDK_CHANNEL}-Swift.h>");
            File.WriteAllText(MMNViOSCoreHapticsInterfacePath, MMNViOSCoreHapticsInterface);
#endif
        }
    }
    
    private static bool BuildFinalTarget(BuildTarget target, string finalTargetName)
    {   
        if (EditorUserBuildSettings.exportAsGoogleAndroidProject && target == BuildTarget.Android)
        {
            if (EditorUtils.CommandLineHasArg(nameof(CommandLine.Enum_Build_CommandLine_Arg.ONLY_EXPORT_PROJECT)))
                return true;
            
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
            Process process = null;
            try
            {
                EditorUtility.DisplayProgressBar("Build Android Gradle", "Working...", 0.0f);
                var androidProjectPath = Path.Combine(Application.dataPath, "../Build/AndroidProject");
                
                var batContent = new StringBuilder();
#if UNITY_EDITOR_WIN
                batContent.AppendLine(androidProjectPath.Substring(0, 2));
#endif
                batContent.AppendLine($"cd {androidProjectPath}");
                batContent.AppendLine("gradle assembleRelease");
                batContent.AppendLine("exit");
                
#if UNITY_EDITOR_WIN
                var batFile = Path.Combine(androidProjectPath, "BuildAndroidProject.bat");
                EditorUtils.WriteToFile(batFile, batContent.ToString());
                process = Process.Start(batFile);
#elif UNITY_EDITOR_OSX
                var start = new ProcessStartInfo("/bin/sh");
                start.Arguments = $" -c \"{batContent} \"";
                start.CreateNoWindow = false;
                start.ErrorDialog = true;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.RedirectStandardInput = true;
                start.StandardOutputEncoding = Encoding.UTF8;
                start.StandardErrorEncoding = Encoding.UTF8;
                process = Process.Start(start);
#endif
                process.WaitForExit();
                process.Close();

                EditorUtility.DisplayProgressBar("Build Android Gradle", "Copy apk...", 1.0f);
                
                var gradleApkPath = Path.Combine(Application.dataPath, "../Build/AndroidProject/dev/build/outputs/apk/release/dev-release.apk");
                var finalTargetPath = Path.Combine(Application.dataPath, "../Build/" + finalTargetName);
                File.Copy(gradleApkPath, finalTargetPath, true);

                Debug.Log("BuildFinalTarget success!");
            }
            catch (Exception e)
            {
                Debug.LogError("BuildFinalTarget fail：" + e.Message + "\n" + e.StackTrace);
                try
                {
                    process?.Kill();
                }
                catch
                {
                }

                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
#else
            Debug.LogError("BuildFinalTarget Failed：目前不支持当前平台");
            return false;
#endif
            
        }

        return true;
    }

    private static void GetBuildTargetName(BuildTarget target, out string middleTargetName, out string finalTargetName)
    {
        var ENABLE_HESDK = false; // ~~!
        if (!ENABLE_HESDK)
        {
            finalTargetName = "/dev-release";
        }
        else
        {
            var productName = PlayerSettings.productName + "-v" + PlayerSettings.bundleVersion ;
            finalTargetName = string.Format("/{0}-{1}", productName, GetTimeForNow());    
        }
        
        switch (target)
        {
            case BuildTarget.Android:
                finalTargetName += ".apk";
                break;
            case BuildTarget.iOS:
                finalTargetName += ".ipa";
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                finalTargetName += ".exe";
                break;
            case BuildTarget.StandaloneOSX:
                finalTargetName += ".app";
                break;
        }

        if (EditorUserBuildSettings.exportAsGoogleAndroidProject && target == BuildTarget.Android)
            middleTargetName = "/AndroidProject";
        else if (target == BuildTarget.iOS)
            middleTargetName = "/iOSProject";
        else
            middleTargetName = finalTargetName;
    }
    public static string GetTimeForNow()
    {
        return DateTime.Now.ToString("yyyyMMdd-HHmmss");
    }
}
