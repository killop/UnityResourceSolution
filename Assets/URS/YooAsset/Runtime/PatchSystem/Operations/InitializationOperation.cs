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
			LoadAppFileManifest,
			CheckAppFileManifest,
            LoadAppBundleManifest,
            CheckAppBundleManifest,
            InitSandbox,
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
			_steps = ESteps.LoadAppFileManifest;
		}
		internal override void Update()
		{
			if (_steps == ESteps.None || _steps == ESteps.Done)
				return;

			if (_steps == ESteps.LoadAppFileManifest)
			{
				string filePath = AssetPathHelper.MakeStreamingSandboxLoadPath(URSRuntimeSetting.instance.FileManifestFileName);
				_downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
				_fileManifestDownloader = new UnityWebRequester();
				_fileManifestDownloader.SendRequest(_downloadURL);
				_steps = ESteps.CheckAppFileManifest;
			}

			if (_steps == ESteps.CheckAppFileManifest)
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
                    _impl.AppFileManifest = FileManifest.Deserialize(_fileManifestDownloader.GetText());
                }

				// 解析APP里的补丁清单
				
				_fileManifestDownloader.Dispose();
                _steps = ESteps.LoadAppBundleManifest;
				
			}
            if (_steps == ESteps.LoadAppBundleManifest)
            {
                string filePath = AssetPathHelper.MakeStreamingSandboxLoadPath(URSRuntimeSetting.instance.BundleManifestFileName);
                _downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
                _BunleManifestDownloader = new UnityWebRequester();
                _BunleManifestDownloader.SendRequest(_downloadURL);
                _steps = ESteps.CheckAppBundleManifest;
            }
            if (_steps == ESteps.CheckAppBundleManifest)
            {
                if (_BunleManifestDownloader.IsDone() == false)
                    return;

                if (_BunleManifestDownloader.HasError())
                {
                   // Error = _BunleManifestDownloader.GetError();
                }
                else
                {
                    _impl.AppBundleManifest = BundleManifest.Deserialize(_BunleManifestDownloader.GetText());

                }
                _BunleManifestDownloader.Dispose();
                _steps = ESteps.InitSandbox;
               
            }
            if(_steps == ESteps.InitSandbox)
            {
                SandboxFileSystem.InitSandboxFileAndBundle();
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
			LoadAppId,
			CheckAppId,
            LoadChannelRouter,
            CheckChannelRouter,

            LoadFilesVersionIndex,
            CheckFilesVersionIndex,


            CheckAppFootPrint,

			LoadAppFileManifest,
			CheckAppFileManifest,
            LoadAppBundleManifest,
            CheckAppBundleManifest,

            InitSandbox,

            Done,
		}

		private HostPlayModeImpl _impl;
		private ESteps _steps = ESteps.None;


        private UnityWebRequester _appIdDownloader;
        private UnityWebRequester _remoteAppToChannelRouterFileDownloader;
        private UnityWebRequester _remoteFilesVersionIndexDownloader;
        private UnityWebRequester _appFileManifestDownloader;
        private UnityWebRequester _appBundleManifestDownloader;

        private string _downloadURL;

		internal HostPlayModeInitializationOperation(HostPlayModeImpl impl)
		{
			_impl = impl;
		}
		internal override void Start()
		{
			_steps = ESteps.LoadAppId;
		}
		internal override void Update()
		{
			if (_steps == ESteps.None || _steps == ESteps.Done)
				return;
            if (_steps == ESteps.LoadAppId)
            {
                
                _impl.AppId = null;
                string filePath = AssetPathHelper.MakeStreamingSandboxLoadPath(URSRuntimeSetting.instance.AppIdFileName);
                // 加载APP内的补丁清单
                Logger.Log($"Load application file manifest.{filePath}");
                var downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
                _appIdDownloader = new UnityWebRequester();
                _appIdDownloader.SendRequest(downloadURL);
                _steps = ESteps.CheckAppId;
            }
            if (_steps == ESteps.CheckAppId)
            {

                if (_appIdDownloader.IsDone() == false)
                    return;

                if (_appIdDownloader.HasError())
                {
                    Error = _appIdDownloader.GetError();
                    Logger.Warning($"can not Load _appId.error {Error}");
                }
                else
                {
                    // 解析补丁清单
                    string appid = _appIdDownloader.GetText();
                    _impl.AppId = appid;
                }
                //_impl.LocalPatchManifest = _impl.AppPatchManifest;
                _appIdDownloader.Dispose();
                _steps = ESteps.LoadChannelRouter;
            }
            if (_steps == ESteps.LoadChannelRouter)
            {
                // 加载APP内的补丁清单
                string filePath =_impl.RemoteAppToChannelRouterFileUrl;
                var downloadURL = (filePath);
                _remoteAppToChannelRouterFileDownloader = new UnityWebRequester();
                _remoteAppToChannelRouterFileDownloader.SendRequest(downloadURL);
                _steps = ESteps.CheckChannelRouter;
            }
            if (_steps == ESteps.CheckChannelRouter)
            {

                if (_remoteAppToChannelRouterFileDownloader.IsDone() == false)
                    return;

                if (_remoteAppToChannelRouterFileDownloader.HasError())
                {
                    Error = _remoteAppToChannelRouterFileDownloader.GetError();
                    Logger.Warning($"can not Load _remoteAppToChannelRouterFile.error {Error}");
                }
                else
                {
                    // 解析补丁清单
                    string jsonText = _remoteAppToChannelRouterFileDownloader.GetText();
                    var  router  = AppToChannelRouter.Deserialize(jsonText);
                    var item = router.GetChanel(_impl.AppId);
                    _impl.InitVersion(item);
                }
                //_impl.LocalPatchManifest = _impl.AppPatchManifest;
                _remoteAppToChannelRouterFileDownloader.Dispose();
                _steps = ESteps.LoadFilesVersionIndex;
            }
            if (_steps == ESteps.LoadFilesVersionIndex)
            {
                // 加载APP内的补丁清单
                string filePath = _impl.GetRemoteFilesVersionIndexUrl();
                var downloadURL = filePath;
                _remoteFilesVersionIndexDownloader = new UnityWebRequester();
                _remoteFilesVersionIndexDownloader.SendRequest(downloadURL);
                _steps = ESteps.CheckFilesVersionIndex;
            }
            if (_steps == ESteps.CheckFilesVersionIndex)
            {

                if (_remoteFilesVersionIndexDownloader.IsDone() == false)
                    return;

                if (_remoteFilesVersionIndexDownloader.HasError())
                {
                    // Error = _appIdDownloader.GetError();
                    Logger.Warning($"can not Load _remoteFilesVersionIndex .error {Error}");
                }
                else
                {
                    // 解析补丁清单
                    string jsonText = _remoteFilesVersionIndexDownloader.GetText();
                    var versionIndex = JsonUtility.FromJson<URSFilesVersionIndex>(jsonText);
                    versionIndex.AfterSerialize();
                    _impl.InitFilesVersion(versionIndex);
                }
                //_impl.LocalPatchManifest = _impl.AppPatchManifest;
                _remoteFilesVersionIndexDownloader.Dispose();
                _steps = ESteps.CheckAppFootPrint;
            }
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
                            SandboxFileSystem.DeleteSandboxFolder();
                            AppFootPrint.Create();
                        }
                    }
				}
				_steps = ESteps.LoadAppFileManifest;
			}

			if (_steps == ESteps.LoadAppFileManifest)
			{
				// 加载APP内的补丁清单
				Logger.Log($"Load application file manifest.");
				string filePath = AssetPathHelper.MakeStreamingSandboxLoadPath(URSRuntimeSetting.instance.FileManifestFileName);
				_downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
				_appFileManifestDownloader = new UnityWebRequester();
				_appFileManifestDownloader.SendRequest(_downloadURL);
				_steps = ESteps.CheckAppFileManifest;
			}

			if (_steps == ESteps.CheckAppFileManifest)
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
                    _impl.AppFileManifest = FileManifest.Deserialize(jsonData);
                }
				//_impl.LocalPatchManifest = _impl.AppPatchManifest;
				_appFileManifestDownloader.Dispose();
				_steps = ESteps.LoadAppBundleManifest;
			}
            if (_steps == ESteps.LoadAppBundleManifest)
            {
                // 加载APP内的补丁清单
                Logger.Log($"Load application bundle manifest.");
                string filePath = AssetPathHelper.MakeStreamingSandboxLoadPath(URSRuntimeSetting.instance.BundleManifestFileName);
                _downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
                _appBundleManifestDownloader = new UnityWebRequester();
                _appBundleManifestDownloader.SendRequest(_downloadURL);
                _steps = ESteps.CheckAppBundleManifest;
            }
            if (_steps == ESteps.CheckAppBundleManifest)
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
                    _impl.AppBundleManifest = BundleManifest.Deserialize(jsonData);
                }
                // 解析补丁清单
                //_impl.LocalPatchManifest = _impl.AppPatchManifest;
                _appBundleManifestDownloader.Dispose();
                _steps = ESteps.InitSandbox;
            }
            if (_steps == ESteps.InitSandbox)
			{

                SandboxFileSystem.InitSandboxFileAndBundle();
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
           
        }
	}
}