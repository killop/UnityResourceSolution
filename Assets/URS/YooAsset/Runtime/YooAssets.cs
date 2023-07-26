using System;
using System.Collections;
using System.Collections.Generic;
using URS;
using UnityEngine.SceneManagement;
namespace YooAsset
{
	public static class YooAssets
	{
		/// <summary>
		/// 运行模式
		/// </summary>
		public  enum EPlayMode
		{
			/// <summary>
			/// 编辑器下模拟运行模式
			/// </summary>
			EditorPlayMode,

			/// <summary>
			/// 离线模式
			/// </summary>
			OfflinePlayMode,

			/// <summary>
			/// 网络模式
			/// </summary>
			HostPlayMode,
		}

		public  class InitParameters
		{
			/// <summary>
			/// 资源定位的根路径
			/// 例如：Assets/MyResource
			/// </summary>
			//public string LocationRoot;


			/// <summary>
			/// 文件解密接口
			/// </summary>
			public IDecryptServices DecryptServices = null;

			/// <summary>
			/// 资源系统自动释放零引用资源的间隔秒数
			/// 注意：如果小于等于零代表不自动释放，可以使用YooAssets.UnloadUnusedAssets接口主动释放
			/// </summary>
			public float AutoReleaseInterval = 1;

			/// <summary>
			/// 资源加载的最大数量
			/// </summary>
			public int AssetLoadingMaxNumber =5;
		}

		

		private static bool _isInitialize = false;
		//private static string _locationRoot;
		private static EPlayMode _playMode;
		private static IFileSystemServices _bundleServices;
		private static EditorPlayModeImpl _editorPlayModeImpl;
		private static OfflinePlayModeImpl _offlinePlayModeImpl;
		private static HostPlayModeImpl _hostPlayModeImpl;

		private static float _releaseTimer;
		private static float _releaseCD = -1f;


		public static EPlayMode GetPlayMode() { 
			return _playMode; 
		}

