using System.Collections.Generic;
using System.Reflection;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal class AssetImportPostprocessor : AssetPostprocessor
    {
        static readonly Dictionary<string, List<AssetProcessor>> s_CachedProcessors = new Dictionary<string, List<AssetProcessor>>();

        static List<AssetProcessor> GetProcessors(string assetPath, string methodName)
        {
            return GetProcessors(assetPath, new[] {methodName});
        }

        static List<AssetProcessor> GetProcessors(string assetPath, string[] methodNames)
        {
            if (!s_CachedProcessors.ContainsKey(assetPath))
            {
                var processors = AssetImportPipeline.GetProcessorsForAsset(assetPath);
                if (s_CachedProcessors.ContainsKey(assetPath))
                {
                    s_CachedProcessors[assetPath] = processors;
                }
                else
                {
                    s_CachedProcessors.Add(assetPath, processors);
                }
            }

            var isOnDeletedAsset = methodNames.Length == 1 && methodNames[0] == "OnDeletedAsset";
            var result = new List<AssetProcessor>(s_CachedProcessors[assetPath].Count);
            foreach (var processor in s_CachedProcessors[assetPath])
            {
                if (processor.FireOnEveryImport()|| AssetProcessor.IsForceApply(assetPath) || ((isOnDeletedAsset || !ImportProfileUserData.HasProcessor(assetPath, processor)) && processor.HasOverriddenMethods(methodNames)))
                {
                    result.Add(processor);
                }
            }

            return result;
        }

        void OnPreprocessAnimation()
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPreprocessAnimation(assetPath, assetImporter as ModelImporter);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPreprocessAsset()
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPreprocessAsset(assetPath, assetImporter);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPreprocessAudio()
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPreprocessAudio(assetPath, assetImporter as AudioImporter);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPreprocessModel()
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPreprocessModel(assetPath, assetImporter as ModelImporter);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPreprocessTexture()
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPreprocessTexture(assetPath, assetImporter as TextureImporter);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPreprocessMaterialDescription(assetPath, assetImporter, description, material, animations);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPreprocessSpeedTree()
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPreprocessSpeedTree(assetPath, assetImporter as SpeedTreeImporter);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            Material outMaterial = null;
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                outMaterial = processor.OnAssignMaterialModel(assetPath, assetImporter, material, renderer);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
            return outMaterial;
        }

        void OnPostprocessAnimation(GameObject root, AnimationClip clip)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessAnimation(assetPath, assetImporter, root, clip);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessAudio(AudioClip audioClip)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessAudio(assetPath, assetImporter as AudioImporter, audioClip);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessCubemap(Cubemap texture)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessCubemap(assetPath, assetImporter as TextureImporter, texture);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessMaterial(Material material)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessMaterial(assetPath, assetImporter, material);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessModel(GameObject go)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessModel(assetPath, assetImporter as ModelImporter, go);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessSprites(assetPath, assetImporter as TextureImporter, texture, sprites);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessTexture(Texture2D texture)
        {
            CustomAssetImportPostprocessor.OnPostprocessTexture(assetPath, assetImporter);
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessTexture(assetPath, assetImporter as TextureImporter, texture);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessMeshHierarchy(GameObject root)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessMeshHierarchy(assetPath, assetImporter, root);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessSpeedTree(GameObject go)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessSpeedTree(assetPath, assetImporter as SpeedTreeImporter, go);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] propNames, object[] values)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessGameObjectWithUserProperties(assetPath, assetImporter, go, propNames, values);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessGameObjectWithAnimatedUserProperties(GameObject go, EditorCurveBinding[] bindings)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessGameObjectWithAnimatedUserProperties(assetPath, assetImporter, go, bindings);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName)
        {
            foreach (var processor in GetProcessors(assetPath, MethodBase.GetCurrentMethod().Name))
            {
                processor.OnPostprocessAssetbundleNameChanged(assetPath, previousAssetBundleName, newAssetBundleName);
            }

            ImportProfileUserData.Get(assetPath).SaveUserData();
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var assetPath in importedAssets)
            {
                foreach (var processor in GetProcessors(assetPath, "OnPostprocess"))
                {
                    processor.OnPostprocess(AssetDatabase.LoadMainAssetAtPath(assetPath), assetPath);
                }

                ImportProfileUserData.Get(assetPath).SaveUserData();
                AssetProcessor.SetForceApply(assetPath, false);
            }

            foreach (var assetPath in deletedAssets) 
            {
                if (!string.IsNullOrEmpty(assetPath))
                {
                    foreach (var processor in GetProcessors(assetPath, "OnDeletedAsset"))
                    {
                        processor.OnDeletedAsset(assetPath);
                    }
                }
            }

            for (var i = 0; i < movedAssets.Length; i++)
            {
                foreach (var processor in GetProcessors(movedAssets[i], "OnMovedAsset"))
                {
                    processor.OnMovedAsset(AssetDatabase.LoadMainAssetAtPath(movedAssets[i]), movedFromAssetPaths[i], movedAssets[i]);
                }

                ImportProfileUserData.Get(movedAssets[i]).SaveUserData();
                AssetProcessor.SetForceApply(movedFromAssetPaths[i], false);
                AssetProcessor.SetForceApply(movedAssets[i], false);
            }

            ImportProfileUserData.ClearCache();
            s_CachedProcessors.Clear();
            AssetImportPipeline.ClearCachedAssetPaths();
        }
    }
}