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
    internal class ImportProfileAssetTableView : AAssetTableView
    {
        enum SortOption
        {
            Path,
            State,
            Message
        }

        enum Columns
        {
            Path,
            State,
            Message
        }

        readonly SortOption[] m_SortOptions = {SortOption.Path, SortOption.State, SortOption.Message};
        readonly AssetImportProfile m_Profile;

        public ImportProfileAssetTableView(TreeViewState state, MultiColumnHeader multiColumnHeader, AssetImportProfile profile) : base(state, multiColumnHeader)
        {
            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(Columns)).Length, "Ensure number of sort options are in sync with number of Columns enum values");
            m_Profile = profile;
        }

        protected override void ContextClickedItem(int id)
        {
            var selection = HasSelection() ? GetSelection() : new List<int> {id};

            var items = FindRows(selection).Cast<ImportProfileAssetTableItem>().Where(x => x != null && x.isAsset && (x.profileState == AssetProfileState.ProcessorsNotApplied || x.profileState == AssetProfileState.Good)).ToArray();
            if (items.Length == 0)
            {
                return;
            }

            var missingProcessorItems = items.Where(x => x.profileState == AssetProfileState.ProcessorsNotApplied).ToArray();

            var menu = new GenericMenu();
            if (missingProcessorItems.Length > 0)
            {
                menu.AddItem(DaiGUIContent.applyMissingProcessors, false, ApplyProcessorsToItems, new Tuple<bool, ImportProfileAssetTableItem[]>(false, missingProcessorItems));
                menu.AddSeparator("");
            }

            menu.AddItem(DaiGUIContent.forceApplyAllProcessors, false, ApplyProcessorsToItems, new Tuple<bool, ImportProfileAssetTableItem[]>(true, items));
            menu.ShowAsContext();
        }

        void ApplyProcessorsToItems(object userdata)
        {
            var (isForce, items) = (Tuple<bool, ImportProfileAssetTableItem[]>) userdata;
            if (items == null || items.Length == 0)
            {
                return;
            }

            var progressMultiplier = 1f / items.Length;
            var strRowCount = items.Length.ToString();
            var assetsProcessed = 0;

            var progressBarFormat = isForce ? "Force applying processors on assets: {0}/{1}" : "Applying missing processors on assets: {0}/{1}";

            AssetDatabase.StartAssetEditing();
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (EditorUtility.DisplayCancelableProgressBar("Asset Import Pipeline", string.Format(progressBarFormat, i + 1, strRowCount), (i + 1) * progressMultiplier))
                {
                    break;
                }

                try
                {
                    item.ApplyMissingProcessors(isForce);
                }
                catch
                {
                    // ignored
                }

                assetsProcessed++;
            }
            AssetDatabase.StopAssetEditing();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Asset Import Pipeline", $"Finished apply processors to {assetsProcessed} assets", "Okay");
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindRows(new[] {id}).Cast<ImportProfileAssetTableItem>().FirstOrDefault();
            if (item != null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.assetPath);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
        }

        protected override void GenerateTreeElements(TreeViewItem root)
        {
            if (!m_Profile)
            {
                root.AddChild(new TreeViewItem(0, 0, "Please reopen this window."));
                return;
            }
            base.GenerateTreeElements(root);
        }

        protected override IList<string> GetFilteredPaths()
        {
            var cachedAssetPaths = AssetImportPipeline.CachedAssetPaths;
            var result = new List<string>();
            foreach (var assetPath in cachedAssetPaths)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(DefaultAsset) && m_Profile.IsMatch(assetPath, false))
                {
                    result.Add(assetPath);
                }
            }

            result.Sort();
            return result;
        }

        protected override AssetTableItem GenerateTreeViewItem(int id, int depth, string displayName, string assetPath, bool isAsset)
        {
            return new ImportProfileAssetTableItem(id, depth, displayName, assetPath, isAsset, isAsset ? m_Profile : null);
        }

        protected override void CellGUI(Rect cellRect, int columnIndex, AssetTableItem assetTableItem, ref RowGUIArgs args)
        {
            var column = (Columns) columnIndex;
            if (!(assetTableItem is ImportProfileAssetTableItem item))
            {
                return;
            }

            switch (column)
            {
                case Columns.State:
                {
                    if (item.assetType != typeof(DefaultAsset) && item.profileState != AssetProfileState.Good)
                    {
                        var tooltip = string.Empty;
                        GUIContent icon = null;
                        switch (item.profileState)
                        {
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
                        EditorGUI.LabelField(cellRect, new GUIContent(ObjectNames.NicifyVariableName(item.profileState.ToString()), tooltip), DaiGUIStyles.treeViewLabel);
                    }
                }
                    break;
                case Columns.Message:
                {
                    if (item.isAsset)
                    {
                        if (item.profileState == AssetProfileState.NoMatchingFilters)
                        {
                            var buttonSize = DaiGUIStyles.miniButton.CalcSize(DaiGUIContent.createFilter);
                            var buttonRect = new Rect(cellRect.x, cellRect.y + (rowHeight - buttonSize.y) * 0.5f, buttonSize.x, buttonSize.y);
                            if (GUI.Button(buttonRect, DaiGUIContent.createFilter, DaiGUIStyles.miniButton))
                            {
                                var assetImportType = item.assetPath.GetImportType();
                                var assetFilter = ScriptableObject.CreateInstance<AssetFilter>();
                                assetFilter.name = $"Filter_{assetImportType}";
                                assetFilter.parent = m_Profile;
                                assetFilter.assetType = assetImportType;
                                assetFilter.file = new NamingConventionRule {name = "assetFilter", pattern = Path.GetFileNameWithoutExtension(item.assetPath)};
                                assetFilter.assetProcessors = new List<AssetProcessor>();
                                assetFilter.AddObjectToUnityAsset(m_Profile);
                                if (assetImportType == ImportAssetType.Other)
                                {
                                    assetFilter.otherAssetExtensions.Add(Path.GetExtension(item.assetPath));
                                }

                                m_Profile.assetFilters.Add(assetFilter);
                                EditorUtility.SetDirty(m_Profile);
                                EditorUtility.SetDirty(assetFilter);
                                ImportProfileWindow.ShowWindow(m_Profile);
                            }
                        }
                        else if (item.profileState == AssetProfileState.ProcessorsNotApplied)
                        {
                            var buttonSize = DaiGUIStyles.miniButton.CalcSize(DaiGUIContent.applyMissingProcessors);
                            var buttonRect = new Rect(cellRect.x, cellRect.y + (rowHeight - buttonSize.y) * 0.5f, buttonSize.x, buttonSize.y);
                            if (GUI.Button(buttonRect, DaiGUIContent.applyMissingProcessors, DaiGUIStyles.miniButton))
                            {
                                item.ApplyMissingProcessors(m_Profile);
                            }
                        }
                    }
                }
                    break;
            }
        }

        protected override void OnSortRows(IList<TreeViewItem> rows)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            var columnAscending = new bool[sortedColumns.Length];
            for (var i = 0; i < sortedColumns.Length; i++)
            {
                columnAscending[i] = multiColumnHeader.IsSortedAscending(sortedColumns[i]);
            }

            var items = rootItem.children.Cast<ImportProfileAssetTableItem>();
            var orderedQuery = SortByColumn(items, sortedColumns[0]);
            for (var i = 1; i < sortedColumns.Length; i++)
            {
                orderedQuery = SortByColumn(orderedQuery, sortedColumns[i]);
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<ImportProfileAssetTableItem> SortByColumn(IEnumerable<ImportProfileAssetTableItem> items, int columnIndex)
        {
            var sortOption = m_SortOptions[columnIndex];
            var ascending = multiColumnHeader.IsSortedAscending(columnIndex);
            switch (sortOption)
            {
                case SortOption.Path:
                    return items.Order(i => i.assetPath, ascending);
                case SortOption.State:
                    return items.Order(i => i.profileState, ascending);
                case SortOption.Message:
                    return items.Order(i => i.missingProcessors, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            return items.Order(i => i.assetPath, ascending);
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.path,
                    headerTextAlignment = TextAlignment.Left,
                    width = 460,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.status,
                    headerTextAlignment = TextAlignment.Left,
                    width = 160,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    width = 80,
                    minWidth = 20,
                    autoResize = true,
                    allowToggleVisibility = false
                },
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(Columns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }
    }

    internal enum AssetProfileState
    {
        NoMatchingFilters = 0,
        ProcessorsNotApplied = 1,
        NoProcessors = 2,
        ProfileDisabled = 3,
        FiltersDisabled = 4,
        ProcessorsDisabled = 5,
        UnknownFileExtension = 6,
        NoImportProfile = 7,
        Good = 8
    }
}