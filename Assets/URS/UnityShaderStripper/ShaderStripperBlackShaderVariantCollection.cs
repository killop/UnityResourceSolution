#if UNITY_2018_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif
using System.IO;
using System.Linq;
namespace Sigtrap.Editors.ShaderStripper
{
    /// <summary>
	/// Strips shaders by shader asset path.
	/// </summary>
	[CreateAssetMenu(menuName = "Sigtrap/Shader Stripper BlackShaderVariantCollection")]
    public class ShaderStripperBlackShaderVariantCollection : ShaderStripperBase
    {
       // public const string BLACK_SHADER_VARIANT_STRIPPER_SAVE_PATH = "Assets/GameResources/ShaderVarians/BlackShaderVariantCollection.shadervariants";

        private ShaderVariantCollection _blackShaderVariantCollection = null;

#if UNITY_EDITOR
        protected override bool _checkShader { get { return false; } }
        protected override bool _checkPass { get { return false; } }
        protected override bool _checkVariants { get { return false; } }
        protected override bool StripCustom(Shader shader, ShaderSnippetData passData, IList<ShaderCompilerData> variantData)
        {
            var c = variantData.Count;
            for (int i = variantData.Count - 1; i >= 0; --i)
            {
                if (IsKeywordMatch(shader, passData, variantData[i]))
                {
                    LogRemoval(this, shader, passData, i, c, variantData[i]);
                    variantData.RemoveAt(i);
                }
            }
            return true;
        }

        public string GetKeywordString(ref ShaderCompilerData variantData)
        {
            var keywords = variantData.shaderKeywordSet.GetShaderKeywords();
            var result = "";
            foreach (var keyword in keywords)
            {
                result += keyword.name;
            }
            return result;
        }
        public bool IsKeywordMatch(Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            if (!File.Exists( URS.URSShaderVariantConstant.BLACK_SHADER_VARIANT_STRIPPER_SAVE_PATH))
            {
                return false;
            }
            if (_blackShaderVariantCollection == null || _blackShaderVariantCollection.variantCount == 0 || _blackShaderVariantCollection.shaderCount == 0)
            {
                Debug.LogError($"load  ShaderVariant,shaderName {shader.name}  passType {passData.passType} keyword: {GetKeywordString(ref variantData)} ");
                _blackShaderVariantCollection =AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(URS.URSShaderVariantConstant.BLACK_SHADER_VARIANT_STRIPPER_SAVE_PATH);
            }
            if (_blackShaderVariantCollection != null && _blackShaderVariantCollection.variantCount > 0)
            {
                ShaderVariantCollection.ShaderVariant shaderVariant = new ShaderVariantCollection.ShaderVariant();
                shaderVariant.shader = shader;
                shaderVariant.passType = passData.passType;
                shaderVariant.keywords = variantData.shaderKeywordSet.GetShaderKeywords().Select(keyword => keyword.name).ToArray();
                bool remove = _blackShaderVariantCollection.Contains(shaderVariant);
                if (remove)
                {
                    Debug.LogError($"success Move  ShaderVariant,shaderName {shader.name}  passType {passData.passType} keyword: {GetKeywordString(ref variantData)} ");
                }
                return remove;
            }
                
            return false;
        }
#endif
    }
}
#endif