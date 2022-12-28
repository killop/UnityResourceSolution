using System;
using System.Collections.Generic;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal class ImportProfilesWindow : EditorWindow
    {
        static ImportProfilesWindow s_Window;

        static ImportProfilesWindow Window
        {
            get
            {
                if (s_Window == null && HasOpenInstances<ImportProfilesWindow>())
                {
                    s_Window = GetWindow<ImportProfilesWindow>(false, null, false);
                }

                return s_Window;
            }
        }

        [NonSerialized] bool m_Initialized;
        [SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
        [SerializeField] MultiColumnHeaderState m_ColumnHeaderState;
        SearchField m_SearchField;
        ImportProfileTableView m_TreeView;
        readonly HashSet<string> m_WaitForAssetDeletion = new HashSet<string>();

        [MenuItem("Tools/Asset Pipeline/Import Profiles")]
        public static void ShowWindow()
        {
            if (!s_Window)
            {
                s_Window = GetWindow<ImportProfilesWindow>();
                s_Window.titleContent = DaiGUIContent.importProfiles;
                s_Window.Focus();
                s_Window.Repaint();
                s_Window.Show();
            }
            else
            {
                s_Window.Focus();
            }
        }

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
            var headerState = ImportProfileTableView.CreateDefaultMultiColumnHeaderState(position.width);
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

            m_TreeView = new ImportProfileTableView(m_TreeViewState, multiColumnHeader);
            m_TreeView.ReloadData();
            m_TreeView.Reload();
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
            m_Initialized = true;
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDestroy()
        {
            s_Window = null;
            EditorApplication.update -= OnEditorUpdate;
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            InitIfNeeded();
            DrawToolbar();
            DrawTreeView();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(new GUIStyle(DaiGUIStyles.toolbar));

            if (GUILayout.Button("Create New", DaiGUIStyles.toolbarButton, GUILayout.Width(120)))
            {
                AssetImportProfile.Create("AssetImportProfile");
                ReloadTreeView(true);
            }

            GUILayout.FlexibleSpace();
            m_TreeView.searchString = m_SearchField.OnToolbarGUI(m_TreeView.searchString);

            if (GUILayout.Button("Settings", DaiGUIStyles.toolbarButton, GUILayout.Width(100)))
            {
                SettingsService.OpenProjectSettings("Project/Daihenka/Asset Pipeline");
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawTreeView()
        {
            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            r.yMin -= 2;
            m_TreeView.OnGUI(r);
        }

        public static void ReloadTreeView(bool fullReload = false)
        {
            if (!Window)
            {
                return;
            }

            if (fullReload)
            {
                Window.m_TreeView?.ReloadData();
            }

            Window.m_TreeView?.Reload();
        }

        public static void ReloadTreeViewAfterFileDeletion(string assetPath)
        {
            Window.m_WaitForAssetDeletion.Add(assetPath);
        }

        void OnEditorUpdate()
        {
            CheckReloadWaitLists();
        }

        void CheckReloadWaitLists()
        {
            if (m_WaitForAssetDeletion.Count <= 0) return;

            var shouldReload = false;
            var assetPaths = m_WaitForAssetDeletion.ToArray();
            foreach (var assetPath in assetPaths)
            {
                if (AssetDatabase.FindAssets($"{assetPath} t:AssetImportProfile").Length == 0)
                {
                    m_WaitForAssetDeletion.Remove(assetPath);
                    shouldReload = true;
                }
            }

            if (shouldReload)
            {
                ReloadTreeView(true);
            }
        }
    }
}