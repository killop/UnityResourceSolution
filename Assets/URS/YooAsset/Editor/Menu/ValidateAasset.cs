using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace URS
{
    public class ValidateAasset : ScriptableWizard
    {
        [SerializeField, Tooltip("检查并且修复材质球")] public bool Material= false;
        [SerializeField, Tooltip("检查并且修复模型的导入")] public bool Model = false;
        [SerializeField, Tooltip("检查并且修复粒子系统")] public bool ParticleSystem = false;
        [SerializeField, Tooltip("检查并且修复动画系统")] public bool Animation= false;

        [MenuItem("URS/ValidateAsset", false, 104)]
        private static void Open()
        {
            var sw = DisplayWizard<ValidateAasset>(ObjectNames.NicifyVariableName(nameof(ValidateAasset)), "Validate");
        }

        private void OnWizardCreate()
        {
            Build.ValidateAsset(Material, Model, ParticleSystem, Animation);
        }
    }
}

