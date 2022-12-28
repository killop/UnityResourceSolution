using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Daihenka.AssetPipeline.Filters;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription(typeof(Material), ImportAssetTypeFlag.Models)]
    public class SetupMaterials : AssetProcessor
    {
        public enum TextureMatchMode
        {
            ContainsMaterialName = 0,
            StartsWithMaterialName = 1,
            ContainsModelName = 2,
            StartsWithModelName = 3,
            TextureName = 4,
        }

        public enum TextureSearchMode
        {
            Local = 0,
            RecursiveUp = 1
        }

        public List<MaterialSetup> materialSetups = new List<MaterialSetup>();
        public override int Priority => 10;

        public override void OnPostprocess(Object asset, string assetPath)
        {
            if (materialSetups.Count == 0)
            {
                return;
            }

            var processedMaterials = new HashSet<Material>();
            var renderers = ((GameObject) asset).GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                foreach (var material in materials)
                {
                    // skip materials that have been processed or no material exists for the slot
                    if (!material || processedMaterials.Contains(material) || ImportProfileUserData.HasProcessor(AssetDatabase.GetAssetPath(material), this))
                    {
                        continue;
                    }

                    foreach (var matSetup in materialSetups)
                    {
                        if (matSetup.materialFilter.IsMatch(material.name))
                        {
                            material.shader = matSetup.shader;
                            var texturePropertyNames = material.GetTexturePropertyNames().ToList();
                            foreach (var map in matSetup.propertyMappings)
                            {
                                if (!map.overridden) {
                                    continue;
                                }
                                switch (map.materialPropertyType)
                                {
                                    case ShaderUtil.ShaderPropertyType.TexEnv:
                                        if (!texturePropertyNames.Contains(map.materialPropertyDescription) || !material.GetTexture(map.materialPropertyDescription))
                                        {
                                            material.SetTexture(map.materialPropertyName, FindTexture(matSetup.textureSearch, map, assetPath, material.name, matSetup.materialFilter));
                                            EditorUtility.SetDirty(material);
                                        }

                                        break;
                                    case ShaderUtil.ShaderPropertyType.Color:
                                        material.SetColor(map.materialPropertyName, map.colorValue);
                                        EditorUtility.SetDirty(material);
                                        break;
                                    case ShaderUtil.ShaderPropertyType.Vector:
                                        material.SetVector(map.materialPropertyName, map.vectorValue);
                                        EditorUtility.SetDirty(material);
                                        break;
                                    case ShaderUtil.ShaderPropertyType.Float:
                                    case ShaderUtil.ShaderPropertyType.Range:
                                        material.SetFloat(map.materialPropertyName, map.floatValue);
                                        EditorUtility.SetDirty(material);
                                        break;
                                }
                            }

                            processedMaterials.Add(material);
                        }
                    }
                }
            }

            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Assigned textures to materials for \"<b>{assetPath}</b>\"");
        }

        Texture FindTexture(TextureSearchMode searchMode, MaterialTextureMap map, string assetPath, string materialName, NamingConventionRule materialFilter)
        {
            var assetFolder = Path.GetDirectoryName(assetPath).FixPathSeparators();

            if (searchMode == TextureSearchMode.Local)
            {
                var texture = FindTextureInFolder(map.textureNameFilter, assetPath, assetFolder, materialName, materialFilter);
                if (texture)
                {
                    return texture;
                }
            }
            else
            {
                var directoryInfo = new DirectoryInfo(assetFolder);
                while (directoryInfo == null || directoryInfo.FullName.FixPathSeparators().StartsWith(Application.dataPath))
                {
                    var folder = directoryInfo.FullName.FixPathSeparators().Replace(Application.dataPath, "Assets/");
                    var texture = FindTextureInFolder(map.textureNameFilter, assetPath, folder, materialName, materialFilter);
                    if (texture)
                    {
                        return texture;
                    }

                    directoryInfo = directoryInfo.Parent;
                }
            }

            return null;
        }

        Texture FindTextureInFolder(PathFilter filter, string assetPath, string folder, string materialName, NamingConventionRule materialFilter)
        {
            var subFilter = new PathFilter {ignoreCase = filter.ignoreCase, matchType = filter.matchType, pattern = ReplaceVariables(filter.pattern, assetPath, materialName, materialFilter)};
            var path = AssetDatabaseUtility.FindAssetPaths("t:texture", folder).FirstOrDefault(x => subFilter.IsMatch(Path.GetFileNameWithoutExtension(x)));
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture>(path);
        }

        string ReplaceVariables(string input, string assetPath, string materialName, NamingConventionRule materialFilter)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var variables = GetVariables(assetPath);
            variables.Add("materialName", materialName);
            variables.Add("modelName", Path.GetFileNameWithoutExtension(assetPath));
            AddTemplateVariables(variables, materialFilter.Parse(materialName));

            var matches = s_VariableCaptureRegex.Matches(input);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                input = ReplaceVariablesFromCaptureGroups(input, variables, assetPath, match);
            }

            return input;
        }

        static bool IsTextureMatch(string textureName, TextureMatchMode textureMatchMode, string assetName, string materialName)
        {
            switch (textureMatchMode)
            {
                case TextureMatchMode.ContainsMaterialName:
                    return textureName.Contains(materialName, StringComparison.OrdinalIgnoreCase);
                case TextureMatchMode.StartsWithMaterialName:
                    return textureName.StartsWith(materialName, StringComparison.OrdinalIgnoreCase);
                case TextureMatchMode.ContainsModelName:
                    return textureName.Contains(assetName, StringComparison.OrdinalIgnoreCase);
                case TextureMatchMode.StartsWithModelName:
                    return textureName.StartsWith(assetName, StringComparison.OrdinalIgnoreCase);
                case TextureMatchMode.TextureName:
                    return true;
            }

            return false;
        }

        [Serializable]
        public class MaterialTextureMap
        {
            public string materialPropertyDescription;
            public string materialPropertyName;
            public ShaderUtil.ShaderPropertyType materialPropertyType;
            public PathFilter textureNameFilter = new PathFilter {matchType = StringMatchType.Contains, pattern = ""};
            public Color colorValue;
            public Vector4 vectorValue;
            public float floatValue;
            public float minRange = 0;
            public float maxRange = 1;
            public bool isHidden;
            public bool overridden = true;

            public MaterialTextureMap(string name, string description, PathFilter textureFilter, bool isHidden)
            {
                materialPropertyName = name;
                materialPropertyDescription = description;
                textureNameFilter = textureFilter;
                materialPropertyType = ShaderUtil.ShaderPropertyType.TexEnv;
                this.isHidden = isHidden;
            }

            public MaterialTextureMap(string name, string description, ShaderUtil.ShaderPropertyType type, Vector4 vector, bool isHidden)
            {
                materialPropertyName = name;
                materialPropertyDescription = description;
                materialPropertyType = type;
                if (type == ShaderUtil.ShaderPropertyType.Color)
                {
                    colorValue = new Color(vector.x, vector.y, vector.z, vector.w);
                }
                else
                {
                    vectorValue = vector;
                }

                this.isHidden = isHidden;
            }

            public MaterialTextureMap(string name, string description, float value, bool isHidden)
            {
                materialPropertyName = name;
                materialPropertyDescription = description;
                materialPropertyType = ShaderUtil.ShaderPropertyType.Float;
                floatValue = value;
                this.isHidden = isHidden;
            }

            public MaterialTextureMap(string name, string description, float value, float min, float max, bool isHidden)
            {
                materialPropertyName = name;
                materialPropertyDescription = description;
                materialPropertyType = ShaderUtil.ShaderPropertyType.Range;
                floatValue = value;
                minRange = min;
                maxRange = max;
                this.isHidden = isHidden;
            }
        }

        [Serializable]
        public class MaterialSetup
        {
            public Shader shader;
            public NamingConventionRule materialFilter = new NamingConventionRule();
            public TextureSearchMode textureSearch = TextureSearchMode.Local;
            public List<MaterialTextureMap> propertyMappings = new List<MaterialTextureMap>();
        }
    }
}
