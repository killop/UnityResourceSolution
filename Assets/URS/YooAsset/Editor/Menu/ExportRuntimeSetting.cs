using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace URS
{
    public class ExportRuntimeSetting : ScriptableWizard
    {

        [SerializeField, Tooltip("存放的resource目录")] public string SaveDiretoryPath = @"Assets/Packages/URS/Setting/Resources";
      
        [MenuItem("URS/ExportDefaultURSRuntimeSetting")]
        private static void Open()
        {
            var sw = DisplayWizard<ExportRuntimeSetting>(ObjectNames.NicifyVariableName(nameof(ExportRuntimeSetting)), "Export");
        }

        private void OnWizardCreate()
        {
            Build.ExportDefaultURSRuntimeSetting(SaveDiretoryPath);
        }
    }
}

