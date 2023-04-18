using BestHTTP.Examples.Helpers.Components;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using URS;

namespace YooAsset
{
	/// <summary>
	/// 初始化操作
	/// </summary>
	public abstract class InitializationOperation : AsyncOperationBase
	{
	}

	/// <summary>
	/// 编辑器下模拟运行的初始化操作
	/// </summary>
	internal class EditorModeInitializationOperation : InitializationOperation
	{
		internal override void Start()
		{
			Status = EOperationStatus.Succeed;
		}
		internal override void Update()
		{
		}
	}

	/// <summary>
	/// 离线模式的初始化操作
	/// </summary>
	internal class OfflinePlayModeInitializationOperation : InitializationOperation
	{
		private enum ESteps
		{
			None,
            CheckAppFootPrint,
            LoadBuildInFileManifest,
			CheckBuildInFileManifest,
            LoadBuildInBundleManifest,
            CheckBuildInBundleManifest,
            InitPersistentFolders,
            Done,
		}

		private OfflinePlayModeImpl _impl;
		private ESteps _steps = ESteps.None;
		private UnityWebRequester _fileManifestDownloader;
        private UnityWebRequester _BunleManifestDownloader;
        private string _downloadURL;

		internal OfflinePlayModeInitializationOperation(OfflinePlayModeImpl impl)
		{
			_impl = impl;
		}
		internal override void Start()
		{
			_steps = ESteps.CheckAppFootPrint;
		}
		internal override void Update()
		{
			if (_steps == ESteps.None || _steps == ESteps.Done)
				return;
            if (_steps == ESteps.CheckAppFootPrint)
            {
                // 每次启动时比对APP版本号是否一致	
                AppFootPrint fp = AppFootPrint.Load();
                if (fp == null)
                {
                    AppFootPrint.Create();
                }
                else
                {
                    if (fp.IsDirty())
                    {
                        if (_impl.ClearCacheWhenDirty)
                        {
                            Logger.Warning("Clear cache files.");
                            URSFileSystem.DeletePersistentRootFolder();
                            AppFootPrint.Create();
                        }
                    }
                }
                _steps = ESteps.LoadBuildInFileManifest;
            }
            if (_steps == ESteps.LoadBuildInFileManifest)
			{
				string filePath = AssetPathHelper.MakeURSBuildInResourcePath(URSRuntimeSetting.instance.FileManifestFileName);
				_downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
				_fileManifestDownloader = new UnityWebRequester();
				_fileManifestDownloader.SendRequest(_downloadURL);
				_steps = ESteps.CheckBuildInFileManifest;
			}

			if (_steps == ESteps.CheckBuildInFileManifest)
			{
				if (_fileManifestDownloader.IsDone() == false)
					return;

                if (_fileManifestDownloader.HasError())
                {
                    //Error = _fileManifestDownloader.GetError();
                    _fileManifestDownloader.Dispose();

                }
                else
                {
                   URSFileSystem.BuildInFolder.FileManifest = FileManifest.Deserialize(_fileManifestDownloader.GetText());
                }

				// 解析APP里的补丁清单
				
				_fileManifestDownloader.Dispose();
                _steps = ESteps.LoadBuildInBundleManifest;
				
			}
            if (_steps == ESteps.LoadBuildInBundleManifest)
            {
                string filePath = AssetPathHelper.MakeURSBuildInResourcePath(URSRuntimeSetting.instance.BundleManifestFileRelativePath);
                _downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
                _BunleManifestDownloader = new UnityWebRequester();
                _BunleManifestDownloader.SendRequest(_downloadURL);
                _steps = ESteps.CheckBuildInBundleManifest;
            }
            if (_steps == ESteps.CheckBuildInBundleManifest)
            {
                if (_BunleManifestDownloader.IsDone() == false)
                    return;

                if (_BunleManifestDownloader.HasError())
                {
                   // Error = _BunleManifestDownloader.GetError();
                }
                else
                {
                    URSFileSystem.BuildInFolder.BundleManifest = BundleManifest.Deserialize(_BunleManifestDownloader.GetText());
                }
                _BunleManifestDownloader.Dispose();
                _steps = ESteps.InitPersistentFolders;
               
            }
            if(_steps == ESteps.InitPersistentFolders)
            {
                URSFileSystem.InitPersistentFolders();
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }

        }
	}

