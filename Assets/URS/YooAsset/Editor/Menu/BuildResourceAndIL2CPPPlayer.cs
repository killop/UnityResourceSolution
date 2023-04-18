using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace URS 
{
    public class BuildResourceAndIL2CPPPlayer : ScriptableWizard
    {
        [SerializeField, Tooltip("正要构建的资源版本")] public string BuildingResVersion;
        [SerializeField, Tooltip("要内置资源版本")] public string BuildInResVersion;
        [SerializeField, Tooltip("渠道名字")] public string Channel = "default_channel";

        [MenuItem("URS/Build(Resource And Player)-（IL2CPP）",false,100)]
        private static void Open()
        {
            DisplayWizard<BuildResourceAndIL2CPPPlayer>(ObjectNames.NicifyVariableName(nameof(BuildResourceAndIL2CPPPlayer)), "Build");
        }

        private void OnWizardCreate()
        {
            Build.BuildResourceAndPlayer_Standard(BuildingResVersion, BuildInResVersion, Channel);
        }
    }
}

