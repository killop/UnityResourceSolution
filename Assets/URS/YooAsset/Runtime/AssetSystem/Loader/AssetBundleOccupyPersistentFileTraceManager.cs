using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class AssetBundleOccupyPersistentFileTraceManager 
{
    public static AssetBundleOccupyPersistentFileTraceManager Instance = new AssetBundleOccupyPersistentFileTraceManager();

    public HashSet<AssetBundleLoader> _workingAssetBundleLoaders = new HashSet<AssetBundleLoader>();

    public HashSet<string> _filePaths= new HashSet<string>();

    private bool enable = false;

    public void Register(AssetBundleLoader assetBundleLoader) 
    {
        if (!enable) return;
        _workingAssetBundleLoaders.Add(assetBundleLoader);
        _filePaths.Add(assetBundleLoader.HardiskFileSearchResult.HardiskPath);
    }

    public void Unregister(AssetBundleLoader assetBundleLoader) 
    {
        if (!enable) return;
        _workingAssetBundleLoaders.Remove(assetBundleLoader);
        _filePaths.Remove(assetBundleLoader.HardiskFileSearchResult.HardiskPath);
    }

   

    public bool IsFileOccupy(string filePath) 
    { 
        return _filePaths.Contains(filePath);
    }

    public void BeginTrace() 
    {
        enable = true;
    }

    public void EndTrace()
    {
        enable = false;
    }
}
