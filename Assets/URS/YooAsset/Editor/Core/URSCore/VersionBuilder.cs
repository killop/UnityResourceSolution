using System;
using System.Collections.Generic;
using MHLab.Patch.Core.IO;
using MHLab.Patch.Core.Utilities;
using URS;
using System.IO;
using UnityEngine;
using SemanticVersion = SemanticVersioning.SemanticVersion;
using MHLab.Patch.Core.Octodiff;
using YooAsset.Utility;

namespace URS.Editor 
{
    public class VersionBuilder
    {
        public static void BuildVersion(string directoryPath, IFileSystem fileSystem,string version,Dictionary<string,AdditionFileInfo> additionFileInfo,out string newDirectoryName)
        {
            var orignDirectoryName = Path.GetDirectoryName(directoryPath);
            newDirectoryName = orignDirectoryName;
            if (SemanticVersioning.SemanticVersion.TryParse(version, out var sVersion))
            {
                var localFiles = fileSystem.GetFilesInfo((FilePath)directoryPath);
                List<FileMeta> final = new List<FileMeta>();
                
              //  string hashFileSavePath = $"{versionDirectory}/{YooAsset.ResourceSettingData.Setting.FileManifestHashFileName}";
              //  File.WriteAllText(hashFileSavePath, fileManifestHash);
               // Directory.Move(tempVersionDirectory, $"{versionDirectory}/{fileManifestHash}");
                //var definitions = new FileManifest();
               // definitions.FileMetas = new URSFileMeta[localFiles.Length];
               // var 
                for (var i = 0; i < localFiles.Length; i++)
                {
                    var file = localFiles[i];
                    var fullPath = (fileSystem.CombinePaths(directoryPath, file.RelativePath)).FullPath;
                    if (additionFileInfo.TryGetValue(file.RelativePath, out var additionFile))
                    {
                        var fileMeta = new FileMeta(
                            Path.GetFileName(file.RelativePath),
                            Hashing.GetFileXXhash(fullPath, fileSystem),
                            file.Size,
                            additionFile.Tags,
                            additionFile.IsEncrypted,
                            additionFile.IsUnityBundle);
                        fileMeta.SetRelativePath(file.RelativePath);
                        final.Add(fileMeta);
                    }
                    else {
                        var fileMeta = new FileMeta(
                               Path.GetFileName(file.RelativePath),
                               Hashing.GetFileXXhash(fullPath, fileSystem),
                               file.Size,
                               null,
                               false,
                               false);
                        fileMeta.SetRelativePath(file.RelativePath);
                        final.Add(fileMeta);

                    }
                    
                }
                var finalAarry = final.ToArray();
                System.Array.Sort(finalAarry, (a, b) =>
                {
                    return a.RelativePath.CompareTo(b.RelativePath);
                });
                var fileManifest = new FileManifest(finalAarry);
                var json = JsonUtility.ToJson(fileManifest, true);
                var setting = URSRuntimeSetting.instance;
                var jsonPath = fileSystem.CombinePaths(directoryPath, setting.FileManifestFileName);
                fileSystem.WriteAllTextToFile(jsonPath, json);
                var hashCode = Hashing.GetFileXXhash(jsonPath.FullPath);

                var tmpNewDirectoryName = $"{sVersion.ToString()}---{hashCode}";
                if (tmpNewDirectoryName != orignDirectoryName) 
                {
                    newDirectoryName = tmpNewDirectoryName;
                    fileSystem.DirectoryRename(directoryPath, newDirectoryName);
                }
                
            }
            else 
            {
                Debug.LogError("输入的版本号格式不对 "+ version);
            }
          
        }

        public static void BuildVersions(string versionRootDirectory,IFileSystem fileSystem)
        {
            var setting = URSRuntimeSetting.instance;
            URSFilesVersionIndex index = new URSFilesVersionIndex();
            var sVersions = new List<SemanticVersion>();
            var lookup = new Dictionary<string,uint>();
            var versionItems = new List<URSFilesVersionItem>();
            var di = new DirectoryInfo(versionRootDirectory);
            foreach (DirectoryInfo subDirectory in di.GetDirectories())
            {
                Debug.LogError(subDirectory.Name);
                var name = subDirectory.Name;
                var names = name.Split("---");
                var versionCode = names[0];
                if (SemanticVersioning.SemanticVersion.TryParse(versionCode,out var sVersion))
                {
                    var jsonPath = fileSystem.CombinePaths(subDirectory.FullName, setting.FileManifestFileName).FullPath;
                    var hashCode = Hashing.GetFileXXhash(jsonPath, fileSystem);
                    var newName = $"{versionCode}---{hashCode}";
                    if (name != newName) 
                    {
                        fileSystem.DirectoryRename(subDirectory.FullName, newName);
                    }
                    sVersions.Add(sVersion);
                    lookup.Add(sVersion.ToString(), hashCode);
                }
            }

            sVersions.Sort();
            for (int i = 0; i < sVersions.Count; i++)
            {
                var versionCode = sVersions[i].ToString();
                versionItems.Add(new URSFilesVersionItem()
                {
                    VersionCode = versionCode,
                    FilesVersionHash = lookup[versionCode]
                }); 
            }
            index.Versions = versionItems.ToArray();
            var IndexJson = JsonUtility.ToJson(index, true);
            var IndexJsonPath = fileSystem.CombinePaths(versionRootDirectory, setting.FilesVersionIndexFileName);
            fileSystem.WriteAllTextToFile(IndexJsonPath, IndexJson);
        }

