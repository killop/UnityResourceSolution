using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using YooAsset.Utility;
using URS;
using MHLab.Patch.Core.Utilities;
using YooAsset;

namespace URS
{
    public enum EnumHardiskDirectoryType
    {
        Invalid = 0,
        PersistentDownload = 1,
        PersistentReadonly = 2,
        BuildIn = 3,
    }
    public class URSFileSystem
    {
        public  static PersistentDownloadFolder PersistentDownloadFolder;

        public static PersistentReadOnlyFolder PersistentReadOnlyFolder;

        public static BuildInFolder BuildInFolder;

        public static RemoteFolder RemoteFolder;

        public static List<Folder> _localFolders= new List<Folder>();

        private static string _downloadTempFolder;

        public static void Init() 
        {
            PersistentDownloadFolder = new PersistentDownloadFolder(AssetPathHelper.GetPersistentDownloadFolder());
            PersistentReadOnlyFolder = new PersistentReadOnlyFolder(AssetPathHelper.GetPersistentReadonlyFolder());
            BuildInFolder = new BuildInFolder(AssetPathHelper.GetURSBuildInResourceFolder());
            
            /// 在离线模式下的固定资源优先级
            _localFolders.Add(PersistentDownloadFolder);
            _localFolders.Add(PersistentReadOnlyFolder);
            _localFolders.Add(BuildInFolder);

            _downloadTempFolder = AssetPathHelper.GetDownloadTempFolder();
            if (Directory.Exists(_downloadTempFolder)) 
            {
                Directory.Delete(_downloadTempFolder,true);
            }
        }

        public static void InitPersistentFolders() 
        {
            PersistentDownloadFolder.InitManifest();
            PersistentReadOnlyFolder.InitManifest();
        }
        public static void InitRemoteFolder(HostPlayModeImpl hostPlayModeImpl) 
        {
            RemoteFolder = new RemoteFolder(hostPlayModeImpl);
        }

        public static void MoveAssetFromDownloadToReadOnly(string relativePath)
        { 
             var bundleManifest= GetTrustBundleManifest();
            if (bundleManifest != null) {
                var fileMetas = new List<FileMeta>();
                var mainFileMeta=  bundleManifest.GetBundleFileMeta(relativePath);
                if (mainFileMeta.IsValid()) 
                {
                    fileMetas.Add(mainFileMeta);
                }
                var dps = bundleManifest.GetAllDependenciesRelativePath(relativePath);
                fileMetas.AddRange(dps);
                if (fileMetas.Count > 0)
                {
                    bool anyChange = false;
                    foreach (var fm in fileMetas)
                    {
                        if (PersistentDownloadFolder.SafeContainsFile(fm))
                        {
                            if (!PersistentReadOnlyFolder.SafeContainsFile(fm))
                            {
                                var src = PersistentDownloadFolder.GetFileHardiskPath(fm);
                                var dest = PersistentReadOnlyFolder.GetFileHardiskPath(fm);
                                var directory = Path.GetDirectoryName(dest);
                                Directory.CreateDirectory(directory);
                                File.Move(src, dest);
                                anyChange = true;
                                PersistentDownloadFolder.DeleteFile(fm.RelativePath);
                                PersistentReadOnlyFolder.RegisterVerifyFile(fm);
                            }
                        }
                        else if (BuildInFolder.ContainsFile(fm))
                        {
                          //  Logger.Error("存在于build中,跳过检查");
                        }
                    }
                    if (anyChange) 
                    {
                        PersistentReadOnlyFolder.FlushFileManifestToHardisk();
                        PersistentDownloadFolder.FlushFileManifestToHardisk();
                    }
                }
            }
        }

        public static BundleManifest GetTrustBundleManifest() 
        {
            if (RemoteFolder != null&& RemoteFolder.BundleManifest!=null) 
            {
                return RemoteFolder.BundleManifest;
            }

            for (int i = 0; i < _localFolders.Count; i++)
            {
                var folder = _localFolders[i];
                if (folder.BundleManifest != null)
                { 
                    return folder.BundleManifest;
                }
            }
            Logger.Error("没有找到值得信赖的bundleManifest");
            return null;
        }
        public static string GetDownloadTempPath(string relativePath) 
        {
            return $"{_downloadTempFolder}/{relativePath}";
        }

