using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Daihenka.AssetPipeline.NamingConvention;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Daihenka.AssetPipeline.Import
{
    public abstract class AssetProcessor : ScriptableObject
    {
        static readonly List<string> s_ForceApplyPaths = new List<string>();
        protected static readonly Regex s_VariableCaptureRegex = new Regex(@"{(?<placeholder>.*?)(:(?<expression>(\\}|.)+?))?}");
        [HideInInspector] public AssetFilter parent;
        public bool enabled;
        public bool runOnImport;
        public virtual int Priority => 0;
        public virtual int Version => 0;
        public Texture Icon => GetType().GetIcon();

        public string GetName(bool useFullName = false)
        {
            return GetType().GetProcessorName(useFullName);
        }

        internal bool HasOverriddenMethod(string methodName)
        {
            return GetType().HasOverriddenMethod(methodName);
        }

        internal bool HasOverriddenMethods(IList<string> methodNames)
        {
            return GetType().HasOverriddenMethods(methodNames);
        }

        public static bool IsForceApply(string assetPath)
        {
            return s_ForceApplyPaths.Contains(assetPath);
        }

        public static bool IsForceApply(AssetImporter importer)
        {
            return s_ForceApplyPaths.Contains(importer.assetPath);
        }

        public static void SetForceApply(IEnumerable<string> assetPaths, bool value)
        {
            foreach (var assetPath in assetPaths)
            {
                SetForceApply(assetPath, value);
            }
        }

        public static void SetForceApply(string assetPath, bool value)
        {
            if (value && !s_ForceApplyPaths.Contains(assetPath))
            {
                s_ForceApplyPaths.Add(assetPath);
            }
            else if (!value)
            {
                s_ForceApplyPaths.Remove(assetPath);
            }
        }

        public virtual bool ShouldImport(AssetImporter importer)
        {
            return ShouldImport(importer.assetPath) || importer.importSettingsMissing;
        }

        public virtual bool ShouldImport(string assetPath)
        {
            return runOnImport || IsForceApply(assetPath);
        }

        protected virtual Object[] PrepareEmbeddedObjects(ImportAssetType assetType)
        {
            return null;
        }

        public virtual void OnPostprocess(Object asset, string assetPath)
        {
        }

        public virtual void OnDeletedAsset (string assetPath)
        {
        }

        public virtual void OnMovedAsset(Object asset, string sourcePath, string destinationPath)
        {
        }

        /// <summary>
        /// Feeds a source material.
        ///
        /// The returned material will be assigned to the renderer. If you return null, Unity will use its default material finding / generation method to assign a material. The sourceMaterial is generated directly from the model before importing and will be destroyed immediately after OnAssignMaterial.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="renderer"></param>
        public virtual Material OnAssignMaterialModel(string assetPath, AssetImporter importer, Material material, Renderer renderer)
        {
            return null;
        }

        public virtual void OnPostprocessAnimation(string assetPath, AssetImporter importer, GameObject root, AnimationClip clip)
        {
        }

        public virtual void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName)
        {
        }

        public virtual void OnPostprocessAudio(string assetPath, AudioImporter importer, AudioClip audioClip)
        {
        }

        public virtual void OnPostprocessCubemap(string assetPath, TextureImporter importer, Cubemap texture)
        {
        }

        /// <summary>
        /// This function is called when the animation curves for a custom property are finished importing.
        ///
        /// It is called for every GameObject with an animated custom property. Each animated property has an animation curve that is represented by an EditorCurveBinding. This lets you dynamically add components to a GameObject and retarget the EditorCurveBindings to any animatable property.
        /// </summary>
        /// <param name="gameObject">The GameObject with animated custom properties</param>
        /// <param name="bindings">The animation curves bound to the custom properties</param>
        public virtual void OnPostprocessGameObjectWithAnimatedUserProperties(string assetPath, AssetImporter importer, GameObject gameObject, EditorCurveBinding[] bindings)
        {
        }

        /// <summary>
        /// Gets called for each GameObject that had at least one user property attached to it in the imported file.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="propNames">Contains all the names of the properties found</param>
        /// <param name="values">Contains all the actual values that match propNames</param>
        public virtual void OnPostprocessGameObjectWithUserProperties(string assetPath, AssetImporter importer, GameObject go, string[] propNames, object[] values)
        {
        }

        public virtual void OnPostprocessMaterial(string assetPath, AssetImporter importer, Material material)
        {
        }

        /// <summary>
        /// This function is called when a new transform hierarchy has finished importing.
        ///
        /// The ModelImporter calls this function for every root transform hierarchy in the source model file. This lets you change each root transform hierarchy before other artifacts are imported, such as the avatar or animation clips.
        ///
        /// If this function destroys the root hierarchy, any associated animation clips are not imported.
        /// </summary>
        /// <param name="root">The root GameObject of the imported asset</param>
        public virtual void OnPostprocessMeshHierarchy(string assetPath, AssetImporter importer, GameObject root)
        {
        }

        /// <summary>
        /// Add this function to a subclass to get a notification when a model has completed importing.
        ///
        /// This lets you modify the imported Game Object, Meshes, AnimationClips referenced by it. Please note that the GameObjects, AnimationClips and Meshes only exist during the import and will be destroyed immediately afterwards.
        ///
        /// This function is called before the final Prefab is created and before it is written to disk, thus you have full control over the generated game objects and components.
        ///
        /// Any references to game objects or meshes will become invalid after the import has been completed. Thus it is not possible to create a new Prefab in a different file from OnPostprocessModel that references meshes in the imported fbx file
        /// </summary>
        public virtual void OnPostprocessModel(string assetPath, ModelImporter importer, GameObject root)
        {
        }

        /// <summary>
        /// Add this function to a subclass to get a notification when a SpeedTree asset has completed importing.
        ///
        /// This function behaves much like OnPostprocessModel where modifications are allowed on the final imported Prefab before being saved on the disk.
        /// </summary>
        /// <param name="root">The root GameObject of the imported asset</param>
        public virtual void OnPostprocessSpeedTree(string assetPath, SpeedTreeImporter importer, GameObject root)
        {
        }

        /// <summary>
        /// Add this function to a subclass to get a notification when an texture of sprite(s) has completed importing.
        ///
        /// For Multiple sprite-mode assets each sprite will be passed in the second argument as an array of sprites.
        /// </summary>
        public virtual void OnPostprocessSprites(string assetPath, TextureImporter importer, Texture2D texture, Sprite[] sprites)
        {
        }

        public virtual void OnPostprocessTexture(string assetPath, TextureImporter importer, Texture2D texture)
        {
        }

        /// <summary>
        /// This is called before animation from a model (.fbx, .mb file, etc.) is imported.
        /// </summary>
        public virtual void OnPreprocessAnimation(string assetPath, ModelImporter importer)
        {
        }

        /// <summary>
        /// This is called before any asset is imported.
        /// </summary>
        public virtual void OnPreprocessAsset(string assetPath, AssetImporter importer)
        {
        }

        public virtual void OnPreprocessAudio(string assetPath, AudioImporter importer)
        {
        }

        public virtual void OnPreprocessModel(string assetPath, ModelImporter importer)
        {
        }

        public virtual void OnPreprocessSpeedTree(string assetPath, SpeedTreeImporter importer)
        {
        }

        public virtual void OnPreprocessTexture(string assetPath, TextureImporter importer)
        {
        }

        /// <summary>
        /// Add this function to a subclass to recieve a notification when a material is imported from a Model Importer.
        ///
        /// This function is only called when ModelImporter.UseMaterialDescriptionPostprocessor is true. This function provides user control over material properties and animations during model import. The MaterialDescription structure contains all the material data read in the imported file which can be used to populate the material and animation clips in parameters.
        /// </summary>
        /// <param name="description">MaterialDescription structure describing the imported material properties and animations</param>
        /// <param name="material">The material generated by the Model Importer</param>
        /// <param name="animations">The animation clips generated by the Model Importer</param>
        public virtual void OnPreprocessMaterialDescription(string assetPath, AssetImporter importer, MaterialDescription description, Material material, AnimationClip[] animations)
        {
        }

        public virtual bool IsConfigOK(AssetImporter importer) 
        {
            return true;
        }

        protected string ReplaceVariables(string input, string assetPath)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var matches = s_VariableCaptureRegex.Matches(input);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                input = ReplaceVariablesFromCaptureGroups(input, assetPath, match);
            }

            return input;
        }

        static readonly Dictionary<string, StringConvention> s_ConventionExpressionMap = new Dictionary<string, StringConvention>
        {
            {@"\snake", StringConvention.SnakeCase},
            {@"\usnake", StringConvention.UpperSnakeCase},
            {@"\kebab", StringConvention.KebabCase},
            {@"\camel", StringConvention.CamelCase},
            {@"\pascal", StringConvention.PascalCase},
            {@"\upper", StringConvention.UpperCase},
            {@"\lower", StringConvention.LowerCase},
            {@"\none", StringConvention.None}
        };


        protected string ReplaceVariablesFromCaptureGroups(string input, string assetPath, Match match)
        {
            return ReplaceVariablesFromCaptureGroups(input, GetVariables(assetPath), assetPath, match);
        }

        protected string ReplaceVariablesFromCaptureGroups(string input, Dictionary<string, string> variables, string assetPath, Match match)
        {
            var placeholderGroup = match.Groups["placeholder"];
            if (!placeholderGroup.Success)
            {
                return input;
            }

            var variableName = placeholderGroup.Value;
            if (string.IsNullOrWhiteSpace(variableName))
            {
                return input;
            }

            var expressionGroup = match.Groups["expression"];
            var convention = StringConvention.None;
            if (expressionGroup.Success)
            {
                if (s_ConventionExpressionMap.ContainsKey(expressionGroup.Value))
                {
                    convention = s_ConventionExpressionMap[expressionGroup.Value];
                }
            }

            if (variables.ContainsKey(variableName))
            {
                var value = variables[variableName];
                if (convention != StringConvention.None)
                {
                    value = value.ToConvention(convention);
                }

                input = input.Replace(match.Groups[0].Value, value);
            }

            return input;
        }

        protected Dictionary<string, string> GetVariables(string assetPath)
        {
            var variables = new Dictionary<string, string>();
            variables.Add("assetFilename", Path.GetFileNameWithoutExtension(assetPath));
            variables.Add("assetFileExtension", Path.GetExtension(assetPath));
            var dirInfo = new DirectoryInfo(Path.GetDirectoryName(assetPath));
            variables.Add("assetFolderName", dirInfo.Name);
            var parentDirInfo = dirInfo.Parent;
            variables.Add("assetParentFolderName", parentDirInfo == null ? dirInfo.Name : parentDirInfo.Name);
            var parentParentDirInfo = parentDirInfo?.Parent;
            variables.Add("assetParentParentFolderName", parentParentDirInfo == null ? dirInfo.Name : parentParentDirInfo.Name);

            AddTemplateVariables(variables, parent.parent.path.Parse(assetPath));
            AddTemplateVariables(variables, parent.file.Parse(Path.GetFileNameWithoutExtension(assetPath)));
            return variables;
        }

        protected static void AddTemplateVariables(Dictionary<string, string> variables, TemplateData parentVariables)
        {
            foreach (var kvp in parentVariables)
            {
                if (!variables.ContainsKey(kvp.Key))
                {
                    variables.Add(kvp.Key, kvp.Value.value);
                }
                else
                {
                    variables[kvp.Key] = kvp.Value.value;
                }
            }
        }

    }
}