        public static void BuildPatch(string versionRootDirectory,IFileSystem fileSystem, string targetVersionCode = null)
        {
            var setting = URSRuntimeSetting.instance;
            var IndexJsonPath = fileSystem.CombinePaths(versionRootDirectory, setting.FilesVersionIndexFileName);
            var jsonText = fileSystem.ReadAllTextFromFile(IndexJsonPath);
            var versionIndex = JsonUtility.FromJson<URSFilesVersionIndex>(jsonText);
            versionIndex.AfterSerialize();
            if (versionIndex.Versions == null || versionIndex.Versions.Length <= 1)
            {
                Debug.LogWarning("构建补丁失败，因为当前文件夹没有可用版本,或者版本数量少于2个");
                return;
            }
            else 
            {
                int targetIndex = -1;
                if (string.IsNullOrEmpty(targetVersionCode))
                {
                    targetIndex = versionIndex.Versions.Length - 1;
                }
                else 
                {
                    for (int i = 0; i < versionIndex.Versions.Length; i++)
                    {
                        if (versionIndex.Versions[i].VersionCode == targetVersionCode) 
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                  
                }
                if (targetIndex < 0)
                {
                    Debug.LogError("构建补丁失败，因为当前当前文件夹没有找到指定的版本 " + targetVersionCode);
                    return;
                }
                else if (targetIndex == 0)
                {
                    Debug.LogError("构建补丁失败，因为当前当前文件夹没有找到指定的版本 " + targetVersionCode);
                    return;
                }
                else
                {
                    var item = versionIndex.Versions[targetIndex];
                    var targetVersionDiretoryName = $"{item.VersionCode}---{item.FilesVersionHash}";
                    var patchDirecrtory = fileSystem.CombinePaths(versionRootDirectory, setting.PatchDirectory).FullPath;
                    var tempDirectory = fileSystem.CombinePaths(versionRootDirectory, setting.PatchTempDirectory).FullPath;
                    var patchItemInfos = new Dictionary<string, List<PatchItemVersion>>();

                    for (int i = 0; i < versionIndex.Versions.Length; i++)
                    {
                        if (i != targetIndex) 
                        {
                            var currentDirectoryName = $"{versionIndex.Versions[i].VersionCode}---{versionIndex.Versions[i].FilesVersionHash}";
                            BinaryDiffVersionDirectroy(versionRootDirectory, patchDirecrtory, tempDirectory, currentDirectoryName, targetVersionDiretoryName, fileSystem, ref patchItemInfos);
                        }
                    }
                    var patchItems = new List<PatchItem>();
                    foreach (var kv in patchItemInfos)
                    {
                        PatchItem p = new PatchItem();
                        p.RelativePath = kv.Key;
                        p.PatchVersions = kv.Value.ToArray();
                        patchItems.Add(p);
                    }
                    var pathItemArray= patchItems.ToArray();
                    System.Array.Sort(pathItemArray, (a, b) =>
                    {
                        return a.RelativePath.CompareTo(b.RelativePath);
                    });
                    versionIndex.Patches = pathItemArray;
                    fileSystem.DeleteDirectory((FilePath)tempDirectory);

                    var IndexJson = JsonUtility.ToJson(versionIndex, true);
                    File.Delete(IndexJsonPath.FullPath);
                    fileSystem.WriteAllTextToFile(IndexJsonPath, IndexJson);
                }
            }
        }

        private static void BinaryDiffVersionDirectroy(string versionRootDirectory,string patchDirectory,string patchSigDirectory, string fromDirectoryName, string toDirectoryName, IFileSystem fileSystem,ref Dictionary<string,List<PatchItemVersion>> collector)
        {
            var setting = URSRuntimeSetting.instance;
            fromDirectoryName = fileSystem.CombinePaths(versionRootDirectory, fromDirectoryName).FullPath;
            toDirectoryName = fileSystem.CombinePaths(versionRootDirectory, toDirectoryName).FullPath;
            var fromJsonPath = fileSystem.CombinePaths(fromDirectoryName, setting.FileManifestFileName);
            var fromJsonText = fileSystem.ReadAllTextFromFile(fromJsonPath);
            var fromVersion = JsonUtility.FromJson<FileManifest>(fromJsonText);
            Dictionary<string, uint> fromFiles = new Dictionary<string, uint>();
            foreach (var item in fromVersion.FileMetas)
            {
                fromFiles[item.RelativePath] = item.Hash;
            }

            var toJsonPath = fileSystem.CombinePaths(toDirectoryName, setting.FileManifestFileName);
            var toJsonText = fileSystem.ReadAllTextFromFile(toJsonPath);
            var toVersion = JsonUtility.FromJson<FileManifest>(toJsonText);

            Dictionary<string, uint> toFiles = new Dictionary<string, uint>();
            foreach (var item in toVersion.FileMetas)
            {
                toFiles[item.RelativePath] = item.Hash;
            }
            //var pathDirecrtory = fileSystem.CombinePaths(versionRootDirectory, setting.PatchDirectory).FullPath;
          
            foreach (var item in toFiles) 
            {
                var relativePath = item.Key;
                if (fromFiles.ContainsKey(relativePath))
                {
                    var toHashCode = item.Value;
                    var fromHashCode = fromFiles[relativePath];
                    if (fromHashCode != toHashCode)
                    {
                        var fromPath = fileSystem.CombinePaths(fromDirectoryName, relativePath).FullPath;
                        var toPath = fileSystem.CombinePaths(toDirectoryName, relativePath).FullPath;
                        var patchPath = fileSystem.CombinePaths(patchDirectory, $"{relativePath}---{fromHashCode}---{toHashCode}.patch").FullPath;
                        if (File.Exists(patchPath))
                        {
                            File.Delete(patchPath);
                        }
                        fileSystem.EnsureFileDirectory(patchPath);
                        var sigPath = fileSystem.CombinePaths(patchSigDirectory, $"{relativePath}---{fromHashCode}---{toHashCode}.patch.sig").FullPath;
                        if (File.Exists(sigPath))
                        {
                            File.Delete(sigPath);
                        }
                        fileSystem.EnsureFileDirectory(sigPath);
                        DeltaFileBuilder.Build(fromPath, toPath, patchPath, sigPath);

                        if (!collector.ContainsKey(relativePath))
                        {
                            var list = new List<PatchItemVersion>();
                            list.Add(new PatchItemVersion()
                            {
                                FromHashCode = fromHashCode,
                                ToHashCode = toHashCode
                            });
                            collector.Add(relativePath, list);
                        }
                        else
                        {
                            var list = collector[relativePath];
                            bool find = false;
                            for (int i = 0; i < list.Count; i++)
                            {
                                var pVersion = list[i];
                                if (pVersion.FromHashCode == fromHashCode && pVersion.ToHashCode == toHashCode)
                                {
                                    find = true;
                                    break;
                                }
                            }
                            if (!find)
                            {
                                list.Add(new PatchItemVersion()
                                {
                                    FromHashCode = fromHashCode,
                                    ToHashCode = toHashCode
                                });
                            }
                        }
                    }
                    
                }
            }
        }
        public static void BuildChannleVersionPatches(string channelRoot)
        {
            var routerFilePath = $"{channelRoot}/{URSRuntimeSetting.instance.RemoteAppToChannelRouterFileName}";
            var jsonText = File.ReadAllText(routerFilePath);
            var router = JsonUtility.FromJson<AppToChannelRouter>(jsonText);
            var fileSystem = new FileSystem();
            for (int i = 0; i < router.Items.Length; i++)
            {
                var item = router.Items[i];
                var channel = item.ChannelId;
                var version = item.VersionCode;

                var channelDirectoryName=  $"{Build.GetChannelRoot()}/{channel}";
                DirectoryInfo directoryInfo = new DirectoryInfo(channelDirectoryName);
                foreach (var di in directoryInfo.GetDirectories())
                {
                    var diName = di.Name;// android,ios,windows...
                    var versionRootDirectory = $"{channelDirectoryName}/{diName}";
                    BuildVersions(versionRootDirectory, fileSystem);
                    BuildPatch(versionRootDirectory, fileSystem, version);
                }
            }
          
        }
       
    }
}

