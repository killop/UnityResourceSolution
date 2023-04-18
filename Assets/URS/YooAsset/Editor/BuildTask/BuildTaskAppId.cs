using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;

public class BuildTaskChannelId : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        var channelFilePath = $"{YooAsset.AssetPathHelper.GetBuildInChannelFilePath()}" ;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(channelFilePath));
        if (File.Exists(channelFilePath))
        {
            File.Delete(channelFilePath);
        }
        var channel = GetData<string>(CONTEXT_CHANNEL);
        File.WriteAllText(channelFilePath, channel);
        UnityEditor.AssetDatabase.Refresh();
        UnityEditor.AssetDatabase.SaveAssets();
        this.FinishTask();
    }


}
