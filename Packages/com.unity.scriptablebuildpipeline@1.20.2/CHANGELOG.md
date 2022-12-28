# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.20.2] - 2022-05-24
- Fix an issue where Scene build ordering would cause Scenes to not load.

## [1.20.1] - 2022-05-03
- Fix an issue where cached Sprite state could be stale.

## [1.20.0] - 2022-04-22
- Adds build support for Content Files.
    - New public methods.
        - DefaultBuildTasks.ContentFileCompatible
    - New public classes.
        - ClusterOutput
        - ContentFileIdentifiers

## [1.19.6] - 2021-10-21
- Fixed an issue where the Build Progress Bar would not go away after the build is complete.

## [1.19.5] - 2021-10-21
- Fixed an issue where MonoScript bundle will attempt to pull in data it should not have.
- Fixed an issue where NONRECURSIVE_DEPENDENCY_DATA would not properly calculate Shader Variants from Scenes.
- Improved ArchiveAndCompress path handling to detect too long paths and log errors.
- Improve performance of UpdateBundleObjectLayout build task.
- Improved BuildCache to rebuild cache entries in case of exceptions instead of failing the build.

## [1.19.3] - 2021-09-14
- Fixed an edge case where Link.xml ordering was not deterministic causing incremental player rebuilds to occur unnecessarily.
- Fixed an issue where Cache Server integration was not update to use USerializer.

## [1.19.2] - 2021-07-20
- Fixed an edge case where moving a scene would fail the build.
- Added additional details to the Trace Event Profiler output where the data was missing or unclear.
- Fixed an issue where NONRECURSIVE_DEPENDENCY_DATA would return NonRecursive bundle dependencies instead of the expected Recursive bundle dependencies.
- Updated version define for ENABLE_TYPE_HASHING to match backported Unity 2020.2.2f1 version.
- Fixed USerialize bug with handling of Type[] containing null values.
- Fixed an issue where the hash for Scene bundles would be calculated with a few missing bytes, returning an incorrect hash.
- Fixed an edge case with NONRECURSIVE_DEPENDENCY_DATA in which Scene Bundles would be unable to load MonoScripts and log Missing Behaviour warnings at runtime.
- New Project Behavior Change: PrefabPacked bundles now use a header size of 2 bytes instead of 4 bytes to reduce file identifier collision frequency in large projects.
  - Previous behavior can be restored via the Scriptable Build Pipeline Preferences window.
- New Preference: FileID Generator Seed allows you to set a seed for file identifier generation to avoid project specific collisions.

## [1.19.1] - 2021-06-04
- Improved performance of the GenerateBundlePacking build task.
- Updated version define for NONRECURSIVE_DEPENDENCY_DATA to match backported Unity 2019.4.19f1 version.

## [1.19.0] - 2021-05-20
- Replaced our use of BinaryFormatter with new "USerialize"
	- USerializer performance in synthetic tests is about 40x faster than BinaryFormatter
	- Real world project tests have seen about 1.8x improvement of cold cache build times, and about 6x improvement of warm cache build times.
- Fixed a case where internal type hash was not being cached correctly causing constant cold cache hits.
- Fixed a case where previous build results could influence a new build incorrectly by including the wrong dlls.
- Fixed a case where multiple scenes in the same asset bundle could generate invalid or incorrect dependencies and not load all necessary data.
- Minor fix for native tep profiling results to separate the event name from the event context and to properly string escape the context.
- Added the DisableVisibleSubAssetRepresentations build parameter. 

## [1.18.0] - 2021-04-08
- Added an option to build MonoScripts into their own bundle reducing duplication and potential loading errors on certain project setups.
- Added a type remap in Link.xml generation for UnityEditor.MonoScript to the correct runtime type.
- Added an option to build bundles using Non-Recursive Dependency calculation methods. 
  - This approach helps reduce asset bundle rebuilds and runtime memory consumption.

