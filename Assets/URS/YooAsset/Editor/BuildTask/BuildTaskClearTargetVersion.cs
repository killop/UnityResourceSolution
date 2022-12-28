using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using URS;
using MHLab.Patch.Core.IO;
using MHLab.Patch.Core.Utilities;
using Version = SemanticVersioning.SemanticVersion;
public class BuildTaskClearTargetVersion : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
        var path = (string)_context[CONTEXT_VERSION_DIRECTORY];
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        var versionRoot= Build.GetVersionRoot();

        if (Directory.Exists(versionRoot)) 
        {
            var di = new DirectoryInfo(versionRoot);
            foreach (DirectoryInfo subDirectory in di.GetDirectories())
            {
                var name = subDirectory.Name;
                var names = name.Split("---");
                var versionCode = names[0];
                if (versionCode == URSEditorUserSettings.instance.BuildVersionCode)
                {
                    Directory.Delete(subDirectory.FullName, true);
                }
            }
        }
        this.FinishTask();
    }
}
