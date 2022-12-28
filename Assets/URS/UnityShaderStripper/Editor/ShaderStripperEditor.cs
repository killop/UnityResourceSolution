#if UNITY_2018_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using System.Linq;

namespace Sigtrap.Editors.ShaderStripper {
    [UnityEditor.Build.Pipeline.Utilities.VersionedCallback(2)]
	public class ShaderStripperEditor : EditorWindow, IPreprocessShaders{
		public const string KEY_LOG = "ShaderStripperLogPath";
		public const string KEY_ENABLE = "ShaderStripperGlobalEnable";
		public const string KEY_DEEP_LOG = "ShaderStripperDeepLog";

		[MenuItem("Tools/Sigtrap/Shader Stripper")]
		public static void Launch(){
			if (_i == null){
				_i = ScriptableObject.CreateInstance<ShaderStripperEditor>();
			}
			_i.Show();
		}
		static ShaderStripperEditor _i;

		static bool _enabled;
		static bool _deepLogs;
		static string _logPath;
		static List<ShaderStripperBase> _strippers = new List<ShaderStripperBase>();
		static System.Diagnostics.Stopwatch _swStrip = new System.Diagnostics.Stopwatch();
		static System.Diagnostics.Stopwatch _swBuild = new System.Diagnostics.Stopwatch();
		
		public int callbackOrder {get {return 0;}}
		Vector2 _scroll;
		static ShaderLog _keptLog = new ShaderLog("SHADERS-KEPT");
        static ShaderLog _allKeywords = new ShaderLog("KEYWORDS");
        static ShaderLog _keptKeywords = new ShaderLog("KEYWORDS-KEPT");
        static ShaderLog _allPlatformKeywordNames = new ShaderLog("PLATFORM-KEYWORDS");
        static ShaderLog _keptPlatformKeywordNames = new ShaderLog("PLATFORM-KEYWORDS-KEPT");
        static List<BuiltinShaderDefine> _allPlatformKeywords = new List<BuiltinShaderDefine>();
        static List<BuiltinShaderDefine> _keptPlatformKeywords = new List<BuiltinShaderDefine>();
        static int _rawCount, _keptCount;

		#region GUI
		static bool GetEnabled(){
			if (EditorPrefs.HasKey(KEY_ENABLE)){
				return EditorPrefs.GetBool(KEY_ENABLE);
			} else {
				EditorPrefs.SetBool(KEY_ENABLE, true);
				return true;
			}
		}

