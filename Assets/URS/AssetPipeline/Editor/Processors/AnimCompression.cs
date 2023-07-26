using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("FilterByLabel@2x", ImportAssetTypeFlag.Models)]
    public class AnimCompression : AssetProcessor
    {
        [SerializeField] private  ModelImporterAnimationCompression animationCompression;
        [SerializeField] private float animationRotationError;
        [SerializeField] private float animationPositionError;
        [SerializeField] private float animationScaleError;
        [SerializeField] private bool resampleCurve=false;
        public override bool IsConfigOK (AssetImporter importer)
        {
            if (importer == null) return false;
            var mi= importer as ModelImporter;
            if (mi == null) return false;
            if (mi.animationCompression != animationCompression) 
            {
                return false;
            }
            if (mi.animationRotationError != animationRotationError)
            {
                return false;
            }
            if (mi.animationPositionError != animationPositionError)
            {
                return false;
            }
            if (mi.animationScaleError != animationScaleError)
            {
                return false;
            }
            if (mi.resampleCurves != resampleCurve)
            {
                return false;
            }
            return true;
        }
        public override void OnPostprocessModel(string assetPath, ModelImporter importer, GameObject go)
        {
            importer.animationCompression = animationCompression;
            importer.animationRotationError = animationRotationError;
            importer.animationPositionError = animationPositionError;
            importer.animationScaleError = animationScaleError;
            importer.resampleCurves = resampleCurve;
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Preset applied for <b>{assetPath}</b>");
        }
        
    }
}