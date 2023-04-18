using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using URS;
namespace YooAsset
{
	internal static class AssetSystem
	{
		private static readonly List<AssetBundleLoader> _loaders = new List<AssetBundleLoader>(1000);
		private static readonly List<ProviderBase> _providers = new List<ProviderBase>(1000);

		/// <summary>
		/// 在编辑器下模拟运行
		/// </summary>
		public static bool SimulationOnEditor { private set; get; }

		/// <summary>
		/// 运行时的最大加载个数
		/// </summary>
		public static int AssetLoadingMaxNumber { private set; get; }

		public static IDecryptServices DecryptionServices { private set; get; }
		public static IFileSystemServices BundleServices { private set; get; }


		/// <summary>
		/// 初始化资源系统
		/// 注意：在使用AssetSystem之前需要初始化
		/// </summary>
		public static void Initialize(bool simulationOnEditor, int assetLoadingMaxNumber, IDecryptServices decryptServices, IFileSystemServices bundleServices)
		{
			SimulationOnEditor = simulationOnEditor;
			AssetLoadingMaxNumber = assetLoadingMaxNumber;
            DecryptionServices = decryptServices;
			BundleServices = bundleServices;
		}

		/// <summary>
		/// 轮询更新
		/// </summary>
		public static void Update()
		{
			// 更新加载器	
			foreach (var loader in _loaders)
			{
				loader.Update();
				
				if (FpsHelper.instance.needWait)
					break;
			}

			// 更新资源提供者
			// 注意：循环更新的时候，可能会扩展列表
			// 注意：不能限制场景对象的加载
			int loadingCount = 0;
			for (int i = 0; i < _providers.Count; i++)
			{
				var provider = _providers[i];
				if (provider.IsSceneProvider())
				{
                    UnityEngine.Profiling.Profiler.BeginSample("IsSceneProvider.Update");
					provider.Update();
                    UnityEngine.Profiling.Profiler.EndSample();
                }
				else if (!FpsHelper.instance.needWait)
				{
					if (loadingCount < AssetLoadingMaxNumber)
						provider.Update();

					if (provider.IsDone == false)
						loadingCount++;
				}
			}
		}

		/// <summary>
		/// 资源回收（卸载引用计数为零的资源）
		/// </summary>
		public static void UnloadUnusedAssets()
		{
			if (SimulationOnEditor)
			{
				for (int i = _providers.Count - 1; i >= 0; i--)
				{
					if (_providers[i].CanDestroy())
					{
						_providers[i].Destory();
						_providers.RemoveAt(i);
					}
				}
			}
			else
			{
				for (int i = _loaders.Count - 1; i >= 0; i--)
				{
					AssetBundleLoader loader = _loaders[i];
					loader.TryDestroyAllProviders();
				}
				for (int i = _loaders.Count - 1; i >= 0; i--)
				{
					AssetBundleLoader loader = _loaders[i];
					if (loader.CanDestroy())
					{
						loader.Destroy(false);
						_loaders.RemoveAt(i);
					}
				}
			}
		}

		/// <summary>
		/// 强制回收所有资源
		/// </summary>
		public static void ForceUnloadAllAssets()
		{
			foreach (var provider in _providers)
			{
				provider.Destory();
			}
			_providers.Clear();

			foreach (var loader in _loaders)
			{
				loader.Destroy(true);
			}
			_loaders.Clear();

			// 注意：调用底层接口释放所有资源
			Resources.UnloadUnusedAssets();
		}

		/// <summary>
		/// 异步加载场景
		/// </summary>
		public static SceneOperationHandle LoadSceneAsync(string scenePath, LoadSceneMode sceneMode, bool activateOnLoad, int priority)
		{
			ProviderBase provider = TryGetProvider(scenePath);
			if (provider == null)
			{
				if (SimulationOnEditor)
					provider = new DatabaseSceneProvider(scenePath, sceneMode, activateOnLoad, priority);
				else
					provider = new BundledSceneProvider(scenePath, sceneMode, activateOnLoad, priority);
                provider.InitSpawnDebugInfo();
                _providers.Add(provider);
			}
			return provider.CreateHandle() as SceneOperationHandle;
		}

		/// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <param name="assetPath">资源路径</param>
		/// <param name="assetType">资源类型</param>
		public static AssetOperationHandle LoadAssetAsync(string assetPath, System.Type assetType,bool requireLocalSecurity=false, bool skipDownloadFolder = true)
		{
			ProviderBase provider = TryGetProvider(assetPath);
			if (provider == null)
			{
				if (SimulationOnEditor)
					provider = new DatabaseAssetProvider(assetPath, assetType);
				else
					provider = new BundledAssetProvider(assetPath, assetType, requireLocalSecurity, skipDownloadFolder);
                provider.InitSpawnDebugInfo();
                _providers.Add(provider);
			}
			return provider.CreateHandle() as AssetOperationHandle;
		}

