using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("FilterByLabel@2x", ImportAssetTypeFlag.Models)]
    public class AnimCompression : AssetProcessor
    {
        [SerializeField] private ModelImporterAnimationCompression animationCompression;
        [SerializeField] private float animationRotationError;
        [SerializeField] private float animationPositionError;
        [SerializeField] private float animationScaleError;
        
        public override void OnPostprocessModel(string assetPath, ModelImporter importer, GameObject go)
        {
            importer.animationCompression = animationCompression;
            importer.animationRotationError = animationRotationError;
            importer.animationPositionError = animationPositionError;
            importer.animationScaleError = animationScaleError;
            
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Preset applied for <b>{assetPath}</b>");
        }
        
    }
}