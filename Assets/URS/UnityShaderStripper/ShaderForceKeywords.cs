#if UNITY_2018_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;
using static Sigtrap.Editors.ShaderStripper.ShaderForceKeywords;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace Sigtrap.Editors.ShaderStripper {
    /// <summary>
	/// Strips shaders by shader asset path.
	/// </summary>
	[CreateAssetMenu(menuName="Sigtrap/Shader Force Keywords")]
    public class ShaderForceKeywords : ShaderStripperBase {
        [System.Serializable]
        public struct ForceBuiltin {
            [SerializeField]
            public BuiltinShaderDefine defineToMatch;
            [SerializeField]
            public BuiltinShaderDefine defineToForce;
            [Tooltip("If true, check Define To Match is NOT enabled")]
            public bool invertMatch;
            [SerializeField]
            [Tooltip("If true, DISABLE Define To Force")]
            public bool invertForce;
        }
        [System.Serializable]
        public struct ForceKeyword {
            [SerializeField]
            public string keywordToMatch;
            [SerializeField]
            public string keywordToForce;
            [Tooltip("If true, check Keyword To Match is NOT enabled")]
            [SerializeField]
            public bool invertMatch;
            [SerializeField]
            [Tooltip("If true, DISABLE Keyword To Force")]
            public bool invertForce;
        }
        [SerializeField]
        public   ForceBuiltin[] _forceBuiltins;
        [SerializeField]
        public string[] _forceKeywords;

        #if UNITY_EDITOR
        protected override bool _checkShader {get {return false;}}
        protected override bool _checkPass {get {return false;}}
        protected override bool _checkVariants {get {return false;}}
        protected override bool StripCustom(Shader shader, ShaderSnippetData passData, IList<ShaderCompilerData> variantData){
            foreach (var d in variantData){
                // Builtins
                foreach (var b in _forceBuiltins){
                    bool matched = d.platformKeywordSet.IsEnabled(b.defineToMatch);
                    if (b.invertMatch) matched = !matched;
                    if (matched){
                        if (b.invertForce){
                            d.platformKeywordSet.Disable(b.defineToForce);
                        } else {
                            d.platformKeywordSet.Enable(b.defineToForce);
                        }
                    }
                }
            }
            var c = variantData.Count;
            for (int i = variantData.Count - 1; i >= 0; --i)
            {
                if (IsKeywordMatch(variantData[i]))
                {
                    LogRemoval(this, shader, passData, i, c, variantData[i]);
                    variantData.RemoveAt(i);
                }
            }
            return true;
        }
        public bool IsKeywordMatch(ShaderCompilerData variantData)
        {
            foreach (var k in _forceKeywords)
            {
                var sk = new ShaderKeyword(k);
                bool matched = variantData.shaderKeywordSet.IsEnabled(sk);
                if (matched) return true;
            }
            return false;
        }
#endif
    }
}
#endif