		void OnEnable(){
			titleContent = new GUIContent("Shader Stripper");
			RefreshSettings();
			_logPath = EditorPrefs.GetString(KEY_LOG);
			_enabled = GetEnabled();
			_deepLogs = EditorPrefs.GetBool(KEY_DEEP_LOG);
		}
		void OnGUI(){
			Color gbc = GUI.backgroundColor;

			EditorGUILayout.Space();
			if (!_enabled){
				GUI.backgroundColor = Color.magenta;
			}
			EditorGUILayout.BeginVertical(EditorStyles.helpBox); {
				GUI.backgroundColor = gbc;

				// Title
				EditorGUILayout.BeginHorizontal(); {
					EditorGUILayout.LabelField(new GUIContent("Shader Stripping","Any checked settings are applied at build time."), EditorStyles.largeLabel, GUILayout.Height(25));
					GUILayout.FlexibleSpace();
					
					GUI.backgroundColor = Color.blue;
					if (GUILayout.Button("Refresh Settings", GUILayout.Width(125))){
						RefreshSettings();
					}
					GUI.backgroundColor = gbc;
				} EditorGUILayout.EndHorizontal();

				// Toggle stripping
				EditorGUI.BeginChangeCheck(); {
					_enabled = EditorGUILayout.ToggleLeft("Enable Stripping", _enabled);
				} if (EditorGUI.EndChangeCheck()){
					EditorPrefs.SetBool(KEY_ENABLE, _enabled);
					Repaint();
				}

				// Log folder
				EditorGUILayout.Space();
				EditorGUI.BeginChangeCheck(); {
					EditorGUILayout.BeginHorizontal(); {
						_logPath = EditorGUILayout.TextField("Log output file folder", _logPath);
						if (GUILayout.Button("...", GUILayout.Width(25))){
							string path = EditorUtility.OpenFolderPanel("Select log output folder", _logPath, "");
							if (!string.IsNullOrEmpty(path)){
								_logPath = path;
							}
						}
					} EditorGUILayout.EndHorizontal();
					_deepLogs = EditorGUILayout.ToggleLeft("Deep logs", _deepLogs);
				} if (EditorGUI.EndChangeCheck()){
					EditorPrefs.SetString(KEY_LOG, _logPath);
					EditorPrefs.SetBool(KEY_DEEP_LOG, _deepLogs);
					Repaint();
				}
				
				// Strippers
				EditorGUILayout.Space();
				bool reSort = false;
				_scroll = EditorGUILayout.BeginScrollView(_scroll, EditorStyles.helpBox); {
					for (int i=0; i<_strippers.Count; ++i){
						var s = _strippers[i];
						if (s == null){
							RefreshSettings();
							break;
						}
						var so = new SerializedObject(s);
						var active = so.FindProperty("_active");
						GUI.backgroundColor = Color.Lerp(Color.grey, Color.red, active.boolValue ? 0 : 1);
						EditorGUILayout.BeginVertical(EditorStyles.helpBox); {
							GUI.backgroundColor = gbc;
							var expanded = so.FindProperty("_expanded");
							EditorGUILayout.BeginHorizontal(); {
								// Info
								EditorGUILayout.BeginHorizontal(); {
									active.boolValue = EditorGUILayout.Toggle(active.boolValue, GUILayout.Width(25));
									expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue, s.name + (active.boolValue ? "" : " (inactive)"));
									GUILayout.FlexibleSpace();
									GUILayout.Label(new GUIContent(s.description, "Class: "+s.GetType().Name));

									// Buttons
									GUILayout.FlexibleSpace();
									GUI.enabled = i > 0;
									if (GUILayout.Button("UP")){
										--so.FindProperty("_order").intValue;
										var soPrev = new SerializedObject(_strippers[i-1]);
										++soPrev.FindProperty("_order").intValue;
										soPrev.ApplyModifiedProperties();
										reSort = true;
									}
									GUI.enabled = i < (_strippers.Count-1);
									if (GUILayout.Button("DOWN")){
										++so.FindProperty("_order").intValue;
										var soNext = new SerializedObject(_strippers[i+1]);
										--soNext.FindProperty("_order").intValue;
										soNext.ApplyModifiedProperties();
										reSort = true;
									}
									GUI.enabled = true;
									if (GUILayout.Button("Select")){
										EditorGUIUtility.PingObject(s);
									}
								} EditorGUILayout.EndHorizontal();
							} EditorGUILayout.EndHorizontal();
							if (expanded.boolValue){
								string help = s.help;
								if (!string.IsNullOrEmpty(help)){
									EditorGUILayout.HelpBox(help, MessageType.Info);
								}
								// Settings
								var sp = so.GetIterator();
								sp.NextVisible(true);
								while (sp.NextVisible(false)){
									if ((sp.name == "_active") || (sp.name == "_expanded")) continue;
									EditorGUILayout.PropertyField(sp, true);
								}
							}

							s.OnGUI();
						} EditorGUILayout.EndVertical();
						EditorGUILayout.Space();

						so.ApplyModifiedProperties();
					}
				} EditorGUILayout.EndScrollView();
				
				if (reSort){
					SortSettings();
				}
			} EditorGUILayout.EndVertical();
			GUI.backgroundColor = gbc;
		}
		static void RefreshSettings(){
			_strippers.Clear();
			foreach (var guid in AssetDatabase.FindAssets("t:ShaderStripperBase")){
				string path = AssetDatabase.GUIDToAssetPath(guid);
				_strippers.Add(AssetDatabase.LoadAssetAtPath<ShaderStripperBase>(path));
			}
			SortSettings();
		}
		static void SortSettings(){
			_strippers = _strippers.OrderBy(x=>new SerializedObject(x).FindProperty("_order").intValue).ToList();
			// Apply new sort orders
			for (int i=0; i<_strippers.Count; ++i){
				var so = new SerializedObject(_strippers[i]);
				so.FindProperty("_order").intValue = i;
				so.ApplyModifiedProperties();
			}
		}
		#endregion

