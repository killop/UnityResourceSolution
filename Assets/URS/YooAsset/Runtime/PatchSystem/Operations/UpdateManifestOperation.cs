using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using URS;

namespace YooAsset
{
	/// <summary>
	/// 更新清单操作
	/// </summary>
	public abstract class UpdateManifestOperation : AsyncOperationBase
	{
	}

	/// <summary>
	/// 编辑器下模拟运行的更新清单操作
	/// </summary>
	internal class EditorModeUpdateManifestOperation : UpdateManifestOperation
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
	/// 离线模式的更新清单操作
	/// </summary>
	internal class OfflinePlayModeUpdateManifestOperation : UpdateManifestOperation
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
	/// 网络模式的更新清单操作
	/// </summary>
	internal class HostPlayModeUpdateManifestOperation : UpdateManifestOperation
	{
		private enum ESteps
		{
			None,

            LoadAppChannel,
            CheckAppChannel,

            LoadChannelRouter,
            CheckChannelRouter,

            LoadFilesVersionIndex,
            CheckFilesVersionIndex,

            LoadWebFileManifest,
			CheckWebFileManifest,

            LoadWebBundleManifest,
            CheckWebBundelManifest,

            BeginCheckDownloadFolder,
            CheckDownloadFolder,

            BeginCheckReadOnlyFolder,
            CheckReadOnlyFolder,

            Done,
		}

		private static int RequestCount = 0;

		private readonly HostPlayModeImpl _impl;
		private readonly int _timeout;
		private ESteps _steps = ESteps.None;

        private UnityWebRequester _appChannelDownloader;
        private UnityWebRequester _remoteAppToChannelRouterFileDownloader;
        private UnityWebRequester _remoteFilesVersionIndexDownloader;
        private UnityWebRequester _webFileManifestDownloader;
		private UnityWebRequester _webBundleManifestDownloader;

        private List<(string,string)> _relativePaths = new List<(string, string)>();
        private int _currentCheckIndex = 0;
        private const int MAX_STEP = 20;

        public HostPlayModeUpdateManifestOperation(HostPlayModeImpl impl , int timeout)
		{
			_impl = impl;
			_timeout = timeout;
           
		}
		internal override void Start()
		{
			RequestCount++;
			_steps = ESteps.LoadAppChannel;
		}
		internal override void Update()
		{
			if (_steps == ESteps.None || _steps == ESteps.Done)
				return;
            if (_steps == ESteps.LoadAppChannel)
            {

                _impl.channel = null;
				string filePath = AssetPathHelper.GetBuildInChannelFilePath();
                // 加载APP内的补丁清单
                Logger.Log($"Load application file manifest.{filePath}");
                var downloadURL = AssetPathHelper.ConvertToWWWPath(filePath);
                _appChannelDownloader = new UnityWebRequester();
                _appChannelDownloader.SendRequest(downloadURL);
                _steps = ESteps.CheckAppChannel;
            }
            if (_steps == ESteps.CheckAppChannel)
            {

                if (_appChannelDownloader.IsDone() == false)
                    return;

                if (_appChannelDownloader.HasError())
                {
                    Error = _appChannelDownloader.GetError();
                    Logger.Warning($"can not Load _appId.error {Error}");
                }
                else
                {
                    // 解析补丁清单
                    string channel = _appChannelDownloader.GetText();
                    _impl.InitChannel(channel);
                }
                //_impl.LocalPatchManifest = _impl.AppPatchManifest;
                _appChannelDownloader.Dispose();
                _steps = ESteps.LoadChannelRouter;
            }
            if (_steps == ESteps.LoadChannelRouter)
            {
                // 加载APP内的补丁清单
                var downloadURL = AppendSeedToFreshCDN(_impl.RemoteAppToChannelRouterFileUrl);
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
                    var router = AppVersionRouter.Deserialize(jsonText);
                    var targetVersion = router.GetChannel(Application.version);
                    _impl.InitVersion(targetVersion);
                }
                //_impl.LocalPatchManifest = _impl.AppPatchManifest;
                _remoteAppToChannelRouterFileDownloader.Dispose();
                _steps = ESteps.LoadFilesVersionIndex;
            }
            if (_steps == ESteps.LoadFilesVersionIndex)
            {
                // 加载APP内的补丁清单
                string downloadURL = AppendSeedToFreshCDN(_impl.GetRemoteFilesVersionIndexUrl());
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
                _steps = ESteps.LoadWebFileManifest;
            }