        public static string GetDownloadFolderPath(string relativePath)
        {
            return PersistentDownloadFolder.GetFileHardiskPath(relativePath);
        }

        public static bool ConfirmDownload(FileMeta fileMeta,string hardiskFilePath) 
        {
            bool result = DownloadFolderCheckContentIntegrity(fileMeta, hardiskFilePath);
            if (result) 
            {
                DownloadFolderRegisterVerifyFile(fileMeta);
            }
            return result;
        }

        public static bool DownloadFolderCheckContentIntegrity(FileMeta fileMeta, string hardiskFilePath) 
        {
            return PersistentDownloadFolder.CheckContentIntegrity(fileMeta, hardiskFilePath);
        }
        public static bool DownloadFolderCheckContentIntegrity(string filePath, long size, uint fileHash)
        {
            return PersistentDownloadFolder.CheckContentIntegrity(filePath,size,fileHash);
        }
        public static void DownloadFolderRegisterVerifyFile(FileMeta fileMeta)
        {
            PersistentDownloadFolder.RegisterVerifyFile(fileMeta);
        }
        public static void DownloadFolderDeleteFile(string fileRelativePath) 
        {
            PersistentDownloadFolder.DeleteFile(fileRelativePath);
        }
        public static void DeletePersistentRootFolder()
        {
            string directoryPath = AssetPathHelper.GetPersistentRootPath();
            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);
        }
        public static HardiskFileSearchResult SearchHardiskFileByPath(string relativePath)
        {
            HardiskFileSearchResult result = null;
            FileMeta trustFileMeta = null;
            if (RemoteFolder != null) 
            { 
                var find= RemoteFolder.ContainsFile(relativePath,out var remoteFileMeta);
                if (find)
                {
                    trustFileMeta = remoteFileMeta;
                }
            }
            if (trustFileMeta != null)
            {
                return SearchHardiskFile(trustFileMeta);
            }
            else
            {
                for (int i = 0; i < _localFolders.Count; i++)
                {
                    var folder = _localFolders[i];
                    if (folder.ContainsFile(relativePath,out var localFileMeta))
                    {
                        result = new HardiskFileSearchResult(localFileMeta, folder.GetFileHardiskPath(localFileMeta.RelativePath), folder.GetDiretoryType());
                        return result;
                    }
                }
            }
            Logger.Error($"Not found bundle in patch manifest : {relativePath}");
            result = new HardiskFileSearchResult(relativePath);
            return result;
        }
        public static HardiskFileSearchResult SearchHardiskFile(FileMeta fileMeta)
        {
            if (!fileMeta.IsValid())
                return HardiskFileSearchResult.EMPTY;
            HardiskFileSearchResult result = null;
            Folder currentLocalFolder = null;
            FileMeta currentFileMeta=   null;
            for (int i = 0; i < _localFolders.Count; i++)
            {
                var folder= _localFolders[i];
                if (folder.ContainsFile(fileMeta)) 
                {
                    currentLocalFolder = folder;
                    currentFileMeta= fileMeta;
                    break;
                }
            }
            if (currentLocalFolder != null)
            {
                result = new HardiskFileSearchResult(currentFileMeta, currentLocalFolder.GetFileHardiskPath(fileMeta.RelativePath), currentLocalFolder.GetDiretoryType());
                return result;
            }
            else 
            {
                if (RemoteFolder != null)
                {
                    var find= RemoteFolder.SearchFile(PersistentDownloadFolder,BuildInFolder, fileMeta,out var hardiskFileSearchResult);
                    if (find) 
                    {
                        result = hardiskFileSearchResult;
                        return result;
                    }
                }
            }
            Logger.Error($"Not found bundle in patch manifest : {fileMeta.RelativePath}");
            result = new HardiskFileSearchResult(fileMeta.RelativePath);
            return result; 
        }

