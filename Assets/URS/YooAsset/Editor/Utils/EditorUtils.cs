using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using System.Runtime.InteropServices;

public static partial class EditorUtils
{

#if UNITY_EDITOR_WIN
    /// <summary>
    /// Struct representing a point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// Retrieves the cursor's position, in screen coordinates.
    /// </summary>
    /// <see>See MSDN documentation for further information.</see>
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

#endif

    private static Regex labelRegex = new Regex("[A-Z_]+[a-z_0-9]*");

    public static Vector2 CursorPosition
    {
        get
        {
#if UNITY_EDITOR_WIN
            POINT lpPoint;
            bool success = GetCursorPos(out lpPoint);
            if (!success)
                return Vector2.zero;
            return new Vector2(lpPoint.X, lpPoint.Y);
#else
            return Vector2.zero;
#endif
        }
    }

    private static char ToUpper(char value)
    {
        return (char)((int)value - (int)'a' + (int)'A');
    }
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
                sb.Append(ToUpper(newLabel[0]));
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
        UnityEditor.Animations.AnimatorController animatorController = animator?.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
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

    private static void CheckFileExtension(List<string> result, string path, string ext)
    {
        if (Path.GetExtension(path).ToLower() == ext)
            result.Add(path);
    }

    public static List<T> LoadAllAssetsAtPath<T>(string path) where T : UnityEngine.Object
    {
        List<T> r = new List<T>();
        var list = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach(var res in list)
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

    public static bool IdxValid(this IList self, int idx)
    {
        return idx >= 0 && idx < (self?.Count ?? 0);
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

        return ForEachFiles(path, "*.*", SearchOption.AllDirectories, (value, i, iCount) =>
        {
            File.Delete(value);
        }, exceptionCallback);
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
    public static void DeleteFilesExcept(string path, HashSet<string> exceptNameHashset, Func<string, bool> extraCallback = null)
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


    public static bool ForEachFiles(string path, string searchPattern, SearchOption searchOption, Action<string, int, int> callback, Action<Exception> exceptionCallback)
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
                try { oldText = File.ReadAllText(filePath); }
                catch { oldText = null; }

                oldText = oldText?.Replace("\r", "");
                var newText = content.Replace("\r", "");
                if (oldText == newText)
                    return;
            }
            File.Delete(filePath);
        }

        using (var fs = File.Create(filePath))
        {
            using (var sw = new StreamWriter(fs, encoding ?? Encoding.Default))
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
    public static void AppendIndentLine(this StringBuilder self, string text)
    {
        for (int i = 0; i < Indent; ++i)
            self.Append('\t');
        self.AppendLine(text);
    }

    public static void AppendIndent(this StringBuilder self, string text)
    {
        for (int i = 0; i < Indent; ++i)
            self.Append('\t');
        self.Append(text);
    }

    private static void AppendIndentCommentInternal(this StringBuilder self, string comment)
    {
        if (string.IsNullOrEmpty(comment))
            return;

        using (StringReader sr = new StringReader(comment))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(line))
                    self.AppendIndentLine($"/// {line}");
            }
        }
    }

    public static void AppendIndentComment(this StringBuilder self, string comment1)
    {
        if (string.IsNullOrEmpty(comment1))
            return;

        self.AppendIndentLine("/// <summary>");
        self.AppendIndentCommentInternal(comment1);
        self.AppendIndentLine("/// </summary>");
    }

    public static void AppendIndentComment(this StringBuilder self, string comment1, string comment2)
    {
        if (string.IsNullOrEmpty(comment1) && string.IsNullOrEmpty(comment2))
            return;

        self.AppendIndentLine("/// <summary>");
        self.AppendIndentCommentInternal(comment1);
        self.AppendIndentCommentInternal(comment2);
        self.AppendIndentLine("/// </summary>");
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

    public static bool CommandLineHasArg(string arg) => Environment.GetCommandLineArgs().IndexOf(x => string.Equals(x, arg, StringComparison.OrdinalIgnoreCase)) != -1;

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
    
    
    private static string s_LogErrorForCommandLinePath = Path.Combine(Application.dataPath, "../Logs/LogErrorForCommandLine.log");
    private static string s_LogErrorForCommandLineAsJsonContentPath = Path.Combine(Application.dataPath, "../Logs/LogErrorForCommandLineAsJsonContent.log");

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
    private static List<LogInfo> s_LogErrorForCommandLine = new();
    public static void CommandLineListenError()
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

    public static void CommandLineSaveError()
    {
        StringBuilder sb = new();
        if (s_LogErrorForCommandLine.Count > 4)
        {
            s_LogErrorForCommandLine[0].Append(sb, 0);
            s_LogErrorForCommandLine[1].Append(sb, 1);

            sb.Append("\n......\n\n");
            
            s_LogErrorForCommandLine[s_LogErrorForCommandLine.Count - 2].Append(sb, s_LogErrorForCommandLine.Count - 2);
            s_LogErrorForCommandLine[s_LogErrorForCommandLine.Count - 1].Append(sb, s_LogErrorForCommandLine.Count - 1);
        }
        else
        {
            for (int i = 0; i < s_LogErrorForCommandLine.Count; ++ i)
                s_LogErrorForCommandLine[i].Append(sb, i);
        }
        var text = sb.ToString();
        File.WriteAllText(s_LogErrorForCommandLinePath, text);

        File.WriteAllText(s_LogErrorForCommandLineAsJsonContentPath, Convert2JsonContent(text));
    }
    public static int IndexOf<T>(this T[] self, Predicate<T> match)
    {
        for (int i = 0; i < self.Length; ++i)
        {
            if (match(self[i]))
                return i;
        }
        return -1;
    }
}
