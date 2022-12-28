using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;
using URS;
using URS.Editor;

public class BuildTaskBuildPatch : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();

        VersionBuilder.BuildChannleVersionPatches(Build.GetChannelRoot());

        this.FinishTask();
    }


}
