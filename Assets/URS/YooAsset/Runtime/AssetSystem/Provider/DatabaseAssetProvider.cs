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

#if UNITY_EDITOR
		private static int _sThisFrameCount = 0;
		private static long _sThisFrameTotalDuration = 0;
		private static Stopwatch _sLoadingStopwatch = new Stopwatch();
		private const long FRAME_LIMIT_DURATION = 12;
#endif
		
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
				else
				{
					Status = EStatus.Loading;
				}
			
				// 注意：模拟异步加载效果提前返回
				if (IsWaitForAsyncComplete == false)
					return;
			}

			// 1. 加载资源对象
			if (Status == EStatus.Loading)
			{
				_SimulateProgress = Mathf.Lerp(_SimulateProgress, 100.0f, Time.deltaTime * 20.0f);
				if (_sThisFrameCount != Time.frameCount)
				{
					_sThisFrameCount = Time.frameCount;
					_sThisFrameTotalDuration = 0;
				}
				if (_sThisFrameTotalDuration <= FRAME_LIMIT_DURATION)
				{
					_sLoadingStopwatch.Restart();
					AssetObject = UnityEditor.AssetDatabase.LoadAssetAtPath(AssetPath, AssetType);
					_sLoadingStopwatch.Stop();
					_sThisFrameTotalDuration += _sLoadingStopwatch.ElapsedMilliseconds;
					Status = EStatus.Checking;
				}
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