## [1.17.0] - 2021-03-03
- Added [VersionedCallback] attribute for flagging build impacting changes to IProcessScene, IProcessSceneWithReport, IPreprocessShaders, and IPreprocessComputeShaders callbacks.
- Fixed an IndexOutOfRange exception thrown by the GenerateSubAssetPathMaps build task.
- Added faster code paths for common hashing operations.
- 2019.4+ added additional threading usage for saving BuildCache data.
- Fixed an edge case where SerializeReference types used across assemblies were being code stripped incorrectly.
- Fixed a false positive cache hit when changing Player Setting's Graphics APIs .

## [1.16.1] - 2021-01-27
- Handling of communication error with cache server. Build will now continue, using the local cache only.
- Regression fix for index out of range error on Unity 2018.4

## [1.16.0] - 2020-10-29
- Added caching support for DOTS Subscene building
- Fixed an issue where DOTS Subscene lighting information was lost

## [1.15.2] - 2021-01-20
- Fixes for automated testing

## [1.15.1] - 2020-10-29
- Added support for per type caching and incremental rebuild triggers

## [1.14.0] - 2020-10-21
- Added API to build player scripts to a separate location from Temp or Output Folders.

## [1.13.1] - 2020-09-24
- Fixed an edge case where changing PlayerSettings.mipStripping did not rebuild asset bundles as required.
- Fixed an edge case where changing QualitySettings.maximumLODLevel did not rebuild scene bundles as required.
- Reduced unnecessary bundle rebuilds due to too much data in BuildReferenceMap
- Removed unnecessary memory overhead when hashing large data sets for caching.
- Fixed SpookyHash and improved it's performance when used on Unity 2020.1 and greater versions.
- SpookyHash will be the default hashing method in Scriptable Build Pipeline on Unity 2021.1 and greater.
- Contiguous Bundles will be Opt-Out in Addressables & Scriptable Build Pipeline in Unity 2021.1 and greater.

## [1.12.0] - 2020-09-15
- Improved caching performance of the WriteSerializedFile build task with projects using many Prefabs in Scenes.

## [1.11.2] - 2020-08-24
- Improved thread handling of the Cache Save, Upload, and Prune operations.

## [1.11.1] - 2020-08-11
- Exposed the ScriptableBuildPipeline static class to allow setting per project properties from script.
- Fixed an edge case where pruning the build cache would not run in Unity's batchmode.
- Added Cache Server Config options to the Scriptable Build Pipeline UI.

## [1.10.0] - 2020-07-28
- Added IBundleBuildParameters.ContiguousBundles option, which when enabled will improve asset loading times.
  - In testing, performance improvements varied from 10% improvement over all, with improvements up to 50% for large complex assets such as extensive UI prefabs.
- Updated HashingMethods to support Unicode string hashing.

## [1.9.0] - 2020-06-17
- Fixed a null reference exception in GenerateBundleCommands.cs when attempting to sort an empty list
- LinkXmlGenerator moved to the Scriptable Build Pipeline package in the UnityEditor.Build.Pipeline.Utilities namespace.
- Added new option WriteLinkXML to BuildParameters to write out a link.xml file containing the type information used in the asset bundles for use in the Unity manage code stripping system.
- Improved performance of the GenerateBundlePacking task.
- Adding the IBuildLogger interface and BuildLog class to capture high-level build performance data, and output it to the Trace Event Format.

## [1.8.6] - 2020-06-11
- Improve caching performance of the WriteSerializedFiles task
- Fixed bug where asset bundles fail to build when not using build cache.
- Fixed an issue where providing additional files for asset bundles required the internal name instead of the bundle name

## [1.8.4] - 2020-05-28
- Updated CalculateAssetDependencyData to use a new fast path API for working with Asset Representations in 2020.2 and onward.
- Fix issue with backslashes in trace event profiler build log report

## [1.8.2] - 2020-05-21
- Improve incremental build performance. Avoid copying archives from the build cache when source and destination creation timestamps are identical.
- Fix caching bug caused by an engine SpookyHash bug. MD5 hashing will be used until this issue is resolved.
- Improve caching performance of the ArchiveAndCompressBundles task

## [1.7.3] - 2020-05-20
- Fix caching bug caused by an engine SpookyHash bug. MD5 hashing will be used until this issue is resolved.

