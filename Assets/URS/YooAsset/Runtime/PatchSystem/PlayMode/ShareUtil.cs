/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace URS {
    public class ShareUtil
    {
        public static void SearchLocalSecurityBundleHardDiskFileByPath(FileManifest appFileManifest, BundleManifest appBundleManifest, string relativePath, out HardiskFileSearchResult mainResult, out List<HardiskFileSearchResult> dependency)
        {
            bool valid = false;
            var sandboxBundleManifest = SandboxFileSystem.GetBundleManifest();

            string mainHardDiskFilePath = "";
            var mainHardiskDirectoryType = EnumHardiskDirectoryType.Invalid;
            FileMeta mainFileMeta=null;
            List<(FileMeta, string, EnumHardiskDirectoryType)> deps = new List<(FileMeta, string, EnumHardiskDirectoryType)>();
            if (sandboxBundleManifest != null)
            {
                var fileMeta = sandboxBundleManifest.GetBundleFileMeta(relativePath);
                var dependencyFileMetas = sandboxBundleManifest.GetAllDependenciesRelativePath(relativePath);
                if (fileMeta != FileMeta.ERROR_FILE_META && dependencyFileMetas != null)
                {
                    bool mainIsOK = SandboxFileSystem.CheckContentIntegrity(fileMeta);
                    if (!mainIsOK)
                    {
                        if (appFileManifest != null)
                        {
                            var exist = appFileManifest.ContainFile(fileMeta);
                            if (exist)
                            {
                                mainFileMeta = fileMeta;
                                mainHardiskDirectoryType = EnumHardiskDirectoryType.StreamAsset;
                                mainHardDiskFilePath = AssetPathHelper.MakeStreamingSandboxLoadPath(fileMeta.RelativePath);
                            }
                            mainIsOK = exist;
                        }

                    }
                    else
                    {
                        mainFileMeta = fileMeta;
                        mainHardiskDirectoryType = EnumHardiskDirectoryType.PersistentDownload;
                        mainHardDiskFilePath = SandboxFileSystem.MakeSandboxFilePath(mainFileMeta.RelativePath);
                    }
                    if (mainIsOK)
                    {

                        bool dependencyIsOK = true;
                        if (dependencyFileMetas.Count > 0)
                        {
                            foreach (var fm in dependencyFileMetas)
                            {
                                bool dpIsOK = SandboxFileSystem.CheckContentIntegrity(fm);
                                if (!dpIsOK)
                                {
                                    if (appFileManifest != null)
                                    {
                                        var exist = appFileManifest.ContainFile(fm);
                                        if (exist)
                                        {
                                            var dpInfo = (fm, AssetPathHelper.MakeStreamingSandboxLoadPath(fm.RelativePath), EnumHardiskDirectoryType.StreamAsset);
                                            deps.Add(dpInfo);
                                        }
                                        dpIsOK = exist;
                                    }

                                }
                                else
                                {
                                    var dpInfo = (fm, SandboxFileSystem.MakeSandboxFilePath(fm.RelativePath), EnumHardiskDirectoryType.PersistentDownload);
                                    deps.Add(dpInfo);
                                }
                                if (!dpIsOK)
                                {
                                    dependencyIsOK = false;
                                    break;
                                }
                            }
                        }
                        if (dependencyIsOK)
                        {
                            valid = true;
                        }
                    }
                }
            }
            if (!valid && appBundleManifest != null)
            {
                var fileMeta = appBundleManifest.GetBundleFileMeta(relativePath);
                valid = (fileMeta != FileMeta.ERROR_FILE_META);
                if (valid)
                {
                    mainFileMeta = fileMeta;
                    mainHardiskDirectoryType = EnumHardiskDirectoryType.StreamAsset;
                    mainHardDiskFilePath = AssetPathHelper.MakeStreamingSandboxLoadPath(fileMeta.RelativePath);

                    var appDps = appBundleManifest.GetAllDependenciesRelativePath(relativePath);
                    if (appDps != null)
                    {
                        foreach (var appDp in appDps)
                        {
                            deps.Add((appDp, AssetPathHelper.MakeStreamingSandboxLoadPath(appDp.RelativePath) ,EnumHardiskDirectoryType.StreamAsset));
                        }
                    }
                }
            }
            if (valid)
            {
                mainResult = new HardiskFileSearchResult(mainFileMeta, mainHardDiskFilePath, mainHardiskDirectoryType );
                dependency = new List<HardiskFileSearchResult>();
                foreach (var dpInfo in deps)
                {
                    dependency.Add(new HardiskFileSearchResult(dpInfo.Item1, dpInfo.Item2, dpInfo.Item3));
                }
            }
            else
            {
                mainResult = new HardiskFileSearchResult(relativePath);
                dependency = new List<HardiskFileSearchResult>();
            }

        }
    }
}
*/
