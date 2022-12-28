using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildTaskAfterShaderComplier : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        Sigtrap.Editors.ShaderStripper.ShaderStripperEditor.OnPostprocessBuild(null);
        this.FinishTask();
    }
}