## [1.7.2] - 2020-04-07
- Merged in DOTS specific functionality into SBP core.
- Scriptable Build Pipeline settings now stored in ProjectSettings/ScriptableBuildPipeline.json
- Added option to remove extended debugging information from WriteResults before caching for better cache performance
- Added option to log Cache Misses to the console
- Switched to SpookyHash for Unity 2019.3 and higher for most hashing methods to edge out just a bit more performance
- Added multi-threading support to the archive and compress task

## [1.6.5-preview] - 2020-03-06
- Updated SBP DOTs preview version with latest SBP Release changes.

## [1.6.4-preview] - 2020-02-07
- Updated SBP DOTs preview version with latest SBP Release changes.

## [1.6.3-preview] - 2019-09-13
- Fixed an issue where switching platforms caused Scene & Shader callbacks to no longer be called
- Improved error messaging when a task fails with an exception
- Removed ENABLE_SUBSCENE_IMPORTER define as everything has landed as of 2019.3.0b5

## [1.6.2-preview] - 2019-09-13
- Refactor of ImportedContent to be more flexible for adding custom content

## [1.6.1-preview] - 2019-09-12
- Added check for define ENABLE_SUBSCENE_IMPORTER

## [1.6.0-preview] - 2019-09-09
- Added support for DOTS SubScene Importer based asset bundles via ImportedContent property
- Added support for adding custom raw files to asset bundles via AddionalFiles property

## [1.5.11] - 2020-03-05
- Fixed poor performance of GenerateBundleCommands with large data sets.

## [1.5.10] - 2020-03-13
- Fixed issue where asset bundles in the build cache weren't having the correct bundle hash assigned to it.

## [1.5.9] - 2020-02-28
- Updated CompatibilityAssetBundleManifest so hash version is properly serializable.
- Renamed "Build Cache" options in the Preferences menu to "Scriptable Build Pipeline"
- Improved performance of the Scriptable Build Pipeline's archiving task.

## [1.5.7] - 2020-02-07
- Updated code to remove obsolete code when used with Unity 2020.1 and newer.

## [1.5.6] - 2020-01-30
- Fixed an issue where texture sources for non-packed sprites were being stripped incorrectly.

## [1.5.5] - 2020-01-21
- Fixed an issue where texture sources for sprites were not being stripped from the build.
- Fixed an issue where scene changes weren't getting picked up in a content re-build.

## [1.5.4] - 2019-10-03
- Fixed an edge case where Optimize Mesh would not apply to all meshes in the build.
- Fixed an edge case where Global Usage was not being updated with latest values from Graphics Settings.

## [1.5.3] - 2019-09-10
- Fixed Scene Bundles not rebuilding when included prefab changes.

## [1.5.2] - 2019-07-19
- Fixed ToString() method of CompatibilityAssetBundleManifest to properly add new line characters and section header where multiple dependencies exist

## [1.5.1] - 2019-07-15
- remove preview tag

## [1.5.0-preview] - 2019-06-13
- Updated for API compatibility with Unity 2019.3
- Moved CacheServerClient package into SBP.  It turns out this package was not as globally useful as we thought, and we are pulling it in to ease support and discoverability.

## [1.4.1-preview] - 2019-04-16
- Fixed "Path is empty" exception in build cache
- Fixed an edge case where a valid cache entry could be returned for an invalid request
- Added BuildCache.PruneCache API to trim the cache down to a limit, called in the background after a build
- Moved BuildCache menu options and preferences to the "Edit/Preferences..." window
- Added SBP_PROFILER_ENABLE define to enable per task profiling output to console
- Fixed an issue preventing PrefabPackedIdentifiers from being passed into ContentPipeline.BuildAssetBundles

## [1.3.5-preview] - 2019-02-28
- Minimum Unity version is now 2018.3 to address a build-time bug with progressive lightmapper.
- Added missing version into CacheEntry calculation
- Fixed build error causing AssetBundle.LoadAssetWithSubAssets to return partial results
- Updated function implementations to be virtual for overriding
- Added GetOutputFilePathForIdentifier function to generate the final file path for a given build identifier

## [1.2.2-preview] - 2018-12-19
- Fixed SpritePacker failing to pack SpriteAtlas objects into AssetBundles

