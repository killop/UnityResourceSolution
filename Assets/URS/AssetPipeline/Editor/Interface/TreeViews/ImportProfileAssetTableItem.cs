using System.Collections.Generic;
using System.IO;
using Daihenka.AssetPipeline.Import;
using UnityEditor;

namespace Daihenka.AssetPipeline
{
    internal class ImportProfileAssetTableItem : AssetTableItem
    {
        public AssetProfileState profileState;
        public List<string> missingProcessors = new List<string>();
        public readonly AssetImportProfile profile;

        internal ImportProfileAssetTableItem(int id, int depth, string displayName, string assetPath, bool isAsset, AssetImportProfile profile) : base(id, depth, displayName, assetPath, isAsset)
        {
            this.profile = profile;
            if (isAsset && profile)
            {
                UpdateProfileState();
            }
        }

        public void UpdateProfileState()
        {
            profileState = AssetProfileState.NoMatchingFilters;
            missingProcessors.Clear();
            if (!isAsset || !profile)
            {
                return;
            }

            if (!IsValidFileExtension())
            {
                profileState = AssetProfileState.UnknownFileExtension;
                return;
            }


            if (!profile.enabled)
            {
                profileState = AssetProfileState.ProfileDisabled;
                return;
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
                profileState = AssetProfileState.NoMatchingFilters;
            }
            else if (enabledFilterCount == 0)
            {
                profileState = AssetProfileState.FiltersDisabled;
            }

            if (userData != null)
            {
                if (processorCount == 0)
                {
                    profileState = AssetProfileState.NoProcessors;
                }
                else if (enabledProcessorCount == 0)
                {
                    profileState = AssetProfileState.ProcessorsDisabled;
                }
                else if (missingProcessors.Count > 0)
                {
                    profileState = AssetProfileState.ProcessorsNotApplied;
                }
                else
                {
                    profileState = AssetProfileState.Good;
                }
            }
        }

        bool IsValidFileExtension()
        {
            var isValidExtension = false;
            var fileExtension = Path.GetExtension(assetPath);
            foreach (var filter in profile.assetFilters)
            {
                if (filter.assetType.IsValidFileExtension(fileExtension, filter.otherAssetExtensions))
                {
                    isValidExtension = true;
                    break;
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