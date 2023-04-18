using System;
using System.Threading;

using BestHTTP.PlatformSupport.Threading;

using UnityEngine;

#if NETFX_CORE
    using System.Threading.Tasks;
#endif

namespace BestHTTP
{
    /// <summary>
    /// Threading mode the plugin will use to call HTTPManager.OnUpdate().
    /// </summary>
    public enum ThreadingMode : int
    {
        /// <summary>
        /// HTTPManager.OnUpdate() is called from the HTTPUpdateDelegator's Update functions (Unity's main thread).
        /// </summary>
        UnityUpdate,

        /// <summary>
        /// The plugin starts a dedicated thread to call HTTPManager.OnUpdate() periodically.
        /// </summary>
        Threaded,

        /// <summary>
        /// HTTPManager.OnUpdate() will not be called automatically.
        /// </summary>
        None
    }

    /// <summary>
    /// Will route some U3D calls to the HTTPManager.
    /// </summary>
    [ExecuteInEditMode]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public sealed class HTTPUpdateDelegator : MonoBehaviour
    {
        #region Public Properties

        /// <summary>
        /// The singleton instance of the HTTPUpdateDelegator
        /// </summary>
        public static HTTPUpdateDelegator Instance { get { return CheckInstance(); } }
        private volatile static HTTPUpdateDelegator instance;

        /// <summary>
        /// True, if the Instance property should hold a valid value.
        /// </summary>
        public static bool IsCreated { get; private set; }

        /// <summary>
        /// It's true if the dispatch thread running.
        /// </summary>
        public static bool IsThreadRunning { get; private set; }

        /// <summary>
        /// The current threading mode the plugin is in.
        /// </summary>
        public ThreadingMode CurrentThreadingMode { get { return _currentThreadingMode; } set { SetThreadingMode(value); } }
        private ThreadingMode _currentThreadingMode = ThreadingMode.UnityUpdate;

        /// <summary>
        /// How much time the plugin should wait between two update call. Its default value 100 ms.
        /// </summary>
        public static int ThreadFrequencyInMS { get; set; }

        /// <summary>
        /// Called in the OnApplicationQuit function. If this function returns False, the plugin will not start to
        /// shut down itself.
        /// </summary>
        public static System.Func<bool> OnBeforeApplicationQuit;

        /// <summary>
        /// Called when the Unity application's foreground state changed.
        /// </summary>
        public static System.Action<bool> OnApplicationForegroundStateChanged;

        #endregion

        private static bool isSetupCalled;
        private int isHTTPManagerOnUpdateRunning;
        private AutoResetEvent pingEvent = new AutoResetEvent(false);
        private int updateThreadCount = 0;

#if UNITY_EDITOR
        /// <summary>
        /// Called after scene loaded to support Configurable Enter Play Mode (https://docs.unity3d.com/2019.3/Documentation/Manual/ConfigurableEnterPlayMode.html)
        /// </summary>
#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void ResetSetup()
        {
            isSetupCalled = false;
            instance?.SetThreadingMode(ThreadingMode.UnityUpdate);

            HTTPManager.Logger.Information("HTTPUpdateDelegator", "Reset called!");
        }
#endif

        static HTTPUpdateDelegator()
        {
            ThreadFrequencyInMS = 100;
        }

        /// <summary>
        /// Will create the HTTPUpdateDelegator instance and set it up.
        /// </summary>
        public static HTTPUpdateDelegator CheckInstance()
        {
            try
            {
                if (!IsCreated)
                {
                    GameObject go = GameObject.Find("HTTP Update Delegator");

                    if (go != null)
                        instance = go.GetComponent<HTTPUpdateDelegator>();

                    if (instance == null)
                    {
                        go = new GameObject("HTTP Update Delegator");
                        go.hideFlags = HideFlags.HideAndDontSave;
                        
                        instance = go.AddComponent<HTTPUpdateDelegator>();
                    }
                    IsCreated = true;

#if UNITY_EDITOR
                    if (!UnityEditor.EditorApplication.isPlaying)
                    {
                        UnityEditor.EditorApplication.update -= instance.Update;
                        UnityEditor.EditorApplication.update += instance.Update;
                    }

#if UNITY_2017_2_OR_NEWER
                    UnityEditor.EditorApplication.playModeStateChanged -= instance.OnPlayModeStateChanged;
                    UnityEditor.EditorApplication.playModeStateChanged += instance.OnPlayModeStateChanged;
#else
                    UnityEditor.EditorApplication.playmodeStateChanged -= instance.OnPlayModeStateChanged;
                    UnityEditor.EditorApplication.playmodeStateChanged += instance.OnPlayModeStateChanged;
#endif
#endif

                    // https://docs.unity3d.com/ScriptReference/Application-wantsToQuit.html
                    Application.wantsToQuit -= UnityApplication_WantsToQuit;
                    Application.wantsToQuit += UnityApplication_WantsToQuit;

                    HTTPManager.Logger.Information("HTTPUpdateDelegator", "Instance Created!");
                }
            }
            catch
            {
                HTTPManager.Logger.Error("HTTPUpdateDelegator", "Please call the BestHTTP.HTTPManager.Setup() from one of Unity's event(eg. awake, start) before you send any request!");
            }

            return instance;
        }

        private void Setup()
        {
            if (isSetupCalled)
                return;

            using (var _ = new Unity.Profiling.ProfilerMarker(nameof(HTTPUpdateDelegator.Setup)).Auto())
            {
                isSetupCalled = true;

                HTTPManager.Logger.Information("HTTPUpdateDelegator", $"Setup called Threading Mode: {this._currentThreadingMode}");

                HTTPManager.Setup();

                SetThreadingMode(this._currentThreadingMode);

                // Unity doesn't tolerate well if the DontDestroyOnLoad called when purely in editor mode. So, we will set the flag
                //  only when we are playing, or not in the editor.
                if (!Application.isEditor || Application.isPlaying)
                    GameObject.DontDestroyOnLoad(this.gameObject);

                HTTPManager.Logger.Information("HTTPUpdateDelegator", "Setup done!");
            }
        }

        /// <summary>
        /// Set directly the threading mode to use.
        /// </summary>
        public void SetThreadingMode(ThreadingMode mode)
        {
            if (_currentThreadingMode == mode)
                return;

            HTTPManager.Logger.Information("HTTPUpdateDelegator", $"SetThreadingMode({mode}, {isSetupCalled})");

            _currentThreadingMode = mode;

            if (!isSetupCalled)
                Setup();

            switch (_currentThreadingMode)
            {
                case ThreadingMode.UnityUpdate:
                case ThreadingMode.None:
                    IsThreadRunning = false;
                    PingUpdateThread();
                    break;

                case ThreadingMode.Threaded:
#if !UNITY_WEBGL || UNITY_EDITOR
                    ThreadedRunner.RunLongLiving(ThreadFunc);
#else
                    HTTPManager.Logger.Warning(nameof(HTTPUpdateDelegator), "Threading mode set to ThreadingMode.Threaded, but threads aren't supported under WebGL!");
#endif
                    break;
            }
        }

        /// <summary>
        /// Swaps threading mode between Unity's Update function or a distinct thread.
        /// </summary>
        public void SwapThreadingMode() => SetThreadingMode(_currentThreadingMode == ThreadingMode.Threaded ? ThreadingMode.UnityUpdate : ThreadingMode.Threaded);

        /// <summary>
        /// Pings the update thread to call HTTPManager.OnUpdate immediately.
        /// </summary>
        /// <remarks>Works only when the current threading mode is Threaded!</remarks>
        public void PingUpdateThread() => pingEvent.Set();

        void ThreadFunc()
        {
            HTTPManager.Logger.Information("HTTPUpdateDelegator", "Update Thread Started");

            ThreadedRunner.SetThreadName("BestHTTP.Update Thread");

            try
            {
                if (Interlocked.Increment(ref updateThreadCount) > 1)
                {
                    HTTPManager.Logger.Information("HTTPUpdateDelegator", "An update thread already started.");
                    return;
                }

                // Threading mode might be already changed, so set IsThreadRunning to IsThreaded's value.
                IsThreadRunning = CurrentThreadingMode == ThreadingMode.Threaded;
                while (IsThreadRunning)
                {
                    CallOnUpdate();

                    pingEvent.WaitOne(ThreadFrequencyInMS);
                }
            }
            finally
            {
                Interlocked.Decrement(ref updateThreadCount);
                HTTPManager.Logger.Information("HTTPUpdateDelegator", "Update Thread Ended");
            }
        }

        void Update()
        {
            if (!isSetupCalled)
                Setup();

            if (CurrentThreadingMode == ThreadingMode.UnityUpdate)
                CallOnUpdate();
        }

        private void CallOnUpdate()
        {
            // Prevent overlapping call of OnUpdate from unity's main thread and a separate thread
            if (Interlocked.CompareExchange(ref isHTTPManagerOnUpdateRunning, 1, 0) == 0)
            {
                try
                {
                    HTTPManager.OnUpdate();
                }
                finally
                {
                    Interlocked.Exchange(ref isHTTPManagerOnUpdateRunning, 0);
                }
            }
        }

#if UNITY_EDITOR
#if UNITY_2017_2_OR_NEWER
        void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange playMode)
        {
            if (playMode == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            {
                UnityEditor.EditorApplication.update -= Update;
            }
            else if (playMode == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                UnityEditor.EditorApplication.update -= Update;
                UnityEditor.EditorApplication.update += Update;

                HTTPUpdateDelegator.ResetSetup();
                HTTPManager.ResetSetup();
            }
        }
#else
        void OnPlayModeStateChanged()
        {
            if (UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.update -= Update;
            else if (!UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.update += Update;
        }

#endif
#endif

        void OnDisable()
        {
            HTTPManager.Logger.Information("HTTPUpdateDelegator", "OnDisable Called!");

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
                UnityApplication_WantsToQuit();
        }

        void OnApplicationPause(bool isPaused)
        {
            HTTPManager.Logger.Information("HTTPUpdateDelegator", "OnApplicationPause isPaused: " + isPaused);

            if (HTTPUpdateDelegator.OnApplicationForegroundStateChanged != null)
                HTTPUpdateDelegator.OnApplicationForegroundStateChanged(isPaused);
        }

        private static bool UnityApplication_WantsToQuit()
        {
            HTTPManager.Logger.Information("HTTPUpdateDelegator", "UnityApplication_WantsToQuit Called!");

            if (OnBeforeApplicationQuit != null)
            {
                try
                {
                    if (!OnBeforeApplicationQuit())
                    {
                        HTTPManager.Logger.Information("HTTPUpdateDelegator", "OnBeforeApplicationQuit call returned false, postponing plugin and application shutdown.");
                        return false;
                    }
                }
                catch (System.Exception ex)
                {
                    HTTPManager.Logger.Exception("HTTPUpdateDelegator", string.Empty, ex);
                }
            }

            IsThreadRunning = false;
            Instance.PingUpdateThread();

            if (!IsCreated)
                return true;

            IsCreated = false;

            HTTPManager.OnQuit();

            return true;
        }
    }
}
