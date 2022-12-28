using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Player;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    internal static class TestTracing
    {
        public static string Callsite([System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            return $"at {memberName} in {sourceFilePath}:{sourceLineNumber}";
        }
    }

    internal class TestBuildParametersBase : IBuildParameters
    {
        public virtual BuildTarget Target { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual BuildTargetGroup Group { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual ContentBuildFlags ContentBuildFlags { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual TypeDB ScriptInfo { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual ScriptCompilationOptions ScriptOptions { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual string TempOutputFolder { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual string ScriptOutputFolder { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual bool UseCache { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual string CacheServerHost { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual int CacheServerPort { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual bool WriteLinkXML { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
        public virtual bool NonRecursiveDependencies { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }

        public virtual UnityEngine.BuildCompression GetCompressionForIdentifier(string identifier)
        {
            throw new System.NotImplementedException(TestTracing.Callsite());
        }

        public virtual BuildSettings GetContentBuildSettings()
        {
            throw new System.NotImplementedException(TestTracing.Callsite());
        }

        public virtual string GetOutputFilePathForIdentifier(string identifier)
        {
            throw new System.NotImplementedException(TestTracing.Callsite());
        }

        public virtual ScriptCompilationSettings GetScriptCompilationSettings()
        {
            throw new System.NotImplementedException(TestTracing.Callsite());
        }
    }

    internal class TestBundleBuildParameters : TestBuildParametersBase, IBundleBuildParameters
    {
        public virtual bool AppendHash { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public virtual bool ContiguousBundles { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public virtual bool DisableVisibleSubAssetRepresentations { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }

    internal class TestBundleBuildContent : IBundleBuildContent
    {
        public virtual Dictionary<string, List<GUID>> BundleLayout => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual Dictionary<GUID, string> Addresses => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual List<GUID> Assets => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual List<GUID> Scenes => throw new System.NotImplementedException(TestTracing.Callsite());

#if UNITY_2019_3_OR_NEWER
        public virtual Dictionary<string, List<ResourceFile>> AdditionalFiles => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual List<CustomContent> CustomAssets => throw new System.NotImplementedException(TestTracing.Callsite());
#endif
    }

    internal class TestDependencyDataBase : IDependencyData
    {
        public virtual Dictionary<GUID, AssetLoadInfo> AssetInfo => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual Dictionary<GUID, BuildUsageTagSet> AssetUsage => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual Dictionary<GUID, SceneDependencyInfo> SceneInfo => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual Dictionary<GUID, BuildUsageTagSet> SceneUsage => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual BuildUsageCache DependencyUsageCache => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual BuildUsageTagGlobal GlobalUsage { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public virtual Dictionary<GUID, Hash128> DependencyHash => throw new System.NotImplementedException(TestTracing.Callsite());
    }

    internal class TestWriteDataBase : IWriteData
    {
        public virtual Dictionary<GUID, List<string>> AssetToFiles => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual Dictionary<string, List<ObjectIdentifier>> FileToObjects => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual List<IWriteOperation> WriteOperations => throw new System.NotImplementedException(TestTracing.Callsite());
    }

    internal class TestBuildResultsBase : IBuildResults
    {
        public virtual ScriptCompilationResult ScriptResults { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }

        public virtual Dictionary<string, WriteResult> WriteResults => throw new System.NotImplementedException(TestTracing.Callsite());

        public virtual Dictionary<string, SerializedFileMetaData> WriteResultsMetaData => throw new System.NotImplementedException(TestTracing.Callsite());
    }

    internal class TestBundleExplictObjectLayout : IBundleExplictObjectLayout
    {
        public virtual Dictionary<ObjectIdentifier, string> ExplicitObjectLocation { get => throw new System.NotImplementedException(TestTracing.Callsite()); set => throw new System.NotImplementedException(TestTracing.Callsite()); }
    }

    internal class TestBundleExtendedAssetData : IBuildExtendedAssetData
    {
        public virtual Dictionary<GUID, ExtendedAssetData> ExtendedData => throw new System.NotImplementedException();
    }
}
