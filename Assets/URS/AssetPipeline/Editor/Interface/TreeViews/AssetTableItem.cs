using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    public class AssetTableItem : TreeViewItem
    {
        public string assetPath;
        public Type assetType;
        public bool isAsset;

        public AssetTableItem(int id, int depth, string displayName, string assetPath, bool isAsset) : base(id, depth, displayName)
        {
            this.assetPath = assetPath;
            this.isAsset = isAsset;
            icon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D ?? Color.clear.GetPixel();
            assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
        }

        public virtual void Refresh()
        {
            icon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D ?? Color.clear.GetPixel();
        }
    }
}