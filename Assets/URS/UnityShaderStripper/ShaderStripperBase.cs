#if UNITY_2018_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace Sigtrap.Editors.ShaderStripper {
	public class ShaderLog {
		public List<string> log = new List<string>();
		public string logName {get; private set;}

		public int Count {get {return log.Count;}}

		public ShaderLog(string logName){
			this.logName = logName;
		}

		public void Add(string s){
			log.Add(s);
		}
		public void Clear(){
			log.Clear();
		}
		public void Insert(int index, string s){
			log.Insert(index, s);
		}
		public bool Contains(string s){
			return log.Contains(s);
		}
		public string[] ToArray(){
			return log.ToArray();
		}
	}

	/// <summary>
	/// Base class for stripping shaders.
	/// </summary>
	public abstract class ShaderStripperBase : ScriptableObject {
		[System.Serializable]
		protected class StringMatch {
			public enum MatchType {EQUALS, CONTAINS, STARTSWITH, ENDSWITH}
			public MatchType matchType;
			public string namePattern;
			public bool caseInsensitive;
			public bool Evaluate(string name){
				if (string.IsNullOrEmpty(namePattern)) return false;
				string n = caseInsensitive ? name.ToLower() : name;
				string p = caseInsensitive ? namePattern.ToLower() : namePattern;
				switch (matchType){
					case MatchType.EQUALS:
						return n == p;
					case MatchType.CONTAINS:
						return n.Contains(p);
					case MatchType.STARTSWITH:
						return n.StartsWith(p);
					case MatchType.ENDSWITH:
						return n.EndsWith(p);
				}
				return false;
			}
		}

		#region Static
		#if UNITY_EDITOR
		static bool _deepLogs = false;
		static List<string> _log = new List<string>();
		public static string GetKeywordName(ShaderKeyword k){
			#if UNITY_2018_3_OR_NEWER
			return k.GetKeywordName();
			#else
			return = k.GetName();
			#endif
		}
		public static void OnPreBuild(bool deepLogs){
			_log.Clear();
			_deepLogs = deepLogs;
		}
		public static void OnPostBuild(string logFolderPath, string header, params ShaderLog[] logs){
			if (!string.IsNullOrEmpty(logFolderPath)){
				string logPath = logFolderPath;
				if (!logPath.EndsWith("/") && !logPath.EndsWith("\\")){
					logPath += "/";
				}
				
				string date = System.DateTime.Now.ToString("yyyy-MM-dd");
				string strippedLogFile = string.Format(
					"{0}ShaderStripperLog_{1}.txt", 
					logPath, date						
				);
				_log.Insert(0, header);
                var directoryPath = System.IO.Path.GetDirectoryName(strippedLogFile);
                if (!System.IO.Directory.Exists(directoryPath)) {
                    System.IO.Directory.CreateDirectory(directoryPath);
                }
				System.IO.File.WriteAllLines(strippedLogFile, _log.ToArray());

				foreach (var l in logs){
					if (l != null && l.Count > 0){
						l.Insert(0, header);
						string logFile = string.Format(
							"{0}ShaderStripperLog_{1}_{2}.txt", 
							logPath, date, l.logName
						);
						System.IO.File.WriteAllLines(logFile, l.ToArray());
					}
				}
				Debug.Log("ShaderStripper logs created at "+logPath);
			}
			_log.Clear();
		}
		static protected void LogRemoval(ShaderStripperBase stripper, Shader shader, ShaderSnippetData pass){
			if (!stripper._logOutput) return;
			string log = string.Format(
				"Stripping shader [{0}] pass type [{1}]\n\tShaderStripper: {2}",
				shader.name, pass.passType, stripper.name
			);
			_log.Add(log);
		}
		static protected void LogRemoval(ShaderStripperBase stripper, Shader shader, ShaderSnippetData pass, int variantIndex, int variantCount, ShaderCompilerData variant){
			if (!stripper._logOutput) return;
			string log = null;
			if (_deepLogs){
				log = string.Format(
					"Stripping shader [{0}] pass type [{1}] variant [{2}/{3}] [{4}]\n\tShaderStripper: {5}\n\tKeywords:",
					shader.name, pass.passType, variantIndex, variantCount-1, variant.graphicsTier, stripper.name
				);
				var ks = variant.shaderKeywordSet.GetShaderKeywords();
				
				if (ks != null && ks.Length > 0){
					bool first = true;
					foreach (var k in variant.shaderKeywordSet.GetShaderKeywords()){
						if (!first) log += ",";
						log += " " + GetKeywordName(k);
						first = false;
					}
				} else {
					log += " <no keywords>";
				}
			} else {
				log = string.Format(
					"Stripping shader [{0}] pass type [{1}] variant [{2}/{3}]\n\tShaderStripper: {4}",
					shader.name, pass.passType, variantIndex, variantCount-1, stripper.name
				);
			}
			_log.Add(log);
		}
		static protected void LogMessage(ShaderStripperBase stripper, string message, MessageType type=MessageType.None){
			if (!stripper._logOutput) return;
			string log = string.Format("ShaderStripper {0}: {1}", stripper.name, message);
			switch (type){
				case MessageType.Info:
				case MessageType.None:
					_log.Add(log);
					break;
				case MessageType.Warning:
					log = "WARN: " + log;
					_log.Add(log);
					break;
				case MessageType.Error:
					log = "ERR: " + log;
					_log.Add(log);
					break;
			}
		}
		#endif
		#endregion

		#region Serialized
		[SerializeField, HideInInspector]
		bool _expanded = true;
		[SerializeField, HideInInspector]
		int _order = -1;

		[SerializeField]
		bool _active = false;
		public bool active {get {return _active;}}

		[SerializeField]
		string _notes;

		[SerializeField]
		protected bool _logOutput;
		#endregion

		#region Instance
		#if UNITY_EDITOR
		public virtual string description {get {return null;}}
		public virtual string help {get {return null;}}
		/// <summary>
		/// Does this ShaderStripper check <see cref="Shader"> data (e.g. shader name)?
		/// Must also override MatchShader().
		/// </summary>
		protected abstract bool _checkShader {get;}
		/// <summary>
		/// Does this ShaderStripper check shader pass data (e.g. pass type)?
		/// Must also override MatchPass().
		/// </summary>
		protected abstract bool _checkPass {get;}
		/// <summary>
		/// Does this ShaderStripper check shader variant data (e.g. keywords)?
		/// Must also override MatchVariant().
		/// </summary>
		protected abstract bool _checkVariants {get;}

		/// <summary>
		/// Selectively strip shader variants.
		/// <para />Returns number of variants stripped.
		/// <para />If StripCustom() is overridden and returns true, ONLY runs StripCustom.
		/// <para />Otherwise:
		/// <para />If (_checkShader) runs MatchShader().
		/// <para />If (_checkPass) then runs MatchPass().
		/// <para />If (_checkVariants) then runs MatchVariant().
		/// <para />e.g. if (_checkShader &amp;&amp; MatchShader() &amp;&amp; !_checkPass &amp;&amp; !_checkVariants) strip shader.
		/// <para />e.g. if (_checkShader &amp;&amp; MatchShader() &amp;&amp; _checkPass &amp;&amp; MatchPass() &amp;&amp; !_checkVariants) strip this pass.
		/// <para />e.g. if (!_checkShader &amp;&amp; _checkPass &amp;&amp; MatchPass() &amp;&amp; _checkVariants &amp;&amp; MatchVariant()) strip this variant.
		/// </summary>
		public int Strip(Shader shader, ShaderSnippetData passData, IList<ShaderCompilerData> variantData){
			if (!active) return 0;

			int initialVariants = variantData.Count;
			bool skipMatch = StripCustom(shader, passData, variantData);

			if (!skipMatch){
				if (!_checkShader && !_checkPass && !_checkVariants){
					LogMessage(this, "Checks nothing; skipping.", MessageType.Warning);
					return 0;
				}

				// If match shader OR match pass, strip all variants
				// If match variant, only strip one variant
				
				bool matchedShader = true;
				if (_checkShader){
					matchedShader = MatchShader(shader);
				}

				if (matchedShader){
					bool matchedPass = true;
					if (_checkPass){
						matchedPass = MatchPass(passData);
					}

					if (matchedPass){
						if (_checkVariants){
							// Iterate backwards to allow index-based removal
							int c = variantData.Count;
							for (int i=variantData.Count-1; i>=0; --i){
								if (MatchVariant(variantData[i])){
									LogRemoval(this, shader, passData, i, c, variantData[i]);
									variantData.RemoveAt(i);
								}
							}
						} else {
							variantData.Clear();
							LogRemoval(this, shader, passData);
						}
					}
				}
			}

			return initialVariants - variantData.Count;
		}

		/// <summary>
		/// Override to get a callback before the build starts.
		/// <summary>
		public virtual void Initialize(){}
		
		/// <summary>
		/// Override to perform completely custom stripping.
		/// In most cases, override Match methods instead.
		/// Return true to skip Match-based stripping.
		/// </summary>
		protected virtual bool StripCustom(Shader shader, ShaderSnippetData passData, IList<ShaderCompilerData> variantData){
			return false;
		}
		/// <summary>
		/// Override to strip based on shader data.
		/// Return true to strip this shader, OR move to next match stage if (_checkPass || _checkVariants)
		/// Must override <see cref="_checkShader"> to return <see cref="true">.
		/// </summary>
		protected virtual bool MatchShader(Shader shader){
			throw new System.NotImplementedException("If _checkShader is true, must override MatchShader()");
		}
		/// <summary>
		/// Override to strip based on pass data.
		/// Return true to strip this pass, OR move to variant matching if (_checkVariants)
		/// Must override <see cref="_checkPass"> to return <see cref="true">.
		/// </summary>
		protected virtual bool MatchPass(ShaderSnippetData passData){
			throw new System.NotImplementedException("If _checkPass is true, must override MatchPass()");
		}
		/// <summary>
		/// Override to strip based on variant data.
		/// Return true to strip this variant.
		/// Must override <see cref="_checkVariants"> to return <see cref="true">.
		/// </summary>
		protected virtual bool MatchVariant(ShaderCompilerData variantData){
			throw new System.NotImplementedException("If _checkVariants is true, must override MatchVariant()");
		}
		public virtual void OnGUI(){}
		#endif
		#endregion
	}
}
#endif