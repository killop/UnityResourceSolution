using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset.Utility;

namespace URS
{
    [Serializable]
    public class FileManifest
    {
        public FileMeta[] FileMetas;

        [NonSerialized]
        private Dictionary<string, FileMeta> _fileMetaMap = new Dictionary<string, FileMeta>();

        public Dictionary<string, FileMeta> GetFileMetaMap()
        {
            return _fileMetaMap;
        }

        public void ClearMetaMap() 
        {
            _fileMetaMap = new Dictionary<string, FileMeta>();
        }
        public FileManifest(FileMeta[] fileMetas)
        {
            FileMetas = fileMetas;
        }
        public void  GetFileMetaByTag(string[] tags, ref List<FileMeta> result)
        {
            for (int i = 0; i < FileMetas.Length; i++)
            {
                var fileMeta = FileMetas[i];
                if (fileMeta.HasAnyTag(tags)) {
                    result.Add(fileMeta);
                }
            }
        }

        public void ReplaceFile(FileMeta fileMeta)
        {
            if (fileMeta != null)
            {
                _fileMetaMap[fileMeta.RelativePath] = fileMeta;
            }
            else
            {
               // Debug.LogWarning("已经存在资源了："+ fileMeta.RelativePath);
            }
        }
        public void RemoveFile(string relativePath)
        {
            if (_fileMetaMap.ContainsKey(relativePath))
            {
                _fileMetaMap.Remove(relativePath);
            }
            else
            {
                Debug.LogWarning("删除不存在的资源：" + relativePath);
            }
        }
        public bool ContainFile(string relativePath,uint hash)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;
            if (!_fileMetaMap.ContainsKey(relativePath)) return false;
            var myFileMeta = _fileMetaMap[relativePath];
            return myFileMeta.Hash == hash;
        }
        public bool ContainFile(FileMeta other)
        {
            if (other==null) return false;
            if (!_fileMetaMap.ContainsKey(other.RelativePath)) return false;
            var myFileMeta = _fileMetaMap[other.RelativePath];
            return myFileMeta.Hash == other.Hash;
        }
        /// <summary>
        /// 序列化
        /// </summary>
        public static void Serialize(string savePath, FileManifest patchManifest,bool pretty=false)
        {
            string json = JsonUtility.ToJson(patchManifest,pretty);
            FileUtility.CreateFile(savePath, json);
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        public static FileManifest Deserialize(string jsonData)
        {
            FileManifest fileManifest = JsonUtility.FromJson<FileManifest>(jsonData);
            if (fileManifest != null)
            {
                fileManifest.AfterDeserialize();
            }
            return fileManifest;
        }
        public void AfterDeserialize()
        {
            if (FileMetas == null)
            {
                FileMetas = new FileMeta[0];
            }
            if (_fileMetaMap == null)
            {
                _fileMetaMap = new Dictionary<string, FileMeta>();
            }
            for (int i = 0; i < FileMetas.Length; i++)
            {
                var fileMeta = FileMetas[i];
                if (fileMeta != null)
                {
                    if (!string.IsNullOrEmpty(fileMeta.RelativePath))
                    {
                        if (!_fileMetaMap.ContainsKey(fileMeta.RelativePath))
                        {
                            _fileMetaMap[fileMeta.RelativePath] = fileMeta;
                        }
                    }
                    else
                    {
                        Debug.LogError($"{fileMeta.FileName} relative path is null");
                    }
                }
                else
                {
                    Debug.LogError($"{i} filemeta is null");
                }
            }
        }
    }
    public struct AdditionFileInfo 
    {
        /// <summary>
        /// Tags
        /// </summary>
        public string[] Tags;


        /// <summary>
        /// 是否为加密文件
        /// </summary>
        public bool IsEncrypted;


        /// <summary>
        /// 是否为Bundle文件
        /// </summary>
        public bool IsUnityBundle;
    }

    [Serializable]
    public class FileMeta
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName;

        /// <summary>
        /// 文件名相对路径
        /// </summary>
        public string RelativePath;

        /// <summary>
        /// 文件哈希值
        /// </summary>
        public System.UInt32 Hash;

        /// <summary>
        /// 文件校验码
        /// </summary>
        //public string CRC;

        /// <summary>
        /// 文件大小（字节数）
        /// </summary>
        public long SizeBytes;


        /// <summary>
        /// Tags
        /// </summary>
        public string[] Tags;


        /// <summary>
        /// 是否为加密文件
        /// </summary>
        public bool IsEncrypted;


        /// <summary>
        /// 是否为Bundle文件
        /// </summary>
        public bool IsUnityBundle;

        public static FileMeta ERROR_FILE_META = null;

        static FileMeta() {
            ERROR_FILE_META = new FileMeta(
                string.Empty,
                0,
                0,
                null,
                false,
                false
           );
        }

        public FileMeta(string fileName, uint hash ,long sizeBytes, string[] tags, bool isEncrypted, bool isUnityBundle)
        {
            FileName = fileName;
          
            Hash = hash;
            SizeBytes = sizeBytes;
            Tags = tags;
            IsEncrypted = isEncrypted;
            IsUnityBundle = isUnityBundle;
        }


        public  FileMeta(string fileName, string[] tags, bool isEncrypted, bool isUnityBundle)
        {
            FileName = fileName;
            Tags = tags;
            IsEncrypted = isEncrypted;
            IsUnityBundle = isUnityBundle;
        }
        public void SetRelativePath(string relativePath)
        {
            RelativePath = relativePath;
        }

        public bool IsValid()
        {
            return !(this == ERROR_FILE_META);
        }

        /// <summary>
        /// 是否包含Tag
        /// </summary>
        public bool HasTag(string tag)
        {
            if (Tags == null || Tags.Length == 0)
                return false;
            for (int i = 0; i < Tags.Length; i++)
            {
                if (Tags[i] == tag)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 是否包含Tag
        /// </summary>
        public bool HasAnyTag(string[] tag)
        {
            if (tag == null || tag.Length == 0)
                return false;
            for (int i = 0; i < tag.Length; i++)
            {
                if (HasTag(tag[i]))
                {
                    return true;
                }
            }
            return false;
        }
        public AdditionFileInfo GetAdditionFileInfo()
        {
            return new AdditionFileInfo
            {
                Tags = this.Tags,
                IsEncrypted = this.IsEncrypted,
                IsUnityBundle = this.IsUnityBundle
            };
        }
    }
}

