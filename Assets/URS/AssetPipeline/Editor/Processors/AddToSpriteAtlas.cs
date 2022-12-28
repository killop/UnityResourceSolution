using System.IO;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription(typeof(SpriteAtlas), ImportAssetTypeFlag.Textures)]
    public class AddToSpriteAtlas : AssetProcessor
    {
        [SerializeField] bool addFolderToSpriteAtlas;
        [SerializeField] TargetPathType pathType;
        [SerializeField] string destination;
        [SerializeField] DefaultAsset targetFolder;
        [SerializeField] string spriteAtlasName;
        [SerializeField] bool includeInBuild = true;
        [SerializeField] bool createVariantAtlas;
        [SerializeField] string variantSuffix = "-SD";
        [SerializeField] bool includeVariantInBuild = true;
        [SerializeField] float variantScale = 0.5f;

        public override void OnPostprocess(Object asset, string assetPath)
        {
            var assetFolder = GetDestinationPath(assetPath);
            var assetFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetFolder);
            var spriteAtlas = GetExistingMasterSpriteAtlas(assetFolder);

            if (spriteAtlas == null)
            {
                var atlasName = ReplaceVariables(spriteAtlasName, assetPath);
                spriteAtlas = CreateMasterSpriteAtlas(assetFolder, atlasName);
                if (createVariantAtlas)
                {
                    CreateVariantSpriteAtlas(spriteAtlas, assetFolder, atlasName);
                }
            }
            else
            {
                var packables = spriteAtlas.GetPackables();
                if ((addFolderToSpriteAtlas && packables.Contains(assetFolderAsset)) || (!addFolderToSpriteAtlas && packables.Contains(asset)))
                {
                    ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
                    return;
                }
            }

            spriteAtlas.Add(new[] {addFolderToSpriteAtlas ? assetFolderAsset : asset});
            EditorUtility.SetDirty(spriteAtlas);
            AssetDatabase.SaveAssets();
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Added \"<b>{(addFolderToSpriteAtlas ? assetFolder : assetPath)}</b>\" to sprite atlas: \"<b>{AssetDatabase.GetAssetPath(spriteAtlas)}</b>\"");
        }

        string GetDestinationPath(string assetPath)
        {
            var destinationPath = destination;
            if (pathType == TargetPathType.Absolute || pathType == TargetPathType.Relative)
            {
                destinationPath = ReplaceVariables(destinationPath, assetPath);
            }

            destinationPath = pathType.GetFolderPath(assetPath, destinationPath, targetFolder);
            if (pathType == TargetPathType.TargetFolder && !targetFolder)
            {
                Debug.LogWarning($"[{GetName()}] Target Folder was not set.  Extracting materials to <b>{destinationPath}</b>");
            }

            destinationPath = destinationPath.FixPathSeparators();
            PathUtility.CreateDirectoryIfNeeded(destinationPath);

            return destinationPath;
        }

        static SpriteAtlas GetExistingMasterSpriteAtlas(string assetFolder)
        {
            var spriteAtlases = AssetDatabaseUtility.FindAndLoadAssets<SpriteAtlas>("t:SpriteAtlas", assetFolder);
            return spriteAtlases.FirstOrDefault(atlas => atlas && !atlas.isVariant);
        }

        SpriteAtlas CreateMasterSpriteAtlas(string assetFolder, string atlasName)
        {
            SpriteAtlas spriteAtlas;
            spriteAtlas = new SpriteAtlas();
            spriteAtlas.SetIncludeInBuild(includeInBuild);
            EditorUtility.SetDirty(spriteAtlas);
            AssetDatabase.CreateAsset(spriteAtlas, Path.Combine(assetFolder, $"{atlasName}.spriteatlas").FixPathSeparators());
            Debug.Log($"[{GetName()}] Created SpriteAtlas: \"<b>{AssetDatabase.GetAssetPath(spriteAtlas)}</b>\"");
            return spriteAtlas;
        }

        void CreateVariantSpriteAtlas(SpriteAtlas spriteAtlas, string assetFolder, string atlasName)
        {
            var variantAtlas = new SpriteAtlas();
            variantAtlas.SetIsVariant(true);
            variantAtlas.SetMasterAtlas(spriteAtlas);
            variantAtlas.SetIncludeInBuild(includeVariantInBuild);
            variantAtlas.SetVariantScale(variantScale);
            EditorUtility.SetDirty(variantAtlas);
            AssetDatabase.CreateAsset(variantAtlas, Path.Combine(assetFolder, $"{atlasName}{variantSuffix}.spriteatlas").FixPathSeparators());
            Debug.Log($"[{GetName()}] Created SpriteAtlas Variant: \"<b>{AssetDatabase.GetAssetPath(variantAtlas)}</b>\"");
        }
    }
}