            if (_steps == ESteps.LoadWebFileManifest)
			{
				string webURL = GetWebRequestURL(URSRuntimeSetting.instance.FileManifestFileName, false);
				_webFileManifestDownloader = new UnityWebRequester();
				_webFileManifestDownloader.SendRequest(webURL, _timeout);
				_steps = ESteps.CheckWebFileManifest;
			}

			if (_steps == ESteps.CheckWebFileManifest)
			{
				if (_webFileManifestDownloader.IsDone() == false)
					return;

				// Check fatal
				if (_webFileManifestDownloader.HasError())
				{
                    URSFileSystem.RemoteFolder.FileManifest = null; 
                    Error = _webFileManifestDownloader.GetError();				
					_webFileManifestDownloader.Dispose();
                    _steps = ESteps.Done;
					Status = EOperationStatus.Failed;
					return;
				}

				// 获取补丁清单文件的哈希值
				string webManifestHash = _webFileManifestDownloader.GetText();
                URSFileSystem.RemoteFolder.FileManifest = FileManifest.Deserialize(webManifestHash);
				_webFileManifestDownloader.Dispose();
                _steps = ESteps.LoadWebBundleManifest;
            }

			if (_steps == ESteps.LoadWebBundleManifest)
			{
				string webURL = GetWebRequestURL(URSRuntimeSetting.instance.BundleManifestFileRelativePath,false);
				Logger.Log($"Beginning to request  web bundle manifest : {webURL}");
				_webBundleManifestDownloader = new UnityWebRequester();
                _webBundleManifestDownloader.SendRequest(webURL, _timeout);
				_steps = ESteps.CheckWebBundelManifest;
			}

			if (_steps == ESteps.CheckWebBundelManifest)
			{
				if (_webBundleManifestDownloader.IsDone() == false)
					return;

                // Check fatal
                if (_webBundleManifestDownloader.HasError())
                {
                    Error = _webBundleManifestDownloader.GetError();
                    Debug.LogError(Error);
                    _webBundleManifestDownloader.Dispose();
                    return;
                }
                else
                {
                    string text = _webBundleManifestDownloader.GetText();
                    URSFileSystem.RemoteFolder.BundleManifest = BundleManifest.Deserialize(text);
                }
                _webFileManifestDownloader.Dispose();
                _steps = ESteps.BeginCheckDownloadFolder;
            }
            if (_steps == ESteps.BeginCheckDownloadFolder)
            {
                URSFileSystem.PersistentDownloadFolder.BeginCheckLocalFile();
                _steps = ESteps.CheckDownloadFolder;
            }
            if (_steps == ESteps.CheckDownloadFolder) {
                // 遍历所有文件然后验证并缓存合法文件
				bool isEnd= URSFileSystem.PersistentDownloadFolder.CheckLocalFile(URSFileSystem.RemoteFolder.FileManifest);
				if (isEnd) {
					URSFileSystem.PersistentDownloadFolder.EndCheck();
                    _steps = ESteps.BeginCheckReadOnlyFolder;
                }
              
            }
            if (_steps == ESteps.BeginCheckReadOnlyFolder)
            {
                URSFileSystem.PersistentReadOnlyFolder.BeginCheckLocalFile();
                _steps = ESteps.CheckReadOnlyFolder;
            }
            if (_steps == ESteps.CheckReadOnlyFolder)
            {
                // 遍历所有文件然后验证并缓存合法文件
                bool isEnd = URSFileSystem.PersistentReadOnlyFolder.CheckLocalFile(URSFileSystem.RemoteFolder.FileManifest);
                if (isEnd)
                {
                    URSFileSystem.PersistentReadOnlyFolder.EndCheck();
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
            }
        }

		private string GetWebRequestURL(string fileName,bool freshCDN)
		{
			string url;

			url = _impl.GetRemoteVersionDownloadURL(fileName); ;

			// 注意：在URL末尾添加时间戳
			if (freshCDN)
				url = AppendSeedToFreshCDN(url);

			return url;
		}

