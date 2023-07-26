
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TagInfo = System.Collections.Generic.Dictionary<Bewildered.SmartLibrary.TagRule.TagRuleType, string>;
using URS;
using YooAsset.Utility;
using System.Linq;
using YooAsset;
using UnityEditor.Build.Content;
using BuildCompression = UnityEngine.BuildCompression;
using UnityEditor.Search;
using Context = System.Collections.Generic.Dictionary<string, object>;
using MHLab.Patch.Core.Utilities;


public class BuildTaskReBuildAssetBundle : BuildTask
{
    private float time = 0;
    public override void BeginTask()
    {
        base.BeginTask();
        time = 0;
      
    }
    public override void OnTaskUpdate()
    {
        time = time + Time.realtimeSinceStartup;
        if (time > 3) 
        {
            BuildBundleResource();
            this.FinishTask();
            time = 0;
        }
    }
    public ReturnCode BuildBundleResource()
    {
        var buildTasks = new List<IBuildTask>();

        buildTasks.Add(new BuildPlayerScripts());
        buildTasks.Add(new PostScriptsCallback());
        // Dependency
        buildTasks.Add(new CalculateSceneDependencyData());
        buildTasks.Add(new CalculateAssetDependencyData());
#if SPRITE_STRIP
        buildTasks.Add(new BuildTaskStripSpriteInAtlas());
#endif
        buildTasks.Add(new StripUnusedSpriteSources());
        buildTasks.Add(new CreateBuiltInShadersBundle("unity_buildin_shader.bundle"));
        buildTasks.Add(new CreateMonoScriptBundle("unity_monoscript.bundle"));

        buildTasks.Add(new PostDependencyCallback());

        // Packing
        buildTasks.Add(new GenerateBundlePacking());
        buildTasks.Add(new UpdateBundleObjectLayout());
        buildTasks.Add(new GenerateBundleCommands());
        buildTasks.Add(new GenerateSubAssetPathMaps());
        buildTasks.Add(new GenerateBundleMaps());
        buildTasks.Add(new PostPackingCallback());

        // Writing
        buildTasks.Add(new WriteSerializedFiles());
        buildTasks.Add(new ArchiveAndCompressBundles());
        //var bundleDependencyTask = new GenerateBundleDependencyTask();
        // buildTasks.Add(bundleDependencyTask);
        buildTasks.Add(new GenerateLinkXml());
        buildTasks.Add(new PostWritingCallback());

        var extractData = new ExtractDataTask();
        buildTasks.Add(extractData);
        var generation = new BuildLayoutGenerationTask();
        buildTasks.Add(generation);
        var checkHash = new BuidTaskCheckBundleHash();
        buildTasks.Add(checkHash);
        var targetGroup = BuildPipeline.GetBuildTargetGroup(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
        var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        var outFolder = Build.GetTempBundleOutDirectoryPath();
        var buildParams = new URSBundleBuildParameters(
               target,
               targetGroup,
               outFolder
         );
        var bundleInfo = GetData<Dictionary<string, BundleInfo>>(CONTEXT_BUNDLE_INFO);
        var assetBundleBuilds = new List<AssetBundleBuild>();
        foreach (var kv in bundleInfo)
        {
            var assetsInputDef = new AssetBundleBuild();
            assetsInputDef.assetBundleName = kv.Key;
            kv.Value.paths.Sort();
            assetsInputDef.assetNames = kv.Value.paths.ToArray();
            assetBundleBuilds.Add(assetsInputDef);
        }
        var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(assetBundleBuilds), out var results, buildTasks);
        if (exitCode < ReturnCode.Success)
        {
            Debug.LogError("extir code" + exitCode);
            return ReturnCode.Error;
        }

        var manifest = ScriptableObject.CreateInstance<UnityEngine.Build.Pipeline.CompatibilityAssetBundleManifest>();
        manifest.SetResults(results.BundleInfos);
        var manifestFileName = outFolder + "/all.manifest";
        if (File.Exists(manifestFileName)) 
        {
            File.Delete(manifestFileName);
        }
        File.WriteAllText(manifestFileName, manifest.ToString());
        SetData(CONTEXT_BUNDLE_RESULT, results);
        SetData(CONTEXT_BUNDLE_LAYOUT, generation.LayoutLookupTables);
        SetData(CONTEXT_VERSION_BUNDLE_HASH, checkHash.BundleHash);
        return ReturnCode.Success;
    }
}
