using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace URS 
{
    public class BuildResourceAndMonoAndroidProject : ScriptableWizard
    {
        [SerializeField,Tooltip("正要构建的资源版本")] public string BuildingResVersion;
        [SerializeField,Tooltip("要内置资源版本")] public string BuildInResVersion;
        [SerializeField,Tooltip("渠道名字")] public string Channel="default_channel";

        [MenuItem("URS/Build(Resource And AndroidProject)-乐元素SDK（Mono）",false,103)]
        private static void Open()
        {
            DisplayWizard<BuildResourceAndMonoAndroidProject>(ObjectNames.NicifyVariableName(nameof(BuildResourceAndMonoAndroidProject)),"Build");
        }

        private void OnWizardCreate()
        {
            Build.ExportAndroidProject_Mono(BuildingResVersion, BuildInResVersion, Channel);
        }
    }
}

