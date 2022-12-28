using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssetBundleBrowser.AssetBundleDataSource;

public class URSBundleDataSource : ABDataSource
{

    public BuildTaskShowAssetBundleBrowser task;
    /// </summary>
    public string Name {
        get { return "URSBundleDataSource"; }
    }
    /// <summary>
    /// Name of provider for DataSource. Displayed in menu as "Name (ProvidorName)"
    /// </summary>
    public string ProviderName {
        get { return "URSBundleDataSource"; }
    }

    /// <summary>
    /// Array of paths in bundle.
    /// </summary>
    public string[] GetAssetPathsFromAssetBundle(string assetBundleName)
    {
        return task.GetAssetPathsFromAssetBundle(assetBundleName);
    }
    /// <summary>
    /// Name of bundle explicitly associated with asset at path.  
    /// </summary>
    public string GetAssetBundleName(string assetPath)
    {
        return task.GetAssetBundleName(assetPath);
    }
    /// <summary>
    /// Name of bundle associated with asset at path.  
    ///  The difference between this and GetAssetBundleName() is for assets unassigned to a bundle, but
    ///  residing inside a folder that is assigned to a bundle.  Those assets will implicitly associate
    ///  with the bundle associated with the parent folder.
    /// </summary>
    public string GetImplicitAssetBundleName(string assetPath) {
        return string.Empty;
    }
    /// <summary>
    /// Array of asset bundle names in project
    /// </summary>
    public string[] GetAllAssetBundleNames()
    {
        return task.GetAllAssetBundleNames();
    }
    /// <summary>
    /// If this data source is read only. 
    ///  If this returns true, much of the Browsers's interface will be disabled (drag&drop, etc.)
    /// </summary>
    public bool IsReadOnly() {
        return true;
    }

    /// <summary>
    /// Sets the asset bundle name (and variant) on a given asset
    /// </summary>
    public  void SetAssetBundleNameAndVariant(string assetPath, string bundleName, string variantName) {
        // do nothing
    }
    /// <summary>
    /// Clears out any asset bundle names that do not have assets associated with them.
    /// </summary>
    public void RemoveUnusedAssetBundleNames() {
        // do nothing
    }

    /// <summary>
    /// Signals if this data source can have build target set by tool
    /// </summary>
    public bool CanSpecifyBuildTarget { get { return false; } }
    /// <summary>
    /// Signals if this data source can have output directory set by tool
    /// </summary>
    public bool CanSpecifyBuildOutputDirectory { get { return false; } }
    /// <summary>
    /// Signals if this data source can have build options set by tool
    /// </summary>
    public bool CanSpecifyBuildOptions { get { return false; } }

    /// <summary>
    /// Executes data source's implementation of asset bundle building.
    ///   Called by "build" button in build tab of tool.
    /// </summary>
    public bool BuildAssetBundles(ABBuildInfo info) {
        return true;
    }
}
public class BuildTaskShowAssetBundleBrowser : BuildTask
{
    private Dictionary<string, BundleInfo> _bundleInfo = null;
    private Dictionary<string, AssetInfo> _assetInfo = null;
    public override void BeginTask()
    {
        base.BeginTask();
        _bundleInfo = GetData<Dictionary<string, BundleInfo>>(CONTEXT_BUNDLE_INFO);
        _assetInfo = GetData<Dictionary<string, AssetInfo>>(CONTEXT_ASSET_INFO);
        URSBundleDataSource uRSBundleDataSource = new URSBundleDataSource() {
            task = this
        };
        //var list = new List<ABDataSource>();
        //list.Add(uRSBundleDataSource);
        AssetBundleBrowser.AssetBundleModel.Model.DataSource = uRSBundleDataSource;
        AssetBundleBrowser.AssetBundleBrowserMain.URSShowWindow();
        this.FinishTask();
    }

    public string[] GetAssetPathsFromAssetBundle(string assetBundleName)
    {
        if (_bundleInfo.ContainsKey(assetBundleName))
        {
            return _bundleInfo[assetBundleName].paths.ToArray();
        }
        else
        {
            return new string[0];
        }
    }
    public string GetAssetBundleName(string assetPath)
    {
        if (_assetInfo.ContainsKey(assetPath))
        {
            var assetInfo = _assetInfo[assetPath];
            if (assetInfo.HasAssetBundleName())
            {
                var abName= assetInfo.GetAssetBundleName();
                return abName;
            }
            else
            {
                return string.Empty;
            }
        }
        else
        {
            return string.Empty;
        }
    }

    public string[] GetAllAssetBundleNames()
    {
        var allBundleName = new List<string>();

        foreach (var item in _bundleInfo)
        {
            allBundleName.Add(item.Value.bundleName);
        }
        return allBundleName.ToArray();
    }
}