		/// <summary>
		/// 异步初始化
		/// </summary>
		public static InitializationOperation InitializeAsync(InitParameters parameters)
		{

            _playMode = URSRuntimeSetting.instance.PlayMode;

#if !UNITY_EDITOR
            if (_playMode == EPlayMode.EditorPlayMode)
            {
				if (UnityMacro.Instance.ENABLE_HOTUPDATE)
					_playMode = EPlayMode.HostPlayMode; // 防止犯错
				else
					_playMode = EPlayMode.OfflinePlayMode;
            }
#endif
            if (parameters == null)
				throw new Exception($"YooAsset create parameters is invalid.");



			// 创建驱动器
			if (_isInitialize == false)
			{
				_isInitialize = true;
				UnityEngine.GameObject driverGo = new UnityEngine.GameObject("[YooAsset]");
				driverGo.AddComponent<YooAssetDriver>();
				UnityEngine.Object.DontDestroyOnLoad(driverGo);
			}
			else
			{
				throw new Exception("YooAsset is initialized yet.");
			}

			// 检测创建参数
			if (parameters.AssetLoadingMaxNumber < 3)
			{
				parameters.AssetLoadingMaxNumber = 3;
				Logger.Warning($"{nameof(parameters.AssetLoadingMaxNumber)} minimum is 3");
			}

			// 创建间隔计时器
			if (parameters.AutoReleaseInterval > 0)
			{
				_releaseCD = parameters.AutoReleaseInterval;
			}

			URSFileSystem.Init();
			// 初始化
			if (_playMode == EPlayMode.EditorPlayMode)
			{
				_editorPlayModeImpl = new EditorPlayModeImpl();
				_bundleServices = _editorPlayModeImpl;
				AssetSystem.Initialize(true, parameters.AssetLoadingMaxNumber, parameters.DecryptServices, _bundleServices);
				return _editorPlayModeImpl.InitializeAsync();
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				_offlinePlayModeImpl = new OfflinePlayModeImpl();
				_bundleServices = _offlinePlayModeImpl;
				AssetSystem.Initialize(false, parameters.AssetLoadingMaxNumber, parameters.DecryptServices, _bundleServices);
				return _offlinePlayModeImpl.InitializeAsync();
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				_hostPlayModeImpl = new HostPlayModeImpl();
				_bundleServices = _hostPlayModeImpl;
				AssetSystem.Initialize(false, parameters.AssetLoadingMaxNumber, parameters.DecryptServices, _bundleServices);
				return _hostPlayModeImpl.InitializeAsync();
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 向网络端请求并更新补丁清单
		/// </summary>
		/// <param name="updateResourceVersion">更新的资源版本号</param>
		/// <param name="timeout">超时时间（默认值：60秒）</param>
		public static UpdateManifestOperation UpdateManifestAsync(int timeout = 60)
		{
			if (_playMode == EPlayMode.EditorPlayMode)
			{
				var operation = new EditorModeUpdateManifestOperation();
				OperationSystem.ProcessOperaiton(operation);
				return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				var operation = new OfflinePlayModeUpdateManifestOperation();
				OperationSystem.ProcessOperaiton(operation);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				if (_hostPlayModeImpl == null)
					throw new Exception("YooAsset is not initialized.");
				return _hostPlayModeImpl.UpdatePatchManifestAsync(timeout);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

        

		/// <summary>
		/// 资源回收（卸载引用计数为零的资源）
		/// </summary>
		public static void UnloadUnusedAssets()
		{
			AssetSystem.Update();
			AssetSystem.UnloadUnusedAssets();
		}

		/// <summary>
		/// 强制回收所有资源
		/// </summary>
		public static void ForceUnloadAllAssets()
		{
			AssetSystem.ForceUnloadAllAssets();
		}

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public static void GetDebugReport(DebugReport report)
        {
            if (report == null)
                Logger.Error($"{nameof(DebugReport)} is null");

            AssetSystem.GetDebugReport(report);
        }

        public static HardiskFileSearchResult SearchFile(string fileRelativePath)
        {
            return AssetSystem.SearchFile(fileRelativePath);
        }
        #region 场景加载接口
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="location">场景对象相对路径</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="activateOnLoad">加载完毕时是否主动激活</param>
        /// <param name="priority">优先级</param>
        public static SceneOperationHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
		{
			var handle = AssetSystem.LoadSceneAsync(location, sceneMode, activateOnLoad, priority);
			return handle;
		}
		#endregion

		#region 资源加载接口
		/// <summary>
		/// 同步加载资源对象
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		public static AssetOperationHandle LoadAssetSync<TObject>(string location) where TObject : class
		{
			return LoadAssetInternal(location, typeof(TObject), true);
		}

		/// <summary>
		/// 同步加载资源对象
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		/// <param name="type">资源类型</param>
		public static AssetOperationHandle LoadAssetSync(string location, System.Type type)
		{
			return LoadAssetInternal(location, type, true);
		}

		/// <summary>
		/// 同步加载子资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源对象相对路径</param>
		public static SubAssetsOperationHandle LoadSubAssetsSync<TObject>(string location)
		{
			return LoadSubAssetsInternal(location, typeof(TObject), true);
		}

		/// <summary>
		/// 同步加载子资源对象
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		/// <param name="type">子对象类型</param>
		public static SubAssetsOperationHandle LoadSubAssetsSync(string location, System.Type type)
		{
			return LoadSubAssetsInternal(location, type, true);
		}

		/// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源对象相对路径</param>
		public static AssetOperationHandle LoadAssetAsync<TObject>(string location)
		{
			return LoadAssetInternal(location, typeof(TObject), false);
		}

		/// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		/// <param name="type">资源类型</param>
		public static AssetOperationHandle LoadAssetAsync(string location, System.Type type)
		{
			return LoadAssetInternal(location, type, false);
		}

		/// <summary>
		/// 异步加载子资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源对象相对路径</param>
		public static SubAssetsOperationHandle LoadSubAssetsAsync<TObject>(string location)
		{
			return LoadSubAssetsInternal(location, typeof(TObject), false);
		}

		/// <summary>
		/// 异步加载子资源对象
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		/// <param name="type">子对象类型</param>
		public static SubAssetsOperationHandle LoadSubAssetsAsync(string location, System.Type type)
		{
			return LoadSubAssetsInternal(location, type, false);
		}
		

		private static AssetOperationHandle LoadAssetInternal(string location, System.Type assetType, bool waitForAsyncComplete)
		{
			var handle = AssetSystem.LoadAssetAsync(location, assetType);
			if (waitForAsyncComplete)
				handle.WaitForAsyncComplete();
			return handle;
		}
		private static SubAssetsOperationHandle LoadSubAssetsInternal(string location, System.Type assetType, bool waitForAsyncComplete)
		{
			string assetPath = ConvertLocationToAssetPath(location);
			var handle = AssetSystem.LoadSubAssetsAsync(assetPath, assetType);
			if (waitForAsyncComplete)
				handle.WaitForAsyncComplete();
			return handle;
		}
		#endregion

		#region 资源下载接口
		/// <summary>
		/// 创建补丁下载器
		/// </summary>
		/// <param name="tag">资源标签</param>
		/// <param name="downloadingMaxNumber">同时下载的最大文件数</param>
		/// <param name="failedTryAgain">下载失败的重试次数</param>
		public static RemoteUpdateOperation CreatePatchDownloader(string tag, int downloadingMaxNumber, int failedTryAgain)
		{
			return CreatePatchDownloader(new string[] { tag }, downloadingMaxNumber, failedTryAgain);
		}

		/// <summary>
		/// 创建补丁下载器
		/// </summary>
		/// <param name="tags">资源标签列表</param>
		/// <param name="downloadingMaxNumber">同时下载的最大文件数</param>
		/// <param name="failedTryAgain">下载失败的重试次数</param>
		public static RemoteUpdateOperation CreatePatchDownloader(string[] tags, int downloadingMaxNumber, int failedTryAgain)
		{
			if (_playMode == EPlayMode.EditorPlayMode)
			{
				List<UpdateEntry> downloadList = new List<UpdateEntry>();
				var operation = new RemoteUpdateOperation(downloadList, downloadingMaxNumber, failedTryAgain);
                OperationSystem.ProcessOperaiton(operation);
                return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				List<UpdateEntry> downloadList = new List<UpdateEntry>();
				var operation = new RemoteUpdateOperation(downloadList, downloadingMaxNumber, failedTryAgain);
                OperationSystem.ProcessOperaiton(operation);
                return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				if (_hostPlayModeImpl == null)
					throw new Exception("YooAsset is not initialized.");
				var operation= _hostPlayModeImpl.CreateDownloaderByTags(tags, downloadingMaxNumber, failedTryAgain);
                OperationSystem.ProcessOperaiton(operation);
                return operation;
            }
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 创建资源包下载器
		/// </summary>
		/// <param name="locations">资源列表</param>
		/// <param name="downloadingMaxNumber">同时下载的最大文件数</param>
		/// <param name="failedTryAgain">下载失败的重试次数</param>
		public static RemoteUpdateOperation CreateBundleDownloader(string[] locations, int downloadingMaxNumber, int failedTryAgain)
		{
			if (_playMode == EPlayMode.EditorPlayMode)
			{
				List<UpdateEntry> downloadList = new List<UpdateEntry>();
				var operation = new RemoteUpdateOperation(downloadList, downloadingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				List<UpdateEntry> downloadList = new List<UpdateEntry>();
				var operation = new RemoteUpdateOperation(downloadList, downloadingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				if (_hostPlayModeImpl == null)
					throw new Exception("YooAsset is not initialized.");
				return _hostPlayModeImpl.CreateDownloaderByPaths(new List<string>(locations), downloadingMaxNumber, failedTryAgain);
			}
			else
			{
				throw new NotImplementedException();
			}
		}
		#endregion

		#region 资源解压接口
		/// <summary>
		/// 创建补丁解压器
		/// </summary>
		/// <param name="tag">资源标签</param>
		/// <param name="unpackingMaxNumber">同时解压的最大文件数</param>
		/// <param name="failedTryAgain">解压失败的重试次数</param>
		public static UnzipOperation CreatePatchUnpacker(string tag, int unpackingMaxNumber, int failedTryAgain)
		{
			return CreatePatchUnpacker(new string[] { tag }, unpackingMaxNumber, failedTryAgain);
		}
		
		/// <summary>
		/// 创建补丁解压器
		/// </summary>
		/// <param name="tags">资源标签列表</param>
		/// <param name="unpackingMaxNumber">同时解压的最大文件数</param>
		/// <param name="failedTryAgain">解压失败的重试次数</param>
		public static UnzipOperation CreatePatchUnpacker(string[] tags, int unpackingMaxNumber, int failedTryAgain)
		{
			if (_playMode == EPlayMode.EditorPlayMode)
			{
				List<UnzipEntry> downloadList = new List<UnzipEntry>();
				var operation = new UnzipOperation(downloadList, unpackingMaxNumber, failedTryAgain);
                OperationSystem.ProcessOperaiton(operation);
                return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
                if (_offlinePlayModeImpl == null)
                    throw new Exception("YooAsset is not initialized.");
                var op=  _offlinePlayModeImpl.CreateUnpackerByTags(tags, unpackingMaxNumber, failedTryAgain);
                OperationSystem.ProcessOperaiton(op);
                return op;
            }
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				if (_hostPlayModeImpl == null)
					throw new Exception("YooAsset is not initialized.");
                var op = _hostPlayModeImpl.CreateUnpackerByTags(tags, unpackingMaxNumber, failedTryAgain);
                OperationSystem.ProcessOperaiton(op);
                return op;
            }
			else
			{
				throw new NotImplementedException();
			}
		}
		#endregion

		#region 沙盒相关
		/// <summary>
		/// 清空沙盒目录
		/// 注意：可以使用该方法修复我们本地的客户端
		/// </summary>
		public static void ClearSandbox()
		{
			Logger.Warning("Clear sandbox.");
            URSFileSystem.DeletePersistentRootFolder();
        }

		
		#endregion

		#region 内部方法
		/// <summary>
		/// 更新资源系统
		/// </summary>
		internal static void InternalUpdate()
		{
            //UnityEngine.Profiling.Profiler.BeginSample("OperationSystem.Update()");
            // 更新异步请求操作
            OperationSystem.Update();

            //UnityEngine.Profiling.Profiler.EndSample();

            //UnityEngine.Profiling.Profiler.BeginSample(" UnzipSystem.Update()");
            // 解压管理系统
            UnzipSystem.Update();

           // UnityEngine.Profiling.Profiler.EndSample();

           // UnityEngine.Profiling.Profiler.BeginSample(" RemoteDownloadSystem.Update()");
            // 下载模块
            RemoteDownloadSystem.Update();

           // UnityEngine.Profiling.Profiler.EndSample();

            //UnityEngine.Profiling.Profiler.BeginSample("AssetSystem.Update");
            // 轮询更新资源系统
            AssetSystem.Update();
            //UnityEngine.Profiling.Profiler.EndSample();
            // 自动释放零引用资源
            if (_releaseCD > 0)
			{
				_releaseTimer += UnityEngine.Time.unscaledDeltaTime;
				if (_releaseTimer >= _releaseCD)
				{
					_releaseTimer = 0f;
				//	UnityEngine.Profiling.Profiler.BeginSample("UnloadUnusedAssets");
					AssetSystem.UnloadUnusedAssets();
                  //  UnityEngine.Profiling.Profiler.EndSample();
                }
			}
		}

		/// <summary>
		/// 定位地址转换为资源路径
		/// </summary>
		private static string ConvertLocationToAssetPath(string relativePath)
		{

            return relativePath;
            /*
            if (_playMode == EPlayMode.EditorPlayMode)
			{
				string filePath = AssetPathHelper.CombineAssetPath(_locationRoot, location);
				return AssetPathHelper.FindDatabaseAssetPath(filePath);
			}
			else
			{
				return AssetPathHelper.CombineAssetPath(_locationRoot, location);
			}
            */
		}
		#endregion
	}
}