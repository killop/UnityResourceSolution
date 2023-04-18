
using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;
using URS;
using URS.Editor;

public class BuildTaskAutoAppVersionRouter : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();

        var versionRootDirectory = (string)_context[CONTEXT_VERSION_ROOT_DIRECTORY];
        var channelTargetVersion = (string)_context[CONTEXT_CHANNEL_TARGET_VERSION];
        VersionBuilder.BuildAutoAppVersionRouter(versionRootDirectory, channelTargetVersion);

        this.FinishTask();
    }


}
