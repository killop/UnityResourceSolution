using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEditor.SceneManagement;
using System.Reflection;
using Debug = UnityEngine.Debug;
using System.Linq;
using Soco.ShaderVariantsCollection;
using URS;
//using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class URSMaterials : IMaterialCollector
{
    public List<Material> Materials { get; set; } = new List<Material>();
    public override void AddMaterialBuildDependency(IList<Material> buildDependencyList) {
        if (Materials.Count > 0) 
        {
            foreach (var material in Materials)
            {
                buildDependencyList.Add(material);
            }
        }
    }
}


public class BuildTaskExportShaderVariantCollection : BuildTask
{
    //public const string SHADER_VARIANT_COLLECT_SAVE_PATH = "Assets/GameResources/ShaderVarians/ShaderVariantCollection.shadervariants";

    // private HashSet<string> _allGlobalKeywords = new();
    // private int _keywordIndex = 0;
    // private List<HashSet<string>> _enabledGlobalKeywordsList = new();
    private List<Material> _materialList = new();

    private double? _beginTime = null;

    public override void BeginTask()
    {
        base.BeginTask();
        _materialList.Clear();
        //ProcessMaterials();
        collect();
        this.FinishTask();
    }

    public override void FinishTask()
    {
        _materialList.Clear();
        _beginTime = null;
        base.FinishTask();
    }

    public void collect()
    {
        var assetInfos = this.GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);

        _materialList = new List<Material>();
        var shaderDict = new Dictionary<Shader, List<Material>>();
        var allMaterial = new HashSet<string>();

        Action<Material> collectAction = (Material material) =>
        {
            if (material)
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
                    // Debug.LogError("add material name " + material.name + " shader keyword count" + material.shaderKeywords.Count() + " instance id" + material.GetInstanceID());
                    _materialList.Add(material);
                }
                else
                {
                    // Debug.LogError("reject material name " + material.name + " shader keyword count" + material.shaderKeywords.Count() + " instance id" + material.GetInstanceID());
                }
            }
        };
        foreach (var assetPath in assetInfos.Keys)
        {
            var extension = Path.GetExtension(assetPath);
            if (extension == ".mat")
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                collectAction(material);
            }
            /*
            else if (extension == ".prefab")
            {
                var gm = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                var mvs = gm.GetComponentsInChildren<UIMaterialVariant>(true);
                foreach (var mv in mvs)
                {
                    if (mv is UIMaterialBlock uIMaterialBlock)
                    {
                        var material = mv.GetModifiedMaterial(uIMaterialBlock.material);
                        if (!material)
                            Debug.LogWarning(assetPath + "material variant is null  name :" + mv.gameObject.name);
                        else
                        {
                            collectAction(material);
                        }
                    }
                    else
                    {
                        var graphic = mv.gameObject.GetComponent<UnityEngine.UI.Graphic>();
                        if (graphic is TMPro.TextMeshProUGUI tmp)
                        {
                            var material = mv.GetModifiedMaterial(tmp.fontMaterial);

                            if (!material)
                                Debug.LogWarning(assetPath + "material variant is null  name :" + mv.gameObject.name);
                            else
                            {
                                collectAction(material);
                            }
                        }
                        else if (graphic)
                        {
                            var material = mv.GetModifiedMaterial(graphic.material);
                            if (!material)
                                Debug.LogWarning(assetPath + "material variant is null  name :" + mv.gameObject.name);
                            else
                            {
                                collectAction(material);
                            }
                        }
                    }
                }
            }
            */
        }
        URSMaterials uRSMaterials = new URSMaterials();
        uRSMaterials.Materials = _materialList;
        ShaderVariantCollectionMaterialVariantConverter converter = new ShaderVariantCollectionMaterialVariantConverter();
        converter.CollectMaterial(new List<IMaterialCollector>() { uRSMaterials });
        converter.CollectVariant();
        ShaderVariantCollection shaderVariantCollection = new ShaderVariantCollection();
        converter.WriteToShaderVariantCollectionFile(shaderVariantCollection);

        ShaderVariantCollectionMapper shaderVariantCollectionMapper = new ShaderVariantCollectionMapper(shaderVariantCollection);
        shaderVariantCollectionMapper.Init(shaderVariantCollection);

        var shaders = shaderVariantCollectionMapper.shaders;
        foreach (var shader in shaders)
        {

            var multiKeywords = GetMultiKeyWordGroups(converter, shader);
            if (multiKeywords.Count > 0) {
                VariantKeywordCombination vkc = new VariantKeywordCombination();
                vkc.mShader = shader;
                vkc.mKeywordDeclareGroups = multiKeywords;
                vkc.Execute(shaderVariantCollectionMapper);
            }
            //VariantKeywordCombinationEditor.TryParseShader(vkc.mShader,vkc.mKeywordDeclareGroups);
        }

        shaderVariantCollectionMapper.Refresh();
        var mapper = shaderVariantCollectionMapper.GetMapper();


        // StripperShaderVariants stripperShaderVariants = new StripperShaderVariants();
        // List<StripperShaderVariant> variants = new List<StripperShaderVariant>();
     #region stripper fog  shader variant;
        ShaderVariantCollection blackShaderCollection = new ShaderVariantCollection();
        ShaderVariantCollectionMapper blackShaderVariantCollectionMapper = new ShaderVariantCollectionMapper(blackShaderCollection);


       
        foreach (var kv in mapper)
        {
            var shader = kv.Key;
            var shaderVariants = kv.Value;

            Dictionary <PassType,ShaderPassFogInfo> shaderPassFogInfo = new Dictionary<PassType, ShaderPassFogInfo> ();
            //bool hasFog = false;
            foreach (var sv in shaderVariants)
            {
                if (!shaderPassFogInfo.ContainsKey(sv.passType))
                {
                    var sfi = new ShaderPassFogInfo()
                    {
                        passType = sv.passType,
                    };
                    sfi.shaderVariants.Add(sv);
                    shaderPassFogInfo.Add(sv.passType, sfi);
                }
                else 
                {
                    var sfi = shaderPassFogInfo[sv.passType];
                    sfi.shaderVariants.Add (sv);
                }
            }
            foreach (var sfi in shaderPassFogInfo)
            {
                bool hasFog = false;
                foreach (var sv in sfi.Value.shaderVariants)
                {
                    if (sv.keywords.Contains("FOG_LINEAR")) {
                        hasFog = true;
                        break;
                    }
                }
                if (hasFog) 
                {
                    foreach (var sv in sfi.Value.shaderVariants)
                    {
                        if (!sv.keywords.Contains("FOG_LINEAR"))
                        {
                            blackShaderCollection.Add(sv.Deserialize());
                        }
                    }

                }
            
            }
          
        }
