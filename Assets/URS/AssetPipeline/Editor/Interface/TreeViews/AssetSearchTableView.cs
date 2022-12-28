using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace Daihenka.AssetPipeline
{
    internal class AssetSearchTableView : AAssetTableView
    {
        enum SortOption
        {
            Path,
            Type,
            ImportProfile,
            ImportProfileStatus,
            ImportProfileActions,
            AssetBundleName
        }

        enum Columns
        {
            Path,
            Type,
            ImportProfile,
            ImportProfileStatus,
            ImportProfileActions,
            AssetBundleName
        }

        public enum Filter
        {
            None,
            All,
            ImportProfile,
            NoImportProfile,
            Unused,
            Duplicates,
            References
        }

        readonly SortOption[] m_SortOptions = {SortOption.Path, SortOption.Type, SortOption.ImportProfile, SortOption.ImportProfileStatus, SortOption.ImportProfileActions, SortOption.AssetBundleName};
        public string filter;
        public Filter assetsFilter = Filter.None;
        public string[] assetPaths;
        static Dictionary<string, string[]> assetBundleAssetPaths = new Dictionary<string, string[]>();

        public AssetSearchTableView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(Columns)).Length, "Ensure number of sort options are in sync with number of Columns enum values");
        }

        static void CacheAssetBundleInfo()
        {
            assetBundleAssetPaths.Clear();

            var assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (var bundleName in assetBundleNames)
            {
                assetBundleAssetPaths.Add(bundleName, AssetDatabase.GetAssetPathsFromAssetBundle(bundleName));
            }
        }

        static string GetAssetBundleNameForAssetPath(string assetPath)
        {
            foreach (var kvp in assetBundleAssetPaths)
            {
                if (kvp.Value.Contains(assetPath))
                {
                    return kvp.Key;
                }
            }

            return AssetImporter.GetAtPath(assetPath).assetBundleName;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return assetsFilter == Filter.Unused;
        }

        protected override void ContextClickedItem(int id)
        {
            var rows = FindRows(new[] {id});
            var item = rows.FirstOrDefault();

            var assetItem = (AssetSearchTableItem) item;
            if (assetItem != null)
            {
                var menu = new GenericMenu();
                if (assetsFilter == Filter.Duplicates)
                {
                    menu.AddItem(DaiGUIContent.replaceReferencesOfDuplicatesWithThisAsset, false, ReplaceDuplicateAssets, id);
                }

                if (assetItem.isAsset && assetItem.importProfiles.Count > 0)
                {
                    menu.AddItem(DaiGUIContent.openImportProfile, false, OpenImportProfile, assetItem.importProfiles[0]);
                    if (assetItem.importProfileState == AssetProfileState.ProcessorsNotApplied)
                    {
                        menu.AddItem(DaiGUIContent.applyMissingProcessors, false, ApplyProcessorsToItems, new Tuple<bool, AssetSearchTableItem>(false, assetItem));
                        menu.AddSeparator("");
                    }

                    menu.AddItem(DaiGUIContent.forceApplyAllProcessors, false, ApplyProcessorsToItems, new Tuple<bool, AssetSearchTableItem>(true, assetItem));
                    menu.AddSeparator("");
                }

                menu.AddItem(DaiGUIContent.deleteAsset, false, DeleteAsset, HasSelection() ? GetSelection() : new[] {id});
                if ((assetsFilter == Filter.Duplicates || assetsFilter == Filter.Unused) && item.hasChildren)
                {
                    menu.AddItem(DaiGUIContent.deleteAssetAndChildren, false, DeleteAssetAndChildren, HasSelection() ? GetSelection() : new[] {id});
                }

                menu.ShowAsContext();
            }
        }

        void ApplyProcessorsToItems(object userdata)
        {
            var (isForce, item) = (Tuple<bool, AssetSearchTableItem>) userdata;
            if (item == null)
            {
                return;
            }

            try
            {
                item.ApplyMissingProcessors(isForce);
            }
            catch
            {
                // ignored
            }
        }

        void OpenImportProfile(object userdata)
        {
            var profile = (AssetImportProfile) userdata;
            ImportProfileWindow.ShowWindow(profile);
        }

        void DeleteAssetAndChildren(object userdata)
        {
            var ids = (int[]) userdata;
            var rows = FindRows(ids);
            if (EditorUtility.DisplayDialog("Delete Assets", $"Are you sure you want to delete this asset and listed children?", "Yes", "No"))
            {
                foreach (var row in rows)
                {
                    var item = (AssetSearchTableItem) row;
                    if (item == null || !item.isAsset || string.IsNullOrEmpty(item.assetPath))
                    {
                        continue;
                    }

                    AssetDatabase.DeleteAsset(item.assetPath);
                    AssetReferenceUtility.RemoveAssetFromCache(item.assetPath);
                    foreach (var treeViewItem in item.children)
                    {
                        var child = (AssetSearchTableItem) treeViewItem;
                        if (child == null || !child.isAsset || string.IsNullOrEmpty(child.assetPath))
                        {
                            continue;
                        }

                        AssetDatabase.DeleteAsset(child.assetPath);
                        AssetReferenceUtility.RemoveAssetFromCache(child.assetPath);
                    }
                }

                Reload();
            }
        }

        void DeleteAsset(object userdata)
        {
            var ids = (int[]) userdata;
            var rows = FindRows(ids);
            if (EditorUtility.DisplayDialog("Delete Asset", "Are you sure you want to delete this asset?", "Yes", "No"))
            {
                foreach (var row in rows)
                {
                    var item = (AssetSearchTableItem) row;
                    if (item == null)
                    {
                        continue;
                    }

                    AssetDatabase.DeleteAsset(item.assetPath);
                    AssetReferenceUtility.RemoveAssetFromCache(item.assetPath);
                }

                Reload();
            }
        }

        void ReplaceDuplicateAssets(object userdata)
        {
            var id = (int) userdata;
            var rows = FindRows(new[] {id});
            var item = (AssetSearchTableItem) rows.FirstOrDefault();
            if (item == null)
            {
                return;
            }

            if (EditorUtility.DisplayDialog("Replace Duplicate Asset References", "Are you sure you want to replace references for the duplicate assets?", "Yes", "No"))
            {
                var duplicateIds = new List<int>();
                if (item.parent.id != -1)
                {
                    duplicateIds.Add(item.parent.id);
                    foreach (var child in item.parent.children)
                    {
                        if (child.id == item.id)
                        {
                            continue;
                        }

                        duplicateIds.Add(child.id);
                    }
                }

                if (item.hasChildren)
                {
                    foreach (var child in item.children)
                    {
                        duplicateIds.Add(child.id);
                    }
                }

                var duplicateItems = FindRows(duplicateIds.ToArray()).Cast<AssetSearchTableItem>().ToArray();
                var duplicateItemPaths = duplicateItems.Select(x => x.assetPath).ToArray();
                AssetReferenceUtility.ReplaceAssetReferences(duplicateItemPaths, item.assetPath);
                EditorUtility.DisplayDialog("Replace Duplicate Assets", "Duplicate assets have been replaced", "OK");
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var rows = FindRows(new[] {id});
            var item = rows.FirstOrDefault();

            var assetItem = item as AssetSearchTableItem;
            if (assetItem != null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetItem.assetPath);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
        }

        protected override AssetTableItem GenerateTreeViewItem(int id, int depth, string displayName, string assetPath, bool isAsset)
        {
            return new AssetSearchTableItem(id, depth, displayName, assetPath, isAsset)
            {
                assetBundleName = GetAssetBundleNameForAssetPath(assetPath)
            };
        }

        protected override void GenerateTreeElements(TreeViewItem root)
        {
            CacheAssetBundleInfo();
            switch (assetsFilter)
            {
                case Filter.None:
                    root.AddChild(new TreeViewItem(0, 0, "Select an option to find assets"));
                    return;
                case Filter.Duplicates:
                    GenerateTreeElements(root, AssetReferenceUtility.FindDuplicateAssets(filter));
                    break;
                case Filter.Unused:
                case Filter.References:
                    var elements = assetsFilter == Filter.References
                        ? AssetReferenceUtility.FindReferencesForAssets(assetPaths)
                        : AssetReferenceUtility.FindUnusedAssets(filter, 1);
                    GenerateTreeElements(root, elements);
                    break;
                default:
                    base.GenerateTreeElements(root);
                    break;
            }
        }

        void GenerateTreeElements(TreeViewItem root, Dictionary<string, List<string>> assetReferences)
        {
            if (assetReferences.Count == 0)
            {
                root.AddChild(new TreeViewItem(0, 0, "No asset references were found"));
                return;
            }

            foreach (var assetReference in assetReferences)
            {
                var assetPath = assetReference.Key;
                if (string.IsNullOrEmpty(assetPath)) continue;
                var treeFolder = new AssetSearchTableItem(assetPath.GetHashCode(), 0, assetPath, assetPath, true);

                if (assetReference.Value.Count == 0)
                {
                    var message = $"No references for {assetReference.Key}";
                    var item = new AssetSearchTableItem(message.GetHashCode(), 1, message, "", false);
                    treeFolder.AddChild(item);
                }
                else
                {
                    for (var i = 0; i < assetReference.Value.Count; i++)
                    {
                        var referencePath = assetReference.Value[i];
                        var item = new AssetSearchTableItem(referencePath.GetHashCode(), 1, referencePath, referencePath, true);
                        treeFolder.AddChild(item);
                    }
                }

                root.AddChild(treeFolder);
            }
        }

        void GenerateTreeElements(TreeViewItem root, List<DuplicateAssets> duplicateAssets)
        {
            if (duplicateAssets.Count == 0)
            {
                root.AddChild(new TreeViewItem(0, 0, "No duplicated assets found"));
                return;
            }

            foreach (var duplicateAsset in duplicateAssets)
            {
                AssetSearchTableItem treeFolder = null;
                for (var i = 0; i < duplicateAsset.paths.Count; i++)
                {
                    var depth = i == 0 ? 0 : 1;
                    var id = duplicateAsset.paths[i].GetHashCode();
                    var item = new AssetSearchTableItem(id, depth, duplicateAsset.paths[i], duplicateAsset.paths[i], true);
                    if (depth == 0)
                    {
                        treeFolder = item;
                        if (treeFolder.children == null)
                        {
                            treeFolder.children = new List<TreeViewItem>();
                        }
                    }
                    else
                    {
                        treeFolder.AddChild(item);
                    }
                }

                root.AddChild(treeFolder);
            }
        }

        static readonly List<string> substringsToIgnore = new List<string>
        {
            "/Editor/",
            "/Editor Default Resources/"
        };

        static bool Contains(string str, IList<string> substrings, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (substrings == null || substrings.Count == 0)
            {
                return false;
            }

            foreach (var substr in substrings)
            {
                if (str.Contains(substr, stringComparison))
                {
                    return true;
                }
            }

            return false;
        }

        protected override IList<string> GetFilteredPaths()
        {
            var filteredPaths = new List<string>(AssetDatabase.GetAllAssetPaths());
            for (var i = filteredPaths.Count - 1; i >= 0; i--)
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(filteredPaths[i]);
                var isAsset = type != null && type != typeof(DefaultAsset);
                var profileMatches = AssetImportProfile.GetProfileMatches(filteredPaths[i]);

                var shouldRemove = !filteredPaths[i].StartsWith("Assets/") || Contains(filteredPaths[i], substringsToIgnore) || !isAsset;
                shouldRemove |= assetsFilter == Filter.ImportProfile && profileMatches.Count == 0;
                shouldRemove |= assetsFilter == Filter.NoImportProfile && profileMatches.Count > 0;
                if (shouldRemove)
                {
                    filteredPaths.RemoveAt(i);
                }
            }

            return filteredPaths;
        }

        protected override void CellGUI(Rect cellRect, int columnIndex, AssetTableItem assetTableItem, ref RowGUIArgs args)
        {
            if (!(assetTableItem is AssetSearchTableItem item))
            {
                return;
            }

            var column = (Columns) columnIndex;

            if (!item.isAsset)
            {
                return;
            }

            switch (column)
            {
                case Columns.Type:
                    EditorGUI.LabelField(cellRect, new GUIContent(item.friendlyType, item.typeIcon), DaiGUIStyles.treeViewLabel);
                    break;
                case Columns.ImportProfile:
                    EditorGUI.LabelField(cellRect, new GUIContent(item.importProfileNames), DaiGUIStyles.treeViewLabel);
                    break;
                case Columns.ImportProfileStatus:
                    if (item.assetType != typeof(DefaultAsset) && item.importProfileState != AssetProfileState.Good)
                    {
                        var tooltip = string.Empty;
                        GUIContent icon = null;
                        switch (item.importProfileState)
                        {
                            case AssetProfileState.NoImportProfile:
                                icon = DaiGUIContent.infoIcon;
                                break;
                            case AssetProfileState.NoMatchingFilters:
                            case AssetProfileState.ProfileDisabled:
                            case AssetProfileState.FiltersDisabled:
                            case AssetProfileState.ProcessorsDisabled:
                                icon = DaiGUIContent.warningIcon;
                                break;
                            case AssetProfileState.UnknownFileExtension:
                                icon = DaiGUIContent.errorIcon;
                                break;
                            case AssetProfileState.ProcessorsNotApplied:
                                tooltip = "Processors that have not been applied:\n" + string.Join("\n\n", item.missingProcessors);
                                icon = DaiGUIContent.errorIcon;
                                break;
                            case AssetProfileState.NoProcessors:
                                icon = DaiGUIContent.errorIcon;
                                break;
                        }

                        EditorGUI.LabelField(new Rect(cellRect.x, cellRect.y, cellRect.height, cellRect.height), icon, DaiGUIStyles.treeViewLabel);
                        cellRect.xMin += cellRect.height;
                        EditorGUI.LabelField(cellRect, new GUIContent(ObjectNames.NicifyVariableName(item.importProfileState.ToString()), tooltip), DaiGUIStyles.treeViewLabel);
                    }

                    break;
                case Columns.ImportProfileActions:
                    if (item.isAsset && item.importProfiles.Count > 0)
                    {
                        var profile = item.importProfiles[0];
                        if (item.importProfileState == AssetProfileState.NoMatchingFilters)
                        {
                            var buttonSize = DaiGUIStyles.miniButton.CalcSize(DaiGUIContent.createFilter);
                            var buttonRect = new Rect(cellRect.x, cellRect.y + (rowHeight - buttonSize.y) * 0.5f, buttonSize.x, buttonSize.y);
                            if (GUI.Button(buttonRect, DaiGUIContent.createFilter, DaiGUIStyles.miniButton))
                            {
                                var assetImportType = item.assetPath.GetImportType();
                                var assetFilter = ScriptableObject.CreateInstance<AssetFilter>();
                                assetFilter.name = $"Filter_{assetImportType}";
                                assetFilter.parent = profile;
                                assetFilter.assetType = assetImportType;
                                assetFilter.file = new NamingConventionRule {name = "assetFilter", pattern = Path.GetFileNameWithoutExtension(item.assetPath)};
                                assetFilter.assetProcessors = new List<AssetProcessor>();
                                assetFilter.AddObjectToUnityAsset(profile);
                                if (assetImportType == ImportAssetType.Other)
                                {
                                    assetFilter.otherAssetExtensions.Add(Path.GetExtension(item.assetPath));
                                }

                                profile.assetFilters.Add(assetFilter);
                                EditorUtility.SetDirty(profile);
                                EditorUtility.SetDirty(assetFilter);
                                ImportProfileWindow.ShowWindow(profile);
                            }
                        }
                        else if (item.importProfileState == AssetProfileState.ProcessorsNotApplied)
                        {
                            var buttonSize = DaiGUIStyles.miniButton.CalcSize(DaiGUIContent.applyMissingProcessors);
                            var buttonRect = new Rect(cellRect.x, cellRect.y + (rowHeight - buttonSize.y) * 0.5f, buttonSize.x, buttonSize.y);
                            if (GUI.Button(buttonRect, DaiGUIContent.applyMissingProcessors, DaiGUIStyles.miniButton))
                            {
                                item.ApplyMissingProcessors(profile);
                            }
                        }
                    }

                    break;
                case Columns.AssetBundleName:
                    EditorGUI.LabelField(cellRect, new GUIContent(item.assetBundleName), DaiGUIStyles.treeViewLabel);
                    break;
            }
        }

        protected override void OnSortRows(IList<TreeViewItem> rows)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
            {
                return;
            }

            var items = rootItem.children.Cast<AssetTableItem>();

            if (FlatView || assetsFilter == Filter.Duplicates || assetsFilter == Filter.Unused || assetsFilter == Filter.References)
            {
                rootItem.children = SortByColumn(items, sortedColumns);
                rows.Clear();
                for (var i = 0; i < rootItem.children.Count; i++)
                {
                    rows.Add(rootItem.children[i]);
                }

                Repaint();
            }
            else
            {
                SortChildren(rootItem);
                Reload();
            }
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                var rows = base.BuildRows(root);
                SortHierarchical(rows);
                return rows;
            }

            SortChildren(root);
            return base.BuildRows(root);
        }

        void SortChildren(TreeViewItem root)
        {
            if (!root.hasChildren)
            {
                return;
            }

            foreach (var child in root.children)
            {
                if (child != null)
                {
                    SortHierarchical(child.children);
                }
            }
        }

        void SortHierarchical(IList<TreeViewItem> children)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0 || children == null)
            {
                return;
            }

            var kids = new List<AssetSearchTableItem>();
            var copy = new List<TreeViewItem>(children);
            children.Clear();
            foreach (var c in copy)
            {
                var child = c as AssetSearchTableItem;
                if (child != null && child.assetType != null && child.assetType != typeof(DefaultAsset))
                {
                    kids.Add(child);
                }
                else
                {
                    children.Add(c);
                }
            }

            var col = m_SortOptions[sortedColumns[0]];
            var ascending = multiColumnHeader.IsSortedAscending(sortedColumns[0]);

            IEnumerable<AssetSearchTableItem> orderedKids = kids;
            switch (col)
            {
                case SortOption.Path:
                    orderedKids = kids.Order(l => l.assetPath, ascending);
                    break;
                case SortOption.Type:
                    orderedKids = kids.Order(l => l.friendlyType, ascending).ThenBy(l => l.assetPath);
                    break;
                case SortOption.ImportProfile:
                    orderedKids = kids.Order(l => l.importProfileNames, ascending).ThenBy(l => l.assetPath);
                    break;
                case SortOption.ImportProfileActions:
                case SortOption.ImportProfileStatus:
                    orderedKids = kids.Order(l => l.importProfileState, ascending).ThenBy(l => l.importProfileNames, ascending).ThenBy(l => l.assetPath);
                    break;
                case SortOption.AssetBundleName:
                    orderedKids = kids.Order(l => l.assetBundleName, ascending).ThenBy(l => l.assetPath);
                    break;
                default:
                    orderedKids = kids.Order(l => l.displayName, ascending);
                    break;
            }

            foreach (var o in orderedKids)
            {
                children.Add(o);
            }

            foreach (var child in children)
            {
                if (child != null)
                {
                    SortHierarchical(child.children);
                }
            }
        }

        List<TreeViewItem> SortByColumn(IEnumerable<AssetTableItem> items, int[] columnList)
        {
            var sortOption = m_SortOptions[columnList[0]];
            var ascending = multiColumnHeader.IsSortedAscending(columnList[0]);

            switch (sortOption)
            {
                case SortOption.Type:
                    return items.Order(i => ((AssetSearchTableItem) i).friendlyType, ascending).ThenBy(i => i.assetPath).Cast<TreeViewItem>().ToList();
                case SortOption.ImportProfile:
                    return items.Order(i => ((AssetSearchTableItem) i).importProfileNames, ascending).ThenBy(i => i.assetPath).Cast<TreeViewItem>().ToList();
                case SortOption.ImportProfileActions:
                case SortOption.ImportProfileStatus:
                    return items.Order(i => ((AssetSearchTableItem) i).importProfileState, ascending).ThenBy(i => ((AssetSearchTableItem) i).importProfileNames).ThenBy(i => i.assetPath).Cast<TreeViewItem>().ToList();
                case SortOption.AssetBundleName:
                    return items.Order(i => ((AssetSearchTableItem) i).assetBundleName, ascending).ThenBy(i => i.assetPath).Cast<TreeViewItem>().ToList();
                default:
                    return items.Order(i => i.assetPath, ascending).Cast<TreeViewItem>().ToList();
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.path,
                    headerTextAlignment = TextAlignment.Left,
                    width = 500,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.type,
                    headerTextAlignment = TextAlignment.Left,
                    width = 60,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.importProfile,
                    headerTextAlignment = TextAlignment.Left,
                    width = 120,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.status,
                    headerTextAlignment = TextAlignment.Left,
                    width = 120,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.actions,
                    headerTextAlignment = TextAlignment.Left,
                    width = 120,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = true,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.assetBundleName,
                    headerTextAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = true
                }
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(Columns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }
    }
}