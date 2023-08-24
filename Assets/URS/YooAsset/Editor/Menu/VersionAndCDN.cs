using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace URS 
{
    public class VersionAndCDN : ScriptableWizard
    {
      
        [SerializeField, Tooltip("渠道名字")]       public string Channel = "default_channel";
        [SerializeField, Tooltip("渠道的目标版本,如果为空,自动为最新的版本")] public string ChannelTargetVersion="";
        [SerializeField, Tooltip("资源版本保留的数量")] public int VersionKeepCount = 4;
        [SerializeField, Tooltip("是否上传CDN")]    public bool UploadCDN = false;
        [SerializeField, Tooltip("Debug")] public bool Debug = false;

        [MenuItem("URS/BuildAutoChannelVersionsAndUploadCDN",false,105)]
        private static void Open()
        {
           var sw=  DisplayWizard<VersionAndCDN>(ObjectNames.NicifyVariableName(nameof(VersionAndCDN)), "Build");
        }

        private void OnWizardCreate()
        {
            Build.BuildAutoChannelVersionsAndUploadCDN(Channel, ChannelTargetVersion, VersionKeepCount, UploadCDN,Debug);
        }
    }
}

