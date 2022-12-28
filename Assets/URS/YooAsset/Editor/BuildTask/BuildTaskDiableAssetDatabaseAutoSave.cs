using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuildTaskDiableDatabaseAutoSave : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        AssetDatabase.DisallowAutoRefresh();
        this.FinishTask();
    }
}
