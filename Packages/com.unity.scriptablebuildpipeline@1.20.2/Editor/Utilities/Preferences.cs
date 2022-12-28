using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Pipeline.Tasks;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Static class containing per project build settings.
    /// </summary>
    public static class ScriptableBuildPipeline
    {
        private class GUIScope : GUI.Scope
        {
            float m_LabelWidth;
            public GUIScope(float layoutMaxWidth)
            {
                m_LabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 250;
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();
                GUILayout.Space(15);
            }

            public GUIScope() : this(500)
            {
            }

            protected override void CloseScope()
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = m_LabelWidth;
            }
        }

        internal class Properties
        {
            public static readonly GUIContent generalSettings = EditorGUIUtility.TrTextContent("General Settings");
            public static readonly GUIContent threadedArchiving = EditorGUIUtility.TrTextContent("Threaded Archiving", "Thread the archiving and compress build stage.");
            public static readonly GUIContent logCacheMiss = EditorGUIUtility.TrTextContent("Log Cache Miss", "Log a warning on build cache misses. Warning will contain which asset and dependency caused the miss.");
            public static readonly GUIContent slimWriteResults = EditorGUIUtility.TrTextContent("Slim Write Results", "Reduces the caching of WriteResults data down to the bare minimum for improved cache performance.");
            public static readonly GUIContent v2Hasher = EditorGUIUtility.TrTextContent("Use V2 Hasher", "Use the same hasher as Asset Database V2. This hasher improves build cache performance, but invalidates the existing build cache.");
            public static readonly GUIContent hashSeed = EditorGUIUtility.TrTextContent("FileID Generator Seed", "Allows you to specify an additional seed to avoid file identifier collisions during build. This changes the layout of all objects in all bundles and we suggest not changing this value after release.");
            public static readonly GUIContent randSeed = EditorGUIUtility.TrTextContent("Random");
            public static readonly GUIContent headerSize = EditorGUIUtility.TrTextContent("Prefab Packed Header Size", "Allows you to specify the size of the header for PrefabPacked asset bundles to avoid file identifier collisions during build. This changes the layout of all objects in all bundles and we suggest not changing this value after release.");
            public static readonly GUIContent maxCacheSize = EditorGUIUtility.TrTextContent("Maximum Cache Size (GB)", "The size of the Build Cache folder will be kept below this maximum value when possible.");
            public static readonly GUIContent buildCache = EditorGUIUtility.TrTextContent("Build Cache");
            public static readonly GUIContent purgeCache = EditorGUIUtility.TrTextContent("Purge Cache");
            public static readonly GUIContent pruneCache = EditorGUIUtility.TrTextContent("Prune Cache");
            public static readonly GUIContent cacheSizeIs = EditorGUIUtility.TrTextContent("Cache size is");
            public static readonly GUIContent pleaseWait = EditorGUIUtility.TrTextContent("Please wait...");
            public static readonly GUIContent cacheServerConfig = EditorGUIUtility.TrTextContent("Cache Server Configuration");
            public static readonly GUIContent useBuildCacheServer = EditorGUIUtility.TrTextContent("Use Build Cache Server");
            public static readonly GUIContent cacheServerHost = EditorGUIUtility.TrTextContent("Cache Server Host");
            public static readonly GUIContent cacheServerPort = EditorGUIUtility.TrTextContent("Cache Server Port");
            public static bool startedCalculation = false;
            public static long currentCacheSize = -1;
            public static readonly GUIContent useDetailedBuildLog = EditorGUIUtility.TrTextContent("Use Detailed Build Log", "Writes detailed event information in the build log.");
        }

        [System.Serializable]
        internal class Settings
        {
            public bool useBuildCacheServer = false;
            public string cacheServerHost = "";
            public int cacheServerPort = 8126;
            public bool threadedArchiving = true;
            public bool logCacheMiss = false;
            public bool slimWriteResults = true;
            public int maximumCacheSize = 20;
            public bool useDetailedBuildLog = false;
#if UNITY_2021_1_OR_NEWER
            public bool useV2Hasher = true;
#elif UNITY_2020_1_OR_NEWER
            public bool useV2Hasher = false;
#endif
            public int fileIDHashSeed = 0;
            public int prefabPackedHeaderSize = 2;
        }

        internal static Settings s_Settings = new Settings();

        /// <summary>
        /// Flag to determine if the Build Cache Server is to be used.
        /// </summary>
        public static bool UseBuildCacheServer
        {
            get => s_Settings.useBuildCacheServer;
            set => CompareAndSet(ref s_Settings.useBuildCacheServer, value);
        }

        /// <summary>
        /// The host of the Build Cache Server.
        /// </summary>
        public static string CacheServerHost
        {
            get => s_Settings.cacheServerHost;
            set => CompareAndSet(ref s_Settings.cacheServerHost, value);
        }

        /// <summary>
        /// The port number for the Build Cache Server.
        /// </summary>
        public static int CacheServerPort
        {
            get => s_Settings.cacheServerPort;
            set => CompareAndSet(ref s_Settings.cacheServerPort, value);
        }

        /// <summary>
        /// Thread the archiving and compress build stage.
        /// </summary>
        public static bool threadedArchiving
        {
            get => s_Settings.threadedArchiving;
            set => CompareAndSet(ref s_Settings.threadedArchiving, value);
        }

        /// <summary>
        /// Log a warning on build cache misses. Warning will contain which asset and dependency caused the miss.
        /// </summary>
        public static bool logCacheMiss
        {
            get => s_Settings.logCacheMiss;
            set => CompareAndSet(ref s_Settings.logCacheMiss, value);
        }

        /// <summary>
        /// Reduces the caching of WriteResults data down to the bare minimum for improved cache performance.
        /// </summary>
        public static bool slimWriteResults
        {
            get => s_Settings.slimWriteResults;
            set => CompareAndSet(ref s_Settings.slimWriteResults, value);
        }

        /// <summary>
        /// The size of the Build Cache folder will be kept below this maximum value when possible.
        /// </summary>
        public static int maximumCacheSize
        {
            get => s_Settings.maximumCacheSize;
            set => CompareAndSet(ref s_Settings.maximumCacheSize, value);
        }

        /// <summary>
        /// Set this to true to write more detailed event information in the build log. Set to false to only write basic event information.
        /// </summary>
        public static bool useDetailedBuildLog
        {
            get => s_Settings.useDetailedBuildLog;
            set => CompareAndSet(ref s_Settings.useDetailedBuildLog, value);
        }

        /// <summary>
        /// Set this to true to use the same hasher as Asset Database V2. This hasher improves build cache performance, but invalidates the existing build cache.
        /// </summary>
