using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Optional build task that extracts all referenced MonoScripts and assigns them to the specified bundle
    /// </summary>
    public class CreateMonoScriptBundle : IBuildTask
    {
        static readonly GUID k_DefaultGuid = new GUID(CommonStrings.UnityDefaultResourceGuid);
        /// <inheritdoc />
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)]
        IBundleExplictObjectLayout m_Layout;
#pragma warning restore 649

        /// <summary>
        /// Stores the name for the MonoScript bundle.
        /// </summary>
        public string MonoScriptBundleName { get; set; }

        /// <summary>
        /// Create the MonoScript bundle.
        /// </summary>
        /// <param name="bundleName">The name of the bundle.</param>
        public CreateMonoScriptBundle(string bundleName)
        {
            MonoScriptBundleName = bundleName;
        }

        /// <inheritdoc />
        public ReturnCode Run()
        {
            HashSet<ObjectIdentifier> buildInObjects = new HashSet<ObjectIdentifier>();
            foreach (AssetLoadInfo dependencyInfo in m_DependencyData.AssetInfo.Values)
                buildInObjects.UnionWith(dependencyInfo.referencedObjects);

            foreach (SceneDependencyInfo dependencyInfo in m_DependencyData.SceneInfo.Values)
                buildInObjects.UnionWith(dependencyInfo.referencedObjects);

            ObjectIdentifier[] usedSet = buildInObjects.ToArray();
            Type[] usedTypes = BuildCacheUtility.GetMainTypeForObjects(usedSet);

            if (m_Layout == null)
                m_Layout = new BundleExplictObjectLayout();

            Type monoScript = typeof(MonoScript);
            for (int i = 0; i < usedTypes.Length; i++)
            {
                if (usedTypes[i] != monoScript || usedSet[i].guid == k_DefaultGuid)
                    continue;

                m_Layout.ExplicitObjectLocation.Add(usedSet[i], MonoScriptBundleName);
            }

            if (m_Layout.ExplicitObjectLocation.Count == 0)
                m_Layout = null;

            return ReturnCode.Success;
        }
    }
}
