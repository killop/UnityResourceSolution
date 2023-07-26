using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Pool;
using NinjaBeats;

namespace YooAsset
{
	public abstract class ProviderBase
	{
		public enum EStatus
		{
			None = 0,
			CheckBundle,
			Loading,
			Checking,
			Success,
			Fail,
		}
		
		public delegate void OnValueChanged(ProviderBase provider);

		private static int s_IidAllocator = 0;
		public int Iid { get; }
		
		/// <summary>
		/// 资源路径
		/// </summary>
		public string AssetPath { private set; get; }

		/// <summary>
		/// 资源对象的名称
		/// </summary>
		public string AssetName { private set; get; }

		/// <summary>
		/// 资源对象的类型
		/// </summary>
		public System.Type AssetType { private set; get; }

		/// <summary>
		/// 获取的资源对象
		/// </summary>
		public UnityEngine.Object AssetObject { protected set; get; }

		/// <summary>
		/// 获取的资源对象集合
		/// </summary>
		public UnityEngine.Object[] AllAssetObjects { protected set; get; }

		/// <summary>
		/// 获取的场景对象
		/// </summary>
		public UnityEngine.SceneManagement.Scene SceneObject { protected set; get; }

		protected EStatus _status = EStatus.None;
		
		/// <summary>
		/// 当前的加载状态
		/// </summary>
		public EStatus Status
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected set
			{
				_status = value;
				IsDone = _status == EStatus.Success || _status == EStatus.Fail;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _status;
		}
		
		private bool _isDone = false;
		public OnValueChanged OnIsDoneChanged = null;
		/// <summary>
		/// 是否完毕（成功或失败）
		/// </summary>
		public bool IsDone
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private set
			{
				var old = _isDone;
				_isDone = value;
				if (old != value)
				{
					OnIsDoneChanged?.Invoke(this);
					RefreshCanDestroy();
				}
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _isDone;
		}

		private int _refCount = 0;

