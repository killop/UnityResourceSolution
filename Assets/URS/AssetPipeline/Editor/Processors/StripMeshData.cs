using System.Collections.Generic;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline.Processors
{
    [AssetProcessorDescription("Profiler.NetworkOperations", ImportAssetTypeFlag.Models)]
    public class StripMeshData : AssetProcessor
    {
        [SerializeField] bool normals;
        [SerializeField] bool normalsIfDefault;
        [SerializeField] bool tangents;
        [SerializeField] bool tangentsIfDefault;
        [SerializeField] bool vertexColor = true;
        [SerializeField] bool vertexColorIfDefault = true;
        [SerializeField] bool uv;
        [SerializeField] bool uvIfDefault;
        [SerializeField] bool uv2 = true;
        [SerializeField] bool uv2IfDefault = true;
        [SerializeField] bool uv3 = true;
        [SerializeField] bool uv3IfDefault = true;
        [SerializeField] bool uv4 = true;
        [SerializeField] bool uv4IfDefault = true;
        [SerializeField] bool uv5 = true;
        [SerializeField] bool uv5IfDefault = true;
        [SerializeField] bool uv6 = true;
        [SerializeField] bool uv6IfDefault = true;
        [SerializeField] bool uv7 = true;
        [SerializeField] bool uv7IfDefault = true;
        [SerializeField] bool uv8 = true;
        [SerializeField] bool uv8IfDefault = true;
        [SerializeField] bool bindPoses;
        [SerializeField] bool bindPosesIfDefault;
        [SerializeField] bool boneWeights;
        [SerializeField] bool boneWeightsIfDefault;
        [SerializeField] bool blendShapes;

        public override void OnPostprocess(Object asset, string assetPath)
        {
            var modifiedMeshes = new List<string>();
            var modifiedProperties = new List<string>();
            var meshes = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x is Mesh).Cast<Mesh>().ToArray();
            foreach (var mesh in meshes)
            {
                modifiedProperties.Clear();
                if (ShouldStripData(normals, normalsIfDefault, mesh.normals))
                {
                    mesh.normals = null;
                    modifiedProperties.Add("normals");
                }

                if (ShouldStripData(tangents, tangentsIfDefault, mesh.tangents))
                {
                    mesh.tangents = null;
                    modifiedProperties.Add("tangents");
                }

                if (ShouldStripData(vertexColor, vertexColorIfDefault, mesh.colors))
                {
                    mesh.colors = null;
                    modifiedProperties.Add("vertexColor");
                }

                if (ShouldStripData(uv, uvIfDefault, mesh.uv))
                {
                    mesh.uv = null;
                    modifiedProperties.Add("uv");
                }

                if (ShouldStripData(uv2, uv2IfDefault, mesh.uv2))
                {
                    mesh.uv2 = null;
                    modifiedProperties.Add("uv2");
                }

                if (ShouldStripData(uv3, uv3IfDefault, mesh.uv3))
                {
                    mesh.uv3 = null;
                    modifiedProperties.Add("uv3");
                }

                if (ShouldStripData(uv4, uv4IfDefault, mesh.uv4))
                {
                    mesh.uv4 = null;
                    modifiedProperties.Add("uv4");
                }

                if (ShouldStripData(uv5, uv5IfDefault, mesh.uv5))
                {
                    mesh.uv5 = null;
                    modifiedProperties.Add("uv5");
                }

                if (ShouldStripData(uv6, uv6IfDefault, mesh.uv6))
                {
                    mesh.uv6 = null;
                    modifiedProperties.Add("uv6");
                }

                if (ShouldStripData(uv7, uv7IfDefault, mesh.uv7))
                {
                    mesh.uv7 = null;
                    modifiedProperties.Add("uv7");
                }

                if (ShouldStripData(uv8, uv8IfDefault, mesh.uv8))
                {
                    mesh.uv8 = null;
                    modifiedProperties.Add("uv8");
                }

                if (ShouldStripData(bindPoses, bindPosesIfDefault, mesh.bindposes))
                {
                    mesh.bindposes = null;
                    modifiedProperties.Add("bindPoses");
                }

                if (ShouldStripData(boneWeights, boneWeightsIfDefault, mesh.boneWeights))
                {
                    mesh.boneWeights = null;
                    modifiedProperties.Add("boneWeights");
                }

                if (blendShapes && mesh.blendShapeCount > 0)
                {
                    mesh.ClearBlendShapes();
                    modifiedProperties.Add("blendShapes");
                }

                if (modifiedProperties.Count > 0)
                {
                    modifiedMeshes.Add($"Stripped {mesh.name}: " + string.Join(", ", modifiedProperties));
                }

                EditorUtility.SetDirty(mesh);
            }

            if (modifiedMeshes.Count > 0)
            {
                AssetDatabase.SaveAssets();
                ImportProfileUserData.AddOrUpdateProcessor(assetPath, this);
                Debug.Log($"[{GetName()}] Stripped mesh data from \"<b>{assetPath}</b>\"\n" + string.Join("\n", modifiedMeshes));
            }
        }

        static bool ShouldStripData<T>(bool shouldStrip, bool shouldStripIfDefault, ICollection<T> data)
        {
            return shouldStrip && data != null && data.Count > 0 && (!shouldStripIfDefault || data.All(v => v.Equals(default)));
        }
    }
}