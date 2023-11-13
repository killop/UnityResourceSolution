using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal class AssetSearchWindow : EditorWindow
    {
        [MenuItem("Tools/资产管理工具（Asset Pipeline）/Asset Search")]
        static void ShowWindow()
        {
            GetWindow<AssetSearchWindow>("Asset Search", typeof(ImportProfilesWindow)).Show();
        }

        [NonSerialized] bool m_Initialized;
        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] MultiColumnHeaderState m_ColumnHeaderState;
        AssetSearchTableView m_TreeView;
        SearchField m_SearchField;

        void InitIfNeeded()
        {
            if (m_Initialized)
            {
                return;
            }

            if (m_TreeViewState == null)
            {
                m_TreeViewState = new TreeViewState();
            }

            var firstInit = m_ColumnHeaderState == null;
            var headerState = AssetSearchTableView.CreateDefaultMultiColumnHeaderState();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_ColumnHeaderState, headerState))
            {
                MultiColumnHeaderState.OverwriteSerializedFields(m_ColumnHeaderState, headerState);
            }

            m_ColumnHeaderState = headerState;
            var multiColumnHeader = new MultiColumnHeader(headerState);
            if (firstInit)
            {
                multiColumnHeader.ResizeToFit();
            }

            m_TreeView = new AssetSearchTableView(m_TreeViewState, multiColumnHeader);
            m_TreeView.Reload();
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
            m_Initialized = true;
        }

        void OnGUI()
        {
            InitIfNeeded();
            DrawToolbar();
            DrawContent();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(DaiGUIStyles.toolbar);
            var treeViewMode = GUILayout.Toggle(!m_TreeView.FlatView, EditorGUIUtility.TrIconContent("UnityEditor.SceneHierarchyWindow", "Tree View"), DaiGUIStyles.toolbarButton);
            if (treeViewMode == m_TreeView.FlatView)
            {
                m_TreeView.FlatView = !treeViewMode;
                m_TreeView.Reload();
            }

            if (GUILayout.Button("All Assets", DaiGUIStyles.toolbarButton))
            {
                m_TreeView.assetsFilter = AssetSearchTableView.Filter.All;
                m_TreeView.Reload();
            }

            if (GUILayout.Button("With Import Profile", DaiGUIStyles.toolbarButton))
            {
                m_TreeView.assetsFilter = AssetSearchTableView.Filter.ImportProfile;
                m_TreeView.Reload();
            }

            if (GUILayout.Button("Without Import Profile", DaiGUIStyles.toolbarButton))
            {
                m_TreeView.assetsFilter = AssetSearchTableView.Filter.NoImportProfile;
                m_TreeView.Reload();
            }

            if (GUILayout.Button("Unused", DaiGUIStyles.toolbarButton))
            {
                m_TreeView.assetsFilter = AssetSearchTableView.Filter.Unused;
                m_TreeView.Reload();
            }

            if (GUILayout.Button("Duplicates", DaiGUIStyles.toolbarButton))
            {
                m_TreeView.assetsFilter = AssetSearchTableView.Filter.Duplicates;
                m_TreeView.Reload();
            }

            GUILayout.FlexibleSpace();
            m_TreeView.searchString = m_SearchField.OnToolbarGUI(m_TreeView.searchString);
            EditorGUILayout.EndHorizontal();
        }

        void DrawContent()
        {
            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            r.yMin -= 2;
            m_TreeView.OnGUI(r);
        }
    }
}