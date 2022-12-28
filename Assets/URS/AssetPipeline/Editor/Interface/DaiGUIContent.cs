using UnityEditor;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class DaiGUIContent
    {
        public static readonly GUIContent developerMode = new GUIContent("Developer Mode");
        public static readonly GUIContent userMode = new GUIContent("User Mode");
        public static readonly GUIContent ignoreCase = new GUIContent("", "Ignore Case");
        public static readonly GUIContent iconToolbarPlus = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add");
        public static readonly GUIContent notPowerOfTwoWarning = EditorGUIUtility.TrTextContent("This scale will produce a Variant Sprite Atlas with a packed Texture that is NPOT (non - power of two). This may cause visual artifacts in certain compression/Texture formats.");
        public static readonly GUIContent destination = new GUIContent("Destination");
        public static readonly GUIContent materialNameFilter = new GUIContent("Material Name Filter");
        public static readonly GUIContent importProfile = new GUIContent("Import Profile");
        public static readonly GUIContent importProfiles = new GUIContent("Import Profiles");
        public static readonly GUIContent openImportProfile = new GUIContent("Open Import Profile");
        public static readonly GUIContent openAssetsViewer = new GUIContent("Open Assets Viewer");
        public static readonly GUIContent noValidAssetProcessorsAvailable = new GUIContent("No valid asset processors available");
        public static readonly GUIContent folder = new GUIContent("Folder");
        public static readonly GUIContent packedTextureName = new GUIContent("Packed Texture Name");
        public static readonly GUIContent applyRecursively = new GUIContent("Apply Recursively");
        public static readonly GUIContent lightProbeUsage = new GUIContent("Light Probe Usage");
        public static readonly GUIContent reflectionProbeUsage = new GUIContent("Reflection Probe Usage");
        public static readonly GUIContent shadowCastingMode = new GUIContent("Shadow Casting Mode");
        public static readonly GUIContent allowOcclusionWhenDynamic = new GUIContent("Allow Occlusion When Dynamic");
        public static readonly GUIContent receiveShadows = new GUIContent("Receive Shadows");
        public static readonly GUIContent contributeGI = new GUIContent("Contribute GI");
        public static readonly GUIContent createAnchorOverride = new GUIContent("Create Anchor Override");
        public static readonly GUIContent receiveGI = new GUIContent("Receive GI");
        public static readonly GUIContent motionVectorGenerationMode = new GUIContent("Motion Vector Generation Mode");
        public static readonly GUIContent runOnEveryImport = new GUIContent("Run On Every Import", "If disabled, this will only run on the first import");
        public static readonly GUIContent editProfile = new GUIContent("Edit Profile");
        public static readonly GUIContent openProfileAssetsViewer = new GUIContent("Open Profile Assets Viewer");
        public static readonly GUIContent selectProfileAsset = new GUIContent("Select Profile Asset");
        public static readonly GUIContent deleteProfile = new GUIContent("Delete Profile");
        public static readonly GUIContent name = new GUIContent("Name");
        public static readonly GUIContent path = new GUIContent("Path");
        public static readonly GUIContent assetTypes = new GUIContent("Asset Types");
        public static readonly GUIContent assetProcessors = new GUIContent("Asset Processors");
        public static readonly GUIContent applyMissingProcessors = new GUIContent("Apply Missing Processors");
        public static readonly GUIContent createFilter = new GUIContent("Create Filter");
        public static readonly GUIContent forceApplyAllProcessors = new GUIContent("Force Apply All Processors");
        public static readonly GUIContent actions = new GUIContent("Actions");
        public static readonly GUIContent status = new GUIContent("Status");
        public static readonly GUIContent isValid = new GUIContent("Is Valid?");
        public static readonly GUIContent replaceReferencesOfDuplicatesWithThisAsset = new GUIContent("Replace references of duplicates with this asset");
        public static readonly GUIContent deleteAsset = new GUIContent("Delete asset");
        public static readonly GUIContent deleteAssetAndChildren = new GUIContent("Delete asset and children");
        public static readonly GUIContent type = new GUIContent("Type");
        public static readonly GUIContent assetBundleName = new GUIContent("AssetBundle Name");
        public static readonly GUIContent assetBundleVariant = new GUIContent("AssetBundle Variant");
        public static readonly GUIContent infoIcon = EditorGUIUtility.TrIconContent(infoIconName, "Info");
        public static readonly GUIContent warningIcon = EditorGUIUtility.TrIconContent(warnIconName, "Warning");
        public static readonly GUIContent errorIcon = EditorGUIUtility.TrIconContent(errorIconName, "Error");
        public const string infoIconName = "console.infoicon";
        public const string warnIconName = "console.warnicon";
        public const string errorIconName = "console.erroricon";
    }
}