	/// <summary>
	/// 网络模式的初始化操作
	/// </summary>
	internal class HostPlayModeInitializationOperation : InitializationOperation
	{
		private enum ESteps
		{
			None,
		


            CheckAppFootPrint,

			LoadBuildInFileManifest,
			CheckBuildInFileManifest,
            LoadBuildInBundleManifest,
            CheckBuildInBundleManifest,

            InitPersistentFolders,

            Done,
		}

		private HostPlayModeImpl _impl;
		private ESteps _steps = ESteps.None;


        
        private UnityWebRequester _appFileManifestDownloader;
        private UnityWebRequester _appBundleManifestDownloader;

        private string _downloadURL;

		internal HostPlayModeInitializationOperation(HostPlayModeImpl impl)
		{
			_impl = impl;
            URSFileSystem.InitRemoteFolder(impl);
		}
		internal override void Start()
		{
			_steps = ESteps.CheckAppFootPrint;
		}
		internal override void Update()
		{
			if (_steps == ESteps.None || _steps == ESteps.Done)
				return;
            
            if (_steps == ESteps.CheckAppFootPrint)
			{
                // 每次启动时比对APP版本号是否一致	
                AppFootPrint fp = AppFootPrint.Load();
				if (fp == null)
				{
					AppFootPrint.Create();
				}
				else 
				{
					if (fp.IsDirty())
					{
                        if (_impl.ClearCacheWhenDirty)
                        {
                            Logger.Warning("Clear cache files.");
                            URSFileSystem.DeletePersistentRootFolder();
                            AppFootPrint.Create();
                        }
                    }
				}
				_steps = ESteps.LoadBuildInFileManifest;
			}

			if (_steps == ESteps.LoadBuildInFileManifest)
			{
				// 加载APP内的补丁清单
				Logger.Log($"Load application file manifest.");
				string filePath = AssetPathHelper.MakeURSBuildInResourcePath(URSRuntimeSetting.instance.FileManifestFileName);
				_downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
				_appFileManifestDownloader = new UnityWebRequester();
				_appFileManifestDownloader.SendRequest(_downloadURL);
				_steps = ESteps.CheckBuildInFileManifest;
			}

			if (_steps == ESteps.CheckBuildInFileManifest)
			{
				if (_appFileManifestDownloader.IsDone() == false)
					return;

                if (_appFileManifestDownloader.HasError())
                {
                   // Error = _appFileManifestDownloader.GetError();
                    Logger.Warning($"can not Load application file manifest.error {Error}");
                }
                else
                {
                    // 解析补丁清单
                    string jsonData = _appFileManifestDownloader.GetText();
                    URSFileSystem.BuildInFolder.FileManifest = FileManifest.Deserialize(jsonData);
                }
				//_impl.LocalPatchManifest = _impl.AppPatchManifest;
				_appFileManifestDownloader.Dispose();
				_steps = ESteps.LoadBuildInBundleManifest;
			}
            if (_steps == ESteps.LoadBuildInBundleManifest)
            {
                // 加载APP内的补丁清单
                Logger.Log($"Load application bundle manifest.");
                string filePath = AssetPathHelper.MakeURSBuildInResourcePath(URSRuntimeSetting.instance.BundleManifestFileRelativePath);
                _downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
                _appBundleManifestDownloader = new UnityWebRequester();
                _appBundleManifestDownloader.SendRequest(_downloadURL);
                _steps = ESteps.CheckBuildInBundleManifest;
            }
            if (_steps == ESteps.CheckBuildInBundleManifest)
            {
                if (_appBundleManifestDownloader.IsDone() == false)
                    return;

                if (_appBundleManifestDownloader.HasError())
                {
                    //Error = _appBundleManifestDownloader.GetError();
                    Logger.Warning($"can not Load application bundle manifest.error {Error}");
                }
                else
                {
                    string jsonData = _appBundleManifestDownloader.GetText();
                    URSFileSystem.BuildInFolder.BundleManifest = BundleManifest.Deserialize(jsonData);
                }
                // 解析补丁清单
                //_impl.LocalPatchManifest = _impl.AppPatchManifest;
                _appBundleManifestDownloader.Dispose();
                _steps = ESteps.InitPersistentFolders;
            }
            if (_steps == ESteps.InitPersistentFolders)
			{

                URSFileSystem.InitPersistentFolders();
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
           
        }
	}
}