        public static bool CheckBundleManifestValid(BundleManifest bundleManifest,
            string mainRelativePath,
            out HardiskFileSearchResult mainResult,
            out List<HardiskFileSearchResult> dependency,
            bool skipDownloadFolder = true
            )
        {

            if (bundleManifest == null) 
            {
                mainResult = new HardiskFileSearchResult(mainRelativePath);
                dependency = new List<HardiskFileSearchResult>();
                return false;
            }
            
            var mainFileMeta = bundleManifest.GetBundleFileMeta(mainRelativePath);
            var dependencyFileMetas = bundleManifest.GetAllDependenciesRelativePath(mainRelativePath);

            if (mainFileMeta == FileMeta.ERROR_FILE_META) 
            {
                mainResult = new HardiskFileSearchResult(mainRelativePath);
                dependency = new List<HardiskFileSearchResult>();
                return false;
            }
            string mainHardDiskFilePath = "";
            var mainHardiskDirectoryType = EnumHardiskDirectoryType.Invalid;
            List<(FileMeta, string, EnumHardiskDirectoryType)> deps = new List<(FileMeta, string, EnumHardiskDirectoryType)>();

            bool valid = false;
            for (int i = 0; i <_localFolders.Count; i++)
            {
                var folder = _localFolders[i];
                if (skipDownloadFolder && folder == PersistentDownloadFolder)
                {
                    continue; 
                }
                if (folder.SafeContainsFile(mainFileMeta)) 
                {
                    mainHardDiskFilePath = folder.GetFileHardiskPath(mainFileMeta.RelativePath);
                    mainHardiskDirectoryType = folder.GetDiretoryType();
                    valid = true; 
                    break;
                }
            }
            if (valid && dependencyFileMetas != null && dependencyFileMetas.Count > 0) 
            {
                for (int i = 0; i < dependencyFileMetas.Count; i++)
                {
                    var dp = dependencyFileMetas[i];
                    var dpValid = false;
                    for (int j = 0; j < _localFolders.Count; j++)
                    {
                        var folder = _localFolders[j];
                        if (skipDownloadFolder && folder == PersistentDownloadFolder)
                        {
                            continue;
                        }
                        if (folder.SafeContainsFile(dp))
                        {
                            deps.Add((dp,folder.GetFileHardiskPath(dp.RelativePath),folder.GetDiretoryType())) ;
                            dpValid = true;
                            break;
                        }
                    }
                    if (!dpValid) 
                    {
                        valid = false;
                    }
                }
            }
            if (valid)
            {
                mainResult = new HardiskFileSearchResult(mainFileMeta, mainHardDiskFilePath, mainHardiskDirectoryType);
                dependency = new List<HardiskFileSearchResult>();
                foreach (var dpInfo in deps)
                {
                    dependency.Add(new HardiskFileSearchResult(dpInfo.Item1, dpInfo.Item2, dpInfo.Item3));
                }
            }
            else 
            {
                mainResult = new HardiskFileSearchResult(mainRelativePath);
                dependency = new List<HardiskFileSearchResult>();
            }
            return valid; 
        }

