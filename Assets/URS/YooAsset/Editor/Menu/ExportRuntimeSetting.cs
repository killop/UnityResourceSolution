using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace URS
{
    public class ExportRuntimeSetting : ScriptableWizard
    {

        [SerializeField, Tooltip("´æ·ÅµÄresourceÄ¿Â¼")] public string SaveDiretoryPath = @"Assets/Packages/URS/Setting/Resources";
      
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

