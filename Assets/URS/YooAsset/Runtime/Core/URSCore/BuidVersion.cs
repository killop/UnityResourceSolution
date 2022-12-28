using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace URS
{
   
    [Serializable]
    public class URSFilesVersionIndex 
    {
        [SerializeField]
        public URSFilesVersionItem[] Versions;
        [SerializeField]
        public PatchItem[] Patches;

        [NonSerialized]
        private Dictionary<string, PatchItem> _patchsLookUp = new Dictionary<string, PatchItem>();

        public URSFilesVersionItem GetVersion(string versionCode) 
        {
            if (Versions != null) 
            {
                for (int i = 0; i < Versions.Length; i++)
                {
                    var v = Versions[i];
                    ; if (v.VersionCode == versionCode)
                    {
                        return v;
                    }
                }
            }
            return null;
        }

        public void AfterSerialize()
        {
            if (Patches == null)
            {
                Patches = new PatchItem[0];
            }
            foreach (var item in Patches)
            {
                _patchsLookUp[item.RelativePath] = item;
            }
        }
        public PatchItemVersion GetPatch(FileMeta fromFileMeta, FileMeta toFileMeta)
        {

            if (_patchsLookUp.ContainsKey(fromFileMeta.RelativePath))
            {
                PatchItem item = _patchsLookUp[fromFileMeta.RelativePath];
                if (item.PatchVersions != null) {

                    foreach (var versionPatch in item.PatchVersions)
                    {
                        if (versionPatch.FromHashCode == fromFileMeta.Hash && versionPatch.ToHashCode == toFileMeta.Hash) 
                        {
                            return versionPatch;
                        }
                    }
                }
            }
            return null;
        }

    }
    [Serializable]
    public class URSFilesVersionItem 
    {
        [SerializeField]
        public string VersionCode;
        [SerializeField]
        public uint FilesVersionHash;
    }
    [Serializable]
    public class PatchItem
    {
        [SerializeField]
        public string RelativePath;
        [SerializeField]
        public PatchItemVersion[] PatchVersions;
    }
    [Serializable]
    public class PatchItemVersion
    {
        [SerializeField]
        public uint FromHashCode;
        [SerializeField]
        public uint ToHashCode;
        [SerializeField]
        public long SizeBytes;
        [SerializeField]
        public uint Hash;
    }
} 