#endregion
        blackShaderVariantCollectionMapper.Refresh();

        var blackMapper = blackShaderVariantCollectionMapper.GetMapper();
        foreach (var kv in blackMapper)
        {
            var variants = kv.Value;
            foreach (var variant in variants)
            {
                shaderVariantCollectionMapper.RemoveVariant(variant.Deserialize());
            }
        }

        HashSet<Shader> warmpShaders = new HashSet<Shader>();
        warmpShaders.Add(Shader.Find("HE/ToonLit")); // ÓÅÏÈ
        warmpShaders.Add(Shader.Find("HE/SceneLit"));
        warmpShaders.Add(Shader.Find("HE/Glass"));

        foreach (var item in mapper)
        {
            var shader = item.Key;
            var variants = item.Value;
            var assetPath = AssetDatabase.GetAssetPath(shader);
            if (assetPath.StartsWith("Assets/") || assetPath.StartsWith("Packages/"))
            {
                if (assetInfos.ContainsKey(assetPath))
                {
                    var assetInfo = assetInfos[assetPath];
                    var referenceCount = assetInfo.refrenceCount;
                    // Debug.LogError($"shader name {shader.name}  reference count {referenceCount}");
                    if (referenceCount > 5)
                    {
                        warmpShaders.Add(shader);
                    }
                }
                else
                {
                    Debug.LogError("can not find shader asset path {assetPath}");
                }
            }
        }

        List<SerializableShaderVariant> allWarmShaderVariant = new List<SerializableShaderVariant>();
        int maxCount = URSShaderVariantConstant.WARM_ONE_SHADER_VARIANT_COUNT;
        ShaderVariantCollection currentShaderVariant = new ShaderVariantCollection();
        List<ShaderVariantCollection> ShaderVariantCollections = new List<ShaderVariantCollection>();
        ShaderVariantCollections.Add(currentShaderVariant);
        foreach (var warmShader in warmpShaders)
        {
            if (mapper.ContainsKey(warmShader))
            {
                var variants = mapper[warmShader];
                allWarmShaderVariant.AddRange(variants);
            }
        }
        while (allWarmShaderVariant.Count > 0)
        {
            var lastIndex = allWarmShaderVariant.Count - 1;
            var last = allWarmShaderVariant[lastIndex];
            allWarmShaderVariant.RemoveAt(lastIndex);
            if (currentShaderVariant.variantCount < maxCount)
            {
                currentShaderVariant.Add(last.Deserialize());
            }
            else
            {
                currentShaderVariant = new ShaderVariantCollection();
                ShaderVariantCollections.Add(currentShaderVariant);
                currentShaderVariant.Add(last.Deserialize());
            }
        }
        WarmShaderVariants warmShaderVariants = new WarmShaderVariants();
        List<string> paths = new List<string>();
        for (int i = 0; i < ShaderVariantCollections.Count; i++)
        {
            var svs = ShaderVariantCollections[i];
            var path = string.Format(URS.URSShaderVariantConstant.WARM_SHADER_FILE_FORMAT, i);
            paths.Add(path);
            AssetDatabase.CreateAsset(svs, path);
        }
        warmShaderVariants.paths = paths.ToArray();

        var warmJsonPath = URSShaderVariantConstant.WARM_SHADER_JSON_FILE;
        if (File.Exists(warmJsonPath))
        {
            File.Delete(warmJsonPath);
        }
        var jsonContent = UnityEngine.JsonUtility.ToJson(warmShaderVariants, true);
        System.IO.File.WriteAllText(warmJsonPath, jsonContent);
       


        ShaderVariantCollection buildInShaderVariantCollection = new ShaderVariantCollection();
        ShaderVariantCollectionMapper buildInShaderVariantCollectionMapper = new ShaderVariantCollectionMapper(buildInShaderVariantCollection);

        // buildin Shader 穷举 ，这里的代码，可以监听 build player的时候，shader的编译，那样可以完全捕获所有的build in shader variant，我这里只选择了自己项目重要的
        var buildInShaders = new List<Shader>();
        /*
        PostProcessData.ReloadDefaultPostProcessData();
        PostProcessData ppd = PostProcessData.GetDefaultPostProcessData();

        

        //  buildInShaders.Add(ppd.shaders.stopNanPS);
        //  buildInShaders.Add(ppd.shaders.subpixelMorphologicalAntialiasingPS);
        // buildInShaders.Add(ppd.shaders.gaussianDepthOfFieldPS);
        // buildInShaders.Add(ppd.shaders.bokehDepthOfFieldPS);
        // buildInShaders.Add(ppd.shaders.cameraMotionBlurPS);
        // buildInShaders.Add(ppd.shaders.paniniProjectionPS);
        buildInShaders.Add(ppd.shaders.lutBuilderLdrPS);
        buildInShaders.Add(ppd.shaders.lutBuilderHdrPS);
        buildInShaders.Add(ppd.shaders.bloomPS);
        // buildInShaders.Add(ppd.shaders.LensFlareDataDrivenPS);
        buildInShaders.Add(ppd.shaders.scalingSetupPS);
        // buildInShaders.Add(ppd.shaders.easuPS);
        buildInShaders.Add(ppd.shaders.uberPostPS);
        buildInShaders.Add(ppd.shaders.finalPostPassPS);

        //buildInShaders.AddRange(ppd.xPostProcessShaders);
        //buildInShaders.Add(ppd.screenFadeShader);
        */
        buildInShaders.Add(Shader.Find("HighlightPlus/Geometry/Mask"));
        buildInShaders.Add(Shader.Find("HighlightPlus/Geometry/SeeThrough"));
        buildInShaders.Add(Shader.Find("UI/Default"));
        buildInShaders.Add(Shader.Find("Hidden/BOXOPHOBIC/Atmospherics/Height Fog Global"));
        buildInShaders.Add(Shader.Find("Custom/RenderFeature/UIBlur"));

        foreach (var shader in buildInShaders)
        {
            //Debug.LogError("shader.name" + shader.name);
            buildInShaderVariantCollectionMapper.AddShader(shader);
            VariantKeywordCombination vkc = new VariantKeywordCombination();
            vkc.mShader = shader;
            vkc.mKeywordDeclareGroups = new List<KeywordDeclareGroup>();
            VariantKeywordCombinationEditor.TryParseShader(vkc.mShader, vkc.mKeywordDeclareGroups);
            vkc.Execute(buildInShaderVariantCollectionMapper);
        }
        // URSshaderVariantCollection ursShaderVariant = new URSshaderVariantCollection();
        // List<URSshaderVariant> uRSshaderVariants= new List<URSshaderVariant>();

        var buildInMapper = buildInShaderVariantCollectionMapper.GetMapper();

        allWarmShaderVariant.Clear();
        currentShaderVariant = new ShaderVariantCollection();
        ShaderVariantCollections.Clear();
        ShaderVariantCollections.Add(currentShaderVariant);
        foreach (var warmShader in buildInShaders)
        {
            if (buildInMapper.ContainsKey(warmShader))
            {
                var variants = buildInMapper[warmShader];
                allWarmShaderVariant.AddRange(variants);
            }
        }
        while (allWarmShaderVariant.Count > 0)
        {
            var lastIndex = allWarmShaderVariant.Count - 1;
            var last = allWarmShaderVariant[lastIndex];
            allWarmShaderVariant.RemoveAt(lastIndex);
            if (currentShaderVariant.variantCount < maxCount)
            {
                currentShaderVariant.Add(last.Deserialize());
            }
            else
            {
                currentShaderVariant = new ShaderVariantCollection();
                ShaderVariantCollections.Add(currentShaderVariant);
                currentShaderVariant.Add(last.Deserialize());
            }
        }
        for (int i = 0; i < ShaderVariantCollections.Count; i++)
        {
            var svs = ShaderVariantCollections[i];
            var path = string.Format(URS.URSShaderVariantConstant.BUILD_IN_WARM_SHADER_SAVE_PATH_FORMAT, i);
            paths.Add(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(svs, path);
        }
        


        AssetDatabase.CreateAsset(shaderVariantCollection, URS.URSShaderVariantConstant.SHADER_VARIANT_SAVE_PATH);
        AssetDatabase.CreateAsset(blackShaderCollection, URS.URSShaderVariantConstant.BLACK_SHADER_VARIANT_STRIPPER_SAVE_PATH);
        AssetDatabase.Refresh();
       
    }

    private static List<KeywordDeclareGroup> GlobalProjectPerhapsMultiKeywords = new List<KeywordDeclareGroup>();

    private class ShaderPassFogInfo{

        public PassType passType;
        public List<SerializableShaderVariant> shaderVariants= new List<SerializableShaderVariant>();
    
    
    }
    static BuildTaskExportShaderVariantCollection()
    {
        // ÕâÀïÊÇ¼ÓËÙ½âÎö¹ý³Ì£¬Ö±½ÓÇî¾ÙÁË ÏîÄ¿ÖÐµÄ ¾²Ì¬¹Ø¼ü×Ö£¨mutil_compiler£©,²»Í¬ÏîÄ¿×Ô¼º¶¨ÖÆ
        AddGlobalProjectPerhapsMultiKeywords("_", "_SCREEN_SPACE_OCCLUSION");
        AddGlobalProjectPerhapsMultiKeywords("_", "LIGHTMAP_ON");
        AddGlobalProjectPerhapsMultiKeywords("_", "FOG_LINEAR");
        AddGlobalProjectPerhapsMultiKeywords("_", "_CASTING_PUNCTUAL_LIGHT_SHADOW");
        AddGlobalProjectPerhapsMultiKeywords("_", "UNITY_UI_CLIP_RECT");
        AddGlobalProjectPerhapsMultiKeywords("_", "UNITY_UI_ALPHACLIP");
    }
    private static void AddGlobalProjectPerhapsMultiKeywords( params string[] keyword) 
    {
        KeywordDeclareGroup kdg= new KeywordDeclareGroup();
        kdg.keywords = new List<string>();
        kdg.keywords.AddRange(keyword);
        GlobalProjectPerhapsMultiKeywords.Add(kdg);
    }


    private List<KeywordDeclareGroup> GetMultiKeyWordGroups(ShaderVariantCollectionMaterialVariantConverter converter,Shader shader)
    {
        List < KeywordDeclareGroup > kgp= new List<KeywordDeclareGroup>();  
        foreach (KeywordDeclareGroup item in GlobalProjectPerhapsMultiKeywords)
        {
            var keywords = item.keywords;
            bool isOk = false;
            for (int i = 0; i < keywords.Count; i++)
            {
                var keyword = keywords[i];
                if (keyword == "_") continue;
                if (!converter.IsKeywordBelongToShader(shader, keyword))
                {
                    break;
                }
                if (i == keywords.Count - 1) {

                    isOk = true;
                }
            }
            if (isOk) 
            {
                kgp.Add(item);
            }
        }
        if (kgp.Count == 0) 
        {
            kgp.Add(new KeywordDeclareGroup()
            {
                keywords = new List<string>()
            }); ;
        }
        return kgp;
    }

    private void ProcessMaterials()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        collect();
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
        camera.Render();
        _beginTime = EditorApplication.timeSinceStartup;
        Debug.Log("begin render shader scene "+ EditorApplication.timeSinceStartup);
    }
    private static void CreateSphere(Material material, Vector3 position, int x, int y, int index)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.GetComponent<Renderer>().material = material;
        go.transform.position = position;
        go.name = string.Format("Sphere_{0}|{1}_{2}|{3}|{4}", index, x, y, material.name, material.shader.name);
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

        if (_beginTime.HasValue && (EditorApplication.timeSinceStartup - _beginTime.Value) > 2) {
            _beginTime = null;
            Debug.Log(InvokeInternalStaticMethod(typeof(ShaderUtil), "GetCurrentShaderVariantCollectionVariantCount"));
            InvokeInternalStaticMethod(typeof(ShaderUtil), "SaveCurrentShaderVariantCollection",URSShaderVariantConstant.SHADER_VARIANT_SAVE_PATH);
            Debug.Log(InvokeInternalStaticMethod(typeof(ShaderUtil), "GetCurrentShaderVariantCollectionShaderCount"));
            Debug.Log("end render shader scene " + EditorApplication.timeSinceStartup);
            this.FinishTask();
        }
     
    }
}
