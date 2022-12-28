
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bewildered.SmartLibrary;
using System.IO;
using System;
using System.Diagnostics;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TagInfo = System.Collections.Generic.Dictionary<Bewildered.SmartLibrary.TagRule.TagRuleType, string>;
using URS;
using YooAsset.Utility;
using System.Linq;
using System.Text;
using YooAsset;
using UnityEditor.Build.Content;
using BuildCompression = UnityEngine.BuildCompression;
using UnityEditor.Search;
using Context = System.Collections.Generic.Dictionary<string, object>;
using Debug = UnityEngine.Debug;
using URS.Editor;
using MHLab.Patch.Core.IO;

namespace URS
{
    public static partial class Build
    {
        public static List<BuildTaskWorkSpace> _buildTaskWorkSpaces = new List<BuildTaskWorkSpace>();

        public static bool hasTask => _buildTaskWorkSpaces.Count > 0;
        static Build()
        {
            EditorApplication.update -= BuildUpdate;
            EditorApplication.update += BuildUpdate;
        }
        public static void BuildUpdate()
        {
            if (_buildTaskWorkSpaces.Count > 0)
            {
                for (int i = _buildTaskWorkSpaces.Count-1; i >=0; i--)
                {
                    var ws = _buildTaskWorkSpaces[i];
                    ws.Update();
                    if (!ws.HasAnyWork())
                    {
                        _buildTaskWorkSpaces.RemoveAt(i);
                    }
                }
            }
        }
        public static void AddBuildTaskWorkSpace(BuildTaskWorkSpace ws)
        {
            _buildTaskWorkSpaces.Clear();
            _buildTaskWorkSpaces.Add(ws);
            ws.DoNextTask();
        }

        static void SetScriptingImplementation(ScriptingImplementation type)
        {
            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup, type);
        }

        [MenuItem("URS/Build(Resource And Player)-（Mono）")]
        public static void BuildResourceAndPlayer_Fast()
        {
            SetScriptingImplementation(ScriptingImplementation.Mono2x);
            BuildResourceAndPlayer();
        }

        [MenuItem("URS/Build(Resource And Player)-（IL2CPP）")]
        public static void BuildResourceAndPlayer_Standard()
        {
            SetScriptingImplementation(ScriptingImplementation.IL2CPP);
            BuildResourceAndPlayer();
        }
        
      
        
