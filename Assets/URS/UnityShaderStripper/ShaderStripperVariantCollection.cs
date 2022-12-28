#if UNITY_2018_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;
using System.Text;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace Sigtrap.Editors.ShaderStripper {
    /// <summary>
    /// Strips ALL shaders and variants except those in the supplied ShaderVariantCollection assets.
    /// Does not strip built-in shaders.
    /// </summary>
    [CreateAssetMenu(menuName="Sigtrap/Shader Stripper Variant Collection")]
    public class ShaderStripperVariantCollection : ShaderStripperBase, ISerializationCallbackReceiver {
		[SerializeField][Tooltip("Set a path like Assets/.../<name> (no extension) to merge whitelisted collections into a new collection asset.\nPath to a whitelisted collection (to overwrite) IS allowed.")]
		string _mergeToFile = null;
        [SerializeField][Tooltip("Only shader variants in these collections will NOT be stripped (except built-in shaders).")]
        List<ShaderVariantCollection> _whitelistedCollections;
		[SerializeField][HideInInspector]
		List<string> _collectionPaths;
        [SerializeField][Tooltip("Strip Hidden shaders. Be careful - shaders in Resources might get stripped.\nHidden shaders in collections will always have their variants stripped.")]
        bool _stripHidden = false;
		[SerializeField][Tooltip("Allow VR versions of variants in collection even when VR keywords not in collection.")]
		bool _allowVrVariants;
		[SerializeField][Tooltip("Allow GPU instanced versions of variants in collection even when instancing keywords not in collection.")]
		bool _allowInstancedVariants;

		[SerializeField][Tooltip("Shaders matching these names will be ignored (not stripped)")]
		StringMatch[] _ignoreShadersByName;
		[SerializeField][Tooltip("These passtypes will be ignored (not stripped)")]
		List<PassType> _ignorePassTypes;

		bool _valid = false;
		bool _dirty = false;

		#region Serialization
		// Automagically pick up new collections which have overwritten existing ones
		public void OnAfterDeserialize(){
			_dirty = true;
		}
		public void OnBeforeSerialize(){
			_dirty = true;
		}
		#endregion

        #if UNITY_EDITOR
		static readonly string[] VR_KEYWORDS = new string[]{
			"UNITY_SINGLE_PASS_STEREO", "STEREO_INSTANCING_ON", "STEREO_MULTIVIEW_ON"
		};
		static readonly string[] INSTANCING_KEYWORDS = new string[]{
			"INSTANCING_ON"
		};
		static List<string> _tempExcludes = new List<string>();
        Dictionary<Shader, Dictionary<PassType, List<ShaderVariantCollection.ShaderVariant>>> _variantsByShader = new Dictionary<Shader, Dictionary<PassType, List<ShaderVariantCollection.ShaderVariant>>>();

		void ReplaceOverwrittenCollections(){
			if (_collectionPaths != null){
				for (int i=0; i<_whitelistedCollections.Count; ++i){
					if (_whitelistedCollections[i] == null){
						_whitelistedCollections[i] = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(_collectionPaths[i]);
					}
				}
			} else {
				_collectionPaths = new List<string>();
			}
			_collectionPaths.Clear();
			foreach (var c in _whitelistedCollections){
				if (c != null){
					_collectionPaths.Add(AssetDatabase.GetAssetPath(c));
				}
			}
		}
		
        #region Parse YAML - thanks Unity for not having a simple ShaderVariantCollection.GetVariants or something
		static List<string> _tempCompareShaderVariants = new List<string>();
		bool ShaderVariantsEqual(ShaderVariantCollection.ShaderVariant a, ShaderVariantCollection.ShaderVariant b){
			if (a.shader != b.shader || a.passType != b.passType) return false;
			if ((a.keywords == null) != (b.keywords == null)) return false;
			if (a.keywords.Length != b.keywords.Length) return false;
			_tempCompareShaderVariants.Clear();
			_tempCompareShaderVariants.AddRange(a.keywords);
			for (int i=0; i<b.keywords.Length; ++i){
				if (!_tempCompareShaderVariants.Contains(b.keywords[i])){
					return false;
				}
			}
			return true;
		}
        public override void Initialize(){
			ReplaceOverwrittenCollections();
			
			_tempExcludes.Clear();
			if (_allowVrVariants){
				_tempExcludes.AddRange(VR_KEYWORDS);
			}
			if (_allowInstancedVariants){
				_tempExcludes.AddRange(INSTANCING_KEYWORDS);
			}

			foreach (var c in _whitelistedCollections){
				// Load asset YAML
				var file = new List<string>(System.IO.File.ReadAllLines(
					(Application.dataPath + AssetDatabase.GetAssetPath(c)).Replace("AssetsAssets","Assets")
				));

				#region Pre-process to get rid of mid-list line breaks
				var yaml = new List<string>();

				// Find shaders list
                int i = 0;
				for (; i<file.Count; ++i){
					if (YamlLineHasKey(file[i], "m_Shaders")) break;
				}

                // Process and fill
                int indent = 0;
				for (; i<file.Count; ++i){
					string f = file[i];
					int myIndent = GetYamlIndent(f);
					if (myIndent > indent){
						// If no "<key>: ", continuation of previous line
						if (!f.EndsWith(":") && !f.Contains(": ")){
							yaml[yaml.Count-1] += " " + f.Trim();
							continue;
						}
					}

					yaml.Add(f);
					indent = myIndent;
				}
                #endregion

				#region Iterate over shaders
				for (i=0; i<yaml.Count; ++i){
					string y = yaml[i];
					if (yaml[i].Contains("first:")){
						string guid = GetValueFromYaml(y, "guid");
						Shader s = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));

						// Move to variants contents (skip current line, "second:" and "variants:")
						i += 3;
						indent = GetYamlIndent(yaml[i]);
						var sv = new ShaderVariantCollection.ShaderVariant();
						for (; i<yaml.Count; ++i){
							y = yaml[i];

                            // If indent changes, variants have ended
							if (GetYamlIndent(y) != indent){
								// Outer loop will increment, so counteract
								i -= 1;
								break;
							}

							if (IsYamlLineNewEntry(y)) {
								// First entry will be a new entry but no variant info present yet, so skip
								// Builtin shaders will also be null
								if (sv.shader != null){	
                                    Dictionary<PassType, List<ShaderVariantCollection.ShaderVariant>> variantsByPass = null;
                                    if (!_variantsByShader.TryGetValue(sv.shader, out variantsByPass)){
                                        variantsByPass = new Dictionary<PassType, List<ShaderVariantCollection.ShaderVariant>>();
                                        _variantsByShader.Add(sv.shader, variantsByPass);
                                    }
                                    List<ShaderVariantCollection.ShaderVariant> variants = null;
                                    if (!variantsByPass.TryGetValue(sv.passType, out variants)){
                                        variants = new List<ShaderVariantCollection.ShaderVariant>();
                                        variantsByPass.Add(sv.passType, variants);
                                    }
									bool dupe = false;
									foreach (var existing in variants){
										if (ShaderVariantsEqual(existing, sv)){
											dupe = true;
											break;
										}
									}
									if (!dupe){
                                    	variants.Add(sv);
									}
								}
								sv = new ShaderVariantCollection.ShaderVariant();
								sv.shader = s;
							}

                            // Get contents
							if (YamlLineHasKey(y, "passType")){
								sv.passType = (PassType)int.Parse(GetValueFromYaml(y, "passType"));
							}
							if (YamlLineHasKey(y, "keywords")){
								sv.keywords = GetValuesFromYaml(y, "keywords", _tempExcludes);
							}
						}
						// Get final variant
						if (sv.shader != null){	
							Dictionary<PassType, List<ShaderVariantCollection.ShaderVariant>> variantsByPass = null;
							if (!_variantsByShader.TryGetValue(sv.shader, out variantsByPass)){
								variantsByPass = new Dictionary<PassType, List<ShaderVariantCollection.ShaderVariant>>();
								_variantsByShader.Add(sv.shader, variantsByPass);
							}
							List<ShaderVariantCollection.ShaderVariant> variants = null;
							if (!variantsByPass.TryGetValue(sv.passType, out variants)){
								variants = new List<ShaderVariantCollection.ShaderVariant>();
								variantsByPass.Add(sv.passType, variants);
							}
							bool dupe = false;
							foreach (var existing in variants){
								if (ShaderVariantsEqual(existing, sv)){
									dupe = true;
									break;
								}
							}
							if (!dupe){
								variants.Add(sv);
							}
						}
					}
                }
                #endregion

                LogMessage(this, "Parsing ShaderVariantCollection "+c.name);
                // Loop over shaders
				foreach (var s in _variantsByShader){
					string log = "Shader: " + s.Key.name;
                    // Loop over passes
                    foreach (var p in s.Value){
                        log += string.Format("\n   Pass: ({1:00}){0}", p.Key, (int)p.Key);
                        // Loop over variants
                        for (int v=0; v<p.Value.Count; ++v){
                            log += string.Format("\n      Variant [{0}]:\t", v);
                            // Loop over keywords
							var ks = p.Value[v].keywords;
							if (ks != null && ks.Length != 0){
								bool first = true;
								foreach (var k in ks){
									if (!first) log += ", ";
									log += k;
									first = false;
								}
							} else {
								log += "<no keywords>";
							}
                        }
                    }
					LogMessage(this, log);
				}
			}

			// Merge collections
			if (!string.IsNullOrEmpty(_mergeToFile) && _whitelistedCollections.Count > 1){
				var svc = new ShaderVariantCollection();
				foreach (var a in _variantsByShader){
					foreach (var b in a.Value){
						foreach (var s in b.Value){
							svc.Add(s);
						}
					}
				}
				try {
					string file = _mergeToFile+".shadervariants";
					string log = string.Format("Merged following ShaderVariantCollections into {0}:\n", file);
					foreach (var s in _whitelistedCollections){
						log += "    "+s.name+"\n";
					}
					
					if (AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(file) != null){
						AssetDatabase.DeleteAsset(file);
					}
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
					AssetDatabase.CreateAsset(svc, file);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();

					Debug.Log(log, AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(file));
				} catch (System.Exception ex){
					Debug.LogError("Error merging ShaderVariantCollections. Exception follows.");
					throw;
				}
			}

			_valid = (_variantsByShader != null && _variantsByShader.Count > 0);
		}
        int GetYamlIndent(string line){
			for (int i=0; i<line.Length; ++i){
				if (line[i] != ' ' && line[i] != '-') return i;
			}
			return 0;
		}
		bool IsYamlLineNewEntry(string line){
			foreach (var c in line){
				// If a dash (before a not-space appears) this is a new entry
				if (c == '-') return true;
				// If not a dash, must be a space or indent has ended
				if (c != ' ') return false;
			}
			return false;
		}
		int GetIndexOfYamlValue(string line, string key){
			int i = line.IndexOf(key+":", System.StringComparison.Ordinal);
			if (i >= 0){
				// Skip to value
				i += key.Length + 2;
			}
			return i;
		}
		bool YamlLineHasKey(string line, string key){
			return GetIndexOfYamlValue(line, key) >= 0;
		}
		string GetValueFromYaml(string line, string key){
			int i = GetIndexOfYamlValue(line, key);
			if (i < 0){
				return "";
				//throw new System.Exception((string.Format("Value not found for key {0} in YAML line {1}", key, line)));
			}
			StringBuilder sb = new StringBuilder();
			for (; i<line.Length; ++i){
				char c = line[i];
				if (c == ',' || c == ' ') break;
				sb.Append(c);
			}
			return sb.ToString();
		}
		string[] GetValuesFromYaml(string line, string key, List<string> exclude=null){
			int i = GetIndexOfYamlValue(line, key);
			if (i < 0){
				throw new System.Exception((string.Format("Value not found for key {0} in YAML line {1}", key, line)));
			}
			List<string> result = new List<string>();
			StringBuilder sb = new StringBuilder();
			for (; i<line.Length; ++i){
				char c = line[i];
				bool end = false;
				bool brk = false;
				if (c == ','){
					// Comma delimits keys
					// Add the current entry and stop parsing
					end = brk = true;
				}
				if (c == ' '){
					// Space delimits entries
					// Add current entry, move to next
					end = true;
				}
				if (end){
					result.Add(sb.ToString());
					sb.Length = 0;
					if (brk) break;
				} else {
					sb.Append(c);
				}
			}
			// Catch last entry if line ends
			if (sb.Length > 0){
				var s = sb.ToString();
				if (exclude==null || exclude.Count==0 || !exclude.Contains(s)){
					result.Add(sb.ToString());
				}
			}
			return result.ToArray();
		}
        #endregion

        static List<string> _tempRequestedKeywordsToMatch = new List<string>();
        static List<string> _tempRequestedKeywordsToMatchCached = new List<string>();
		static List<string> _tempCollectedKeywordsSorted = new List<string>();
        protected override bool StripCustom(Shader shader, ShaderSnippetData passData, IList<ShaderCompilerData> variantData){
			// Don't strip anything if no collections present
			if (!_valid) return true;
            // Always ignore built-in shaders
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(shader))) return true;
			// Ignore shaders by name
			foreach (var s in _ignoreShadersByName){
				if (s.Evaluate(shader.name)) return true;
			}
			// Ignore passes by type
			if (_ignorePassTypes.Contains(passData.passType)) return true;

            // Try to match shader
            Dictionary<PassType, List<ShaderVariantCollection.ShaderVariant>> collectedVariantsByPass = null;
            if (_variantsByShader.TryGetValue(shader, out collectedVariantsByPass)){
                // Try to match pass
                List<ShaderVariantCollection.ShaderVariant> collectedPassVariants = null;
                if (collectedVariantsByPass.TryGetValue(passData.passType, out collectedPassVariants)){
                    // Loop over supplied variants
                    // Iterate backwards over supplied variants to allow index-based removal
                    int count = variantData.Count;
                    for (int i=count-1; i>=0; --i){

                        // Fill temp buffer to fill OTHER temp buffer each time SIGH
                        _tempRequestedKeywordsToMatchCached.Clear();
						var sks = variantData[i].shaderKeywordSet.GetShaderKeywords();
                        foreach (var sk in sks){
							string n = GetKeywordName(sk);
							bool add = true;
							// Don't look for VR or instanced variants
							if (_tempExcludes.Count > 0){
								if (_tempExcludes.Contains(n)){
									add = false;
								}
							}
							if (add){
                            	_tempRequestedKeywordsToMatchCached.Add(n);
							}
                        }
						bool variantMatched = false;

                        // Loop over cached variants
                        foreach (var collectedVariant in collectedPassVariants){
                            // Must match ALL keywords
                            _tempRequestedKeywordsToMatch.Clear();
                            _tempRequestedKeywordsToMatch.AddRange(_tempRequestedKeywordsToMatchCached);

                            // Early out (no match) if keyword counts don't match
                            if (_tempRequestedKeywordsToMatch.Count != collectedVariant.keywords.Length) continue;

                            // Early out (match) if both have no keywords
                            if (_tempRequestedKeywordsToMatch.Count == 0 && collectedVariant.keywords.Length == 0){
                                variantMatched = true;
                                break;
                            }

                            // Check all keywords
							_tempCollectedKeywordsSorted.Clear();
							_tempCollectedKeywordsSorted.AddRange(collectedVariant.keywords);
							_tempCollectedKeywordsSorted.Sort((a,b)=>{return string.CompareOrdinal(a,b);});
                            foreach (var k in _tempCollectedKeywordsSorted){
                                bool keywordMatched = _tempRequestedKeywordsToMatch.Remove(k);
                                if (!keywordMatched) break;
                            }
                            // If all keywords removed, all keywords matched
                            if (_tempRequestedKeywordsToMatch.Count == 0){
                                variantMatched = true;
								break;
                            }
                        }

                        // Strip this variant
                        if (!variantMatched){
                            LogRemoval(this, shader, passData, i, count, variantData[i]);
                            variantData.RemoveAt(i);
                        }
                    }
                } else {
                    // If not matched pass, clear all variants
                    LogRemoval(this, shader, passData);
                    variantData.Clear();
                }
            } else {
                // If not matched shader, clear all
                // Check if shader is hidden
                if (_stripHidden || !shader.name.StartsWith("Hidden/")){
                    LogRemoval(this, shader, passData);
                    variantData.Clear();
                }
            }

            return true;
        }
        
        public override string description {get {return "Strips ALL (non-built-in) shaders not in selected ShaderVariantCollection assets.";}}
        public override string help {
            get {
                string result = _stripHidden ? "WILL strip Hidden shaders." : "Will NOT strip Hidden shaders.";
                result += " Will NOT strip built-in shaders. Use other strippers to remove these.";
                return result;
            }
        }

        protected override bool _checkShader {get {return false;}}
        protected override bool _checkPass {get {return false;}}
        protected override bool _checkVariants {get {return false;}}

		public override void OnGUI(){
			if (_dirty){
				ReplaceOverwrittenCollections();
			}
			_dirty = false;
		}
        #endif
    }
}
#endif