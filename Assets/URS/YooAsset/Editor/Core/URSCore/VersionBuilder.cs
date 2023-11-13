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
using UnityEditor;
using Context = System.Collections.Generic.Dictionary<string, object>;

namespace URS.Editor 
{
    [Serializable]
    public class VersionHistory 
    {
        [SerializeField]
        public AssetVersionHistory AssetVersion;

        [SerializeField]
        public AssetBundleVersionHistory AssetBundleVersion;

        [SerializeField]
        public LayoutLookupTables BundleLayout;

        [SerializeField]
        public BundleHash BundleHash;

        [SerializeField]
        public string BundleBuildLog;
    }
    [Serializable]
    public class AssetVersionHistory
    {
        [SerializeField]
        public AssetVersionItem[] Assets;
    }
    [Serializable]
    public class AssetBundleVersionHistory
    {
        [SerializeField]
        public AssetBundleVersionItem[] AssetBundles;
    }

    [Serializable]
    public class AssetVersionItem
    {
        [SerializeField]
        public string AssetPath;

        [SerializeField]
        public uint AssetFileHash;

        [SerializeField]
        public bool HasAssetBundleName;

        [SerializeField]
        public string AssetBundleName;
    }

    [Serializable]
    public class AssetBundleVersionItem
    {
        [SerializeField]
        public string BundleName;

        [SerializeField]
        public string [] AssetPaths;

        public string SelfHash;

        public string CRC;

