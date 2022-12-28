using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildTaskBeforeShaderComplier : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        Sigtrap.Editors.ShaderStripper.ShaderStripperEditor.OnPreprocessBuild(null);
        this.FinishTask();
    }
}