#if UNITY_2020_1_OR_NEWER
        public static bool useV2Hasher
        {
            get => s_Settings.useV2Hasher;
            set => CompareAndSet(ref s_Settings.useV2Hasher, value);
        }
#endif

        // Internal as we don't want to allow setting these via API. We want to ensure they are saved to json, and checked in to the project version control.
        internal static int fileIDHashSeed
        {
            get => s_Settings.fileIDHashSeed;
            set => CompareAndSet(ref s_Settings.fileIDHashSeed, value);
        }
        
        // Internal as we don't want to allow setting these via API. We want to ensure they are saved to json, and checked in to the project version control.
        internal static int prefabPackedHeaderSize
        {
            get => Mathf.Clamp(s_Settings.prefabPackedHeaderSize, 1, 4);
            set => CompareAndSet(ref s_Settings.prefabPackedHeaderSize, Mathf.Clamp(value, 1, 4));
        }

        static void CompareAndSet<T>(ref T property, T value)
        {
            if (property.Equals(value))
                return;

            property = value;
            SaveSettings();
        }

        internal const string kSettingPath = "ProjectSettings/ScriptableBuildPipeline.json";

        internal static void LoadSettings()
        {
            // Load new settings from Json
            if (File.Exists(kSettingPath))
            {
                // Existing projects should keep previous defaults that are now settings
                s_Settings.prefabPackedHeaderSize = 4;

                var json = File.ReadAllText(kSettingPath);
                EditorJsonUtility.FromJsonOverwrite(json, s_Settings);
            }
        }

        internal static void SaveSettings()
        {
            var json = EditorJsonUtility.ToJson(s_Settings, true);
            File.WriteAllText(kSettingPath, json);
        }

        static ScriptableBuildPipeline()
        {
            LoadSettings();
        }

