using System;
using System.Collections.Generic;
using Daihenka.AssetPipeline.Import;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    [Serializable]
    public class AssetTypeFileExtensions
    {
        [SerializeField] ImportAssetType m_AssetType;
        [SerializeField] List<string> m_FileExtensions = new List<string>();

        public AssetTypeFileExtensions(ImportAssetType assetType, params string[] fileExtensions)
        {
            m_AssetType = assetType;
            m_FileExtensions.AddRange(fileExtensions);
        }

        public ImportAssetType AssetType
        {
            get => m_AssetType;
            set => m_AssetType = value;
        }

        public List<string> FileExtensions => m_FileExtensions;
    }
}