        public string HashCode;
    }
    public class VersionBuilder
    {
        public static void BuildVersion(string channel,string directoryPath, IFileSystem fileSystem,string version,Dictionary<string,AdditionFileInfo> additionFileInfo, VersionHistory versionHistrory,out string newDirectoryName,bool debug)
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
                var fileManifest = new FileManifest(finalAarry, version);
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
                if (debug) 
                {
                    BuildHistory(channel,tmpNewDirectoryName, versionHistrory);
                }
            }
            else 
            {
                Debug.LogError("输入的版本号格式不对 "+ version);
            }
          
        }

        public static void BuildHistory(string channel,string versionName, VersionHistory versionHistrory) 
        {
            if (versionHistrory == null) return;
            var versionDirectory = $"{URS.Build.GetVersionHistoryFolder(channel)}/{versionName}";
            Directory.CreateDirectory(versionDirectory);

            var assetVersionHistoryFilePath = $"{versionDirectory}/AssetVersionHistory.json";
            if (File.Exists(assetVersionHistoryFilePath))
            {
                File.Delete(assetVersionHistoryFilePath);
            }

            var assetVersionHistoryJson = JsonUtility.ToJson(versionHistrory.AssetVersion, true);
            File.WriteAllText(assetVersionHistoryFilePath, assetVersionHistoryJson);


            var assetBundleVersionHistoryFilePath = $"{versionDirectory}/AssetBundleVersionHistory.json";
            if (File.Exists(assetBundleVersionHistoryFilePath))
            {
                File.Delete(assetBundleVersionHistoryFilePath);
            }

            var assetBundleVersionHistoryJson = JsonUtility.ToJson(versionHistrory.AssetBundleVersion, true);
            File.WriteAllText(assetBundleVersionHistoryFilePath, assetBundleVersionHistoryJson);

            var assetBundleHashHistoryFilePath = $"{versionDirectory}/{ASSET_BUNDLE_HASH_HISTORY_FILE_NAME}";
            if (File.Exists(assetBundleHashHistoryFilePath))
            {
                File.Delete(assetBundleHashHistoryFilePath);
            }

            var assetBundleHashHistoryJson = JsonUtility.ToJson(versionHistrory.BundleHash, true);
            File.WriteAllText(assetBundleHashHistoryFilePath, assetBundleHashHistoryJson);

            var bundleBuildLog = $"{versionDirectory}/{ASSET_BUNDLE_BUNDLE_BUILD_LOG_HISTORY_FILE_NAME}";
            if (File.Exists(bundleBuildLog))
            {
                File.Delete(bundleBuildLog);
            }
            File.WriteAllText(bundleBuildLog, versionHistrory.BundleBuildLog);

        }

        public const string ASSET_BUNDLE_HASH_HISTORY_FILE_NAME = "AssetBundleHashHistory.json";

        public const string ASSET_BUNDLE_BUNDLE_BUILD_LOG_HISTORY_FILE_NAME = "buildlogtep.json";

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
                //Debug.LogError(subDirectory.Name);
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

        public static void PurgeVersion(string versionRootDirectory, IFileSystem fileSystem,string targetVersion, int maxVersionCount = 4) 
        {
            var setting = URSRuntimeSetting.instance;
            var IndexJsonPath = fileSystem.CombinePaths(versionRootDirectory, setting.FilesVersionIndexFileName);
            var jsonText = fileSystem.ReadAllTextFromFile(IndexJsonPath);
            var versionIndex = JsonUtility.FromJson<URSFilesVersionIndex>(jsonText);
            versionIndex.AfterSerialize();
            if (versionIndex.Versions == null || versionIndex.Versions.Length <= maxVersionCount)
            {
                Debug.Log($"当前的版本数量{versionIndex.Versions.Length}，小于目标数量{maxVersionCount},跳过跳过版本裁剪");
                return;
            }
            else
            {
                int purgeCount = versionIndex.Versions.Length - maxVersionCount;
                for (int i = 0; i < purgeCount; i++)
                {
                    var versionCode = versionIndex.Versions[i].VersionCode;
                    if (versionCode == targetVersion) 
                    {
                        break;// 这样情况一般就是出现了版本回退，版本回退的时候，一般保留高版本
                    }
                    var currentDirectoryName = $"{versionCode}---{versionIndex.Versions[i].FilesVersionHash}";
                    var versionDirectory = fileSystem.CombinePaths(versionRootDirectory, currentDirectoryName).FullPath;
                    if (!Directory.Exists(versionDirectory))
                    {
                        Debug.LogError($"在版本裁剪的过程中遇到不可预知的错误:{versionDirectory}不存在这个版本");
                        continue;
                    }
                    else 
                    {
                        fileSystem.DeleteDirectory((FilePath)versionDirectory);
                    }
                }

                BuildVersions(versionRootDirectory, fileSystem);
            }
        }
        

        public static void BuildPatch(string channel,string versionRootDirectory,IFileSystem fileSystem, string targetVersionCode = null,bool debug = false)
        {

            var setting = URSRuntimeSetting.instance;
            var IndexJsonPath = fileSystem.CombinePaths(versionRootDirectory, setting.FilesVersionIndexFileName);
            var jsonText = fileSystem.ReadAllTextFromFile(IndexJsonPath);
            var versionIndex = JsonUtility.FromJson<URSFilesVersionIndex>(jsonText);
            versionIndex.AfterSerialize();
            var patchDirecrtory = fileSystem.CombinePaths(versionRootDirectory, setting.PatchDirectory).FullPath;
            if (Directory.Exists(patchDirecrtory)) 
            {
                Directory.Delete(patchDirecrtory,true);
            }
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
                    var targetVersion = versionIndex.Versions[targetIndex];
                    var targetVersionDiretoryName = $"{targetVersion.VersionCode}---{targetVersion.FilesVersionHash}";
                    var tempDirectory = fileSystem.CombinePaths(versionRootDirectory, setting.PatchTempDirectory).FullPath;
                    var patchItemInfos = new Dictionary<string, List<PatchItemVersion>>();
                    //var versionDifference= new Dictionary<string, int>();
                    for (int i = 0; i < versionIndex.Versions.Length; i++)
                    {
                        if (i != targetIndex) 
                        {
                            var versionCode = versionIndex.Versions[i].VersionCode;
                            var currentDirectoryName = $"{versionCode}---{versionIndex.Versions[i].FilesVersionHash}";
                            HashSet<string> patchs = null ;
                            HashSet<string> missPaths= null ;
                            BinaryDiffVersionDirectroy(versionRootDirectory, patchDirecrtory, tempDirectory, currentDirectoryName, targetVersionDiretoryName, fileSystem, ref patchItemInfos,ref patchs, ref missPaths);
                            Debug.Log($"From Version{versionCode},To Version{targetVersion.VersionCode},PatchCount {patchs.Count} missCount{missPaths.Count}");

                            if (debug)
                            {
                                CheckBundleHash(channel, patchs, currentDirectoryName, targetVersionDiretoryName);
                            }
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
                    if (Directory.Exists(tempDirectory)) 
                    {
                        fileSystem.DeleteDirectory((FilePath)tempDirectory);
                    }
                    var IndexJson = JsonUtility.ToJson(versionIndex, true);
                    File.Delete(IndexJsonPath.FullPath);
                    fileSystem.WriteAllTextToFile(IndexJsonPath, IndexJson);
                }
            }
        }

        private static void CheckBundleHash(string channel,HashSet<string> DifferentPaths, string versionDirectoryName1, string versionDirectoryName2)
        {
            if (DifferentPaths == null || DifferentPaths.Count==0) return;
            var historyDirectoryRoot =  Build.GetVersionHistoryFolder(channel);
            var bundleHashHistory1 = $"{historyDirectoryRoot}/{versionDirectoryName1}/{ASSET_BUNDLE_HASH_HISTORY_FILE_NAME}";
            var bundleHashHistory2 = $"{historyDirectoryRoot}/{versionDirectoryName2}/{ASSET_BUNDLE_HASH_HISTORY_FILE_NAME}";
            if (!File.Exists(bundleHashHistory1))
            {
                Debug.LogWarning($"do exist bundle hash history file,path:{bundleHashHistory1}");
                return;
            }
            if (!File.Exists(bundleHashHistory2))
            {
                Debug.LogWarning($"do exist bundle hash history file,path:{bundleHashHistory2}");
                return;
            }
            var bundleHash1= JsonUtility.FromJson<BundleHash>(File.ReadAllText(bundleHashHistory1));
            bundleHash1.Init();
            var bundleHash2 = JsonUtility.FromJson<BundleHash>(File.ReadAllText(bundleHashHistory2));
            bundleHash2.Init();

            HashSet<string> differentBundle= new HashSet<string>();
            foreach (var relativePath in DifferentPaths)
            {
                var extension = Path.GetExtension(relativePath);
                if (extension == ".bundle")
                {
                    var bundleName = Path.GetFileName(relativePath);
                    differentBundle.Add(bundleName);
                }
            }
            bundleHash1.Compare(bundleHash2, differentBundle);
        }
        
        private static void BinaryDiffVersionDirectroy(
            string versionRootDirectory,
            string patchDirectory,
            string patchSigDirectory, 
            string fromDirectoryName, 
            string toDirectoryName, 
            IFileSystem fileSystem,
            ref Dictionary<string,List<PatchItemVersion>> collector,
            ref HashSet<string> differencePaths,
            ref HashSet<string> missPaths)
        {
            differencePaths = new HashSet<string>();
            missPaths = new HashSet<string>() ;
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
                            differencePaths.Add(relativePath);
                            //Debug.Log($"from {fromDirectoryName} to {toDirectoryName} patched  asset path: {relativePath}");
                            list.Add(new PatchItemVersion()
                            {
                                FromHashCode = fromHashCode,
                                ToHashCode = toHashCode,
                                Hash = Hashing.GetFileXXhash(patchPath),
                                SizeBytes = FileUtility.GetFileSize(patchPath)
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
                                differencePaths.Add(relativePath);
                                //Debug.Log($"from {fromDirectoryName} to {toDirectoryName} patched  asset path: {relativePath}");
                                list.Add(new PatchItemVersion()
                                {
                                    FromHashCode = fromHashCode,
                                    ToHashCode = toHashCode,
                                    Hash = Hashing.GetFileXXhash(patchPath),
                                    SizeBytes = FileUtility.GetFileSize(patchPath)
                                });
                            }
                        }
                    }

                }
                else 
                {
                    missPaths.Add(relativePath);
                    //Debug.Log($"from {fromDirectoryName} to {toDirectoryName} miss  asset path: {relativePath}");
                   // missCount++;
                }
            }
        }
        public static void BuildChannelVersions(string channel,string versionRootDirectory , int purgeVersionCount = 4, string targetVersion = null,bool debug=false)
        {
            var fileSystem = new FileSystem();
            BuildVersions(versionRootDirectory, fileSystem);
            if (purgeVersionCount > 0) 
            {
                PurgeVersion(versionRootDirectory, fileSystem, targetVersion, purgeVersionCount);
            }
            BuildPatch(channel,versionRootDirectory, fileSystem, targetVersion,debug);
        }

        /// <summary>
        /// 自动是最新版本
        /// </summary>
        public static void BuildAutoAppVersionRouter(string versionRootDirectory,string targetVersion) 
        {
            var fileSystem = new FileSystem();
            var setting = URSRuntimeSetting.instance;
            var IndexJsonPath = fileSystem.CombinePaths(versionRootDirectory, setting.FilesVersionIndexFileName);
            var jsonText = fileSystem.ReadAllTextFromFile(IndexJsonPath);
            var versionIndex = JsonUtility.FromJson<URSFilesVersionIndex>(jsonText);
            if (string.IsNullOrEmpty(targetVersion)) {
                targetVersion= versionIndex.Versions[versionIndex.Versions.Length - 1].VersionCode;
            }

            var routerFilePath = $"{versionRootDirectory}/{URSRuntimeSetting.instance.RemoteAppVersionRouterFileName}";
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(routerFilePath));
            if (File.Exists(routerFilePath))
            {
                File.Delete(routerFilePath);
            }

            AppVersionRouter router = new AppVersionRouter();
            router.Items = new AppVersionItem[1];
            router.Items[0] = new AppVersionItem() 
            {
                VersionCode = targetVersion,
                ApplicationVersion= @"\d+.\d+.\d+"
            };
            router.DefaultVersion = targetVersion;
            File.WriteAllText(routerFilePath, JsonUtility.ToJson(router, true));
        }
    }
}

