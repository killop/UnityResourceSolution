using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildTaskComplierLua : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
       // ToLuaMenu.BuildLuaToResources();
        this.FinishTask();
    }
}
