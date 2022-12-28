using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuilTaskRecoverAssetDatabaseAutoSave : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        AssetDatabase.AllowAutoRefresh();
        this.FinishTask();
    }
}
