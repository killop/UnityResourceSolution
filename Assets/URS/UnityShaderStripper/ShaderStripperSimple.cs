#if UNITY_2018_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace Sigtrap.Editors.ShaderStripper {
	/// <summary>
	/// Strips shaders by shader name and pass name.
	/// </summary>
	[CreateAssetMenu(menuName="Sigtrap/Shader Stripper Simple")]
	public class ShaderStripperSimple : ShaderStripperBase {
		[SerializeField][Tooltip("If shader name matches ANY of these, will be considered for stripping.")]
		List<StringMatch> _blacklistedShaderNames;
		[SerializeField][Tooltip("If pass type matches ANY of these, will be considerered for stripping.")]
		List<PassType> _blacklistedPassTypes;
		[SerializeField][Tooltip("If variant uses ANY of these keywords, will be considered for stripping.")]
		List<string> _blacklistedKeywords;

		#if UNITY_EDITOR
		public override string description {get {return "Strips shaders based on shader names, pass types and keywords.";}}
		public override string help {
			get {
				if (!(_checkShader || _checkPass || _checkVariants)){
					return "Add shader name(s) and/or pass name(s) and/or keywords to blacklists.";
				}
				
				string result = _checkVariants ? "Variant stripped IF " : "ALL variants stripped IF ";
				if (_checkShader){
					result += "(shader matches any blacklisted name)";
					if (_checkPass || _checkVariants) result += " AND ";
				}
				if (_checkPass){
					result += "(pass matches any blacklisted type)";
					if (_checkVariants) result += "AND ";
				}
				if (_checkVariants){
					result += "(variant uses any blacklisted keyword)";
				}
				result += ".";

				return result;
			}
		}
		protected override bool _checkShader {get {return _blacklistedShaderNames.Count > 0;}}
		protected override bool _checkPass {get {return _blacklistedPassTypes.Count > 0;}}
		protected override bool _checkVariants {get {return _blacklistedKeywords.Count > 0;}}

		protected override bool MatchShader(Shader shader){
			for (int i=0; i<_blacklistedShaderNames.Count; ++i){
				var n = _blacklistedShaderNames[i];
				if (n.Evaluate(shader.name)){
					return true;
				}
			}
			return false;
		}
		protected override bool MatchPass(ShaderSnippetData passData){
			for (int i=0; i<_blacklistedPassTypes.Count; ++i){
				if (passData.passType == _blacklistedPassTypes[i]) return true;
			}
			return false;
		}
		protected override bool MatchVariant(ShaderCompilerData variantData){
			for (int i=0; i<_blacklistedKeywords.Count; ++i){
				var s = _blacklistedKeywords[i];
				var key = new ShaderKeyword(s);
				if (variantData.shaderKeywordSet.IsEnabled(key)){
					return true;
				}
			}
			return false;
		}
		#endif
	}
}
#endif