using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public class BuildTaskValidateParticleSystem : BuildTask
{

    public override void BeginTask()
    {
        base.BeginTask();
        var assetInfos = this.GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);

        foreach (var assetPath in assetInfos.Keys)
        {
            if (Path.GetExtension(assetPath) == ".prefab")
            {
                if (assetPath.Contains("/VFX/"))
                {
                    var gm = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    var particleSystems= gm.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (var particleSystem in particleSystems)
                    {
                        var change = false;
                        var main = particleSystem.main;
                        bool renderMesh = false;
                        var render= particleSystem.GetComponent<ParticleSystemRenderer>();
                        renderMesh = render.renderMode == ParticleSystemRenderMode.Mesh;
                        if (main.prewarm) 
                        {
                           // main.prewarm = false;
                           // change= true;
                            Debug.LogWarning($"需要人工确认 {assetPath}下的 {particleSystem.gameObject.name} prewarm 是否要打开");
                        }

                        // int maxParticleCount = 30;
                        // if (renderMesh&& render.enabled)
                        // {
                        //     maxParticleCount = 5;
                        // }
                        int unityDeault = 1000;
                        int myDefault = 100;
                        if (main.maxParticles == unityDeault)
                        {
                            main.maxParticles= myDefault;
                            change = true;
                            Debug.LogWarning($"修正{assetPath} 下的 {particleSystem.gameObject.name} 最大粒子数为{myDefault}");
                        }
                        if (renderMesh && render.mesh)
                        {
                            var current = render.mesh.triangles.Count();
                            var targetCount = 500;
                            if (current > targetCount) 
                            {
                                Debug.LogError($"需要手工修正 {assetPath} 下的 {particleSystem.gameObject.name} 引用的mesh面数过多 {current}，高于建议的 {targetCount}");
                            }
                        }
                        if (change) 
                        {
                            EditorUtility.SetDirty(gm);
                        }
                    }
                }
            }
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        this.FinishTask();
    }

}
