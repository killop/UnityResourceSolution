#if !UNITY_2019_3_OR_NEWER
#define NOT_UNITY_2019_3_OR_NEWER
#endif

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor.Build.Content;

namespace UnityEditor.Build.Pipeline.Utilities
{
    static class ReflectionExtensions
    {
        static FieldInfo WriteResult_SerializedObjects;
        static FieldInfo WriteResult_ResourceFiles;

        static FieldInfo SceneDependencyInfo_Scene;
        static FieldInfo SceneDependencyInfo_ProcessedScene;
        static FieldInfo SceneDependencyInfo_ReferencedObjects;

        static bool BuildUsageTagSet_SupportsFilterToSubset;
        static bool ContentBuildInterface_SupportsMultiThreadedArchiving;

        static ReflectionExtensions()
        {
            WriteResult_SerializedObjects = typeof(WriteResult).GetField("m_SerializedObjects", BindingFlags.NonPublic | BindingFlags.Instance);
            WriteResult_ResourceFiles = typeof(WriteResult).GetField("m_ResourceFiles", BindingFlags.NonPublic | BindingFlags.Instance);
            SceneDependencyInfo_Scene =  typeof(SceneDependencyInfo).GetField("m_Scene", BindingFlags.Instance | BindingFlags.NonPublic);
            SceneDependencyInfo_ProcessedScene = typeof(SceneDependencyInfo).GetField("m_ProcessedScene", BindingFlags.Instance | BindingFlags.NonPublic);
            SceneDependencyInfo_ReferencedObjects = typeof(SceneDependencyInfo).GetField("m_ReferencedObjects", BindingFlags.Instance | BindingFlags.NonPublic);

            BuildUsageTagSet_SupportsFilterToSubset = typeof(BuildUsageTagSet).GetMethod("FilterToSubset") != null;
            foreach (MethodInfo info in typeof(ContentBuildInterface).GetMethods().Where(x => x.Name == "ArchiveAndCompress"))
            {
                foreach (var attr in info.CustomAttributes)
                {
                    ContentBuildInterface_SupportsMultiThreadedArchiving = attr.AttributeType.Name == "ThreadSafeAttribute";
                    if (ContentBuildInterface_SupportsMultiThreadedArchiving)
                        break;
                }
            }
        }

        public static bool SupportsFilterToSubset => BuildUsageTagSet_SupportsFilterToSubset;

        public static bool SupportsMultiThreadedArchiving => ContentBuildInterface_SupportsMultiThreadedArchiving;

        public static void SetSerializedObjects(this ref WriteResult result, ObjectSerializedInfo[] osis)
        {
            object boxed = result;
            WriteResult_SerializedObjects.SetValue(boxed, osis);
            result = (WriteResult)boxed;
        }

        public static void SetResourceFiles(this ref WriteResult result, ResourceFile[] files)
        {
            object boxed = result;
            WriteResult_ResourceFiles.SetValue(boxed, files);
            result = (WriteResult)boxed;
        }

        public static void SetScene(this ref SceneDependencyInfo dependencyInfo, string scene)
        {
            object boxed = dependencyInfo;
            SceneDependencyInfo_Scene.SetValue(boxed, scene);
            dependencyInfo = (SceneDependencyInfo)boxed;
        }

        // Use conditionals to remove api from callsite
        [Conditional("NOT_UNITY_2019_3_OR_NEWER")]
        public static void SetProcessedScene(this ref SceneDependencyInfo dependencyInfo, string processedScene)
        {
            object boxed = dependencyInfo;
            SceneDependencyInfo_ProcessedScene.SetValue(boxed, processedScene);
            dependencyInfo = (SceneDependencyInfo)boxed;
        }

        public static void SetReferencedObjects(this ref SceneDependencyInfo dependencyInfo, ObjectIdentifier[] references)
        {
            object boxed = dependencyInfo;
            SceneDependencyInfo_ReferencedObjects.SetValue(boxed, references);
            dependencyInfo = (SceneDependencyInfo)boxed;
        }

        // Extension methods are second to explicit methods, no need to define this out, it is being used as an API contract only
        public static void FilterToSubset(this BuildUsageTagSet usageSet, ObjectIdentifier[] objectIds)
        {
            throw new System.Exception("FilterToSubset is not supported in this Unity version");
        }
    }
}
