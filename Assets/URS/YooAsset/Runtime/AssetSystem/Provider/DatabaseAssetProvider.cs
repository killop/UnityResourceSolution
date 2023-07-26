using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace YooAsset
{
	internal sealed class DatabaseAssetProvider : ProviderBase
	{
		private float _SimulateProgress = 0;
		public override float Progress
		{
			get
			{
				if (IsDone)
					return 100f;
				else
					return _SimulateProgress;
			}
		}

		public DatabaseAssetProvider(string assetPath, System.Type assetType)
			: base(assetPath, assetType)
		{
		}
		
		public override void Update()
		{
#if UNITY_EDITOR
			if (IsDone)
				return;

			if (Status == EStatus.None)
			{
				_SimulateProgress = 0;
				
				// 检测资源文件是否存在
				string guid = UnityEditor.AssetDatabase.AssetPathToGUID(AssetPath);
				if (string.IsNullOrEmpty(guid))
				{
					Status = EStatus.Fail;
					InvokeCompletion();
					return;
				}
				
				// 检测资源文件是否在Library中
				if (!s_SmartLibraryAssetPathHashSet.Contains(guid))
				{
					Logger.Error($"URS SmartLibrary 中找不到资源 \'{this.AssetPath}\' Window/SmartLibrary 中添加该资源后重试，如果你是资源的制作者，那么这条信息非常重要！！！！！如果不是可以当作没看见" );
				}
				
				Status = EStatus.Loading;
			
				// 注意：模拟异步加载效果提前返回
				if (IsWaitForAsyncComplete == false)
					return;
			}

			// 1. 加载资源对象
			if (Status == EStatus.Loading)
			{
				_SimulateProgress = Mathf.Lerp(_SimulateProgress, 100.0f, Time.deltaTime * 20.0f);
				AssetObject = UnityEditor.AssetDatabase.LoadAssetAtPath(AssetPath, AssetType);
				Status = EStatus.Checking;
			}

			// 2. 检测加载结果
			if (Status == EStatus.Checking)
			{
				Status = AssetObject == null ? EStatus.Fail : EStatus.Success;
				if (Status == EStatus.Fail)
					Logger.Warning($"Failed to load asset object : {AssetPath}");
				InvokeCompletion();
			}
#endif
		}
	}
}