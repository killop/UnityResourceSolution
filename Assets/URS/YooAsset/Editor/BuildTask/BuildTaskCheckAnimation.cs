
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Rendering;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
using System;

public class BuildTaskCheckAnimation : BuildTask
{

    public override void BeginTask()
    {
        base.BeginTask();
        EditorUtility.DisplayProgressBar("CheckAnimation", "检查动画资源", 0);
        var assetInfos = this.GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);
        var propertyCheck = GetPropertyIsAnimatable();
        foreach (var assetPath in assetInfos.Keys)
        {
            if (Path.GetExtension(assetPath) == ".prefab")
            {
                var gm = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                Check(gm, assetPath, propertyCheck);
            }
        }
        EditorUtility.ClearProgressBar();
        this.FinishTask();
    }


    static void Check(GameObject gm, string path,MethodInfo propertyCheck )
    {
        var animators = gm.GetComponentsInChildren<Animator>();
        if (animators != null)
        {
            foreach (var animator in animators)
            {

                var runtime = animator.runtimeAnimatorController;
                var animatorGameObject = animator.gameObject;

                if (runtime!=null&&runtime.animationClips != null && runtime.animationClips.Length > 0)
                {
                    foreach (var clip in runtime.animationClips)
                    {
                        List<EditorCurveBinding> binds = new List<EditorCurveBinding>();
                        var bindsCurve = AnimationUtility.GetCurveBindings(clip);
                        var bindsObjectReference = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                        binds.AddRange(bindsCurve);
                        binds.AddRange(bindsObjectReference);

                        foreach (var bind in binds)
                        {
                            Transform child = animatorGameObject.transform.Find(bind.path);

                            if (child == null)
                            {
                                Debug.LogError($"ERROR:GameObjectPath {path},animatorPath {GetRelativePath(gm, animatorGameObject)} clipName {clip.name} ,Can not find bindPath {bind.path} ,bindType {bind.type.FullName}, bind.propertyName {bind.propertyName}");
                                continue;
                            }
                            UnityEngine.Object target = null;
                            if (bind.type == typeof(GameObject))
                            {
                                target = child.gameObject;
                            }
                            else 
                            {
                                target = child.GetComponent(bind.type);
                                if (target == null)
                                {
                                    Debug.LogError($"ERROR:GameObjectPath {path},animatorPath {GetRelativePath(gm, animatorGameObject)} clipName {clip.name} ,Can not find bindType {bind.type.FullName} bindPath {bind.path}, bind.propertyName {bind.propertyName}");
                                    continue;
                                }
                            }
                            if (bind.type == typeof(Animator))
                            {
                                if (bind.propertyName.StartsWith("RootQ.") || bind.propertyName.StartsWith("RootT."))
                                {
                                    continue;
                                }
                            }
                           // else if (bind.type == typeof(Transform)) 
                           // {
                           //     if (bind.propertyName.StartsWith("m_LocalRotation.") || bind.propertyName.StartsWith("m_LocalScale.") || bind.propertyName.StartsWith("m_LocalPosition."))
                           //     {
                           //         continue;
                           //     }
                           // }
                            var result= propertyCheck.Invoke(null, new object[] { target, bind.propertyName,null});
                            var PropertyIsAnimatable =(bool) result;
                            if (!PropertyIsAnimatable)
                            {
                                Debug.LogError($"ERROR:GameObjectPath {path},animatorPath {GetRelativePath(gm, animatorGameObject)} clipName {clip.name} ,Can not find bind.propertyName {bind.propertyName}  bindType {bind.type.FullName} bindPath {bind.path}");
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }

    static string GetRelativePath(GameObject root, GameObject children)
    {
        List<string> path = new List<string>();
        path.Add(children.name);
        while (children != root)
        {
            var parent = children.transform.parent.gameObject;
            path.Add(parent.name);
            children = parent;
        }
        path.Reverse();
        return string.Join("/", path);

    }

    static MethodInfo GetPropertyIsAnimatable() {

        Assembly asm = Assembly.GetAssembly(typeof(UnityEditorInternal.AssetStore));
        Type type = asm.GetType("UnityEditorInternal.AnimationWindowUtility");

        if (type == null)
        {
            return null;
        }

        MethodInfo propertyIsAnimatable = type.GetMethod("PropertyIsAnimatable", BindingFlags.Static | BindingFlags.Public);
        return propertyIsAnimatable;
    }

}