		#region Stripping Callbacks
		public static void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report){
			_logPath = EditorPrefs.GetString(KEY_LOG);
			_enabled = GetEnabled();

			if (_enabled){
				Debug.Log("Initialising ShaderStrippers");
				if (!string.IsNullOrEmpty(_logPath)){
					Debug.Log("Logfiles will be created in "+_logPath);
				}
				_keptLog.Clear();
				_keptLog.Add("Unstripped Shaders:");
				RefreshSettings();
				ShaderStripperBase.OnPreBuild(_deepLogs);
				foreach (var s in _strippers){
					if (s.active){
						s.Initialize();
					}
				}
				_swStrip.Reset();
				_swBuild.Reset();
				_swBuild.Start();
			} else {
				Debug.Log("ShaderStripper DISABLED");
			}
		} 
		static readonly BuiltinShaderDefine[] _platformKeywords = (BuiltinShaderDefine[])System.Enum.GetValues(typeof(BuiltinShaderDefine));
		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data){
			if (!_enabled) return;
			_rawCount += data.Count;
             
			var builtins = (BuiltinShaderDefine[])System.Enum.GetValues(typeof(BuiltinShaderDefine));

			if (_deepLogs){
				for (int i=0; i<data.Count; ++i){
					foreach (var k in data[i].shaderKeywordSet.GetShaderKeywords()){
						string sn = ShaderStripperBase.GetKeywordName(k);
						if (!_allKeywords.Contains(sn)){
							_allKeywords.Add(sn);
						}
					}
					var pks = data[i].platformKeywordSet;
					foreach (var b in builtins){
						if (pks.IsEnabled(b)){
							if (!_allPlatformKeywords.Contains(b)){
								_allPlatformKeywords.Add(b);
								_allPlatformKeywordNames.Add(b.ToString());
							}
						}
					}
				}
			}

			_swStrip.Start();
			for (int i=0; i<_strippers.Count; ++i){
				var s = _strippers[i];
				if (!s.active) continue;
				s.Strip(shader, snippet, data);
				if (data.Count == 0) break;
			}
			_swStrip.Stop();
			if (data.Count > 0){
				_keptCount += data.Count;
				_keptLog.Add(string.Format(
					"    {0}::[{1}]{2} [{3} variants]", shader.name, 
					snippet.passType, snippet.passName, data.Count
				));

				if (_deepLogs){
					foreach (var d in data){
						string varLog = string.Format(
							"\t\t[{0}][{1}] ", d.graphicsTier, d.shaderCompilerPlatform
						);
						foreach (var k in d.shaderKeywordSet.GetShaderKeywords()){
							varLog += ShaderStripperBase.GetKeywordName(k) + " ";
						}

						varLog += "\n\t\t\t";
						foreach (var b in _platformKeywords){
							if (d.platformKeywordSet.IsEnabled(b)){
								varLog += b.ToString() + " ";
							}
						}

						varLog += string.Format("\n\t\t\tREQ: {0}", d.shaderRequirements.ToString());
						_keptLog.Add(varLog);

						foreach (var k in d.shaderKeywordSet.GetShaderKeywords()){
							string sn = ShaderStripperBase.GetKeywordName(k);
							if (!_keptKeywords.Contains(sn)){
								_keptKeywords.Add(sn);
							}
						}

						var pks = d.platformKeywordSet;
						foreach (var b in builtins){
							if (pks.IsEnabled(b)){
								if (!_keptPlatformKeywords.Contains(b)){
									_keptPlatformKeywords.Add(b);
									_keptPlatformKeywordNames.Add(b.ToString());
								}
							}
						}
					}
				}
			}
		}
		public static void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report){
			if (!_enabled) return;

			_swBuild.Stop();
			
			string header = string.Format(
				"Build Time: {0}ms\nStrip Time: {1}ms\nTotal shaders built: {2}\nTotal shaders stripped: {3}",
				_swBuild.ElapsedMilliseconds, _swStrip.ElapsedMilliseconds, _keptCount, _rawCount-_keptCount
			);
			Debug.Log(header);

			var strippedKeywords = new ShaderLog("KEYWORDS-STRIPPED");
			foreach (var k in _allKeywords.log){
				if (!_keptKeywords.Contains(k)){
					strippedKeywords.Add(k);
				}
			}

			var strippedPlatformKeywords = new ShaderLog("PLATFORM-KEYWORDS-STRIPPED");
			foreach (var k in _allPlatformKeywordNames.log){
				if (!_keptPlatformKeywordNames.Contains(k)){
					strippedPlatformKeywords.Add(k);
				}
			}

			string logPath = EditorPrefs.GetString(ShaderStripperEditor.KEY_LOG);
			ShaderStripperBase.OnPostBuild(
				logPath, header, _keptLog, _allKeywords, _keptKeywords,
				_allPlatformKeywordNames, _keptPlatformKeywordNames,
				strippedKeywords, strippedPlatformKeywords
			);

			_swStrip.Reset();
			_swBuild.Reset();
			_keptLog.Clear();
			_keptCount = 0;
			_allKeywords.Clear();
			_keptKeywords.Clear();
			_allPlatformKeywordNames.Clear();
			_allPlatformKeywords.Clear();
			_keptPlatformKeywordNames.Clear();
			_keptPlatformKeywords.Clear();
		}
		#endregion
	}
}
#endif