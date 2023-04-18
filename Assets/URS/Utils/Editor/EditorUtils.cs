using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor.Animations;
using UnityEngine.Pool;

namespace NinjaBeats
{
    public static partial class EditorUtils
    {
        private static Regex labelRegex = new Regex("[A-Z_]+[a-z_0-9]*");

        static Dictionary<string, string> formatLabelDict = new Dictionary<string, string>();

        public static string FormatLabel(string label)
        {
            if (!formatLabelDict.TryGetValue(label, out var result))
            {
                StringBuilder sb = new StringBuilder();
                var matches = labelRegex.Matches(label);
                var newLabel = labelRegex.Replace(label, "");
                if (newLabel.Length > 0)
                {
                    sb.Append(newLabel[0].ToUpper());
                    for (int i = 1; i < newLabel.Length; ++i)
                    {
                        sb.Append(newLabel[i]);
                    }
                }

                for (int i = 0; i < matches.Count; ++i)
                {
                    sb.Append(" ");
                    sb.Append(matches[i].Value);
                }

                result = sb.ToString();
                formatLabelDict.Add(label, result);
            }

            return result;
        }



        /// <summary>
        /// 将指定的自然数转换为26进制表示。映射关系：[1-26] ->[A-Z]。
        /// </summary>
        /// <param name="n">自然数（如果无效，则返回空字符串）。</param>
        /// <returns>26进制表示。</returns>
        public static string ToNumberSystem26(int n)
        {
            string s = string.Empty;
            while (n > 0)
            {
                int m = n % 26;
                if (m == 0) m = 26;
                s = (char)(m + 64) + s;
                n = (n - m) / 26;
            }

            return s;
        }

        /// <summary>
        /// 将指定的26进制表示转换为自然数。映射关系：[A-Z] ->[1-26]。
        /// </summary>
        /// <param name="s">26进制表示（如果无效，则返回0）。</param>
        /// <returns>自然数。</returns>
        public static int FromNumberSystem26(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int n = 0;
            for (int i = s.Length - 1, j = 1; i >= 0; i--, j *= 26)
            {
                char c = Char.ToUpper(s[i]);
                if (c < 'A' || c > 'Z') return 0;
                n += ((int)c - 64) * j;
            }

            return n;
        }

        public static string[] GetAnimStateNames(Animator animator)
        {
            UnityEditor.Animations.AnimatorController animatorController =
                animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
            if (animatorController != null)
            {
                UnityEditor.Animations.AnimatorStateMachine stateMachine = animatorController.layers?[0]?.stateMachine;
                if (stateMachine != null)
                {
                    string[] animatorState = new string[stateMachine.states.Length];
                    for (int i = 0; i < stateMachine.states.Length; i++)
                    {
                        animatorState[i] = stateMachine.states[i].state.name;
                    }

                    return animatorState;
                }
            }

            return new string[] { };
        }

        public static GUILayoutOption[] Add(this GUILayoutOption[] self, GUILayoutOption option)
        {
            int selfCount = self?.Length ?? 0;
            GUILayoutOption[] r = new GUILayoutOption[selfCount + 1];
            for (int i = 0; i < selfCount; ++i)
                r[i] = self[i];
            r[selfCount] = option;
            return r;
        }

        public static List<string> GetAllSelectionPrefab()
        {
            List<string> result = new List<string>();
            foreach (var guid in Selection.assetGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Directory.Exists(path))
                    GetAllFilesInDir(result, path, ".prefab");
                else if (File.Exists(path))
                    CheckFileExtension(result, path, ".prefab");

            }

            return result;
        }

        public static List<string> GetAllSectionFiles(string ext)
        {
            List<string> result = new List<string>();
            foreach (var guid in Selection.assetGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Directory.Exists(path))
                    GetAllFilesInDir(result, path, ext);
                else if (File.Exists(path))
                    CheckFileExtension(result, path, ext);

            }