		/// <summary>
		/// 引用计数
		/// </summary>
		public int RefCount
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private set
			{
				var old = _refCount;
				_refCount = value;
				if (old != value)
				{
					RefreshCanDestroy();	
				}
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _refCount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RefreshCanDestroy()
		{
			CanDestroy = IsDone && RefCount <= 0;
		}
		public OnValueChanged OnCanDestroyChanged = null;
		private bool _canDestroy = false;

		/// <summary>
		/// 是否可以销毁
		/// </summary>
		public bool CanDestroy
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private set
			{
				var old = _canDestroy;
				_canDestroy = value;
				if (old != value)
					OnCanDestroyChanged?.Invoke(this);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _canDestroy;
		}

		/// <summary>
		/// 是否已经销毁
		/// </summary>
		public bool IsDestroyed { private set; get; } = false;


		/// <summary>
		/// 加载进度
		/// </summary>
		public virtual float Progress
		{
			get
			{
				return 0;
			}
		}


		protected bool IsWaitForAsyncComplete { private set; get; } = false;
		private readonly List<OperationHandleBase> _handles = new List<OperationHandleBase>();


		public ProviderBase(string assetPath, System.Type assetType)
		{
			Iid = ++s_IidAllocator;
			AssetPath = assetPath;
			AssetName = assetPath;
			AssetType = assetType;
		}

		/// <summary>
		/// 轮询更新方法
		/// </summary>
		public abstract void Update();

		/// <summary>
		/// 销毁资源对象
		/// </summary>
		public virtual void Destroy()
		{
			IsDestroyed = true;
			OnIsDoneChanged = null;
			OnCanDestroyChanged = null;
		}

		/// <summary>
		/// 创建操作句柄
		/// </summary>
		/// <returns></returns>
		public OperationHandleBase CreateHandle()
		{
			// 引用计数增加
			RefCount++;

			OperationHandleBase handle;
			if (IsSceneProvider())
				handle = new SceneOperationHandle(this);
			else if (IsSubAssetsProvider())
				handle = new SubAssetsOperationHandle(this);
			else
				handle = new AssetOperationHandle(this);

			_handles.Add(handle);
			return handle;
		}

		/// <summary>
		/// 释放操作句柄
		/// </summary>
		public void ReleaseHandle(OperationHandleBase handle)
		{
			if (RefCount <= 0)
				Logger.Warning("Asset provider reference count is already zero. There may be resource leaks !");

			if (_handles.Remove(handle) == false)
				throw new System.Exception("Should never get here !");

			// 引用计数减少
			RefCount--;
		}

		/// <summary>
		/// 是否为场景提供者
		/// </summary>
		public bool IsSceneProvider()
		{
			if (this is BundledSceneProvider || this is DatabaseSceneProvider)
				return true;
			else
				return false;
		}

		/// <summary>
		/// 是否为子资源对象提供者
		/// </summary>
		public bool IsSubAssetsProvider()
		{
			if (this is BundledSubAssetsProvider || this is DatabaseSubAssetsProvider)
				return true;
			else
				return false;
		}

		/// <summary>
		/// 等待异步执行完毕
		/// </summary>
		public void WaitForAsyncComplete()
		{
			IsWaitForAsyncComplete = true;

			// 注意：主动轮询更新完成同步加载
			Update();

			// 验证结果
			if (IsDone == false)
			{
				Logger.Warning($"WaitForAsyncComplete failed to loading : {AssetPath}");
			}
		}

		/// <summary>
		/// 异步操作任务
		/// </summary>
		public System.Threading.Tasks.Task<object> Task
		{
			get
			{
				var handle = WaitHandle;
				return System.Threading.Tasks.Task.Factory.StartNew(o =>
				{
					handle.WaitOne();
					return AssetObject as object;
				}, this);
			}
		}

		#region 异步编程相关
		private System.Threading.EventWaitHandle _waitHandle;
		private System.Threading.WaitHandle WaitHandle
		{
			get
			{
				if (_waitHandle == null)
					_waitHandle = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
				_waitHandle.Reset();
				return _waitHandle;
			}
		}
		protected void InvokeCompletion()
		{
            var tempers = ListPool<OperationHandleBase>.Get();
            tempers.AddRange(_handles);
            foreach (var hande in tempers)
            {
                if (hande.IsValid)
                {
                    hande.InvokeCallback();
                }
            }
            _waitHandle?.Set();
            ListPool<OperationHandleBase>.Release(tempers);
		}
        #endregion
        #region 调试信息相关
        /// <summary>
        /// 出生的场景
        /// </summary>
        public string SpawnScene = string.Empty;

        /// <summary>
        /// 出生的时间
        /// </summary>
        public string SpawnTime = string.Empty;

		public bool PERFORMANCE = true;

        [Conditional("DEBUG")]
        public void InitSpawnDebugInfo()
        {
	        if (PERFORMANCE)
	        {
		        SpawnScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; ;
		        SpawnTime = SpawnTimeToString(UnityEngine.Time.realtimeSinceStartup);   
	        }
        }
        private string SpawnTimeToString(float spawnTime)
        {
            float h = UnityEngine.Mathf.FloorToInt(spawnTime / 3600f);
            float m = UnityEngine.Mathf.FloorToInt(spawnTime / 60f - h * 60f);
            float s = UnityEngine.Mathf.FloorToInt(spawnTime - m * 60f - h * 3600f);
            return h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00");
        }
        #endregion
        
        
#if UNITY_EDITOR
		private static HashSet<string> _s_SmartLibraryAssetPathHashSet = null;
		public static HashSet<string> s_SmartLibraryAssetPathHashSet
		{
			get
			{
				if (_s_SmartLibraryAssetPathHashSet == null)
					InitSmartLibrary();
				return _s_SmartLibraryAssetPathHashSet;
			}
		}
        private static void InitSmartLibrary()
        {
	        _s_SmartLibraryAssetPathHashSet = new();
	        try
	        {
		        var TypeLibraryDatabase = NinjaBeats.EditorUtils.GetTypeByFullName("Bewildered.SmartLibrary.LibraryDatabase");
		        var PropertyRootCollection = TypeLibraryDatabase.GetProperty("RootCollection", (System.Reflection.BindingFlags)(-1));
		        var RootCollection = PropertyRootCollection.GetValue(null);

		        var TypeLibraryCollection = NinjaBeats.EditorUtils.GetTypeByFullName("Bewildered.SmartLibrary.RootLibraryCollection");
		        var Field_subcollections = TypeLibraryCollection.GetField("_subcollections", (System.Reflection.BindingFlags)(-1));
		        var Field_items = TypeLibraryCollection.GetField("_items", (System.Reflection.BindingFlags)(-1));
		        
		        static void _DeepCollect(object rootCollection, System.Reflection.FieldInfo Field_subcollections, System.Reflection.FieldInfo Field_items, HashSet<string> result)
		        {
                    if (rootCollection == null)
				        return;

                    var TypeLibraryCollection2 = NinjaBeats.EditorUtils.GetTypeByFullName("Bewildered.SmartLibrary.LibraryCollection");
                    var Field_subcollections2 = TypeLibraryCollection2.GetField("_subcollections", (System.Reflection.BindingFlags)(-1));
                    if (Field_subcollections2.GetValue(rootCollection) is IList subcollections)
			        {
                        foreach (var subcollection in subcollections)
				        {
                           
                            var subCollectionType = subcollection.GetType();
                            var subCollectionItems = subCollectionType.GetField("_items", (System.Reflection.BindingFlags)(-1));
                            var method = subCollectionType.GetMethod("UpdateItems", (System.Reflection.BindingFlags)(-1));
                            method.Invoke(subcollection, new object[] { true });

                            if (subCollectionItems.GetValue(subcollection) is ISet<string> items)
                            {
                                foreach (var item in items)
                                {
                                    result.Add(item);
                                }
                            }
				        }
			        }
		        }
		        _DeepCollect(RootCollection, Field_subcollections, Field_items, _s_SmartLibraryAssetPathHashSet);
	        }
	        catch (System.Exception e)
	        {
		        Logger.Error("读取 URS SmartLibrary 失败: " + e.Message + "\n" + e.StackTrace);
		        _s_SmartLibraryAssetPathHashSet = new();
	        }
        }
#endif
    }
}