		/// <summary>
		/// 异步加载所有子资源对象
		/// </summary>
		/// <param name="assetPath">资源路径</param>
		/// <param name="assetType">资源类型</param>、
		public static SubAssetsOperationHandle LoadSubAssetsAsync(string assetPath, System.Type assetType)
		{
			ProviderBase provider = TryGetProvider(assetPath);
			if (provider == null)
			{
				if (SimulationOnEditor)
					provider = new DatabaseSubAssetsProvider(assetPath, assetType);
				else
					provider = new BundledSubAssetsProvider(assetPath, assetType);
                provider.InitSpawnDebugInfo();
                _providers.Add(provider);
			}
			return provider.CreateHandle() as SubAssetsOperationHandle;
		}


		internal static AssetBundleLoader CreateOwnerAssetBundleLoader(string assetPath)
		{
			var fileMeta = BundleServices.GetBundleRelativePath(assetPath);
			var searchResult = BundleServices.SearchHardiskFile(fileMeta);
			return CreateAssetBundleLoaderInternal(searchResult);
		}
		internal static List<AssetBundleLoader> CreateDependAssetBundleLoaders(string assetPath)
		{
			List<AssetBundleLoader> result = new List<AssetBundleLoader>();
			var depends = BundleServices.GetAllDependencieBundleRelativePaths(assetPath);
			if (depends != null)
			{
				foreach (var dp in depends)
				{
					var reslt = BundleServices.SearchHardiskFile(dp);
					AssetBundleLoader dependLoader = CreateAssetBundleLoaderInternal(reslt);
					result.Add(dependLoader);
				}
			}
			return result;
		}
		internal static void CreateLocalSecurityAssetBundleLoader(
			string assetPath,
			out AssetBundleLoader mainloader,
			out List<AssetBundleLoader> dependencyLoaders,
			bool skipDownloadFolder= true)
        {
			BundleServices.SearchLocalSecurityBundleHardDiskFileByPath(assetPath, out var mainHardiskFileSearchResult,out var dependencyHardiskFileSearchResults, skipDownloadFolder);
            mainloader = CreateAssetBundleLoaderInternal(mainHardiskFileSearchResult);
            dependencyLoaders = new List<AssetBundleLoader>();
            if (dependencyHardiskFileSearchResults != null)
            {
                foreach (var dp in dependencyHardiskFileSearchResults)
                {
                    AssetBundleLoader dependLoader = CreateAssetBundleLoaderInternal(dp);
                    dependencyLoaders.Add(dependLoader);
                }
            }
        }
        public static HardiskFileSearchResult SearchFile(string fileRelativePath) {

            var searchResult = BundleServices.SearchHardiskFileByPath(fileRelativePath);
            return searchResult;
        }
		internal static void RemoveBundleProviders(List<ProviderBase> providers)
		{
			foreach (var provider in providers)
			{
				_providers.Remove(provider);
			}
		}

		private static AssetBundleLoader CreateAssetBundleLoaderInternal(HardiskFileSearchResult localFileSearchResult)
		{
			// 如果加载器已经存在
			AssetBundleLoader loader = TryGetAssetBundleLoader(localFileSearchResult.OrignRelativePath);
			if (loader != null)
				return loader;
			// 新增下载需求
			loader = new AssetBundleLoader(localFileSearchResult);
			_loaders.Add(loader);
			return loader;
		}
		private static AssetBundleLoader TryGetAssetBundleLoader(string relativePath)
		{
			AssetBundleLoader loader = null;
			for (int i = 0; i < _loaders.Count; i++)
			{
				AssetBundleLoader temp = _loaders[i];
				if (temp.HardiskFileSearchResult.OrignRelativePath.Equals(relativePath))
				{
					loader = temp;
					break;
				}
			}
			return loader;
		}
		private static ProviderBase TryGetProvider(string assetPath)
		{
			ProviderBase provider = null;
			for (int i = 0; i < _providers.Count; i++)
			{
				ProviderBase temp = _providers[i];
				if (temp.AssetPath.Equals(assetPath))
				{
					provider = temp;
					break;
				}
			}
			return provider;
		}

        #region 调试专属方法
        internal static void GetDebugReport(DebugReport report)
        {
            report.ClearAll();
            report.BundleCount = _loaders.Count;
            report.AssetCount = _providers.Count;

            foreach (var provider in _providers)
            {
                DebugProviderInfo providerInfo = new DebugProviderInfo();
                providerInfo.AssetPath = provider.AssetPath;
                providerInfo.SpawnScene = provider.SpawnScene;
                providerInfo.SpawnTime = provider.SpawnTime;
                providerInfo.RefCount = provider.RefCount;
                providerInfo.Status = provider.Status;
                providerInfo.BundleInfos.Clear();
                report.ProviderInfos.Add(providerInfo);

                if (provider is BundledProvider)
                {
                    BundledProvider temp = provider as BundledProvider;
                    temp.GetBundleDebugInfos(providerInfo.BundleInfos);
                }
            }

            // 重新排序
            report.ProviderInfos.Sort();
        }
        #endregion
    }
}