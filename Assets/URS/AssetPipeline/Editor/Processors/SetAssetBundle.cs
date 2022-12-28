using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("Packages/com.daihenka.assetpipeline/Editor/Resources/Icons/AssetBundleIcon.png")]
    public class SetAssetBundle : AssetProcessor
    {
        [SerializeField] string assetBundleName;
        [SerializeField] string assetBundleVariant;

        public override void OnPreprocessAsset(string assetPath, AssetImporter importer)
        {
            var bundleNameSet = !string.IsNullOrWhiteSpace(importer.assetBundleName);
            var bundleVariantSet = !string.IsNullOrWhiteSpace(importer.assetBundleVariant) || string.IsNullOrWhiteSpace(assetBundleVariant);
            if (bundleNameSet && bundleVariantSet && !ShouldImport(importer))
            {
                return;
            }

            var bundleName = ReplaceVariables(assetBundleName, assetPath);
            importer.SetAssetBundleNameAndVariant(bundleName, assetBundleVariant);
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] AssetBundle name: \"<b>{bundleName}</b>\" variant: \"<b>{assetBundleVariant}</b>\" set for <b>{assetPath}</b>");
        }
    }
}