        public static void BuildResourceAndPlayer()
        {
            var versionDirectory = GetVersionDirectory();
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = versionDirectory;
            context[BuildTask.CONTEXT_COPY_STREAM_TARGET_VERSION] = URSEditorUserSettings.instance.BuildVersionCode;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
           // buildWS.EnqueueTask(new BuildTaskDiableDatabaseAutoSave());
            buildWS.EnqueueTask(new BuildTaskClearTargetVersion());
            RegisterBuildBundleTask(buildWS);
            buildWS.EnqueueTask(new BuildTaskBuildRaw());
            buildWS.EnqueueTask(new BuildTaskGenerateVersion());
            buildWS.EnqueueTask(new BuildTaskCopyLatestResourceToStreamAsset());
            buildWS.EnqueueTask(new BuilTaskAppId());
            // buildWS.EnqueueTask(new BuilTaskRecoverAssetDatabaseAutoSave());
            buildWS.EnqueueTask(new BuildTaskBuildPlayer());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/BuildPlayer")]
        public static void BuildPlayer()
        {
            var versionDirectory = GetVersionDirectory();
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskBuildPlayer());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/BuildBundleAndRaw.ThenCopyToStream")]
        public static void BuildResource()
        {
            var versionDirectory = GetVersionDirectory();
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = versionDirectory;
            context[BuildTask.CONTEXT_COPY_STREAM_TARGET_VERSION] = URSEditorUserSettings.instance.BuildVersionCode;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
           // buildWS.EnqueueTask(new BuildTaskDiableDatabaseAutoSave());
            buildWS.EnqueueTask(new BuildTaskClearTargetVersion());
            RegisterBuildBundleTask(buildWS);
            buildWS.EnqueueTask(new BuildTaskBuildRaw());
            buildWS.EnqueueTask(new BuildTaskGenerateVersion());
            buildWS.EnqueueTask(new BuildTaskCopyLatestResourceToStreamAsset());
            buildWS.EnqueueTask(new BuilTaskAppId());
            // buildWS.EnqueueTask(new BuilTaskRecoverAssetDatabaseAutoSave());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/BuildBundleAndRaw")]
        public static void BuildBundleAndRaw()
        {
            var versionDirectory = GetVersionDirectory();
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = versionDirectory;
            context[BuildTask.CONTEXT_COPY_STREAM_TARGET_VERSION] = URSEditorUserSettings.instance.BuildVersionCode;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            // buildWS.EnqueueTask(new BuildTaskDiableDatabaseAutoSave());
            buildWS.EnqueueTask(new BuildTaskClearTargetVersion());
            RegisterBuildBundleTask(buildWS);
            buildWS.EnqueueTask(new BuildTaskBuildRaw());
            buildWS.EnqueueTask(new BuildTaskGenerateVersion());
            buildWS.EnqueueTask(new BuilTaskAppId());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/BuildBundle")]
        public static void OnlyBuildBundle()
        {
            var versionDirectory = GetVersionDirectory();
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = versionDirectory;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskDiableDatabaseAutoSave());
            buildWS.EnqueueTask(new BuildTaskClearTargetVersion());
            //buildWS.EnqueueTask(new BuildTaskComplierLua());
            RegisterBuildBundleTask(buildWS);
            buildWS.EnqueueTask(new BuildTaskGenerateVersion());
            buildWS.EnqueueTask(new BuilTaskAppId());
            // buildWS.EnqueueTask(new BuildTaskCopyLatestResourceToStreamAsset());
            buildWS.EnqueueTask(new BuilTaskRecoverAssetDatabaseAutoSave());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/BuildRaw")]
        public static void BuildRawResourceCommand()
        {
            var versionDirectory = GetVersionDirectory();
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = versionDirectory;
            context[BuildTask.CONTEXT_COPY_STREAM_TARGET_VERSION] = URSEditorUserSettings.instance.BuildVersionCode;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskClearTargetVersion());
            buildWS.EnqueueTask(new BuildTaskBuildRaw());
            buildWS.EnqueueTask(new BuildTaskGenerateVersion());
            buildWS.EnqueueTask(new BuilTaskAppId());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/CopySettingVersionToStreamAsset")]
        public static void CopyResourceToStreamAsset()
        {
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = GetVersionDirectory(); ;
            context[BuildTask.CONTEXT_COPY_STREAM_TARGET_VERSION] = GetSettingCopyToStreamTargetVersion();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskCopyLatestResourceToStreamAsset());
            buildWS.EnqueueTask(new BuilTaskAppId());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/UpdateCollection")]
        public static void UpdateCollection()
        {
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskUpdateCollection());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/ClearTargetVersion")]
        public static void ClearTargetVersion()
        {
            var versionDirectory = GetVersionDirectory();
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = versionDirectory;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskClearTargetVersion());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/ShowAssetBundleBrowser")]
        public static void ShowAssetBundleBrowser()
        {
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskUpdateCollection());
            buildWS.EnqueueTask(new BuildTaskCollectAsset());
            buildWS.EnqueueTask(new BuildTaskGenerateBundleLayout());
            buildWS.EnqueueTask(new BuildTaskShowAssetBundleBrowser());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/ExportShaderVariantCollection")]
        public static void ExportShaderVariantCollection()
        {
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskUpdateCollection());
            buildWS.EnqueueTask(new BuildTaskCollectAsset());
            buildWS.EnqueueTask(new BuildTaskExportShaderVariantCollection());
            AddBuildTaskWorkSpace(buildWS);
        }

        [MenuItem("URS/CleanMaterialPropertys")]
        public static void CleanMaterialPropertys()
        {
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskUpdateCollection());
            buildWS.EnqueueTask(new BuildTaskCollectAsset());
            buildWS.EnqueueTask(new BuildTaskMaterialCleaner());
            AddBuildTaskWorkSpace(buildWS);
        }
        [MenuItem("URS/ClearBundleCache")]
        public static void ClearBundleCache()
        {
            BuildCache.PurgeCache(false);
        }

      
        [MenuItem("URS/BuildChannelRouterAndPatch")]
        public static void BuildChannelRouter()
        {
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskChannelRouter());
            buildWS.EnqueueTask(new BuildTaskBuildPatch());
            AddBuildTaskWorkSpace(buildWS);
        }

        [MenuItem("URS/BuildAppId")]
        public static void BuildAppId()
        {
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuilTaskAppId());
            AddBuildTaskWorkSpace(buildWS);
        }

        public static void RegisterBuildBundleTask(BuildTaskWorkSpace workSpace,bool combineShareBundle= true)
        {
            workSpace.EnqueueTask(new BuildTaskUpdateCollection());
            workSpace.EnqueueTask(new BuildTaskBeforeShaderComplier());
            workSpace.EnqueueTask(new BuildTaskCollectAsset());
            workSpace.EnqueueTask(new BuildTaskGenerateBundleLayout());
            workSpace.EnqueueTask(new BuildTaskBuidBundle());
            if (combineShareBundle) 
            {
                workSpace.EnqueueTask(new BuildTaskRegenerateAssetBundleName());
                workSpace.EnqueueTask(new BuildTaskGenerateBundleLayout());
                workSpace.EnqueueTask(new BuildTaskReBuildAssetBundle());
            }
            workSpace.EnqueueTask(new BuildTaskCopyAsssetBundle());
            workSpace.EnqueueTask(new BuildTaskAfterShaderComplier());
        }
        public static string GetVersionDirectory()
        {
            var setting=  URSEditorUserSettings.instance;
            return $"{GetVersionRoot()}/{setting.BuildVersionCode}";
        }

        public static string GetVersionRoot() 
        {
            var setting = URSEditorUserSettings.instance;
            return $"{GetChannelRoot()}/{setting.BuildChannel}/{URS.PlatformMappingService.GetPlatformPathSubFolder()}";
        }
        public static string GetChannelRoot()
        {
            return $"URS/channels";
        }
        public static string GetSettingCopyToStreamTargetVersion() {
            var setting = URSEditorUserSettings.instance;
            return setting.CopyToStreamTargetVersion;
        }

        public static string GetTempBundleOutDirectoryPath() 
        {
            return $"URS/temp_bundle_out/{URS.PlatformMappingService.GetPlatformPathSubFolder()}";
        }
        
    }
}
   
