using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace URS 
{
    public class HybridBuild : ScriptableWizard
    {
        [SerializeField, Tooltip("正要构建的资源版本")] public string BuildingResVersion;
        [SerializeField, Tooltip("要内置资源版本")] public string BuildInResVersion;
        [SerializeField, Tooltip("渠道名字")] public string Channel = "default_channel";
        [SerializeField, Tooltip("是不是要buildResource")] public bool BuildResource = true;
        [SerializeField, Tooltip("是不是要buildRaw")] public bool BuildRaw= true;
        [SerializeField, Tooltip("是不是要copy BuildInRes")] public bool CopyBuildInRes = true;
        [SerializeField, Tooltip("是不是要build player")] public bool BuildPlayer = false;
        [SerializeField, Tooltip("是不是要build player")] public bool Debug = false;
        [MenuItem("URS/CustomBuild(自定义构建）",false,104)]
        private static void Open()
        {
           var sw=  DisplayWizard<HybridBuild>(ObjectNames.NicifyVariableName(nameof(HybridBuild)), "Build");
        }

        private void OnWizardCreate()
        {
            Build.HybridBuild(BuildingResVersion, BuildInResVersion, Channel, BuildResource, BuildRaw, CopyBuildInRes, BuildPlayer,Debug);
        }
    }
}

