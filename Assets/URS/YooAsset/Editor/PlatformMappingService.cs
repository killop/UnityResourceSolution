using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using URSPlatform = URS.URSPlatform;

namespace URS
{
    public enum URSPlatform
    {
        /// <summary>
        /// Use to indicate that the build platform is unknown.
        /// </summary>
        Unknown,
        /// <summary>
        /// Use to indicate that the build platform is Windows.
        /// </summary>
        Windows,
        /// <summary>
        /// Use to indicate that the build platform is OSX.
        /// </summary>
        OSX,
        /// <summary>
        /// Use to indicate that the build platform is Linux.
        /// </summary>
        Linux,
        /// <summary>
        /// Use to indicate that the build platform is PS4.
        /// </summary>
        PS4,
        /// <summary>
        /// Use to indicate that the build platform is PS4.
        /// </summary>
        Switch,
        /// <summary>
        /// Use to indicate that the build platform is XboxOne.
        /// </summary>
        XboxOne,
        /// <summary>
        /// Use to indicate that the build platform is WebGL.
        /// </summary>
        WebGL,
        /// <summary>
        /// Use to indicate that the build platform is iOS.
        /// </summary>
        iOS,
        /// <summary>
        /// Use to indicate that the build platform is Android.
        /// </summary>
        Android,
        /// <summary>
        /// Use to indicate that the build platform is WindowsUniversal.
        /// </summary>
        WindowsUniversal
    }

    public class PlatformMappingService
    {
#if UNITY_EDITOR
        internal static readonly Dictionary<BuildTarget, URSPlatform> s_BuildTargetMapping =
            new Dictionary<BuildTarget, URSPlatform>()
        {
            {BuildTarget.XboxOne, URSPlatform.XboxOne},
            {BuildTarget.Switch, URSPlatform.Switch},
            {BuildTarget.PS4, URSPlatform.PS4},
            {BuildTarget.iOS, URSPlatform.iOS},
            {BuildTarget.Android, URSPlatform.Android},
            {BuildTarget.WebGL, URSPlatform.WebGL},
            {BuildTarget.StandaloneWindows, URSPlatform.Windows},
            {BuildTarget.StandaloneWindows64, URSPlatform.Windows},
            {BuildTarget.StandaloneOSX, URSPlatform.OSX},
            {BuildTarget.StandaloneLinux64, URSPlatform.Linux},
            {BuildTarget.WSAPlayer, URSPlatform.WindowsUniversal},
        };
#endif
        internal static readonly Dictionary<RuntimePlatform, URSPlatform> s_RuntimeTargetMapping =
            new Dictionary<RuntimePlatform, URSPlatform>()
        {
            {RuntimePlatform.XboxOne, URSPlatform.XboxOne},
            {RuntimePlatform.Switch, URSPlatform.Switch},
            {RuntimePlatform.PS4, URSPlatform.PS4},
            {RuntimePlatform.IPhonePlayer, URSPlatform.iOS},
            {RuntimePlatform.Android, URSPlatform.Android},
            {RuntimePlatform.WebGLPlayer, URSPlatform.WebGL},
            {RuntimePlatform.WindowsPlayer, URSPlatform.Windows},
            {RuntimePlatform.OSXPlayer, URSPlatform.OSX},
            {RuntimePlatform.LinuxPlayer, URSPlatform.Linux},
            {RuntimePlatform.WindowsEditor, URSPlatform.Windows},
            {RuntimePlatform.OSXEditor, URSPlatform.OSX},
            {RuntimePlatform.LinuxEditor, URSPlatform.Linux},
            {RuntimePlatform.WSAPlayerARM, URSPlatform.WindowsUniversal},
            {RuntimePlatform.WSAPlayerX64, URSPlatform.WindowsUniversal},
            {RuntimePlatform.WSAPlayerX86, URSPlatform.WindowsUniversal},
        };

#if UNITY_EDITOR
        internal static URSPlatform GetURSPlatformInternal(BuildTarget target)
        {
            if (s_BuildTargetMapping.ContainsKey(target))
                return s_BuildTargetMapping[target];
            return URSPlatform.Unknown;
        }

        internal static string GetURSPlatformPathInternal(BuildTarget target)
        {
            if (s_BuildTargetMapping.ContainsKey(target))
                return s_BuildTargetMapping[target].ToString();
            return target.ToString();
        }

#endif
        internal static URSPlatform GetURSPlatformInternal(RuntimePlatform platform)
        {
            if (s_RuntimeTargetMapping.ContainsKey(platform))
                return s_RuntimeTargetMapping[platform];
            return URSPlatform.Unknown;
        }

        internal static string GetURSPlatformPathInternal(RuntimePlatform platform)
        {
            if (s_RuntimeTargetMapping.ContainsKey(platform))
                return s_RuntimeTargetMapping[platform].ToString();
            return platform.ToString();
        }

    

        public static string GetPlatformPathSubFolder()
        {
#if UNITY_EDITOR
            return GetURSPlatformPathInternal(EditorUserBuildSettings.activeBuildTarget);
#else
            return GetURSPlatformInternal(Application.platform);
#endif
        }
    }
}