            return result;
        }

        public static List<string> GetAllSelectionScene()
        {
            List<string> result = new List<string>();
            foreach (var guid in Selection.assetGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Directory.Exists(path))
                    GetAllFilesInDir(result, path, ".unity");
                else if (File.Exists(path))
                    CheckFileExtension(result, path, ".unity");

            }

            return result;
        }

        public static void GetAllFilesInDir(List<string> result, string path, string ext)
        {
            foreach (var dir in Directory.GetDirectories(path))
                GetAllFilesInDir(result, dir, ext);

            foreach (var dir in Directory.GetFiles(path))
                CheckFileExtension(result, dir, ext);
        }

        public static string GetFileFullPathInDir(string dir, string fileName)
        {
            var fixFileName = fileName.Replace('\\', '/');
            var realName = Path.GetFileName(fixFileName);
            foreach (var fullFileName in Directory.GetFiles(dir, realName, SearchOption.AllDirectories))
            {
                var fixFullFileName = fullFileName.Replace('\\', '/');
                if (fixFullFileName.EndsWith(fixFileName, StringComparison.OrdinalIgnoreCase))
                    return fixFullFileName;
            }

            return string.Empty;
        }

        private static void CheckFileExtension(List<string> result, string path, string ext)
        {
            if (string.Equals(Path.GetExtension(path), ext, StringComparison.OrdinalIgnoreCase))
                result.Add(path);
        }

        public static List<T> LoadAllAssetsAtPath<T>(string path) where T : UnityEngine.Object
        {
            List<T> r = new List<T>();
            var list = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var res in list)
            {
                if (res is T)
                    r.Add((T)res);
            }

            return r;
        }

        public static T LoadAssetAtPath<T>(string path, string name) where T : UnityEngine.Object
        {
            var list = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var res in list)
            {
                if (res is T && res.name == name)
                    return (T)res;
            }

            return null;
        }

        public static void TryCopyFile(string src, string dst)
        {
            try
            {
                string dir = Path.GetDirectoryName(dst);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.Copy(src, dst, true);
            }
            catch (Exception e)
            {

            }
        }

        public static string CalculateMD5(string fileName, params object[] options)
        {
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = null;
                    if (options != null && options.Length > 0)
                    {
                        var content = ReadAllText(fileName);
                        foreach (var op in options)
                            content += "\n" + op.ToString();

                        var bytes = Encoding.UTF8.GetBytes(content);
                        hash = md5.ComputeHash(bytes);
                    }
                    else
                    {
                        using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            hash = md5.ComputeHash(fs);
                    }

                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return "";
            }
        }

        public static string CalculateMD5(List<string> fileNameList, params object[] options)
        {
            if (fileNameList.Count <= 0)
                return "";

            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = null;
                    if (fileNameList.Count > 1 || (options != null && options.Length > 0))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var fileName in fileNameList)
                        {
                            var content = ReadAllText(fileName);
                            sb.Append(content);
                        }

                        foreach (var op in options)
                        {
                            sb.Append("\n");
                            sb.Append(op.ToString());
                        }

                        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                        hash = md5.ComputeHash(bytes);
                    }
                    else
                    {
                        using (var fs = File.Open(fileNameList[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            hash = md5.ComputeHash(fs);
                    }

                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return "";
            }
        }

        public static string ReadFileFirstLine(string fileName)
        {
            try
            {
                using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        return sr.ReadLine();
                    }
                }
            }
            catch
            {
                return "";
            }
        }


        public static bool ClearPath(string path, Action<Exception> exceptionCallback)
        {
            if (!Directory.Exists(path))
                return true;

            return ForEachFiles(path, "*.*", SearchOption.AllDirectories, (value, i, iCount) => { File.Delete(value); },
                exceptionCallback);
        }

        public static void DeleteFiles(string path, Func<string, bool> callback)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            if (!Directory.Exists(path))
                return;

            var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                if (callback(file))
                {
                    try
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(e.Message + "\n" + e.StackTrace);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="exceptNameHashset"></param>
        /// <param name="extraCallback">返回true删除</param>
        public static void DeleteFilesExcept(string path, HashSet<string> exceptNameHashset,
            Func<string, bool> extraCallback = null)
        {
            DeleteFiles(path, v =>
            {
                var filename = Path.GetFileName(v);
                var dotIdx = filename.IndexOf('.');
                if (dotIdx == -1)
                    return true;

                var purename = filename.Substring(0, dotIdx);
                if (exceptNameHashset.Contains(purename))
                    return false;

                if (extraCallback != null)
                    return extraCallback(filename);

                return true;
            });
        }

        class AssetWatcherPostprocessor : AssetPostprocessor
        {
            public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                if (s_AssetFileWatcherList.Count == 0)
                    return;

                if (EditorApplication.isCompiling)
                {
                    s_AssetFileWatcherList.Clear();
                    return;
                }
                
                using (HashSetPool<string>.Get(out var pathSet))
                {
                    foreach (var path in importedAssets)
                        pathSet.Add(path);
                    foreach (var path in deletedAssets)
                        pathSet.Add(path);
                    foreach (var path in movedAssets)
                        pathSet.Add(path);
                    foreach (var path in movedFromAssetPaths)
                        pathSet.Add(path);

                    foreach (var path in pathSet)
                    {
                        var extension = Path.GetExtension(path);
                        foreach (var info in s_AssetFileWatcherList)
                        {
                            if (path.Contains(info.path, StringComparison.OrdinalIgnoreCase) &&
                                info.extension.Contains(extension, StringComparison.OrdinalIgnoreCase))
                            {
                                info.callback(path);
                            }
                        }
                    }
                }
                
            }
        }
        
        private class AssetFileWatcherInfo
        {
            public string path;
            public string extension;
            public Action<string> callback;
        }

        private static List<AssetFileWatcherInfo> s_AssetFileWatcherList = new();

        public static void AddAssetFileWatcher(string path, string extension, Action<string> callback)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(extension) || callback == null)
                return;

            AssetFileWatcherInfo watcher = new();
            watcher.path = path.Replace('\\', '/');
            if (!watcher.path.EndsWith('/'))
                watcher.path += "/";
            watcher.extension = extension;
            watcher.callback = callback;
            s_AssetFileWatcherList.Add(watcher);
        }


        public static bool ForEachFiles(string path, string searchPattern, SearchOption searchOption,
            Action<string, int, int> callback, Action<Exception> exceptionCallback)
        {
            var files = Directory.GetFiles(path, "*.*", searchOption);
            try
            {
                int len = files.Length;
                for (int i = 0; i < len; ++i)
                {
                    if (!searchPattern.Contains(Path.GetExtension(files[i]).ToLower()))
                        continue;
                    if (callback != null)
                        callback(files[i], i, len);
                }
            }
            catch (Exception e)
            {
                if (exceptionCallback != null)
                    exceptionCallback(e);
                return false;
            }

            return true;
        }

       

        public static void WriteToFile(string filePath, string content, bool checkSame = true, Encoding encoding = null)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(filePath))
            {
                if (checkSame)
                {
                    string oldText = null;
                    try
                    {
                        oldText = File.ReadAllText(filePath);
                    }
                    catch
                    {
                        oldText = null;
                    }

                    oldText = oldText?.Replace("\r", "");
                    var newText = content.Replace("\r", "");
                    if (oldText == newText)
                        return;
                }

                File.Delete(filePath);
            }

            using (var fs = File.Create(filePath))
            {
                using (var sw = encoding != null ? new StreamWriter(fs, encoding) : new StreamWriter(fs))
                {
                    sw.Write(content);
                }
            }
        }

       

        public static string ReadAllText(string filePath)
        {
            if (!File.Exists(filePath))
                return "";

            try
            {
                using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    return sr.ReadToEnd();
                }
            }
            catch
            {
                return "";
            }
        }

        public static void WriteAllText(string filePath, string content)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(content);
                }
            }
            catch
            {

            }
        }


        public static string FormatSelf(this string self, string format)
        {
            return string.Format(format, self);
        }

        static int Indent = 0;

        public static void AppendIndentLine(this StringBuilder self, string text, int? indent = null)
        {
            var r = indent ?? Indent;
            for (int i = 0; i < r; ++i)
                self.Append('\t');
            self.AppendLine(text);
        }

        public static void AppendIndent(this StringBuilder self, string text, int? indent = null)
        {
            var r = indent ?? Indent;
            for (int i = 0; i < r; ++i)
                self.Append('\t');
            self.Append(text);
        }

        private static void AppendIndentCommentInternal(this StringBuilder self, string comment, int? indent = null)
        {
            if (string.IsNullOrEmpty(comment))
                return;

            using (StringReader sr = new StringReader(comment))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                        self.AppendIndentLine($"/// {line}", indent);
                }
            }
        }

        public static void AppendIndentComment(this StringBuilder self, string comment1, int? indent = null)
        {
            if (string.IsNullOrEmpty(comment1))
                return;

            self.AppendIndentLine("/// <summary>", indent);
            self.AppendIndentCommentInternal(comment1, indent);
            self.AppendIndentLine("/// </summary>", indent);
        }

        public static void AppendIndentComment(this StringBuilder self, string comment1, string comment2, int? indent = null)
        {
            if (string.IsNullOrEmpty(comment1) && string.IsNullOrEmpty(comment2))
                return;

            self.AppendIndentLine("/// <summary>", indent);
            self.AppendIndentCommentInternal(comment1, indent);
            self.AppendIndentCommentInternal(comment2, indent);
            self.AppendIndentLine("/// </summary>", indent);
        }

        public static void AppendIndentCommentAttribute(this StringBuilder self, string comment1, int? indent = null)
        {
            if (string.IsNullOrEmpty(comment1))
                return;

            self.AppendIndentLine(
                $"[EditorCommentAttribute(\"{comment1.Replace("\n", "\\n").Replace("\r", "")}\")]", indent);
        }

        public static void AppendIndentCommentAttribute(this StringBuilder self, string comment1, string comment2, int? indent = null)
        {
            if (string.IsNullOrWhiteSpace(comment1))
            {
                AppendIndentCommentAttribute(self, comment2, indent);
            }
            else if (string.IsNullOrWhiteSpace(comment2))
            {
                AppendIndentCommentAttribute(self, comment1, indent);
            }
            else
            {
                AppendIndentCommentAttribute(self, comment1 + "\n" + comment2, indent);
            }
        }

        public static void AppendIndentSDOwnerFieldAttribute(this StringBuilder sb, string className, string fieldName, int? indent = null)
        {
            sb.AppendIndentLine(
                $"[SDOwnerFieldAttribute(typeof({className}), nameof({className}.{fieldName}))]", indent);
        }

        public class StringBuilderIndentScope : IDisposable
        {
            public StringBuilderIndentScope()
            {
                Indent++;
            }

            public void Dispose()
            {
                Indent--;
            }
        }

        public static bool CommandLineHasArg(string arg) => Environment.GetCommandLineArgs()
            .IndexOf(x => string.Equals(x, arg, StringComparison.OrdinalIgnoreCase)) != -1;

        public static string CommandLineGetArgValue(string arg)
        {
            var argEquals = arg + "=";
            var args = Environment.GetCommandLineArgs();
            var idx = args.IndexOf(x => x.StartsWith(argEquals, StringComparison.OrdinalIgnoreCase));
            if (idx == -1)
                return null;
            return args[idx].Substring(argEquals.Length);
        }
        
        public static int CommandLineGetArgValueInt(string arg, int defaultValue = 0)
        {
            try
            {
                return int.TryParse(CommandLineGetArgValue(arg), out var result) ? result : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool CommandLineGetArgValueBool(string arg, bool defaultValue = false)
        {
            try
            {
                var str = CommandLineGetArgValue(arg);
                if (str == null)
                    return defaultValue;
                if (str.Equals("1") || str.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return true;
                return false;
            }
            catch
            {
                return defaultValue;
            }
        }


        private static string s_LogErrorForCommandLinePath =
            Path.Combine(Application.dataPath, "../Logs/LogErrorForCommandLine.log");

        private static string s_LogErrorForCommandLineAsJsonContentPath =
            Path.Combine(Application.dataPath, "../Logs/LogErrorForCommandLineAsJsonContent.log");

        class LogInfo
        {
            public string msg;
            public string trace;
            public LogType type;

            public void Append(StringBuilder sb, int index)
            {
                sb.Append($"[{type}-{index}] {(msg.Length > 500 ? msg.Substring(0, 500) : msg)}\n\n");
            }
        }

        public static string Convert2JsonContent(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("/", "\\/")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\t", "\\t")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }

        private static List<LogInfo> s_LogErrorForCommandLine = new();

        private static void CommandLineListenError()
        {
            try
            {
                if (File.Exists(s_LogErrorForCommandLinePath))
                    File.Delete(s_LogErrorForCommandLinePath);
                if (File.Exists(s_LogErrorForCommandLineAsJsonContentPath))
                    File.Delete(s_LogErrorForCommandLineAsJsonContentPath);
            }
            catch
            {

            }

            Application.logMessageReceived += (condition, trace, type) =>
            {
                switch (type)
                {
                    case LogType.Assert:
                    case LogType.Exception:
                    case LogType.Error:
                    {
                        s_LogErrorForCommandLine.Add(new LogInfo()
                        {
                            msg = condition,
                            trace = trace,
                            type = type,
                        });
                        break;
                    }
                }
            };
        }

        private static void CommandLineSaveError()
        {
            try
            {
                StringBuilder sb = new();
                if (s_LogErrorForCommandLine.Count > 4)
                {
                    s_LogErrorForCommandLine[0].Append(sb, 0);
                    s_LogErrorForCommandLine[1].Append(sb, 1);

                    sb.Append("\n......\n\n");

                    s_LogErrorForCommandLine[s_LogErrorForCommandLine.Count - 2]
                        .Append(sb, s_LogErrorForCommandLine.Count - 2);
                    s_LogErrorForCommandLine[s_LogErrorForCommandLine.Count - 1]
                        .Append(sb, s_LogErrorForCommandLine.Count - 1);
                }
                else
                {
                    for (int i = 0; i < s_LogErrorForCommandLine.Count; ++i)
                        s_LogErrorForCommandLine[i].Append(sb, i);
                }

                var text = sb.ToString();
                File.WriteAllText(s_LogErrorForCommandLinePath, text);

                File.WriteAllText(s_LogErrorForCommandLineAsJsonContentPath, Convert2JsonContent(text));
            }
            catch
            {
                
            }
        }
        
        public static void RunCommandLine(Func<bool> task)
        {
            int code = 0;
            try
            {
                CommandLineListenError();
                code = task() ? 0 : 1;
            }
            catch (Exception e)
            {
                code = 1;
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                CommandLineSaveError();
                EditorApplication.Exit(code);
                
            }
        }
        
        public static async void RunCommandLineAsync(Func<Task<bool>> task)
        {
            int code = 0;
            try
            {
                CommandLineListenError();
                var success = await task();
                code = success ? 0 : 1;
            }
            catch (Exception e)
            {
                code = 1;
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                CommandLineSaveError();
                EditorApplication.Exit(code);
                
            }
        }
        
      

     
        public delegate TValue DrawDelegate<in TLabel, TValue>(TLabel label, TValue value);
        public delegate TValue DrawWithOptionDelegate<in TLabel, TValue>(TLabel label, TValue value, params GUILayoutOption[] options);

        public static bool DrawMixedField<TLabel, TValue>(this IList<TValue> valueList, DrawDelegate<TLabel, TValue> drawFunc, TLabel label)
        {
            var isSame = valueList.GroupBy(x => x).Count() == 1;
            
            using (EditorUtils.ShowMixedValueScope(!isSame))
            {
                EditorGUI.BeginChangeCheck();
                var newValue = drawFunc(label, valueList.FirstOrDefault());    
                if (EditorGUI.EndChangeCheck())
                {
                    for (int i = 0; i < valueList.Count; ++i)
                        valueList[i] = newValue;
                    return true;
                }
            }

            return false;
        }

        public static bool DrawMixedField<TLabel, TValue>(this IList<TValue> valueList,
            DrawWithOptionDelegate<TLabel, TValue> drawFunc, TLabel label) =>
            DrawMixedField(valueList, (a, b) => drawFunc(a, b), label);

        public static bool GetMixedValue<TValue>(this IList<TValue> valueList, out TValue value)
        {
            value = valueList.FirstOrDefault();
            return valueList.GroupBy(x => x).Count() == 1;
        }

       
        
        public static string HttpRequest(string method, string url, (string, string)[] headers, string req_body)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                if (headers != null)
                {
                    foreach (var pair in headers)
                    {
                        request.Headers.Add(pair.Item1, pair.Item2);
                    }
                }

                if (method != null)
                    request.Method = method;
                request.Timeout = 3000;
                request.ContentType = "application/json";

                if (req_body != null)
                {
                    Debug.Log($"{method} url:{url} request:{req_body}");
                    var bytes = Encoding.UTF8.GetBytes(req_body);
                    request.ContentLength = bytes.Length;

                    using (var st = request.GetRequestStream())
                    {
                        st.Write(bytes, 0, bytes.Length);
                        st.Close();
                    }
                }
                else
                {
                    Debug.Log($"{method} url:{url}");
                }

                string result = null;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Debug.Log($"response status:{response.StatusCode}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var st = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(st))
                        {
                            result = reader.ReadToEnd();
                            Debug.Log($"response:{result}");
                        }
                    }
                }

                response.Close();
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        public static IEnumerable<Mesh> GetMeshes(this ModelImporter modelImporter)
        {
            var assetPath = AssetDatabase.GetAssetPath(modelImporter);
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x is Mesh mesh && mesh != null).Cast<Mesh>();
        }
    }
}