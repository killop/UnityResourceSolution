using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;



public class BuildTaskExportShaderVariantCollection : BuildTask
{
    public const string SHADER_VARIANT_COLLECT_SAVE_PATH = "Assets/GameResources/ShaderVarians/ShaderVariantCollection.shadervariants";
    private HashSet<string> _allGlobalKeywords = new();
    private int _keywordIndex = 0;
    private List<HashSet<string>> _enabledGlobalKeywordsList = new();
    private List<Material> _materialList = new();

    public override void BeginTask()
    {
        base.BeginTask();
        _materialList.Clear();
        Prepare();
    }

    public override void FinishTask()
    {
        _materialList.Clear();
        base.FinishTask();
    }
    
    public void Prepare()
    {
        var assetInfos = this.GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);
        var allMaterial = new HashSet<string>();

        foreach (var assetPath in assetInfos.Keys)
        {
            if (Path.GetExtension(assetPath) == ".mat")
            {
                if (!allMaterial.Contains(assetPath))
                {
                    allMaterial.Add(assetPath);
                }
            }
        }
        _materialList = new List<Material>();
        var shaderDict = new Dictionary<Shader, List<Material>>();
        foreach (var key in allMaterial)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(key);
            if (material != null)
            {
                if (material.shader != null)
                {
                    if (!shaderDict.ContainsKey(material.shader))
                    {
                        shaderDict.Add(material.shader, new List<Material>());
                    }
                    if (!shaderDict[material.shader].Contains(material))
                    {
                        shaderDict[material.shader].Add(material);
                    }
                }
                if (!_materialList.Contains(material))
                {
                    _materialList.Add(material);
                }
            }
        }

        ProcessMaterials();
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in shaderDict)
        {
            sb.AppendLine(kvp.Key + " " + kvp.Value.Count + " times");

            if (kvp.Value.Count <= 5)
            {
                Debug.LogWarning("Shader: " + kvp.Key.name, kvp.Key);

                foreach (var m in kvp.Value)
                {
                    Debug.Log(AssetDatabase.GetAssetPath(m), m);
                }
            }
        }
        Debug.Log(sb.ToString());
    }
    private void ProcessMaterials()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        InvokeInternalStaticMethod(typeof(ShaderUtil), "ClearCurrentShaderVariantCollection");
        Debug.Log(InvokeInternalStaticMethod(typeof(ShaderUtil), "GetCurrentShaderVariantCollectionShaderCount"));

        int totalMaterials = _materialList.Count;

        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Main Camera didn't exist");
            return;
        }

        float aspect = camera.aspect;

        float height = Mathf.Sqrt(totalMaterials / aspect) + 1;
        float width = Mathf.Sqrt(totalMaterials / aspect) * aspect + 1;

        float halfHeight = Mathf.CeilToInt(height / 2f);
        float halfWidth = Mathf.CeilToInt(width / 2f);

        camera.orthographic = false;
        camera.orthographicSize = halfHeight;
        camera.transform.position = new Vector3(0f, 0f, -100f);

        Selection.activeGameObject = camera.gameObject;
        EditorApplication.ExecuteMenuItem("GameObject/Align View to Selected");

        int xMax = (int)(width - 1);

        int x = 0;
        int y = 0;

        for (int i = 0; i < _materialList.Count; i++)
        {
            var material = _materialList[i];
            var newMaterial = new Material(material);
            _materialList[i] = newMaterial;

            var position = new Vector3(x - halfWidth + 1f, y - halfHeight + 1f, 0f);
            CreateSphere(newMaterial, position, x, y, i);

            if (x == xMax)
            {
                x = 0;
                y++;
            }
            else
            {
                x++;
            }
        }

        _enabledGlobalKeywordsList.Add(new HashSet<string>());
        _enabledGlobalKeywordsList.Add(new HashSet<string>()
        {
            "FOG_LINEAR"
        });

        foreach (var keywords in _enabledGlobalKeywordsList)
        {
            foreach (var keyword in keywords)
            {
                if (!string.IsNullOrWhiteSpace(keyword))
                    _allGlobalKeywords.Add(keyword);
            }
        }

        _keywordIndex = 0;

        //  Debug.LogError("开始收集计时"+ EditorApplication.timeSinceStartup+" frameCount "+ sFrameCount);
    }
    private static void CreateSphere(Material material, Vector3 position, int x, int y, int index)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.GetComponent<Renderer>().material = material;
        go.transform.position = position;
        go.name = string.Format("Sphere_{0}|{1}_{2}|{3}", index, x, y, material.name);
    }

    private static object InvokeInternalStaticMethod(System.Type type, string method, params object[] parameters)
    {
        var methodInfo = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
        if (methodInfo == null)
        {
            Debug.LogError(string.Format("{0} method didn't exist", method));
            return null;
        }

        return methodInfo.Invoke(null, parameters);
    }

    public override void OnTaskUpdate()
    {
        base.OnTaskUpdate();

        if (_keywordIndex < _enabledGlobalKeywordsList.Count)
        {
            var camera = Camera.main;
            if (camera != null)
            {
                var keywords = _enabledGlobalKeywordsList[_keywordIndex];

                foreach (var v in _allGlobalKeywords)
                {
                    foreach (var mat in _materialList)
                    {
                        mat.DisableKeyword(v);
                    }
                }

                foreach (var v in keywords)
                {
                    foreach (var mat in _materialList)
                    {
                        mat.EnableKeyword(v);
                    }
                }
                
                camera.Render();
            }

            _keywordIndex++;
        }
        else
        {
            Debug.Log(InvokeInternalStaticMethod(typeof(ShaderUtil), "GetCurrentShaderVariantCollectionVariantCount"));
            InvokeInternalStaticMethod(typeof(ShaderUtil), "SaveCurrentShaderVariantCollection", SHADER_VARIANT_COLLECT_SAVE_PATH);
            Debug.Log(InvokeInternalStaticMethod(typeof(ShaderUtil), "GetCurrentShaderVariantCollectionShaderCount"));
            Debug.Log("结束收集");
            this.FinishTask();
        }
    }
}