#if UNITY_2019_1_OR_NEWER
        [SettingsProvider]
        static SettingsProvider CreateBuildCacheProvider()
        {
            var provider = new SettingsProvider("Preferences/Scriptable Build Pipeline", SettingsScope.User, SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Properties>());
            provider.guiHandler = sarchContext => OnGUI();
            return provider;
        }

#else
        [PreferenceItem("Scriptable Build Pipeline")]
#endif
        static void OnGUI()
        {
            using (new GUIScope())
            {
                EditorGUI.BeginChangeCheck();
                DrawGeneralProperties();
                GUILayout.Space(15);
                DrawBuildCacheProperties();
                if (EditorGUI.EndChangeCheck())
                    SaveSettings();
            }
        }

        static void DrawGeneralProperties()
        {
            GUILayout.Label(Properties.generalSettings, EditorStyles.boldLabel);

            if (ReflectionExtensions.SupportsMultiThreadedArchiving)
                s_Settings.threadedArchiving = EditorGUILayout.Toggle(Properties.threadedArchiving, s_Settings.threadedArchiving);

            s_Settings.logCacheMiss = EditorGUILayout.Toggle(Properties.logCacheMiss, s_Settings.logCacheMiss);
            s_Settings.slimWriteResults = EditorGUILayout.Toggle(Properties.slimWriteResults, s_Settings.slimWriteResults);
            s_Settings.useDetailedBuildLog = EditorGUILayout.Toggle(Properties.useDetailedBuildLog, s_Settings.useDetailedBuildLog);
#if UNITY_2020_1_OR_NEWER
            s_Settings.useV2Hasher = EditorGUILayout.Toggle(Properties.v2Hasher, s_Settings.useV2Hasher);
#endif
            GUILayout.BeginHorizontal();
            s_Settings.fileIDHashSeed = EditorGUILayout.IntField(Properties.hashSeed, s_Settings.fileIDHashSeed);
            if (GUILayout.Button(Properties.randSeed, GUILayout.Width(120)))
                s_Settings.fileIDHashSeed = (int)(Random.value * uint.MaxValue);
            GUILayout.EndHorizontal();
            s_Settings.prefabPackedHeaderSize = EditorGUILayout.IntSlider(Properties.headerSize, s_Settings.prefabPackedHeaderSize, 1, 4);
        }

        static void DrawBuildCacheProperties()
        {
            GUILayout.Label(Properties.buildCache, EditorStyles.boldLabel);
            // Show Gigabytes to the user.
            const int kMinSizeInGigabytes = 1;
            const int kMaxSizeInGigabytes = 200;

            // Write size in GigaBytes.
            s_Settings.maximumCacheSize = EditorGUILayout.IntSlider(Properties.maxCacheSize, s_Settings.maximumCacheSize, kMinSizeInGigabytes, kMaxSizeInGigabytes);

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(500));
            if (GUILayout.Button(Properties.purgeCache, GUILayout.Width(120)))
            {
                BuildCache.PurgeCache(true);
                Properties.startedCalculation = false;
            }

            if (GUILayout.Button(Properties.pruneCache, GUILayout.Width(120)))
            {
                BuildCache.PruneCache();
                Properties.startedCalculation = false;
            }
            GUILayout.EndHorizontal();

            // Current cache size
            if (!Properties.startedCalculation)
            {
                Properties.startedCalculation = true;
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    BuildCache.ComputeCacheSizeAndFolders(out Properties.currentCacheSize, out List<BuildCache.CacheFolder> cacheFolders);
                });
            }

            if (Properties.currentCacheSize >= 0)
                GUILayout.Label(Properties.cacheSizeIs.text + " " + EditorUtility.FormatBytes(Properties.currentCacheSize));
            else
                GUILayout.Label(Properties.cacheSizeIs.text + " is being calculated...");

            GUILayout.Space(15);
            GUILayout.Label(Properties.cacheServerConfig, EditorStyles.boldLabel);

            s_Settings.useBuildCacheServer = EditorGUILayout.Toggle(Properties.useBuildCacheServer, s_Settings.useBuildCacheServer);
            if (s_Settings.useBuildCacheServer)
            {
                s_Settings.cacheServerHost = EditorGUILayout.TextField(Properties.cacheServerHost, s_Settings.cacheServerHost);
                s_Settings.cacheServerPort = EditorGUILayout.IntField(Properties.cacheServerPort, s_Settings.cacheServerPort);
            }
        }
    }
}