		public string AppendSeedToFreshCDN(string url)
		{
			return $"{url}?{System.DateTime.UtcNow.Ticks}";
        }
        /*
		private void ParseAndSaveRemotePatchManifest(string content)
		{
			_impl.LocalPatchManifest = PatchManifest.Deserialize(content);

			// 注意：这里会覆盖掉沙盒内的补丁清单文件
			Logger.Log("Save remote patch manifest file.");
			string savePath = AssetPathHelper.MakePersistentLoadPath(ResourceSettingData.Setting.FileManifestFileName);
			PatchManifest.Serialize(savePath, _impl.LocalPatchManifest);
		}
     

        private void CheckLoalFileSystemIntegrity()
        {
            
            // 遍历所有文件然后验证并缓存合法文件
            foreach (var fileMeta in _impl.WebFileManifest.FileMetas)
            {
                // 忽略缓存文件
                if (SandboxFileSystem.ContainsVerifyFile(fileMeta.RelativePath))
                    continue;

                // 忽略APP资源
                // 注意：如果是APP资源并且哈希值相同，则不需要下载
                if (_impl.AppPatchManifest.Bundles.TryGetValue(patchBundle.BundleName, out PatchBundle appPatchBundle))
                {
                    if (appPatchBundle.IsBuildin && appPatchBundle.Hash == patchBundle.Hash)
                        continue;
                }

                // 查看文件是否存在
                string filePath = SandboxFileSystem.MakeSandboxCacheFilePath(patchBundle.Hash);
                if (File.Exists(filePath) == false)
                    continue;

                _cacheList.Add(patchBundle);
            }
        }
		#region 多线程相关
		private class ThreadInfo
		{
			public bool Result = false;
			public string FilePath { private set; get; }
			public FileMeta FileMeta { private set; get; }
			public ThreadInfo(string filePath, FileMeta fileMeta)
			{
				FilePath = filePath;
                FileMeta = fileMeta;
			}
		}

		private readonly List<PatchBundle> _cacheList = new List<PatchBundle>(1000);
		private readonly List<PatchBundle> _verifyList = new List<PatchBundle>(100);
		private readonly ThreadSyncContext _syncContext = new ThreadSyncContext();
		private const int VerifyMaxCount = 32;

		private void InitPrepareCache()
		{
			// 遍历所有文件然后验证并缓存合法文件
			foreach (var fileMeta in _impl.WebFileManifest.FileMetas)
			{
				// 忽略缓存文件
				if (SandboxFileSystem.ContainsVerifyFile(fileMeta.RelativePath))
					continue;

				// 忽略APP资源
				// 注意：如果是APP资源并且哈希值相同，则不需要下载
				if (_impl.AppPatchManifest.Bundles.TryGetValue(patchBundle.BundleName, out PatchBundle appPatchBundle))
				{
					if (appPatchBundle.IsBuildin && appPatchBundle.Hash == patchBundle.Hash)
						continue;
				}

				// 查看文件是否存在
				string filePath = SandboxFileSystem.MakeSandboxCacheFilePath(patchBundle.Hash);
				if (File.Exists(filePath) == false)
					continue;

				_cacheList.Add(patchBundle);
			}
		}
		private bool UpdatePrepareCache()
		{
			_syncContext.Update();

			if (_cacheList.Count == 0 && _verifyList.Count == 0)
				return true;

			if (_verifyList.Count >= VerifyMaxCount)
				return false;

			for (int i = _cacheList.Count - 1; i >= 0; i--)
			{
				if (_verifyList.Count >= VerifyMaxCount)
					break;

				var patchBundle = _cacheList[i];
				if (RunThread(patchBundle))
				{
					_cacheList.RemoveAt(i);
					_verifyList.Add(patchBundle);
				}
				else
				{
					Logger.Warning("Failed to run verify thread.");
					break;
				}
			}

			return false;
		}
		private bool RunThread(PatchBundle patchBundle)
		{
			string filePath = SandboxFileSystem.MakeSandboxCacheFilePath(patchBundle.Hash);
			ThreadInfo info = new ThreadInfo(filePath, patchBundle);
			return ThreadPool.QueueUserWorkItem(new WaitCallback(VerifyFile), info);
		}
		private void VerifyFile(object infoObj)
		{
			// 验证沙盒内的文件
			ThreadInfo info = (ThreadInfo)infoObj;
			info.Result = SandboxFileSystem.CheckContentIntegrity(info.FilePath, info.Bundle.SizeBytes, info.Bundle.CRC);
			_syncContext.Post(VerifyCallback, info);
		}
		private void VerifyCallback(object obj)
		{
			ThreadInfo info = (ThreadInfo)obj;
			if (info.Result)
                SandboxFileSystem.CacheVerifyFile(info.Bundle.Hash, info.Bundle.BundleName);
			_verifyList.Remove(info.Bundle);
		}
		#endregion
        */
	}
}