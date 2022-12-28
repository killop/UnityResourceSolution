using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("FilterByLabel@2x")]
    public class SetAssetLabels : AssetProcessor
    {
        [SerializeField] string[] labels;

        public override void OnPostprocess(Object asset, string assetPath)
        {
            var assetLabels = AssetDatabase.GetLabels(asset).ToHashSet();
            assetLabels.AddRange(labels);
            AssetDatabase.SetLabels(asset, assetLabels.ToArray());
            ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
            Debug.Log($"[{GetName()}] Labels \"<b>{string.Join("</b>\", \"<b>", labels)}</b>\" set for <b>{assetPath}</b>");
        }
    }
}