using System.Collections.Generic;
using System.IO;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal class AssetSearchTableItem : AssetTableItem
    {
        public readonly string friendlyType;
        public readonly Texture2D typeIcon;
        public string assetBundleName;
        public IList<AssetImportProfile> importProfiles;
        public string importProfileNames;

        public AssetProfileState importProfileState;
        public List<string> missingProcessors = new List<string>();

        internal AssetSearchTableItem(int id, int depth, string displayName, string assetPath, bool isAsset) : base(id, depth, displayName, assetPath, isAsset)
        {
            friendlyType = AssetImportPipeline.GetAssetType(assetPath);
            typeIcon = InternalEditorUtility.FindIconForFile(assetPath);

            if (isAsset)
            {
                importProfiles = AssetImportProfile.GetProfileMatches(assetPath);
                UpdateProfileState();
            }
            else
            {
                importProfiles = new List<AssetImportProfile>();
            }

            importProfileNames = string.Join(", ", importProfiles.Select(x => x.name));

            if (!typeIcon && assetType != null)
            {
                typeIcon = (Texture2D) UnityEditorDynamic.EditorGUIUtility.FindTextureByType(assetType);
            }

            if (!typeIcon)
            {
                typeIcon = icon;
            }
        }

        public void UpdateProfileState()
        {
            importProfileState = AssetProfileState.NoImportProfile;
            missingProcessors.Clear();
            if (!isAsset || importProfiles.Count == 0 || !importProfiles.Any(x => x))
            {
                return;
            }

            importProfileState = AssetProfileState.NoMatchingFilters;
            if (!IsValidFileExtension())
            {
                importProfileState = AssetProfileState.UnknownFileExtension;
                return;
            }

            foreach (var profile in importProfiles)
            {
                if (!profile.enabled)
                {
                    importProfileState = AssetProfileState.ProfileDisabled;
                    continue;
                }

                ImportProfileUserData userData = null;
                var processorCount = 0;
                var enabledProcessorCount = 0;
                var enabledFilterCount = 0;
                var matchingFilterCount = 0;
                foreach (var filter in profile.assetFilters)
                {
                    if (filter.IsMatch(assetPath, false))
                    {
                        matchingFilterCount++;
                        if (!filter.enabled)
                        {
                            continue;
                        }

                        enabledFilterCount++;
                        if (userData == null)
                        {
                            userData = new ImportProfileUserData(assetPath);
                        }

                        processorCount += filter.assetProcessors.Count;
                        foreach (var processor in filter.assetProcessors)
                        {
                            if (!processor.enabled)
                            {
                                continue;
                            }

                            enabledProcessorCount++;
                            if (!processor.IsConfigOK(userData.GetAssetImporter()) ||!userData.HasProcessor(processor))
                            {
                                missingProcessors.Add($"Processor: {processor.GetName()}\nFilter: {filter.file.pattern}");
                            }
                        }
                    }
                }

                if (matchingFilterCount == 0)
                {
                    importProfileState = AssetProfileState.NoMatchingFilters;
                }
                else if (enabledFilterCount == 0)
                {
                    importProfileState = AssetProfileState.FiltersDisabled;
                }

                if (userData != null)
                {
                    if (processorCount == 0)
                    {
                        importProfileState = AssetProfileState.NoProcessors;
                    }
                    else if (enabledProcessorCount == 0)
                    {
                        importProfileState = AssetProfileState.ProcessorsDisabled;
                    }
                    else if (missingProcessors.Count > 0)
                    {
                        importProfileState = AssetProfileState.ProcessorsNotApplied;
                    }
                    else
                    {
                        importProfileState = AssetProfileState.Good;
                    }
                }
            }
        }

        bool IsValidFileExtension()
        {
            var isValidExtension = false;
            var fileExtension = Path.GetExtension(assetPath);
            foreach (var profile in importProfiles)
            {
                foreach (var filter in profile.assetFilters)
                {
                    if (filter.assetType.IsValidFileExtension(fileExtension, filter.otherAssetExtensions))
                    {
                        isValidExtension = true;
                        break;
                    }
                }
            }

            return isValidExtension;
        }

        public void ApplyMissingProcessors(bool forceApply = false)
        {
            if (forceApply)
            {
                AssetProcessor.SetForceApply(assetPath, true);
            }

            AssetDatabase.ImportAsset(assetPath, forceApply ? ImportAssetOptions.ForceUpdate : ImportAssetOptions.Default);
            UpdateProfileState();
            Refresh();
        }
    }
}