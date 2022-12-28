
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TagInfo = System.Collections.Generic.Dictionary<Bewildered.SmartLibrary.TagRule.TagRuleType, string>;
using URS;
using YooAsset.Utility;
using System.Linq;
using YooAsset;
using UnityEditor.Build.Content;
using BuildCompression = UnityEngine.BuildCompression;
using UnityEditor.Search;
public class BuildTaskBuildRaw : BuildTask
{
    public override void BeginTask()
    {
        base.BeginTask();
       // var fileMetas = new List<FileMeta>();
        CopyWwiseRawResource();
        CopyMediaRawResource();
        //_context[CONTEXT_RAW_FILES] = fileMetas;
        this.FinishTask();
    }
    public void CopyWwiseRawResource()
    {
        var versionDirectory = (string)_context[BuildTask.CONTEXT_VERSION_DIRECTORY];
       // versionDirectory = $"{versionDirectory}/{BuildTask.TEMP_VERSION_DIRECTORY}";
        string wwiseBnkDirectory = $"ninjabeats_WwiseProject/GeneratedSoundBanks/{URS.PlatformMappingService.GetPlatformPathSubFolder()}";
        var bnks = Directory.GetFiles(wwiseBnkDirectory, "*.bnk");
        string relativeDirectory = $"raw_files/wwise/{ URS.PlatformMappingService.GetPlatformPathSubFolder()}";
        var defaultTag = URSRuntimeSetting.instance.DefaultTag;
        var buildinTag = URSRuntimeSetting.instance.BuildinTag;
        for (int i = 0; i < bnks.Length; i++)
        {
            string bnk = bnks[i];
            string fileName = Path.GetFileName(bnk);
           // string hash = HashUtility.FileMD5(bnk);
           // string crc32 = HashUtility.FileCRC32(bnk);
           // long size = FileUtility.GetFileSize(bnk);
           
            string[] tags = new string[] { defaultTag, buildinTag, "wwise" };
           // FileMeta fileMeta = new FileMeta(fileName, hash, crc32, size, tags, false, false);
            var rlPath = $"{relativeDirectory}/{fileName}";
            // fileMeta.SetRelativePath(rlPath);
            //fileMetas.Add(fileMeta);
            var additionFileInfos = GetData<Dictionary<string, AdditionFileInfo>>(CONTEXT_FILE_ADDITION_INFO);
            if (additionFileInfos == null)
            {
                additionFileInfos = new Dictionary<string, AdditionFileInfo>();
                SetData(CONTEXT_FILE_ADDITION_INFO, additionFileInfos);
            }

            if (additionFileInfos.ContainsKey(rlPath))
            {
                Debug.LogWarning("already exist path " + rlPath);
            }
            additionFileInfos[rlPath] = new AdditionFileInfo()
            {
                Tags = tags,
                IsEncrypted = false,
                IsUnityBundle=false
            };
            var dstPath = $"{versionDirectory}/{rlPath}";
            var directoryName = Path.GetDirectoryName(dstPath);
            Directory.CreateDirectory(directoryName);
            File.Copy(bnk, dstPath);
        }
    }
    public void CopyMediaRawResource()
    {
        var versionDirectory = (string)_context[BuildTask.CONTEXT_VERSION_DIRECTORY];
       // versionDirectory = $"{versionDirectory}/{BuildTask.TEMP_VERSION_DIRECTORY}";
        string mediaDirectory = $"Media";
        var medias = Directory.GetFiles(mediaDirectory, "*.*");
        string relativeDirectory = $"raw_files/media";
        var defaultTag = URSRuntimeSetting.instance.DefaultTag;
        var buildinTag = URSRuntimeSetting.instance.BuildinTag;
        for (int i = 0; i < medias.Length; i++)
        {
            string media = medias[i];
            string fileName = Path.GetFileName(media);
          //  string hash = HashUtility.FileMD5(media);
           // string crc32 = HashUtility.FileCRC32(media);
           // long size = FileUtility.GetFileSize(media);
            string[] tags = new string[] { defaultTag, buildinTag, "media" };
          //  FileMeta fileMeta = new FileMeta(fileName, hash, crc32, size, tags, false, false);
            var rlPath = $"{relativeDirectory}/{fileName}";
           // fileMeta.SetRelativePath(rlPath);
           // fileMetas.Add(fileMeta);
            var dstPath = $"{versionDirectory}/{rlPath}";
            var additionFileInfos = GetData<Dictionary<string, AdditionFileInfo>>(CONTEXT_FILE_ADDITION_INFO);
            if (additionFileInfos == null)
            {
                additionFileInfos = new Dictionary<string, AdditionFileInfo>();
                SetData(CONTEXT_FILE_ADDITION_INFO, additionFileInfos);
            }

            if (additionFileInfos.ContainsKey(rlPath))
            {
                Debug.LogWarning("already exist path " + rlPath);
            }
            additionFileInfos[rlPath] = new AdditionFileInfo()
            {
                Tags = tags,
                IsEncrypted = false,
                IsUnityBundle = false
            };
            var directoryName = Path.GetDirectoryName(dstPath);
            Directory.CreateDirectory(directoryName);
            File.Copy(media, dstPath);
        }

    }
}
