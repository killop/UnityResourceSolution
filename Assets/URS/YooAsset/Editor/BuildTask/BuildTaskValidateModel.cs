using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Rendering;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NinjaBeats;

public class BuildTaskValidateModel : BuildTask
{

    public override void BeginTask()
    {
        base.BeginTask();
        var assetInfos = this.GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);

        foreach (var assetPath in assetInfos.Keys)
        {
            if (Path.GetExtension(assetPath) == ".fbx")
            {
                var m = ModelImporter.GetAtPath(assetPath) as ModelImporter;
                bool isReadable = assetPath.Contains("/VFX/");
                if (m.isReadable!= isReadable)
                {
                    Debug.LogWarning($"纠正{assetPath} mesh  isReadable 为 {isReadable}");
                    m.isReadable = isReadable;
                }
            }
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        this.FinishTask();
    }

}
