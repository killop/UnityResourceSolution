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
			LoadWebFileManifest,
			CheckWebFileManifest,
            LoadWebBundleManifest,
            CheckWebBundelManifest,

            LoadLocalFilePaths,
            CheckLocalFileSystemIntegrity,

			Done,
		}

		private static int RequestCount = 0;

		private readonly HostPlayModeImpl _impl;
		private readonly int _updateResourceVersion;
		private readonly int _timeout;
		private ESteps _steps = ESteps.None;
		private UnityWebRequester _webFileManifestDownloader;
		private UnityWebRequester _webBundleManifestDownloader;
		private float _verifyTime;
        private List<(string,string)> _relativePaths = new List<(string, string)>();
        private int _currentCheckIndex = 0;
        private const int MAX_STEP = 20;

        public HostPlayModeUpdateManifestOperation(HostPlayModeImpl impl, int updateResourceVersion, int timeout)
		{
			_impl = impl;
			_updateResourceVersion = updateResourceVersion;
			_timeout = timeout;
		}
		internal override void Start()
		{
			RequestCount++;
			_steps = ESteps.LoadWebFileManifest;

			if (_impl.IgnoreResourceVersion && _updateResourceVersion > 0)
			{
				Logger.Warning($"Update resource version {_updateResourceVersion} is invalid when ignore resource version.");
			}
			else
			{
				Logger.Log($"Update patch manifest : update resource version is  {_updateResourceVersion}");
			}
		}
		internal override void Update()
		{
			if (_steps == ESteps.None || _steps == ESteps.Done)
				return;

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
                    _impl.WebFileManifest = null;
                    Error = _webFileManifestDownloader.GetError();				
					_webFileManifestDownloader.Dispose();
                    Debug.LogError(Error+" url "+ _webFileManifestDownloader.URL);
                    _steps = ESteps.Done;
					Status = EOperationStatus.Failed;
					return;
				}

				// 获取补丁清单文件的哈希值
				string webManifestHash = _webFileManifestDownloader.GetText();
                _impl.WebFileManifest = FileManifest.Deserialize(webManifestHash);
				_webFileManifestDownloader.Dispose();
                _steps = ESteps.LoadWebBundleManifest;
            }

			if (_steps == ESteps.LoadWebBundleManifest)
			{
				string webURL = GetWebRequestURL(URSRuntimeSetting.instance.BundleManifestFileName,false);
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
                    _impl.WebBundleManifest = BundleManifest.Deserialize(text);
                }
                _webFileManifestDownloader.Dispose();
                _steps = ESteps.LoadLocalFilePaths;
            }
            if (_steps == ESteps.LoadLocalFilePaths)
            {
                _relativePaths.Clear();
                string sandboxPath = SandboxFileSystem.GetSandboxDirectory();
				Directory.CreateDirectory(sandboxPath);
                var sandboxPathLength = sandboxPath.Length;
                var paths = Directory.GetFiles(sandboxPath,"*", SearchOption.AllDirectories);
                if (paths != null && paths.Length > 0)
                {
                    for (int i = 0; i < paths.Length; i++)
                    {
						var fullPath = paths[i];
                        var relativePath = paths[i].Substring(sandboxPathLength + 1);
                        var pure = relativePath.Replace('\\', '/').Replace("//", "/");
                        _relativePaths.Add((pure, fullPath));
                    }
                }
                _currentCheckIndex = 0;
                _steps = ESteps.CheckLocalFileSystemIntegrity;
            }
            if (_steps == ESteps.CheckLocalFileSystemIntegrity) {
                // 遍历所有文件然后验证并缓存合法文件
               
                for (int i = 0; i < MAX_STEP; i++)
                {
                    _currentCheckIndex++;
                    if (_currentCheckIndex < _relativePaths.Count)
                    {
                        var relativePath = _relativePaths[_currentCheckIndex].Item1;
						var fullPath= _relativePaths[_currentCheckIndex].Item2;
                        if (relativePath == URSRuntimeSetting.instance.FileManifestFileName)
                        {
                            continue;
                        }
                        var webFileManifest = _impl.WebFileManifest;
                        var fileMap = webFileManifest.GetFileMetaMap();
                        if (fileMap.ContainsKey(relativePath))
                        {
                            var fileMeta = fileMap[relativePath];
                            if (SandboxFileSystem.CheckContentIntegrity(fullPath, fileMeta.SizeBytes, fileMeta.Hash))
                            {
                                SandboxFileSystem.RegisterVerifyFile(fileMeta);
                            }
                        }
                        else
                        {
                            SandboxFileSystem.DeleteSandboxFile(relativePath);
                        }
                    }
                    else
                    {
						SandboxFileSystem.FlushSandboxFileManifestToHardisk();
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeed;
                    }
                }
            }
            
		}

		private string GetWebRequestURL(string fileName,bool freshCDN)
		{
			string url;

			url = _impl.GetRemoteVersionDownloadURL(fileName); ;

			// 注意：在URL末尾添加时间戳
			if (freshCDN)
				url = $"{url}?{System.DateTime.UtcNow.Ticks}";

			return url;
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