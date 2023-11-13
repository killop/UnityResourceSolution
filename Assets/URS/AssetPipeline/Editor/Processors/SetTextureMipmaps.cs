using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("FilterByLabel@2x", ImportAssetTypeFlag.Textures)]
    public class SetTextureMipmaps : AssetProcessor
    {
        [SerializeField] public bool enableMipmaps;

        public override bool IsConfigOK(AssetImporter importer)
        {
            if (importer == null) return false;
            var ti = importer as TextureImporter;
            if (ti == null) return false;

            if (ti.mipmapEnabled != enableMipmaps || ti.streamingMipmaps != enableMipmaps)
                return false;
            return true;
        }

        void OnPostprocessTexture(string assetPath, TextureImporter importer)
        {
            if (importer.mipmapEnabled == enableMipmaps && importer.streamingMipmaps == enableMipmaps)
                return;

            importer.mipmapEnabled = enableMipmaps;
            importer.streamingMipmaps = enableMipmaps;

            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Preset applied for <b>{assetPath}</b>");
        }

        public override void OnPostprocessTexture(string assetPath, TextureImporter importer, Texture2D tex)
        {
            OnPostprocessTexture(assetPath, importer);
        }

        public override void OnPostprocessCubemap(string assetPath, TextureImporter importer, Cubemap texture)
        {
            OnPostprocessTexture(assetPath, importer);
        }
    }
}