## [1.2.1-preview] - 2018-12-14
- Fixed a null reference error in GenerateBundleCommands::GetSortIndex

## [1.2.0-preview] - 2018-11-29
- Renamed LegacyBuildPipeline & LegacyAssetBundleManifest to CompatibilityBuildPipeline & CompatibilityAssetBundleManifest.
- Moved CompatibilityAssetBundleManifest & BundleDetails into a new runtime assembly: com.unity.scriptablebuildpipeline.
- Changed CompatibilityAssetBundleManifest to inherit from ScriptableObject and updated it and BundleDetails to properly serialize using Unity's serialization systems.

## [1.1.1-preview] - 2018-10-20
- Reduced object duplication for scene asset bundles
- Fixed object order for scene asset bundles not being deterministic
- Fixed Shader Stripping values from Graphics Settings not applying to built data
- Fixed various code warnings for Unity 2018.3 and newer version
- Added forcing asset save on build

## [1.1.0-preview] - 2018-10-01
- Fixed an issue where a string hash was being used instead of a file hash causing data to not rebuild

## [1.0.1-preview] - 2018-08-24
- removed compile warning
- Fixed an issue where we were not using the addressableNames field of the AssetBundleBuild struct

## [1.0.0-preview] - 2018-08-20
- Fixed an issue in  ArchiveAndCompressBundles where previous output location was being used failing to copy cached bundles to the new location
- Fixed an issue in BuildCache were built in plugin dlls did not have a hash version causing cache misses
- Fixed invalid access errors when reading access controlled files for hashing.
- Fixed an issue where you could not force rebuild asset bundles using LegacyBuildPipeline
- Implemented IEquatable<T> on public structs
- Breaking API Change: LegacyBuildPipeline.BuildAssetBundles now returns LegacyAssetBundleManifest
    - LegacyAssetBundleManifest's API is identical to AssetBundleManifest

## [0.2.0-preview] - 2018-07-23
- Removed ProjectInCleanState & ValidateBundleAssignments tasks and integrated them directly indo the data validation or running methods
- Added build task to append hash to asset bundle files
- Large rework of how IBuildTasks are implemented. Now using dependency injection to handle passing data.
- Added reusable BuildUsageCache for usage tag calculation performance improvements
- - Unity minimum version now 2018.2.0b9
- Improved asset bundle hash version calculation to be unity version agnostic

## [0.1.0-preview] - 2018-06-06
- Added support for Cache Server integration of the Build Cache
- Refactored Build Cache internals for even more performance gains

## [0.0.15-preview] - 2018-05-21
- Hardened build cache against failures due to invalid data

## [0.0.14-preview] - 2018-05-03
- temporarily removed progress bar as it causes a recompile on mac.  Will attempt to re-add selectively later.

## [0.0.13-preview] - 2018-05-03
- fixed hash serialization bug.

## [0.0.12-preview] - 2018-05-02
- Added build task for generating extra data for sprite loading edge case

## [0.0.11-preview] - 2018-05-01
- Updated BuildAssetBundles API to also take custom context objects
- Added support for extracting Built-In Shaders to a common bundle

## [0.0.10-preview] - 2018-04-26
- Added BuildAssetBundles API that takes a custom task list
- Added null checks for BuildSettings.typeDB & AssetBundleBuild.addressableNames

## [0.0.9-preview] - 2018-04-04
- Added documentation for IWriteOperation implementations
- Added documentation for ReturnCodes, LegacyBuildPipeline, and ContentPipeline
- Ran Unity code analysis & cleaned up warnings (boxing, performance issues, name consistency)
- Breaking api change: Changed build tasks' public static run method to private.
- Added null checks and ArgumentNullExceptions

## [0.0.8-preview] - 2018-03-27
- Test rename & meta file cleanup
- Added documentation for shared classes / structs
- Updated inconsistent interface / class names
- Added missing parameter to IBuildParameters
- Ran spell check
- Moved IWriteOperation to Interfaces
- Update IWriteOperation properties to PascalCase
- Added IWriteOperation documentation

## [0.0.6-preview] - 2018-03-20
- doc updates

## [0.0.5-preview] - 2018-02-08
- Initial submission for package distribution
