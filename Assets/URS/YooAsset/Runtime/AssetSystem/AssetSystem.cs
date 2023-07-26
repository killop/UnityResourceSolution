using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NinjaBeats;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using URS;

namespace YooAsset
{
	internal static class AssetSystem
	{
		private static readonly Dictionary<int, AssetBundleLoader> _loaders = new (1000);
		private static readonly Dictionary<int, ProviderBase> _providers = new (1000);

		internal static readonly Updater updater = new Updater();
		internal static readonly Unloader unloader = new Unloader();

		internal class Updater
		{
			public Dictionary<int, AssetBundleLoader> loaders = new(100);
			public Dictionary<int, ProviderBase> providers = new(100);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]			
			public void Clear()
			{
				loaders.Clear();
				providers.Clear();
			}

			public void Update()
			{
				// 更新加载器	
				if (loaders.Count > 0)
				{
					using (ListPool<AssetBundleLoader>.Get(out var tempList))
					{
						tempList.AddValues(loaders);
						foreach (var loader in tempList)
						{
							if (loader.IsDone)
								continue;

							loader.Update();

							if (FpsHelper.instance.needWait)
								break;
						}
					}
				}

				// 更新资源提供者
				// 注意：循环更新的时候，可能会扩展列表
				// 注意：不能限制场景对象的加载
				if (providers.Count > 0)
				{
					using (ListPool<ProviderBase>.Get(out var tempList))
					{
						tempList.AddValues(providers);
						foreach (var provider in tempList)
						{
							if (provider.IsDone)
								continue;

							provider.Update();
							
							if (FpsHelper.instance.needWait)
								break;
						}
					}
				}
			}
		}

		internal class Unloader
		{
			public Dictionary<int, AssetBundleLoader> destroyProviderLoaders = new(100);
			public Dictionary<int, AssetBundleLoader> destroyLoaders = new(100);
			public Dictionary<int, ProviderBase> destroyProviders = new(100);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]			
			public void Clear()
			{
				destroyProviderLoaders.Clear();
				destroyLoaders.Clear();
				destroyProviders.Clear();
			}
			
			public void UnloadUnusedAssets()
			{
				if (SimulationOnEditor)
				{
					UnityEngine.Profiling.Profiler.BeginSample("AssetSystem.UnloadUnusedAssets TryDestroyAllProviders");
					if (destroyProviders.Count > 0)
					{
						using (ListPool<ProviderBase>.Get(out var tempList))
						{
							tempList.AddValues(destroyProviders);
							foreach (var provider in tempList)
							{
								if (!provider.CanDestroy)
									continue;

								provider.Destroy();
								updater.providers.Remove(provider.Iid);
								this.destroyProviders.Remove(provider.Iid);
								_providers.Remove(provider.Iid);
							}
						}
					}
					UnityEngine.Profiling.Profiler.EndSample();
				}
				else
				{
					UnityEngine.Profiling.Profiler.BeginSample("AssetSystem.UnloadUnusedAssets TryDestroyAllProviders");
					if (destroyProviderLoaders.Count > 0)
					{
						using (ListPool<AssetBundleLoader>.Get(out var tempList))
						{
							tempList.AddValues(destroyProviderLoaders);
							foreach (var loader in tempList)
							{
								if (!loader.CanDestroyProvider)
									continue;

								loader.TryDestroyAllProviders();
							}
						}
					}
					UnityEngine.Profiling.Profiler.EndSample();
					UnityEngine.Profiling.Profiler.BeginSample("AssetSystem.UnloadUnusedAssets TryDestroyAllLoader");
					if (destroyLoaders.Count > 0)
					{
						using (ListPool<AssetBundleLoader>.Get(out var tempList))
						{
							tempList.AddValues(destroyLoaders);
							foreach (var loader in tempList)
							{
								if (!loader.CanDestroy)
									continue;

								loader.Destroy(false);
								updater.loaders.Remove(loader.Iid);
								this.destroyLoaders.Remove(loader.Iid);
								_loaders.Remove(loader.Iid);
							}
						}
					}
					UnityEngine.Profiling.Profiler.EndSample();
				}
			}
		}


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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Update()
		{
			updater.Update();
		}

		/// <summary>
		/// 资源回收（卸载引用计数为零的资源）
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void UnloadUnusedAssets()
		{
			unloader.UnloadUnusedAssets();
		}

		/// <summary>
		/// 强制回收所有资源
		/// </summary>
		public static void ForceUnloadAllAssets()
		{
			foreach (var pair in _providers)
			{
				var provider = pair.Value;
				provider.Destroy();
			}
			_providers.Clear();

			foreach (var pair in _loaders)
			{
				var loader = pair.Value;
				loader.Destroy(true);
			}
			_loaders.Clear();

			updater.Clear();
			unloader.Clear();

			// 注意：调用底层接口释放所有资源
			Resources.UnloadUnusedAssets();
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void AddNewProvider(ProviderBase provider)
		{
			provider.InitSpawnDebugInfo();
			provider.OnIsDoneChanged += static x => updater.providers.Update(x.Iid, x, !x.IsDone);
			if (SimulationOnEditor)
				provider.OnCanDestroyChanged += static x => unloader.destroyProviders.Update(x.Iid, x, x.CanDestroy);
			
			updater.providers.Add(provider.Iid, provider);
			_providers.Add(provider.Iid, provider);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RemoveBundleProviders(Dictionary<int, ProviderBase> providers)
		{
			foreach (var pair in providers)
			{
				updater.providers.Remove(pair.Key);
				_providers.Remove(pair.Key);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void AddNewLoader(AssetBundleLoader loader)
		{
			loader.OnIsDoneChanged += static x => updater.loaders.Update(x.Iid, x, !x.IsDone);
			loader.OnCanDestroyChanged += static x => unloader.destroyLoaders.Update(x.Iid, x, x.CanDestroy);
			loader.OnCanDestroyProviderChanged += static x => unloader.destroyProviderLoaders.Update(x.Iid, x, x.CanDestroyProvider);
			
			updater.loaders.Add(loader.Iid, loader);
			_loaders.Add(loader.Iid, loader);
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
				AddNewProvider(provider);
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
				AddNewProvider(provider);
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
				AddNewProvider(provider);
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

		private static AssetBundleLoader CreateAssetBundleLoaderInternal(HardiskFileSearchResult localFileSearchResult)
		{
			// 如果加载器已经存在
			AssetBundleLoader loader = TryGetAssetBundleLoader(localFileSearchResult.OrignRelativePath);
			if (loader != null)
				return loader;
			// 新增下载需求
			loader = new AssetBundleLoader(localFileSearchResult);
			AddNewLoader(loader);
			return loader;
		}
		private static AssetBundleLoader TryGetAssetBundleLoader(string relativePath)
		{
			AssetBundleLoader loader = null;
			foreach (var pair in _loaders)
			{
				AssetBundleLoader temp = pair.Value;
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
			foreach (var pair in _providers)
			{
				ProviderBase temp = pair.Value;
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

            foreach (var pair in _providers)
            {
	            var provider = pair.Value;
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