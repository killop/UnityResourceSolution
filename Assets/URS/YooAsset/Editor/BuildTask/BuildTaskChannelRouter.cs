
using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;
using URS;

public class BuildTaskChannelRouter : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();

        var routerFilePath = $"{Build.GetChannelRoot()}/{URSRuntimeSetting.instance.RemoteAppToChannelRouterFileName}";
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(routerFilePath));
        if (File.Exists(routerFilePath))
        {
            File.Delete(routerFilePath);
        }
        File.WriteAllText(routerFilePath, JsonUtility.ToJson(URSEditorUserSettings.instance.AppToChannelRouter,true));
        this.FinishTask();
    }


}