        public static FileMeta GetBundleRelativePath(string assetPath)
        {
            var trustBundleManifest = GetTrustBundleManifest();
            return trustBundleManifest.GetBundleFileMeta(assetPath);
        }
        public static List<FileMeta> GetAllDependencieBundleRelativePaths(string assetPath)
        {
            var trustBundleManifest = GetTrustBundleManifest();
            return trustBundleManifest.GetAllDependenciesRelativePath(assetPath);
        }
        public static void SearchLocalSecurityBundleHardDiskFileByPath(
            string relativePath,
            out HardiskFileSearchResult mainResult, 
            out List<HardiskFileSearchResult> dependency,
            bool skipDownloadFolder= true)
        {
            for (int i = 0; i <_localFolders.Count; i++)
            {
                var localFolder = _localFolders[i];
                bool isValid = CheckBundleManifestValid(localFolder.BundleManifest, relativePath,out mainResult,out dependency, skipDownloadFolder);
                if (isValid) 
                {
                    return;
                }
            }
            Logger.Error("没有找到"+ relativePath);
            mainResult = new HardiskFileSearchResult(relativePath);
            dependency = new List<HardiskFileSearchResult>();
        }
    }
    public class RemoteFolder : Folder
    {
        private HostPlayModeImpl _hostPlayModeImpl= null;
        public RemoteFolder(HostPlayModeImpl hostPlayModeImpl) : base(hostPlayModeImpl.RemoteVersionRootUrl)
        {
            _hostPlayModeImpl= hostPlayModeImpl;
        }

        public bool SearchFile(PersistentDownloadFolder downloadFolder,BuildInFolder buildInFolder, FileMeta fileMeta, out HardiskFileSearchResult hardiskFileSearchResult)
        {
            hardiskFileSearchResult = null;
            if (ContainsFile(fileMeta)) 
            {
                FileMeta patchFileMetaCandidate = null;
                EnumHardiskDirectoryType directoryType = EnumHardiskDirectoryType.Invalid;
                if (downloadFolder.ContainsFile(fileMeta.RelativePath, out var downloadFileMeta))
                {
                    if (downloadFileMeta.Hash != fileMeta.Hash) 
                    {
                        patchFileMetaCandidate = downloadFileMeta;
                        directoryType = EnumHardiskDirectoryType.PersistentDownload;
                    }
                }
                if (patchFileMetaCandidate == null)
                {
                    if (buildInFolder.ContainsFile(fileMeta.RelativePath, out var buildInFileMeta))
                    {
                        if (buildInFileMeta.Hash != fileMeta.Hash)
                        {
                            patchFileMetaCandidate = buildInFileMeta;
                            directoryType = EnumHardiskDirectoryType.BuildIn;
                        }
                    }
                }
                hardiskFileSearchResult = this._hostPlayModeImpl.ConvertToDownloadInfo(fileMeta, patchFileMetaCandidate, directoryType);
                return true;
            }
            return false;
        }

       
        public override bool IsWriteable()
        {
            return false;
        }
    }
    public class PersistentDownloadFolder : Folder 
    {
        private List<(string, string)> _relativePaths = new List<(string, string)>();
        private int _currentCheckIndex = 0;
        private const int MAX_STEP = 20;
        
        public PersistentDownloadFolder(string basePath):base(basePath)
        {
           
        }

        public void InitManifest()
        {
            string filePath = GetFileHardiskPath(URSRuntimeSetting.instance.FileManifestFileName);
            if (File.Exists(filePath))
            {
                Logger.Log($"Load  FileManifest manifest." + filePath);
                string jsonData = File.ReadAllText(filePath);
                _fileManifest = FileManifest.Deserialize(jsonData);
                //  _cachedFileMap = sInitSandboxFileManifest.GetFileMetaMap();
            }
            filePath = GetFileHardiskPath(URSRuntimeSetting.instance.BundleManifestFileRelativePath);
            if (File.Exists(filePath))
            {
                Logger.Log($"Load  BundleManifest manifest." + filePath);
                string jsonData = File.ReadAllText(filePath);
                _bundleManifest = BundleManifest.Deserialize(jsonData);
            }
        }

        public void BeginCheckLocalFile()
        {
            _relativePaths.Clear();
            HashSet<string> relativePathSet = new HashSet<string>();
            string directoryRoot = _basePath;
            Directory.CreateDirectory(directoryRoot);
            var pathLength = directoryRoot.Length;
            var paths = Directory.GetFiles(directoryRoot, "*", SearchOption.AllDirectories);
            if (paths != null && paths.Length > 0)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    var fullPath = paths[i];
                    var relativePath = paths[i].Substring(pathLength + 1);
                    var pure = relativePath.Replace('\\', '/').Replace("//", "/");
                    _relativePaths.Add((pure, fullPath));
                    relativePathSet.Add(pure);
                }
            }
            RemoveFileMetaMissed( relativePathSet);
            _currentCheckIndex = 0;
        }

        private void RemoveFileMetaMissed(HashSet<string> rPaths)
        {
            if (_fileManifest != null) 
            {
                var fileMap = _fileManifest.GetFileMetaMap();
                List<string> removes = null;
                foreach (var rPath in fileMap.Keys)
                {

                    if (!rPaths.Contains(rPath))
                    {
                        if (removes == null)
                        {
                            removes = new List<string>();
                        }

                        removes.Add(rPath);
                    }
                }
                if (removes != null)
                {
                    for (int i = 0; i < removes.Count; i++)
                    {
                        var rPath = removes[i];
                        DeleteFile(rPath);
                    }
                }
            }
        }

        public bool CheckLocalFile(FileManifest fileManifest)
        {
            if (fileManifest == null) return true;
            for (int i = 0; i < MAX_STEP; i++)
            {
                var currentIndex= _currentCheckIndex++;
                if (currentIndex < _relativePaths.Count)
                {
                    var relativePath = _relativePaths[currentIndex].Item1;
                    var fullPath = _relativePaths[currentIndex].Item2;
                    if (relativePath == URSRuntimeSetting.instance.FileManifestFileName)
                    {
                        continue;
                    }
                   // var webFileManifest = _impl.WebFileManifest;
                    var fileMap = fileManifest.GetFileMetaMap();
                    if (fileMap.ContainsKey(relativePath))
                    {
                        var fileMeta = fileMap[relativePath];
                      
                        if (CheckContentSize(fullPath, fileMeta.SizeBytes)) // 不检测hash啦，任何资源进去这个文件夹的时候，检测hash就可以了，
                        {
                            RegisterVerifyFile(fileMeta);
                        }
                        else
                        {
                            DeleteFile(relativePath);
                        }
                    }
                }
                else
                {
                    FlushFileManifestToHardisk();
                    return true;
                }
            }
            return false;
        }

        public void EndCheck() 
        {
            _relativePaths.Clear();
            _currentCheckIndex = 0;
        }
        /// <summary>
        /// 缓存验证过的文件
        /// </summary>
        public void RegisterVerifyFile(FileMeta fileMeta)
        {
            if (_fileManifest == null)
            {
                _fileManifest = new FileManifest(null,null);
            }
            _fileManifest.ReplaceFile(fileMeta);
        }

        public void DeleteFile(string fileRelativePath)
        {
            string filePath = GetFileHardiskPath(fileRelativePath);
            if (File.Exists(filePath))
                File.Delete(filePath);
            _fileManifest.RemoveFile(fileRelativePath);
        }
        // 验证文件完整性
        public bool CheckContentIntegrity(FileMeta fileMeta, string hardiskFilePath)
        {
            return CheckContentIntegrity(hardiskFilePath, fileMeta.SizeBytes, fileMeta.Hash);
        }
        public bool CheckContentIntegrity(FileMeta fileMeta)
        {
            string filePath = GetFileHardiskPath(fileMeta.RelativePath);
            return CheckContentIntegrity(filePath, fileMeta.SizeBytes, fileMeta.Hash);
        }
        public virtual bool CheckContentIntegrity(string filePath, long size, uint fileHash)
        {
            if (File.Exists(filePath) == false)
            {
                //Logger.Error($"验证失败，文件不存在{filePath}");
                return false;
            }


            // 先验证文件大小
            long fileSize = FileUtility.GetFileSize(filePath);
            if (fileSize != size)
            {
                //Logger.Error($"验证失败，文件大小不通过 目标大小 {size} ，当前文件大小{fileSize}");
                return false;
            }


            // 再验证文件CRC
            var currentFileHash = Hashing.GetFileXXhash(filePath);
            var success = fileHash == currentFileHash;
            if (!success)
            {
                //Logger.Error($"验证失败，文件大小不通过 目标hash  {fileHash} ，当前文件大小{currentFileHash}");
                return false;
            }
            return success;
        }
        public virtual bool CheckContentSize(string filePath, long size)
        {
            if (File.Exists(filePath) == false)
            {
                //Logger.Error($"验证失败，文件不存在{filePath}");
                return false;
            }


            // 先验证文件大小
            long fileSize = FileUtility.GetFileSize(filePath);
            if (fileSize != size)
            {
                //Logger.Error($"验证失败，文件大小不通过 目标大小 {size} ，当前文件大小{fileSize}");
                return false;
            }
           
            return true;
        }

        public override bool SafeContainsFile(FileMeta fileMeta)
        {
            var selfContains = ContainsFile(fileMeta);
            if (selfContains) 
            {
                var check= CheckContentIntegrity(fileMeta);
                if (check)
                {
                    return true;
                }
            }
            return false;
           
        }
        public void FlushFileManifestToHardisk()
        {
            if (_fileManifest != null)
            {
                var fileMap = _fileManifest.GetFileMetaMap();
                if (fileMap != null)
                {
                    _fileManifest.FileMetas = new List<FileMeta>(fileMap.Values).ToArray();
                    System.Array.Sort(_fileManifest.FileMetas, (a, b) =>
                    {
                        return a.RelativePath.CompareTo(b.RelativePath);
                    });
                    var savePath = GetFileHardiskPath(URSRuntimeSetting.instance.FileManifestFileName);
                    FileManifest.Serialize(savePath, _fileManifest, true);
                }
            }
        }

        public override bool IsWriteable()
        {
            return true;
        }
    }

    public class PersistentReadOnlyFolder : PersistentDownloadFolder
    {
        public PersistentReadOnlyFolder(string basePath) : base(basePath)
        {

        }

        public override bool IsWriteable()
        {
            return false;
        }
    }

    public class BuildInFolder : Folder
    {
        public BuildInFolder(string basePath) : base(basePath)
        {

        }
        public override bool IsWriteable()
        {
            return false;
        }

        public override bool SafeContainsFile(FileMeta fileMeta)
        {
            return ContainsFile(fileMeta);
        }
    }

   
    public class Folder
    {
        public string _basePath;

        public FileManifest _fileManifest;

        public BundleManifest _bundleManifest;


        public virtual void InitFolder() 
        { 
            
        }

        public virtual EnumHardiskDirectoryType GetDiretoryType() 
        {
            return EnumHardiskDirectoryType.Invalid;
        }
        public Folder(string basePath)
        {
            _basePath = basePath;
        }

        public FileManifest FileManifest
        {
            get 
            { 
                return _fileManifest;
            }
            set 
            {
                _fileManifest = value;
            }
        }

        public BundleManifest BundleManifest
        {
            get
            {
                return _bundleManifest;
            }
            set
            {
                _bundleManifest = value;
            }
        }

        public  Dictionary<string, FileMeta> GetFileMap()
        {
            if (_fileManifest != null)
            {
                return _fileManifest.GetFileMetaMap();
            }
            return null;
        }

        public  virtual bool TryGetHardiskFilePath(string relativePath, out string hardiskPath, out FileMeta fileMeta)
        {
            hardiskPath = null;
            fileMeta = null;
            var filemap = GetFileMap();
            if (filemap == null)
            {
                return false;
            }
            if (filemap.ContainsKey(relativePath))
            {
                var fm = filemap[relativePath];
                hardiskPath = GetFileHardiskPath(relativePath);
                fileMeta = fm;
            }
            return false;
        }

        public virtual string GetFileHardiskPath(string relativePath)
        {
            return $"{_basePath}/{relativePath}";
        }
        public virtual string GetFileHardiskPath(FileMeta fileMeta)
        {
            return $"{_basePath}/{fileMeta.RelativePath}";
        }
        /// <summary>
        /// 查询是否为验证文件
        /// 注意：被收录的文件完整性是绝对有效的
        /// </summary>
        public bool ContainsFile(string relativePath)
        {
            var filemap = GetFileMap();
            if (filemap == null)
            {
                return false;
            }
            if (filemap.ContainsKey(relativePath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ContainsFile(string relativePath,out FileMeta fileMeta)
        {
            var filemap = GetFileMap();
            fileMeta = null;
            if (filemap == null)
            {
                return false;
            }
            if (filemap.ContainsKey(relativePath))
            {
                fileMeta = filemap[relativePath];
                return true;
            }
            else
            {
                return false;
            }
        }
        public  bool ContainsFile(FileMeta fileMeta)
        {
            var selfContains=  ContainsFile(fileMeta.RelativePath,out var selfFileMeta);
            if (!selfContains) return false;
            if (selfFileMeta.Hash != fileMeta.Hash) return false;
            if (selfFileMeta.SizeBytes != fileMeta.SizeBytes) return false;
            return true;
        }

        public virtual bool SafeContainsFile(FileMeta fileMeta) 
        {
            return false;
        }

       

        public void GetFileMetaByTag(string[] tags, ref List<FileMeta> result)
        {
            if (_fileManifest != null)
            {
                _fileManifest.GetFileMetaByTag(tags, ref result);
            }
        }

        public virtual bool IsWriteable() 
        {
            return true;
        }
       
    }
}



