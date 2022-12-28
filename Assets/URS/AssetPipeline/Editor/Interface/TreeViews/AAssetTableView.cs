using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    public abstract class AAssetTableView : TreeView
    {
        public bool FlatView { get; set; }

        public AAssetTableView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = 20;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1, "root");

            GenerateTreeElements(root);

            return root;
        }

        protected virtual void GenerateTreeElements(TreeViewItem root)
        {
            var filteredPaths = GetFilteredPaths();
            if (filteredPaths.Count == 0)
            {
                root.AddChild(new TreeViewItem(0, 0, "No assets found"));
                return;
            }

            var items = new Dictionary<int, AssetTableItem>();
            if (FlatView)
            {
                foreach (var assetPath in filteredPaths)
                {
                    var id = assetPath.GetHashCode();
                    var item = GenerateTreeViewItem(id, 0, assetPath, assetPath, true);
                    root.AddChild(item);
                    if (items.ContainsKey(id))
                    {
                        Debug.LogError($"id already in for {assetPath}");
                        continue;
                    }

                    items.Add(id, item);
                }
            }
            else
            {
                var activePath = "Assets";
                var treeFolder = new AssetTableItem(activePath.GetHashCode(), 0, activePath, activePath, false);
                if (treeFolder.children == null)
                {
                    treeFolder.children = new List<TreeViewItem>();
                }

                root.AddChild(treeFolder);

                items.Add(activePath.GetHashCode(), treeFolder);

                foreach (var assetPath in filteredPaths)
                {
                    var path = assetPath.Substring(7);
                    var strings = path.Split(new[] {'/'}, StringSplitOptions.None);
                    activePath = "Assets";

                    var active = treeFolder;
                    for (var i = 0; i < strings.Length; i++)
                    {
                        activePath += $"/{strings[i]}";
                        var id = activePath.GetHashCode();

                        var type = AssetDatabase.GetMainAssetTypeAtPath(activePath);
                        var isAsset = type != null && type != typeof(DefaultAsset);
                        if (i == strings.Length - 1 && isAsset)
                        {
                            var item = GenerateTreeViewItem(id, i + 1, strings[i], activePath, isAsset);
                            active.AddChild(item);
                            active = item;
                            if (items.ContainsKey(id))
                            {
                                Debug.LogError($"id already in for {activePath}");
                                continue;
                            }

                            items.Add(id, item);
                        }
                        else
                        {
                            AssetTableItem item;
                            if (!items.TryGetValue(id, out item))
                            {
                                item = GenerateTreeViewItem(id, i + 1, strings[i], activePath, false);
                                active.AddChild(item);
                                items.Add(id, item);
                            }

                            active = item;
                        }
                    }
                }
            }

            if (!root.hasChildren)
            {
                root.AddChild(new TreeViewItem(0, 0, "No assets found"));
            }
        }

        protected virtual AssetTableItem GenerateTreeViewItem(int id, int depth, string displayName, string assetPath, bool isAsset)
        {
            return new AssetTableItem(id, depth, displayName, assetPath, isAsset);
        }

        protected virtual IList<string> GetFilteredPaths()
        {
            return AssetImportPipeline.CachedAssetPaths;
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

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                var columnIndex = args.GetColumn(i);
                var cellRect = args.GetCellRect(i);
                if (columnIndex == 0)
                {
                    var indent = GetContentIndent(args.item) + extraSpaceBeforeIconAndLabel;
                    cellRect.xMin += indent;
                    CenterRectUsingSingleLineHeight(ref cellRect);
                    EditorGUI.LabelField(cellRect, new GUIContent(args.item.displayName, args.item.icon), DaiGUIStyles.treeViewLabel);
                }
                else
                {
                    CellGUI(cellRect, columnIndex, args.item as AssetTableItem, ref args);
                }
            }
        }

        protected virtual void CellGUI(Rect cellRect, int columnIndex, AssetTableItem assetTableItem, ref RowGUIArgs args)
        {
        }

        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            var rows = GetRows();
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return;
            }

            if (multiColumnHeader.state.sortedColumns.Length == 0)
            {
                return;
            }

            OnSortRows(rows);
            Repaint();
        }

        protected virtual void OnSortRows(IList<TreeViewItem> rows)
        {
        }
    }
}