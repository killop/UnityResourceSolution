using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("Transform Icon", ImportAssetTypeFlag.Models)]
    public class ResetTransform : AssetProcessor
    {
        public override int Priority => 1;

        [SerializeField] bool position;
        [SerializeField] bool rotation;
        [SerializeField] bool scale;

        public override void OnPostprocessModel (string assetPath, ModelImporter importer, GameObject root)
        {
            if (position) {
                root.transform.position = Vector3.zero;
            }

            if (rotation) {
                root.transform.rotation = Quaternion.identity;
            }

            if (scale) {
                root.transform.localScale = Vector3.one;
            }
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Reset Transform for \"<b>{assetPath}</b>\"");
        }
    }
}