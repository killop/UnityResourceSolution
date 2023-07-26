
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
using Context = System.Collections.Generic.Dictionary<string, object>;
using Debug = UnityEngine.Debug;
using URS.Editor;

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
                for (int i = _buildTaskWorkSpaces.Count - 1; i >= 0; i--)
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
            if (type == ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            }
        }

        public static bool BuildResourceAndPlayer_Fast_Config()
        {
           
            SetScriptingImplementation(ScriptingImplementation.Mono2x);
            return true;
        }
       // [MenuItem("URS/Build(Resource And Player)-（Mono）")]
        public static void BuildResourceAndPlayer_Fast(
                string buildingResourceVersionCode,
                string buildInResourceVersionCode,
                string channel
            
            )
        {
            var configSuccess = BuildResourceAndPlayer_Fast_Config();
            if (configSuccess)
            {
                BuildResourceAndPlayer(
                    buildingResourceVersionCode,
                    buildInResourceVersionCode,
                    channel
                );
            }
        }
        public static bool BuildResourceAndPlayer_Standard_Config()
        {
          
            SetScriptingImplementation(ScriptingImplementation.IL2CPP);
            return true;
        }
       
        public static void BuildResourceAndPlayer_Standard(
                string buildingResourceVersionCode,
                string buildInResourceVersionCode,
                string channel)
        {
            var configSuccess = BuildResourceAndPlayer_Standard_Config();
            if (configSuccess)
            {
                BuildResourceAndPlayer(
                    buildingResourceVersionCode,
                    buildInResourceVersionCode,
                    channel
                    );
            }
        }
        public static bool ExportAndroidProject_Mono_Config()
        {
            
            SetScriptingImplementation(ScriptingImplementation.Mono2x);
            return true;
        }
        public static void ExportAndroidProject_Mono(
                string buildingResourceVersionCode,
                string buildInResourceVersionCode,
                string channel)
        {
            var configSuccess = ExportAndroidProject_Mono_Config();
            if (configSuccess)
            {
                BuildResourceAndPlayer(
                    buildingResourceVersionCode,
                    buildInResourceVersionCode,
                    channel
                    );
            }
        }
        public static bool ExportAndroidProject_IL2CPP_Config()
        {
           
            SetScriptingImplementation(ScriptingImplementation.IL2CPP);
            return true;
        }
        public static void ExportAndroidProject_IL2CPP(
                string buildingResourceVersionCode,
                string buildInResourceVersionCode,
                string channel)
        {
            var configSuccess = ExportAndroidProject_IL2CPP_Config();
            if (configSuccess)
            {
                BuildResourceAndPlayer(
                    buildingResourceVersionCode,
                    buildInResourceVersionCode,
                    channel
                    );
            }
        }
        public static void HybridBuild(
                string buildingResourceVersionCode,
                string buildInResourceVersionCode,
                string channel,
                bool buildResourceVersion,
                bool buildRaw,
                bool copyBuildInRes,
                bool buildPlayer,
                bool debug)
        {
            var versionDirectory = GetVersionDirectory(channel, buildingResourceVersionCode);
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_ROOT_DIRECTORY] = GetVersionRoot(channel); ;
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = versionDirectory;
            context[BuildTask.CONTEXT_COPY_STREAM_TARGET_VERSION] = buildInResourceVersionCode;
            context[BuildTask.CONTEXT_BUILDING_VERSION] = buildingResourceVersionCode;
            context[BuildTask.CONTEXT_CHANNEL] = channel;
            context[BuildTask.CONTEXT_DEBUG] = debug;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            if (buildResourceVersion) 
            {
                buildWS.EnqueueTask(new BuildTaskClearTargetVersion());
                RegisterBuildBundleTask(buildWS);
                if (buildRaw)
                {
                    buildWS.EnqueueTask(new BuildTaskBuildRaw());
                }
                buildWS.EnqueueTask(new BuildTaskGenerateVersion());
            }
            
            if (copyBuildInRes) 
            {
                buildWS.EnqueueTask(new BuildTaskCopyLatestResourceToStreamAsset(buildPlayer));
            }
            buildWS.EnqueueTask(new BuildTaskChannelId());
            if (buildPlayer) 
            {
                buildWS.EnqueueTask(new BuildTaskBuildPlayer());
            }
            // buildWS.EnqueueTask(new BuilTaskRecoverAssetDatabaseAutoSave());
            
            AddBuildTaskWorkSpace(buildWS);
        }

        public static void BuildPlayerWithBuildInResource(
                string buildInResourceVersionCode,
                string channel)
        {
          //  var versionDirectory = GetVersionDirectory(channel, buildingResourceVersionCode);
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_ROOT_DIRECTORY] = GetVersionRoot(channel); ;
            context[BuildTask.CONTEXT_COPY_STREAM_TARGET_VERSION] = buildInResourceVersionCode;
            context[BuildTask.CONTEXT_CHANNEL] = channel;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskCopyLatestResourceToStreamAsset(true));
            buildWS.EnqueueTask(new BuildTaskChannelId());
            // buildWS.EnqueueTask(new BuilTaskRecoverAssetDatabaseAutoSave());
            buildWS.EnqueueTask(new BuildTaskBuildPlayer());
            AddBuildTaskWorkSpace(buildWS);
        }
        //[MenuItem("URS/BuildPlayer")]
        public static void BuildResourceAndPlayer(
                string buildingResourceVersionCode,
                string buildInResourceVersionCode,
                string channel)
        {
            HybridBuild(
                buildingResourceVersionCode,
                buildInResourceVersionCode,
                channel,
                true,
                true,
                true,
                true,
                false);
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

        public static void ValidateAsset(bool validateMaterial, bool validateModel,bool validateParticleSystem,bool validateAnimation)
        {
            var context = new Context();
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskUpdateCollection());
            buildWS.EnqueueTask(new BuildTaskCollectAsset());
            if (validateMaterial) 
            {
                buildWS.EnqueueTask(new BuildTaskValidateMaterial());
            }
            if (validateModel) {
                buildWS.EnqueueTask(new BuildTaskValidateModel());
            }
            if (validateParticleSystem)
            {
                buildWS.EnqueueTask(new BuildTaskValidateParticleSystem());
            }
            if (validateAnimation) 
            {
                buildWS.EnqueueTask(new BuildTaskValidateAnimation());
            }
           
            AddBuildTaskWorkSpace(buildWS);
        }
      
        [MenuItem("URS/ClearBundleCache")]
        public static void ClearBundleCache()
        {
            BuildCache.PurgeCache(false);
        }
       
        public static void BuildAutoChannelVersionsAndUploadCDN(
               string channel,
               string channelTargetVersion = "",
               int versionKeepCount=4,
               bool uploadCDN=false,
               bool debug = false)
        {
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_ROOT_DIRECTORY] = GetVersionRoot(channel); ;
            context[BuildTask.CONTEXT_CHANNEL] = channel;
            context[BuildTask.CONTEXT_CHANNEL_TARGET_VERSION] = channelTargetVersion;
            context[BuildTask.CONTEXT_VERSION_KEEP_COUNT] = versionKeepCount;
            context[BuildTask.CONTEXT_DEBUG] = debug;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildChannelVersions());
            buildWS.EnqueueTask(new BuildTaskAutoAppVersionRouter());
            if (uploadCDN)
            {
                buildWS.EnqueueTask(GenUploadCdnBuildTask(channel));
            }
            
            AddBuildTaskWorkSpace(buildWS);
        }
        


        [MenuItem("URS/BuildAppId")]
        public static void BuildChannelId(
                string buildingResourceVersionCode,
                string buildInResourceVersionCode,
                string channel)
        {
            var versionDirectory = GetVersionDirectory(channel, buildingResourceVersionCode);
            var context = new Context();
            context[BuildTask.CONTEXT_VERSION_ROOT_DIRECTORY] = GetVersionRoot(channel); ;
            context[BuildTask.CONTEXT_VERSION_DIRECTORY] = versionDirectory;
            context[BuildTask.CONTEXT_COPY_STREAM_TARGET_VERSION] = buildInResourceVersionCode;
            context[BuildTask.CONTEXT_CHANNEL] = channel;
            var buildWS = new BuildTaskWorkSpace();
            buildWS.Init(context);
            buildWS.EnqueueTask(new BuildTaskChannelId());
            AddBuildTaskWorkSpace(buildWS);
        }


        public static void RegisterBuildBundleTask(BuildTaskWorkSpace workSpace, bool combineShareBundle = true)
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
                workSpace.EnqueueTask(new BuildTaskOptimizeShareAssetBundleName());
                workSpace.EnqueueTask(new BuildTaskReBuildAssetBundle());
            }
            workSpace.EnqueueTask(new BuildTaskCopyAsssetBundle());
            workSpace.EnqueueTask(new BuildTaskAfterShaderComplier());
        }
        private static string GetVersionDirectory( string channel,string buildingResourceVersionCode)
        {
           // var setting = URSEditorUserSettings.instance;
            return $"{GetVersionRoot(channel)}/{buildingResourceVersionCode}";
        }

        private static string GetVersionRoot(string channel)
        {
           // var setting = URSEditorUserSettings.instance;
            return $"{GetChannelRoot()}/{channel}/{URS.PlatformMappingService.GetPlatformPathSubFolder()}";
        }
        public static string GetChannelRoot()
        {
            return $"URS/channels";
        }

        public static string GetBuildInResourceTempFolder()
        {
            return $"URS/temp_buildin_resource";
        }

        public static string GetTempBundleOutDirectoryPath() 
        {
            return $"URS/temp_bundle_out/{URS.PlatformMappingService.GetPlatformPathSubFolder()}";
        }

        public static string GetVersionHistoryFolder(string channel) 
        {
            return $"URS/version_history/{channel}/{URS.PlatformMappingService.GetPlatformPathSubFolder()}";
        }

        public static string GetShareAssetBundleNameConfigFilePath() 
        {
            return $"URS/ShareAssetBundleName.json";
        }
        
        public static void ExportDefaultURSRuntimeSetting(string SaveDiretoryPath)
        {
            var defaultSetting = new URSRuntimeSetting();
            var content= UnityEngine.JsonUtility.ToJson(defaultSetting,true);
            var savePath = $"{SaveDiretoryPath}/{URSRuntimeSetting.SAVE_RESOUCE_PATH}";
            if (File.Exists(savePath)) { 
                File.Delete(savePath);
            }
            File.WriteAllText(savePath, content );
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.AssetDatabase.SaveAssets();
        }
    }
}
   
