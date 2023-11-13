using System;
using System.Collections.Generic;
using System.Linq;
using Daihenka.AssetPipeline.Filters;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal class ImportProfileWindow : EditorWindow
    {
        [SerializeField] AssetImportProfile m_Target;
        SerializedObject serializedObject;

        const string kRenameTextField = "RenameImportProfileTextField";
        const float kSpacing = 3;
        const float kAssetRowHeight = 34;
        const float kCrossButtonSize = 18;

        string m_Name;
        ReorderableList m_PathExclusionList;
        SerializedProperty m_EnabledProp;
        SerializedProperty m_PathFilterProp;
        SerializedProperty m_PathExclusionsProp;
        Editor m_CachedProcessorEditor;
        bool m_AssetChanged;
        bool m_InRenameMode;
        bool m_FocusRenameField;
        bool m_ResizingSplitter;
        float m_SplitterPercent = 0.5f;
        AssetProcessor m_SelectedProcessor;
        readonly Dictionary<int, ReorderableList> m_CachedOtherExtensionsLists = new Dictionary<int, ReorderableList>();
        readonly Dictionary<int, ReorderableList> m_CachedSubPathsLists = new Dictionary<int, ReorderableList>();
        readonly Dictionary<int, ReorderableList> m_CachedFileExclusionsLists = new Dictionary<int, ReorderableList>();
        Vector2 m_InspectorScrollView;
        Vector2 m_ProfileScrollView;
        float m_ProfileRectHeight = 0;

        public static void ShowWindow(AssetImportProfile profile)
        {
            var windows = EditorWindowUtility.GetWindows<ImportProfileWindow>();
            var window = windows.FirstOrDefault(x => x.m_Target == profile);
            if (window)
            {
                window.Focus();
                return;
            }

            window = CreateInstance<ImportProfileWindow>();
            window.TryDockNextTo(typeof(ImportProfilesWindow));
            window.m_Target = profile;
            window.OnEnable();
            window.Show();
        }

        void UpdateWindowTitle()
        {
            titleContent = new GUIContent($"Import Profile - {m_Name}");
        }

        void OnEnable()
        {
            if (m_Target == null)
            {
                return;
            }

            serializedObject = new SerializedObject(m_Target);
            m_Name = m_Target.name;
            m_EnabledProp = serializedObject.FindProperty("enabled");
            m_PathFilterProp = serializedObject.FindProperty("path");
            m_PathExclusionsProp = serializedObject.FindProperty("pathExclusions");

            m_PathExclusionList = new ReorderableList(serializedObject, m_PathExclusionsProp, true, true, true, true);
            m_PathExclusionList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Exclusions");
            m_PathExclusionList.drawElementCallback = (rect, index, active, focused) => EditorGUI.PropertyField(rect, m_PathExclusionList.GetArrayElement(index), GUIContent.none);

            UpdateWindowTitle();
        }

        void OnDisable()
        {
            DestroyCachedEditor();
        }


        void OnGUI()
        {
            m_AssetChanged = false;
            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            DrawHeader();
            GUI.Box(new Rect(0, 39.5f, position.width, 1.5f), GUIContent.none, DaiGUIStyles.horizontalSeparator);

            EditorGUILayout.BeginHorizontal();
            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var viewX = r.xMin;
            DrawImportProfile(new Rect(viewX, 41, (int) (position.width * m_SplitterPercent) - viewX, position.height - 41));
            HandleResize();

            viewX = (int) (position.width * m_SplitterPercent);
            DrawSelectedProcessor(new Rect(viewX, 41, position.width - viewX, position.height - 41));

            if (m_ResizingSplitter)
            {
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUI.Box(new Rect(viewX + 0.75f, 41, 1.5f, position.height), GUIContent.none, DaiGUIStyles.verticalSeparator);

            m_AssetChanged = serializedObject.ApplyModifiedProperties();
            if (m_AssetChanged)
            {
                ImportProfilesWindow.ReloadTreeView();
                EditorUtility.SetDirty(m_Target);
            }
        }

        void DrawImportProfile(Rect r)
        {
            var viewRect = new Rect(0, 0, r.width - 16, m_ProfileRectHeight == 0 ? r.height : m_ProfileRectHeight);
            m_ProfileScrollView = GUI.BeginScrollView(r, m_ProfileScrollView, viewRect);
            viewRect = viewRect.Pad(10, viewRect.height > r.height ? 10 : -6, 10, 10);

            var r2 = GUILayoutUtility.GetRect(DaiGUIContent.folder, DaiGUIStyles.sectionSubHeader);
            EditorGUI.LabelField(new Rect(viewRect.x, viewRect.y, r2.width, r2.height), "Folder", DaiGUIStyles.sectionSubHeader);

            viewRect.y += r2.height + kSpacing;
            var propHeight = EditorGUI.GetPropertyHeight(m_PathFilterProp);
            EditorGUI.PropertyField(new Rect(viewRect.x, viewRect.y, viewRect.width, propHeight), m_PathFilterProp, GUIContent.none);

            viewRect.y += propHeight + kSpacing;
            propHeight = m_PathExclusionList.GetHeight();
            m_PathExclusionList.DoList(new Rect(viewRect.x, viewRect.y, viewRect.width, propHeight));

            viewRect.y += propHeight + kSpacing * 2;
            EditorGUI.LabelField(new Rect(viewRect.x, viewRect.y, viewRect.width, r2.height), "File Processors", DaiGUIStyles.sectionSubHeader);

            var buttonSize = DaiGUIStyles.miniButton.CalcSize(DaiGUIContent.openAssetsViewer);
            if (GUI.Button(new Rect(viewRect.xMax - buttonSize.x, viewRect.y + (r2.height - buttonSize.y) * 0.5f, buttonSize.x, buttonSize.y), DaiGUIContent.openAssetsViewer, DaiGUIStyles.miniButton))
            {
                ImportProfileAssetsViewerWindow.ShowWindow(m_Target);
            }

            viewRect.y += r2.height + kSpacing;

            m_Target.RemoveNullFilters();
            var assetTypes = Enum.GetValues(typeof(ImportAssetType));
            foreach (ImportAssetType assetType in assetTypes)
            {
                viewRect = DrawAssetTypeRow(viewRect, assetType);
                var yStart = viewRect.y;
                var filterCount = 0;
                var assetFilters = m_Target.assetFilters.Where(x => x.assetType == assetType).ToList();
                for (var i = 0; i < assetFilters.Count; i++)
                {
                    var aboveFilter = i == 0 ? null : assetFilters[i - 1];
                    var belowFilter = i == assetFilters.Count - 1 ? null : assetFilters[i + 1];
                    var assetFilter = assetFilters[i];
                    if (filterCount > 0)
                    {
                        DaiGUIUtility.HorizontalSeparator(viewRect.x, viewRect.y, viewRect.width);
                    }

                    viewRect.y += 4;
                    filterCount++;
                    viewRect = DrawFilterRow(viewRect, assetFilter, aboveFilter, belowFilter);
                    assetFilter.RemoveNullProcessors();
                    foreach (var assetProcessor in assetFilter.assetProcessors.OrderByDescending(x => x.Priority))
                    {
                        viewRect.y += DrawProcessorRow(viewRect, assetProcessor);
                    }

                    if (assetFilter.assetProcessors.Count > 0)
                    {
                        viewRect.y += 4;
                    }
                }

                if (filterCount > 0)
                {
                    EditorGUI.DrawRect(new Rect(viewRect.xMin, yStart, 1, viewRect.y - yStart), ColorPalette.DarkLineColor);
                    EditorGUI.DrawRect(new Rect(viewRect.xMax, yStart, 1, viewRect.y - yStart), ColorPalette.DarkLineColor);
                    EditorGUI.DrawRect(new Rect(viewRect.x, viewRect.y, viewRect.width, 1), ColorPalette.DarkLineColor);
                }
                else
                {
                    viewRect.y -= 1;
                }
            }

            GUI.EndScrollView();
            m_ProfileRectHeight = viewRect.y + 60;
        }

        Rect DrawFilterRow(Rect r, AssetFilter assetFilter, AssetFilter aboveFilter, AssetFilter belowFilter)
        {
            var filterSo = new SerializedObject(assetFilter);
            var rect = new Rect(r.x, r.y + 2, r.width, EditorGUIUtility.singleLineHeight);
            rect.xMin += 12;
            var fileProp = filterSo.FindProperty("file");
            var height = EditorGUI.GetPropertyHeight(fileProp, GUIContent.none);
            var rowHeight = height + 4;
            EditorGUI.DrawRect(new Rect(r.x, r.y - 3, r.width, height + 9), ColorPalette.BackgroundLight);
            EditorGUI.DrawRect(new Rect(r.x, r.y + height + 5, r.width, 1), ColorPalette.DarkLineColor);

            var foldoutRect = new Rect(rect.x, rect.y, 18, 18);
            var enabledRect = new Rect(foldoutRect.xMax + 4, rect.y, 18, 18);
            var removeButtonRect = new Rect(rect.xMax - (kCrossButtonSize + 6), rect.y, kCrossButtonSize, kCrossButtonSize);
            var moveDownButtonRect = new Rect(removeButtonRect.xMin - (kCrossButtonSize + 4), rect.y, kCrossButtonSize, kCrossButtonSize);
            var moveUpButtonRect = new Rect(moveDownButtonRect.xMin - (kCrossButtonSize + 4), rect.y, kCrossButtonSize, kCrossButtonSize);
            var filePropRect = new Rect(enabledRect.xMax + 4, rect.y, (aboveFilter || belowFilter ? moveUpButtonRect.xMin : removeButtonRect.xMin) - (enabledRect.xMax + 8), height);

            assetFilter.showOptions = EditorGUI.Foldout(foldoutRect, assetFilter.showOptions, GUIContent.none);
            assetFilter.enabled = EditorGUI.Toggle(enabledRect, assetFilter.enabled);
            EditorGUI.LabelField(enabledRect, new GUIContent("", "Enabled"));
            filterSo.Update();
            EditorGUI.PropertyField(filePropRect, fileProp, GUIContent.none);
            filterSo.ApplyModifiedProperties();
            if (aboveFilter || belowFilter)
            {
                GUI.enabled = aboveFilter;
                if (DaiGUIUtility.IconButton(moveUpButtonRect, EditorTextures.Up, "Move Up"))
                {
                    SwapFilters(assetFilter, aboveFilter);
                }

                GUI.enabled = belowFilter;
                if (DaiGUIUtility.IconButton(moveDownButtonRect, EditorTextures.Down, "Move Down"))
                {
                    SwapFilters(assetFilter, belowFilter);
                }

                GUI.enabled = true;
            }

            if (DaiGUIUtility.IconButton(removeButtonRect, EditorTextures.X, "Remove Filter"))
            {
                OnRemoveAssetFilterButtonClicked(assetFilter);
            }

            rect.y += height + 6;
            rowHeight += height + 6;

            if (assetFilter.assetType == ImportAssetType.Other)
            {
                var list = GetCachedOtherExtensionsList(assetFilter);
                var listHeight = list.GetHeight();
                EditorGUI.DrawRect(new Rect(r.x, rect.y - 3, r.width, listHeight + 6), ColorPalette.BackgroundLight);
                EditorGUI.DrawRect(new Rect(r.x, rect.y + listHeight + 3, r.width, 1), ColorPalette.DarkLineColor);
                list.DoList(new Rect(rect.x, rect.y, rect.width - 10, listHeight));
                rect.y += listHeight + 6;
                rowHeight += listHeight + 6;
            }

            rect.y += 2;
            rowHeight += 2;

            if (assetFilter.showOptions)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {

                    EditorGUI.DrawRect(new Rect(r.x, rect.y - 5, r.width, EditorGUIUtility.singleLineHeight + 8), ColorPalette.BackgroundLight);
                    EditorGUI.DrawRect(new Rect(r.x, rect.y + EditorGUIUtility.singleLineHeight + 8, r.width, 1), ColorPalette.DarkLineColor);
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Filter Options", DaiGUIStyles.boldLabel);
                    rect.y += EditorGUIUtility.singleLineHeight + 2;
                    rowHeight += EditorGUIUtility.singleLineHeight + 2;

                    filterSo.Update();
                    var fileExclusionsList = GetCachedFileExclusionsList(assetFilter);
                    var fileExclusionsListHeight = fileExclusionsList.GetHeight();
                    EditorGUI.DrawRect(new Rect(r.x, rect.y - 3, r.width, fileExclusionsListHeight + 6), ColorPalette.BackgroundLight);
                    fileExclusionsList.DoList(new Rect(rect.x, rect.y, rect.width - 10, fileExclusionsListHeight));
                    rect.y += fileExclusionsListHeight + 6;
                    rowHeight += fileExclusionsListHeight + 6;
                    filterSo.ApplyModifiedProperties();

                    var subPathsList = GetCachedSubPathsList(assetFilter);
                    var subPathsListHeight = subPathsList.GetHeight();
                    EditorGUI.DrawRect(new Rect(r.x, rect.y - 3, r.width, subPathsListHeight + 6), ColorPalette.BackgroundLight);
                    EditorGUI.DrawRect(new Rect(r.x, rect.y + subPathsListHeight + 3, r.width, 1), ColorPalette.DarkLineColor);
                    subPathsList.DoList(new Rect(rect.x, rect.y, rect.width - 10, subPathsListHeight));
                    rect.y += subPathsListHeight + 6;
                    rowHeight += subPathsListHeight + 6;
                    
                    if (check.changed)
                        EditorUtility.SetDirty(assetFilter);
                }
            }

            EditorGUI.LabelField(rect, "Processors", DaiGUIStyles.boldLabel);
            var addProcessorButtonRect = new Rect(rect.x + rect.width - 103, rect.y, 95, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(addProcessorButtonRect, "Add Processor", DaiGUIStyles.button))
            {
                OnAddAssetProcessorButtonClicked(assetFilter);
            }

            return new Rect(r.x, r.y + rowHeight, r.width, r.height - rowHeight);
        }

        float DrawProcessorRow(Rect rect, AssetProcessor assetProcessor)
        {
            if (!assetProcessor)
            {
                return 0;
            }

            const float indentSize = 12;
            const float toggleIconSize = 20;

            rect.height = EditorGUIUtility.singleLineHeight + 2;
            rect.xMin += indentSize;
            var enabledRect = new Rect(rect.x, rect.y + 1, 16, 16);
            var removeButtonRect = new Rect(rect.xMax - (kCrossButtonSize + 6), rect.y, kCrossButtonSize, kCrossButtonSize);

            if (rect.Contains(Event.current.mousePosition) && !enabledRect.Contains(Event.current.mousePosition) && !removeButtonRect.Contains(Event.current.mousePosition)
                && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                m_SelectedProcessor = assetProcessor;
                Event.current.Use();
                Repaint();
                GUIUtility.ExitGUI();
            }

            if (m_SelectedProcessor == assetProcessor && Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x - toggleIconSize, rect.y - 1, rect.width + toggleIconSize, rect.height), ColorPalette.MenuButtonActiveBgColor);
            }

            assetProcessor.enabled = EditorGUI.Toggle(enabledRect, assetProcessor.enabled);
            rect.xMin += toggleIconSize;
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y + 1, 16, 16), assetProcessor.Icon);
            }

            rect.xMin += toggleIconSize;
            rect.y -= 2;
            EditorGUI.LabelField(rect, assetProcessor.GetName());

            if (DaiGUIUtility.IconButton(removeButtonRect, EditorTextures.X, "Remove Processor"))
            {
                OnAssetProcessorRemoveClicked(assetProcessor);
            }

            return EditorGUIUtility.singleLineHeight + 4f;
        }

        void OnAddAssetFilterButtonClicked(ImportAssetType assetType)
        {
            var assetFilter = CreateInstance<AssetFilter>();
            assetFilter.name = $"Filter_{assetType}";
            assetFilter.parent = m_Target;
            assetFilter.assetType = assetType;
            assetFilter.file = new NamingConventionRule {name = "assetFilter"};
            assetFilter.assetProcessors = new List<AssetProcessor>();
            assetFilter.AddObjectToUnityAsset(m_Target);
            m_Target.assetFilters.Add(assetFilter);
            serializedObject.Update();
        }

        void OnRemoveAssetFilterButtonClicked(AssetFilter assetFilter)
        {
            if (assetFilter)
            {
                m_Target.assetFilters.Remove(assetFilter);
                assetFilter.parent = null;
                if (assetFilter.assetProcessors.Count > 0)
                {
                    foreach (var processor in assetFilter.assetProcessors)
                    {
                        processor.parent = null;
                        processor.RemoveNestedObjectsFromUnityAsset(AssetDatabase.GetAssetPath(m_Target));
                    }

                    assetFilter.assetProcessors.Clear();
                }

                assetFilter.parent = null;
                assetFilter.RemoveNestedObjectsFromUnityAsset(AssetDatabase.GetAssetPath(m_Target));
                serializedObject.Update();
                EditorUtility.SetDirty(m_Target);
                AssetDatabase.SaveAssets();
                m_AssetChanged = true;
            }

            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        void OnAssetProcessorRemoveClicked(AssetProcessor assetProcessor)
        {
            if (assetProcessor)
            {
                if (assetProcessor.parent)
                {
                    assetProcessor.parent.assetProcessors.Remove(assetProcessor);
                }

                assetProcessor.parent = null;
                assetProcessor.RemoveNestedObjectsFromUnityAsset(AssetDatabase.GetAssetPath(m_Target));
            }

            EditorUtility.SetDirty(m_Target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DestroyImmediate(assetProcessor, true);
            GUIUtility.ExitGUI();
        }

        void OnAddAssetProcessorButtonClicked(AssetFilter assetFilter)
        {
            var menu = new GenericMenu();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assetProcessorClasses = new List<Type>();
            foreach (var assembly in assemblies) {
                assetProcessorClasses.AddRange(assembly.GetTypes().Where(t =>
                    t.IsSubclassOf(typeof(AssetProcessor)) && assetFilter.assetProcessors.All(x => x.GetType() != t)));
            }

            foreach (var processorClass in assetProcessorClasses.Where(x => x.IsValidAssetType(assetFilter.assetType)))
            {
                menu.AddItem(new GUIContent(processorClass.GetProcessorName(true)), false, OnAddAssetProcessorItemClicked, new AddAssetProcessorData(assetFilter, processorClass));
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(DaiGUIContent.noValidAssetProcessorsAvailable);
            }

            menu.ShowAsContext();
        }

        void OnAddAssetProcessorItemClicked(object userdata)
        {
            var data = (AddAssetProcessorData) userdata;
            var instance = CreateInstance(data.processorType);
            var assetProcessor = (AssetProcessor) instance;
            data.assetFilter.assetProcessors.Add(assetProcessor);
            assetProcessor.PrepareEmbeddedObjects(m_Target, data.assetFilter.assetType, data.processorType);
            assetProcessor.parent = data.assetFilter;
            assetProcessor.name = $"{data.assetFilter.assetType}_{data.processorType.Name}";
            assetProcessor.AddObjectToUnityAsset(m_Target);
            serializedObject.Update();
            EditorUtility.SetDirty(m_Target);
            AssetDatabase.SaveAssets();
        }

        ReorderableList GetCachedFileExclusionsList(AssetFilter assetFilter)
        {
            if (assetFilter.fileExclusions == null)
            {
                assetFilter.fileExclusions = new List<PathFilter>();
            }

            var key = assetFilter.fileExclusions.GetHashCode();
            if (!m_CachedFileExclusionsLists.ContainsKey(key) || m_CachedFileExclusionsLists[key] == null)
            {
                m_CachedFileExclusionsLists.Add(key, new ReorderableList(assetFilter.fileExclusions, typeof(PathFilter), false, true, true, true));
                m_CachedFileExclusionsLists[key].drawHeaderCallback = headerRect => EditorGUI.LabelField(headerRect, "File Exclusions", DaiGUIStyles.boldLabel);
                m_CachedFileExclusionsLists[key].drawElementCallback = (elementRect, i, active, focused) =>
                {
                    var so = new SerializedObject(assetFilter);
                    var prop = so.FindProperty("fileExclusions").GetArrayElementAtIndex(i);
                    EditorGUI.PropertyField(elementRect, prop, GUIContent.none);
                    so.ApplyModifiedProperties();
                };
            }

            return m_CachedFileExclusionsLists[key];
        }

        ReorderableList GetCachedSubPathsList(AssetFilter assetFilter)
        {
            if (assetFilter.subPaths == null)
            {
                assetFilter.subPaths = new List<string>();
            }

            var key = assetFilter.subPaths.GetHashCode();
            if (!m_CachedSubPathsLists.ContainsKey(key) || m_CachedSubPathsLists[key] == null)
            {
                m_CachedSubPathsLists.Add(key, new ReorderableList(assetFilter.subPaths, typeof(string), false, true, true, true));
                m_CachedSubPathsLists[key].drawHeaderCallback = headerRect => EditorGUI.LabelField(headerRect, "Sub Paths", DaiGUIStyles.boldLabel);
                m_CachedSubPathsLists[key].drawElementCallback = (elementRect, i, active, focused) => { m_CachedSubPathsLists[key].list[i] = EditorGUI.TextField(elementRect, m_CachedSubPathsLists[key].list[i].ToString()); };
                m_CachedSubPathsLists[key].onAddCallback = list => list.list.Add(string.Empty);
            }

            return m_CachedSubPathsLists[key];
        }

        ReorderableList GetCachedOtherExtensionsList(AssetFilter assetFilter)
        {
            if (assetFilter.otherAssetExtensions == null)
            {
                assetFilter.otherAssetExtensions = new List<string>();
            }

            var key = assetFilter.otherAssetExtensions.GetHashCode();
            if (!m_CachedOtherExtensionsLists.ContainsKey(key) || m_CachedOtherExtensionsLists[key] == null)
            {
                m_CachedOtherExtensionsLists.Add(key, new ReorderableList(assetFilter.otherAssetExtensions, typeof(string), false, true, true, true));
                m_CachedOtherExtensionsLists[key].drawHeaderCallback = headerRect => EditorGUI.LabelField(headerRect, "File Extensions", DaiGUIStyles.boldLabel);
                m_CachedOtherExtensionsLists[key].drawElementCallback = (elementRect, i, active, focused) => { m_CachedOtherExtensionsLists[key].list[i] = EditorGUI.TextField(elementRect, m_CachedOtherExtensionsLists[key].list[i].ToString()); };
                m_CachedOtherExtensionsLists[key].onAddCallback = list => list.list.Add(string.Empty);
            }

            return m_CachedOtherExtensionsLists[key];
        }

        Rect DrawAssetTypeRow(Rect rect, ImportAssetType assetType)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, kAssetRowHeight), ColorPalette.BackgroundLighter);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), ColorPalette.DarkLineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + kAssetRowHeight - 1, rect.width, 1), ColorPalette.DarkLineColor);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.y, 1, kAssetRowHeight), ColorPalette.DarkLineColor);
            EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, 1, kAssetRowHeight), ColorPalette.DarkLineColor);
            var icon = AssetImportPipeline.AssetTypeIcons[assetType];
            var labelContent = new GUIContent(assetType.ToString());
            GUI.DrawTexture(new Rect(24, rect.y + (kAssetRowHeight - 16) * 0.5f, 16, 16), icon);
            var labelRect = GUILayoutUtility.GetRect(labelContent, DaiGUIStyles.label);
            EditorGUI.LabelField(new Rect(44, rect.y + (kAssetRowHeight - labelRect.height) * 0.5f, rect.width, labelRect.height), labelContent);

            var addButtonContent = EditorGUIUtility.TrIconContent("Toolbar Plus", $"Add {assetType} Filter");
            if (GUI.Button(new Rect(rect.xMax - 32, rect.y + 1, 32, 32), addButtonContent, DaiGUIStyles.addProcessorButton))
            {
                OnAddAssetFilterButtonClicked(assetType);
            }

            rect.y += kAssetRowHeight;
            return rect;
        }

        void DrawSelectedProcessor(Rect rect)
        {
            if (m_SelectedProcessor)
            {
                GUILayout.BeginArea(rect);
                m_InspectorScrollView = GUILayout.BeginScrollView(m_InspectorScrollView);
                GUILayout.BeginVertical(new GUIStyle {padding = new RectOffset(10, 10, 10, 10)});
                EditorGUILayout.LabelField($"{m_SelectedProcessor.GetName()} Processor Settings", DaiGUIStyles.sectionSubHeader);
                EditorGUILayout.Space(2);
                Editor.CreateCachedEditor(m_SelectedProcessor, null, ref m_CachedProcessorEditor);
                m_CachedProcessorEditor.OnInspectorGUI();
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            else
            {
                GUILayout.BeginArea(rect);
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a processor to configure", DaiGUIStyles.sectionHeader);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40));
            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(m_EnabledProp, GUIContent.none, GUILayout.MaxWidth(20), GUILayout.Height(35));
            EditorGUILayout.LabelField("Import Profile -", DaiGUIStyles.importProfileHeader);
            if (m_InRenameMode)
            {
                GUI.SetNextControlName(kRenameTextField);
                m_Name = EditorGUILayout.TextField(m_Name, DaiGUIStyles.renameImportProfileTextField, GUILayout.Height(30));
                if (m_FocusRenameField)
                {
                    EditorGUI.FocusTextInControl(kRenameTextField);
                    m_FocusRenameField = false;
                }

                if (GUI.GetNameOfFocusedControl() == kRenameTextField && Event.current.type == EventType.KeyUp)
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        m_Name = m_Target.name;
                        m_InRenameMode = false;
                        Repaint();
                    }
                    else if (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return)
                    {
                        RenameAsset();
                        m_InRenameMode = false;
                        Repaint();
                    }
                }

                GUILayout.FlexibleSpace();
            }
            else
            {
                EditorGUILayout.LabelField(m_Name, DaiGUIStyles.importProfileHeader, GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Rename", DaiGUIStyles.renameImportProfileButton, GUILayout.Height(25)))
                {
                    m_InRenameMode = true;
                    m_FocusRenameField = true;
                }

                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndHorizontal();
        }

        void HandleResize()
        {
            var splitterRect = new Rect((int) (position.width * m_SplitterPercent), position.y, 3, position.height);

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                m_ResizingSplitter = true;
            }

            if (m_ResizingSplitter)
            {
                m_SplitterPercent = Mathf.Clamp(Event.current.mousePosition.x / position.width, 0.3f, 0.7f);
                splitterRect.x = (int) (position.width * m_SplitterPercent);
            }

            if (Event.current.type == EventType.MouseUp)
            {
                m_ResizingSplitter = false;
            }
        }


        #region Utility Methods

        void DestroyCachedEditor()
        {
            DestroyImmediate(m_CachedProcessorEditor);
        }

        void RenameAsset()
        {
            if (m_Target.name == m_Name) return;
            if (string.IsNullOrWhiteSpace(m_Name))
            {
                m_Name = m_Target.name;
                return;
            }

            var error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(m_Target), $"{m_Name}.asset");
            if (string.IsNullOrEmpty(error))
            {
                AssetDatabase.SaveAssets();
                serializedObject.Update();
                m_Name = m_Target.name;
                m_PathFilterProp.FindPropertyRelative("name").stringValue = m_Name.ToSnakeCase();
                UpdateWindowTitle();
            }
            else
            {
                Debug.LogError($"Failed to rename import profile: {error}");
                m_Name = m_Target.name;
            }
        }

        void SwapFilters(AssetFilter a, AssetFilter b)
        {
            var aIndex = m_Target.assetFilters.IndexOf(a);
            var bIndex = m_Target.assetFilters.IndexOf(b);
            m_Target.assetFilters[aIndex] = b;
            m_Target.assetFilters[bIndex] = a;
            EditorUtility.SetDirty(m_Target);
        }

        #endregion

        class AddAssetProcessorData
        {
            public AssetFilter assetFilter;
            public Type processorType;

            public AddAssetProcessorData(AssetFilter assetFilter, Type processorType)
            {
                this.assetFilter = assetFilter;
                this.processorType = processorType;
            }
        }
    }
}