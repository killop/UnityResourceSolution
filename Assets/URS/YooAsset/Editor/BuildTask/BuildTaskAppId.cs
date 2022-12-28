using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;

public class BuilTaskAppId : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        var streamSandboxFolderName = YooAsset.AssetPathHelper.GetStreamingSandboxDirectory();
      
        var appFilePath = $"{YooAsset.AssetPathHelper.GetStreamingSandboxDirectory()}/{URSRuntimeSetting.instance.AppIdFileName}" ;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(appFilePath));
        if (File.Exists(appFilePath))
        {
            File.Delete(appFilePath);
        }
        File.WriteAllText(appFilePath, URSEditorUserSettings.instance.AppId);
        UnityEditor.AssetDatabase.Refresh();
        UnityEditor.AssetDatabase.SaveAssets();
        this.FinishTask();
    }


}
