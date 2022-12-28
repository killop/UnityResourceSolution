using System;
using System.Collections.Generic;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal class ImportProfileTableView : TreeView
    {
        const float kToggleWidth = 18f;

        enum SortOption
        {
            Name,
            Path
        }

        enum Columns
        {
            Enabled,
            Name,
            Path,
            AssetTypes,
            Processors
        }

        readonly SortOption[] m_SortOptions =
        {
            SortOption.Name,
            SortOption.Name,
            SortOption.Path,
            SortOption.Name,
            SortOption.Name
        };

        const int kFirstId = 1;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);
        ImportProfileTableItem[] m_TreeViewItems;
        int m_NextId;
        int m_NumMatching;

        public ImportProfileTableView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(Columns)).Length, "Ensure number of sort options are in sync with number of Columns enum values");

            showBorder = true;
            showAlternatingRowBackgrounds = true;
            m_NextId = kFirstId;
            extraSpaceBeforeIconAndLabel = kToggleWidth;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }

        void Clear()
        {
            m_NextId = kFirstId;
            m_TreeViewItems = null;
        }

        void AddProfiles(AssetImportProfile[] profiles)
        {
            var itemsList = new List<ImportProfileTableItem>(profiles.Length);
            if (m_TreeViewItems != null)
            {
                itemsList.AddRange(m_TreeViewItems);
            }

            foreach (var profile in profiles)
            {
                var item = new ImportProfileTableItem(m_NextId++, 0, profile);
                itemsList.Add(item);
            }

            m_TreeViewItems = itemsList.ToArray();
        }

        protected override TreeViewItem BuildRoot()
        {
            const int idForHiddenRoot = -1;
            const int depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");
            foreach (var item in m_TreeViewItems)
            {
                root.AddChild(item);
            }

            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();
            var filterItems = m_TreeViewItems.Where(item => IsMatch(item.Profile)).ToArray();

            m_NumMatching = filterItems.Length;
            if (m_NumMatching == 0)
            {
                return m_Rows;
            }

            m_Rows.AddRange(filterItems);
            SortIfNeeded(m_Rows);
            return m_Rows;
        }

        bool IsMatch(AssetImportProfile profile)
        {
            return IsMatch(profile.name) || IsMatch(profile.path.pattern);
        }

        bool IsMatch(string text)
        {
            return !string.IsNullOrEmpty(text) && (string.IsNullOrEmpty(searchString) || text.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        protected override IList<int> GetAncestors(int id)
        {
            return m_TreeViewItems == null || m_TreeViewItems.Length == 0 ? new List<int>() : base.GetAncestors(id);
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            return m_TreeViewItems == null || m_TreeViewItems.Length == 0 ? new List<int>() : base.GetDescendantsThatHaveChildren(id);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (ImportProfileTableItem) args.item;
            var serializedObject = item.SerializedObject;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            for (var i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), item.Profile, serializedObject, (Columns) args.GetColumn(i), ref args);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(item.Profile);
            }
        }

        void CellGUI(Rect cellRect, AssetImportProfile profile, SerializedObject serializedObject, Columns column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case Columns.Enabled:
                {
                    cellRect.xMin += 8;
                    cellRect.xMax = cellRect.xMin + kToggleWidth;
                    EditorGUI.PropertyField(cellRect, serializedObject.FindProperty("enabled"), GUIContent.none);
                }
                    break;
                case Columns.Name:
                {
                    EditorGUI.LabelField(cellRect, profile.name);
                }
                    break;
                case Columns.Path:
                {
                    EditorGUI.LabelField(cellRect, profile.path.pattern);
                }
                    break;
                case Columns.AssetTypes:
                {
                    var assetTypes = profile.assetFilters.OrderBy(x => x.assetType).Select(x => x.assetType).Distinct().ToArray();
                    var cellX = cellRect.x;
                    var iconY = cellRect.y + (cellRect.height - 16) / 2f;
                    foreach (var assetType in assetTypes)
                    {
                        var icon = AssetImportPipeline.AssetTypeIcons[assetType];
                        EditorGUI.LabelField(new Rect(cellX, iconY, 16, 16), new GUIContent(icon, assetType.ToString()));
                        cellX += cellRect.height + 2;
                    }
                }
                    break;
                case Columns.Processors:
                {
                    var processorList = profile.assetFilters.Where(x => x.assetType >= 0)
                        .SelectMany(x => x.assetProcessors)
                        .Where(x => x != null)
                        .Select(x => new GUIContent(x.Icon, x.GetName()))
                        .ToArray();

                    var cellX = cellRect.x;
                    var iconY = cellRect.y + (cellRect.height - 16) / 2f;
                    foreach (var guiContent in processorList)
                    {
                        EditorGUI.LabelField(new Rect(cellX, iconY, 16, 16), guiContent);
                        cellX += cellRect.height + 2;
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(column), column, null);
            }
        }

        new void CenterRectUsingSingleLineHeight(ref Rect rect)
        {
            var singleLineHeight = rowHeight;
            if (rect.height > singleLineHeight)
            {
                rect.y += (rect.height - singleLineHeight) * 0.5f;
                rect.height = singleLineHeight;
            }
        }

        protected override void KeyEvent()
        {
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace) && GetSelection().Count > 0)
            {
                RemoveSelection();
                return;
            }

            base.KeyEvent();
        }

        void RemoveSelection()
        {
            var selections = GetSelection();
            foreach (var selectedId in selections)
            {
                var selectedItem = FindItem(selectedId, rootItem);
                rootItem.children.Remove(selectedItem);
                var tableItem = (ImportProfileTableItem) selectedItem;
                if (tableItem == null || !tableItem.Profile)
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tableItem.Profile));
            }

            ReloadData();
            Reload();
            Repaint();
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 1)
            {
                SingleClickedItem(selectedIds[0]);
                return;
            }

            base.SelectionChanged(selectedIds);
        }

        protected override void DoubleClickedItem(int id)
        {
            var rows = FindRows(new[] {id});
            var item = rows.FirstOrDefault();

            var tableItem = item as ImportProfileTableItem;
            if (tableItem == null || tableItem.Profile == null)
            {
                return;
            }

            OpenProfileEditor(tableItem.Profile);
        }

        protected override void ContextClickedItem(int id)
        {
            var rows = FindRows(new[] {id});
            var item = rows.FirstOrDefault();

            var tableItem = item as ImportProfileTableItem;
            if (tableItem == null || tableItem.Profile == null)
            {
                return;
            }

            var menu = new GenericMenu();
            menu.AddItem(DaiGUIContent.editProfile, false, OpenProfileEditor, tableItem.Profile);
            menu.AddItem(DaiGUIContent.openProfileAssetsViewer, false, ShowAssetsForProfile, tableItem.Profile);
            menu.AddSeparator("");
            menu.AddItem(DaiGUIContent.selectProfileAsset, false, SelectProfileAsset, tableItem.Profile);
            menu.AddSeparator("");
            menu.AddItem(DaiGUIContent.deleteProfile, false, DeleteProfile, tableItem.Profile);
            menu.ShowAsContext();
        }

        void SelectProfileAsset(object userdata)
        {
            var importProfile = (AssetImportProfile) userdata;
            if (!importProfile)
            {
                return;
            }

            Selection.activeObject = importProfile;
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        void DeleteProfile(object userdata)
        {
            var importProfile = (AssetImportProfile) userdata;
            if (!importProfile)
            {
                return;
            }

            if (EditorUtility.DisplayDialog("Delete Import Profile", $"Are you sure you want to delete the import profile called \"{importProfile.name}\"?", "Yes", "No"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(importProfile));
                ReloadData();
                Reload();
                Repaint();
            }
        }

        void OpenProfileEditor(object userdata)
        {
            var importProfile = (AssetImportProfile) userdata;
            if (importProfile)
            {
                ImportProfileWindow.ShowWindow(importProfile);
            }
        }

        static void ShowAssetsForProfile(object userdata)
        {
            var importProfile = (AssetImportProfile) userdata;
            if (!importProfile)
            {
                return;
            }

            if (string.IsNullOrEmpty(importProfile.path.pattern))
            {
                EditorUtility.DisplayDialog("Asset Import Profile", $"{importProfile.name} does not have a path set.", "Okay");
                return;
            }

            ImportProfileAssetsViewerWindow.ShowWindow(importProfile);
        }

        public int GetNumMatchingProfiles()
        {
            return m_NumMatching;
        }

        public ImportProfileTableItem[] GetSelectedItems()
        {
            var ids = GetSelection();
            if (ids.Count > 0)
            {
                return FindRows(ids).OfType<ImportProfileTableItem>().ToArray();
            }

            return new ImportProfileTableItem[0];
        }

        void OnSortingChanged(MultiColumnHeader header)
        {
            SortIfNeeded(GetRows());
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1 || multiColumnHeader.sortedColumnIndex == -1)
            {
                return;
            }

            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
            {
                return;
            }

            var items = rootItem.children.Cast<ImportProfileTableItem>();
            var orderedQuery = SortByColumn(items, sortedColumns[0]);

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
            rows.Clear();
            for (var i = 0; i < rootItem.children.Count; i++)
            {
                rows.Add(rootItem.children[i]);
            }

            Repaint();
        }

        IOrderedEnumerable<ImportProfileTableItem> SortByColumn(IEnumerable<ImportProfileTableItem> items, int columnIndex)
        {
            var sortOption = m_SortOptions[columnIndex];
            var ascending = multiColumnHeader.IsSortedAscending(columnIndex);
            switch (sortOption)
            {
                case SortOption.Name:
                    return items.Order(i => i.Profile.name, ascending);
                case SortOption.Path:
                    return items.Order(i => i.Profile.path.pattern, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            return items.Order(i => i.Profile.name, ascending);
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    width = 30,
                    minWidth = 30,
                    maxWidth = 30,
                    canSort = false,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.name,
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.path,
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 200,
                    minWidth = 150,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.assetTypes,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    width = 70,
                    minWidth = 70,
                    maxWidth = 166,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = DaiGUIContent.assetProcessors,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    width = 70,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = false
                }
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(Columns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        public void ReloadData()
        {
            Clear();
            AssetImportProfile.InvalidateCachedProfiles();
            AddProfiles(AssetImportProfile.AllProfiles);
        }
    }
}