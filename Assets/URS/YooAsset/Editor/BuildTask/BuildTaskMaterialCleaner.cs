using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Rendering;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class BuildTaskMaterialCleaner : BuildTask
{

    public override void BeginTask()
    {
        base.BeginTask();
        var assetInfos = this.GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);

        EditorApplication.ExecuteMenuItem("Window/Inspector");
        foreach (var assetPath in assetInfos.Keys)
        {
            if (Path.GetExtension(assetPath) == ".mat")
            {
                var m =  AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                ClearnMaterial(m, assetPath);
                // AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
               
                // EditorUtility.SetDirty(m);
                //Editor.CreateEditor(m);
            }
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        this.FinishTask();
    }
    //获取shader中所有的宏
    public static bool GetShaderKeywords(Shader target, out string[] global, out string[] local)
    {
        try
        {
            MethodInfo globalKeywords = typeof(ShaderUtil).GetMethod("GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            global = (string[])globalKeywords.Invoke(null, new object[] { target });
            MethodInfo localKeywords = typeof(ShaderUtil).GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            local = (string[])localKeywords.Invoke(null, new object[] { target });
            return true;
        }
        catch
        {
            global = local = null;
            return false;
        }
    }

    static void ClearnMaterial(Material m,string path)
    {
        /*
        if (GetShaderKeywords(m.shader, out var global, out var local))
        {
            HashSet<string> keywords = new HashSet<string>();
            foreach (var g in global)
            {
                keywords.Add(g);
            }
            foreach (var l in local)
            {
                keywords.Add(l);
            }
            //重置keywords
            List<string> resetKeywords = new List<string>(m.shaderKeywords);
            foreach (var item in m.shaderKeywords)
            {
                if (!keywords.Contains(item))
                {
                    //Debug.LogError("非法关键字 ："+ item+"  path "+path);                 
                    resetKeywords.Remove(item);
                }
                   
            }
            m.shaderKeywords = resetKeywords.ToArray();
        }
        */
        HashSet<string> property = new HashSet<string>();
        int count = m.shader.GetPropertyCount();
        for (int i = 0; i < count; i++)
        {
            property.Add(m.shader.GetPropertyName(i));
        }

        SerializedObject o = new SerializedObject(m);
        //SerializedProperty disabledShaderPasses = o.FindProperty("disabledShaderPasses");
        SerializedProperty SavedProperties = o.FindProperty("m_SavedProperties");
        SerializedProperty TexEnvs = SavedProperties.FindPropertyRelative("m_TexEnvs");
        SerializedProperty Floats = SavedProperties.FindPropertyRelative("m_Floats");
        SerializedProperty Colors = SavedProperties.FindPropertyRelative("m_Colors");
        //对比属性删除残留的属性
        // for (int i = disabledShaderPasses.arraySize - 1; i >= 0; i--)
        // {
        //     if (!property.Contains(disabledShaderPasses.GetArrayElementAtIndex(i).displayName))
        //     {
        //         //Debug.LogError("非法通道 ：" + disabledShaderPasses.GetArrayElementAtIndex(i).displayName + "  path " + path);
        //          disabledShaderPasses.DeleteArrayElementAtIndex(i);
        //     }
        // }
        bool dirty = false;
        for (int i = TexEnvs.arraySize - 1; i >= 0; i--)
        {
            if (!property.Contains(TexEnvs.GetArrayElementAtIndex(i).displayName))
            {
               // Debug.LogError("非法TexEnvs ：" + TexEnvs.GetArrayElementAtIndex(i).displayName + "  path " + path);
                TexEnvs.DeleteArrayElementAtIndex(i);
                if (!dirty) {
                    dirty = true;
                }
            }
        }
        for (int i = Floats.arraySize - 1; i >= 0; i--)
        {
            if (!property.Contains(Floats.GetArrayElementAtIndex(i).displayName))
            {
               // Debug.LogError("非法Floats ：" + Floats.GetArrayElementAtIndex(i).displayName + "  path " + path);
                Floats.DeleteArrayElementAtIndex(i);
                if (!dirty)
                {
                    dirty = true;
                }
            }
        }
        for (int i = Colors.arraySize - 1; i >= 0; i--)
        {
            if (!property.Contains(Colors.GetArrayElementAtIndex(i).displayName))
            {
                //Debug.LogError("非法Colors ：" + Colors.GetArrayElementAtIndex(i).displayName + "  path " + path);
                Colors.DeleteArrayElementAtIndex(i);
                if (!dirty)
                {
                    dirty = true;
                }
            }
        }
        if (dirty)
        {
            o.ApplyModifiedProperties();
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssetIfDirty(m);
        }
       // var words=  m.shaderKeywords;
        //AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        EditorGUIUtility.PingObject(m);
        AssetDatabase.OpenAsset(m);
        //  Debug.Log("Done!");
    }

}
#if UNITY_EDITOR

 
public class EditorWindowControl
{
 

    private enum SelectWindowType { Inspector, ProjectBrowser, Game, Console, Hierarchy, Scene };

    private static void FocusUnityEditorWindow(SelectWindowType swt)
    {
        System.Type unityEditorWindowType = null;
        EditorWindow editorWindow = null;

        switch (swt)
        {
            case SelectWindowType.Inspector:
                unityEditorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
                break;
            case SelectWindowType.ProjectBrowser:
                unityEditorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
                break;
            case SelectWindowType.Game:
                unityEditorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                break;
            case SelectWindowType.Console:
                unityEditorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ConsoleView");
                break;
            case SelectWindowType.Hierarchy:
                unityEditorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                break;
            case SelectWindowType.Scene:
                unityEditorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneView");
                break;
        }

        editorWindow = EditorWindow.GetWindow(unityEditorWindowType);
    }

    public static void ShowInspectorEditorWindow()
    {
        string inspectorWindowTypeName = "UnityEditor.InspectorWindow";
        ShowEditorWindowWithTypeName(inspectorWindowTypeName);
    }

    public static void ShowSceneEditorWindow()
    {
        string sceneWindowTypeName = "UnityEditor.SceneView";
        ShowEditorWindowWithTypeName(sceneWindowTypeName);
    }

    public static void ShowEditorWindowWithTypeName(string windowTypeName)
    {
        var windowType = typeof(Editor).Assembly.GetType(windowTypeName);
        EditorWindow.GetWindow(windowType);
    }
}
#endif