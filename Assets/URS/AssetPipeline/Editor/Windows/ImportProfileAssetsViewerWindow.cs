using System;
using System.Linq;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal class ImportProfileAssetsViewerWindow : EditorWindow
    {
        public static void ShowWindow(AssetImportProfile profile)
        {
            var windows = EditorWindowUtility.GetWindows<ImportProfileAssetsViewerWindow>();
            var window = windows.FirstOrDefault(x => x.m_Target == profile);
            if (window)
            {
                window.Focus();
                return;
            }

            window = CreateInstance<ImportProfileAssetsViewerWindow>();
            var dockTarget = windows.FirstOrDefault(x => x);
            if (dockTarget)
            {
                window.TryDockNextTo(typeof(ImportProfileAssetsViewerWindow));
            }
            else
            {
                window.position = new Rect(window.position.position, new Vector2(700, 700));
            }

            window.titleContent = new GUIContent($"Assets Viewer for {profile.name}");
            window.m_Target = profile;
            window.Show();
        }

        static string[] cachedAssetPaths => AssetImportPipeline.CachedAssetPaths;
        AssetImportProfile m_Target;
        [NonSerialized] bool m_Initialized;
        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] MultiColumnHeaderState m_ColumnHeaderState;
        ImportProfileAssetTableView m_TreeView;
        SearchField m_SearchField;
        bool m_IsLoadingData;
        bool m_UpdateSubscribed = false;

        void InitIfNeeded()
        {
            if (m_Initialized)
            {
                return;
            }

            if (!m_Target)
            {
                EditorUtility.DisplayDialog("Asset Pipeline", "Something went wrong with the Import Profile Assets Viewer window.  Please try opening it again from the Import Profile inspector.", "Okay");
                Close();
            }

            if (m_TreeViewState == null)
            {
                m_TreeViewState = new TreeViewState();
            }

            var firstInit = m_ColumnHeaderState == null;
            var headerState = ImportProfileAssetTableView.CreateDefaultMultiColumnHeaderState();
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

            m_TreeView = new ImportProfileAssetTableView(m_TreeViewState, multiColumnHeader, m_Target);
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
            m_IsLoadingData = true;
            EditorUtility.DisplayProgressBar("Please wait...", "Loading asset data", 0);
            SubscribeToUpdate();
            m_Initialized = true;
        }

        void SubscribeToUpdate()
        {
            if (!m_UpdateSubscribed)
            {
                EditorApplication.update += OnUpdate;
                m_UpdateSubscribed = true;
            }
        }

        void UnsubscribeToUpdate()
        {
            if (m_UpdateSubscribed)
            {
                EditorApplication.update -= OnUpdate;
                m_UpdateSubscribed = false;
            }
        }

        void OnDisable()
        {
            UnsubscribeToUpdate();
        }

        void OnUpdate()
        {
            LoadData();
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
            GUI.enabled = !m_IsLoadingData;
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                EditorUtility.DisplayProgressBar("Please wait...", "Loading asset data", 0);
                m_TreeView.Reload();
                EditorUtility.DisplayProgressBar("Please wait...", "Loading asset data", 1);
                EditorUtility.ClearProgressBar();
                Repaint();
            }

            if (GUILayout.Button("Apply All Missing Processors", EditorStyles.toolbarButton))
            {
                var rows = m_TreeView.GetRows().Where(x => x is ImportProfileAssetTableItem).Cast<ImportProfileAssetTableItem>().Where(x => x.isAsset && x.missingProcessors.Count > 0).ToArray();
                var progressMultiplier = 1f / rows.Length;
                var strRowCount = rows.Length.ToString();
                var assetsProcessed = 0;
                AssetDatabase.StartAssetEditing();
                for (var i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Asset Import Pipeline", $"Apply processors: {i + 1}/{strRowCount}", (i + 1) * progressMultiplier))
                    {
                        break;
                    }

                    try
                    {
                        row.ApplyMissingProcessors(m_Target);
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
                m_TreeView.Reload();
                GUIUtility.ExitGUI();
            }

            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            m_TreeView.searchString = m_SearchField.OnToolbarGUI(m_TreeView.searchString);
            EditorGUILayout.EndHorizontal();
        }

        void DrawContent()
        {
            if (!m_IsLoadingData)
            {
                var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                r.yMin -= 2;
                m_TreeView.OnGUI(r);
            }
            else
            {
                DaiGUIUtility.CenterMiddleLabel("Please wait...\nLoading asset data", DaiGUIStyles.sectionHeader);
            }
        }

        void LoadData()
        {
            UnsubscribeToUpdate();
            if (!m_IsLoadingData)
            {
                return;
            }

            m_TreeView.Reload();
            EditorUtility.DisplayProgressBar("Please wait...", "Loading asset data", 1);
            m_TreeView.ExpandAll();

            m_IsLoadingData = false;
            EditorUtility.ClearProgressBar();
        }
    }
}