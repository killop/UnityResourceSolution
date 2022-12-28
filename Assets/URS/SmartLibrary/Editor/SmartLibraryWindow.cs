using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityObject = UnityEngine.Object;
using static BuildTaskUpdateCollection;

namespace Bewildered.SmartLibrary.UI
{
    /// <summary>
    /// Display and Manage the <see cref="LibraryCollection"/>s in the collections library.
    /// </summary>
    public class SmartLibraryWindow : EditorWindow
    {
        private static List<SmartLibraryWindow> _libraryWindows = new List<SmartLibraryWindow>();

        public static event Action<DropdownMenu, LibraryCollection, LibraryItem> ContextualItemMenu;
        public static event Action<DropdownMenu, LibraryCollection> ContextualCollectionMenu;

        //public static event Action<DropdownMenu, LibraryCollection, UnityObject> DropObjectCollectMenu;

        private SplitView _splitView;
        private LibraryCollectionsView _collectionsView;
        private LibraryItemsView _itemsView;
        private ToolbarToggle _useDefaultParentToggle;

        private VisualElement _headerContainer;
        private VisualElement _footerContainer;
        private VisualElement _overlayContainer;

        private Texture2D _gridModeDisplayIcon;
        private Texture2D _listModeDisplayIcon;
        private Texture2D _collectionLinkedIcon;
        private Texture2D _collectionUnlinkedIcon;
        private Texture2D _treeViewToggleIcon;
        private Texture2D _defaultParentToggleIcon;
        private Texture2D _updateItemsIcon;

        private LibraryCollection _selectedCollection;

        // UI save state values.
        // General.
        [SerializeField] private int _selectedCollectionViewId = -1;
        [SerializeField] private UniqueID _selectedCollectionId = UniqueID.Empty;
        [SerializeField] private bool _useDefaultParent = false;
        // Collections TreeView.
        [SerializeField] private bool _showCollectionsPanel = true;
        [SerializeField] private float _treeViewWidth = 250;
        [SerializeField] private List<int> _expandedTreeViewIds = new List<int>();
        [SerializeField] private Vector2 _collectionsScrollPosition;
        // ItemsView.
        [SerializeField] private bool _isGridMode = true;
        [SerializeField] private float _itemSize = 100;
        [SerializeField] private List<int> _selectedItemIndices = new List<int>();
        [SerializeField] private ItemSortOrder _sortOrder;
        [SerializeField] private Vector2 _itemsScrollPosition;

        /// <summary>
        /// The order that the <see cref="LibraryItem"/>s in the currently selected <see cref="LibraryCollection"/> are sorted.
        /// </summary>
        public ItemSortOrder SortOrder
        {
            get { return _sortOrder; }
            set { UpdateSort(value); }
        }

        /// <summary>
        /// The <see cref="LibraryCollection"/> selected in the <see cref="SmartLibraryWindow"/>.
        /// </summary>
        public LibraryCollection SelectedCollection
        {
            get { return _selectedCollection; }
        }

        /// <summary>
        /// Determines whether to use the default parent in the Hierarchy when dragging assets from
        /// the <see cref="SmartLibraryWindow"/>.
        /// </summary>
        public bool UseDefaultParent
        {
            get { return _useDefaultParent; }
            set { _useDefaultParentToggle.value = value; }
        }

        /// <summary>
        /// All currently open <see cref="SmartLibraryWindow"/>s.
        /// </summary>
        public static IReadOnlyList<SmartLibraryWindow> LibraryWindows
        {
            get { return _libraryWindows; }
        }

        [MenuItem("Window/Smart Library #l", priority = 1000)]
        private static void Open()
        {
            var window = GetWindow<SmartLibraryWindow>();
            window.Show();
            window.UpdateCollection();
        }


        private void OnEnable()
        {
            _libraryWindows.Add(this);
            
            LibraryUtility.HDRPPrompt();
            
            // Needs mouseMove so the hover state of the LibraryItemsView items is updated on mouse over.
            wantsMouseMove = true;
            minSize = new Vector2(400, 200);
            var icon = LibraryUtility.LoadLibraryIcon("standard-collection");
            titleContent = new GUIContent(LibraryConstants.LibraryWindowTitle, icon);

            _selectedCollection = LibraryDatabase.FindCollectionByID(_selectedCollectionId);
            
            // Setup and bind UI.
            LoadAndApplyContent();

            SetupHeader();
            SetupSplitView();

            // Set state from saved data.
            EnableCollectionsPanel(_showCollectionsPanel);
            UpdateSort(_sortOrder);

            Undo.undoRedoPerformed += OnUndoRedo;
            LibraryCollectionsView.OnCollectionRenamed += OnCollectionRenamed;
        }

        private void Update()
        {
            if (AssetPreviewManager.IsLoadingPreviews)
                Repaint();
            OnTaskUpdate();
        }

        private void UpdateCollection() 
        {
            _tasks = new List<CollectionSearchTask>();
            var root = SessionData.instance;
            if (root != null)
            {
                foreach (var kv in root.IDToCollectionMap)
                {
                    var collection = kv.Value;
                    _tasks.Add(new CollectionSearchTask()
                    {
                        collection = collection,
                        started = false
                    });
                }
            }
        }

        public class CollectionSearchTask
        {
            public LibraryCollection collection;
            public bool started = false;
        }
        private List<CollectionSearchTask> _tasks = null;

        public  void OnTaskUpdate()
        {
            if (_tasks!=null&&_tasks.Count > 0)
            {
                var lastIndex = _tasks.Count - 1;
                var last = _tasks[lastIndex];
                if (!last.started)
                {
                    last.started = true;
                    last.collection.UpdateItems(true);
                    _tasks.RemoveAt(lastIndex);
                    return;
                }
            }
        }
        private void OnDisable()
        {
            _libraryWindows.Remove(this);
            
            Undo.undoRedoPerformed -= OnUndoRedo;
            LibraryCollectionsView.OnCollectionRenamed -= OnCollectionRenamed;

            _treeViewWidth = _splitView.ElementAt(0).localBound.width;
            _expandedTreeViewIds = _collectionsView.ExpandedIds;
            _collectionsScrollPosition = _collectionsView.Q<ScrollView>().scrollOffset;

            _isGridMode = _itemsView.LocalViewStyle == ItemsViewStyle.Grid;
            _itemSize = _itemsView.LocalItemSize;
            _sortOrder = _itemsView.SortOrder;
            _itemsScrollPosition = _itemsView.ScrollPosition;
            
            _selectedItemIndices = new List<int>(_itemsView.SelectedIndices);
            AssetPreviewManager.ClearTemporaryAssetPreviews(GetInstanceID());
        }

        /// <summary>
        /// Loads all the assets required for the UI and applies them where necessary. Loads and clones UXML, loads and applies USS, loads and caches textures.
        /// </summary>
        private void LoadAndApplyContent()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LibraryUtility.RootPath + "/UI/SmartLibraryWindow.uxml").CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("SmartLibraryWindow"));
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet($"SmartLibraryWindow{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}"));
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("Common"));
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet($"Common{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}"));

            _gridModeDisplayIcon = LibraryUtility.LoadLibraryIcon("grid_display_mode");
            _listModeDisplayIcon = LibraryUtility.LoadLibraryIcon("list_display_mode");
            _collectionLinkedIcon = LibraryUtility.LoadLibraryIcon("collection-view-linked");
            _collectionUnlinkedIcon = LibraryUtility.LoadLibraryIcon("collection-view-unlinked");
            _defaultParentToggleIcon = LibraryUtility.LoadLibraryIcon("default_parent");
            _treeViewToggleIcon = (Texture2D)EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow").image;
            _updateItemsIcon = (Texture2D)EditorGUIUtility.IconContent("Refresh").image;
        }

        /// <summary>
        /// Setup the elements of the toolbar header. Should be called after the VisualTreeAsset has been loaded.
        /// </summary>
        private void SetupHeader()
        {
            _headerContainer = rootVisualElement.Q("header");

            // Create new collection button.
            var addMenuButton = _headerContainer.Q<ToolbarMenu>("toolbarAddMenu");
            var treeViewToggle = _headerContainer.Q<ToolbarToggle>("toolbarCollectionsPanelToggle");
            var treeViewToggleCheckmark = treeViewToggle.Q(className: Toggle.checkmarkUssClassName);
            _useDefaultParentToggle = _headerContainer.Q<ToolbarToggle>("toolbarDefaultParentToggle");
            var defaultParentToggleCheckmark = _useDefaultParentToggle.Q(className: Toggle.checkmarkUssClassName);
            var sortDropdown = _headerContainer.Q<ToolbarMenu>("toolbarSortMenu");
            var searchField = _headerContainer.Q<ToolbarSearchField>("toolbarSearch");

            // Create the menu for the Add Collection button.
            LibraryUtility.BuildCreateCollectionMenu(addMenuButton.menu, rootVisualElement.Q<LibraryCollectionsView>(), false);

            // Set current toggle value to the saved value.
            treeViewToggle.value = _showCollectionsPanel;
            treeViewToggle.RegisterValueChangedCallback(evt => EnableCollectionsPanel(evt.newValue));
            treeViewToggleCheckmark.style.backgroundImage = _treeViewToggleIcon;
            treeViewToggleCheckmark.style.width = 16;
            treeViewToggleCheckmark.style.height = 16;

            _useDefaultParentToggle.value = _useDefaultParent;
            _useDefaultParentToggle.RegisterValueChangedCallback(evt =>
            {
                _useDefaultParent = evt.newValue;
                // We repaint the hierarchy so that DefaultParent indicator will update.
                EditorApplication.RepaintHierarchyWindow();
            });
            defaultParentToggleCheckmark.style.backgroundImage = _defaultParentToggleIcon;
            defaultParentToggleCheckmark.style.width = 16;
            defaultParentToggleCheckmark.style.height = 16;
            
            CreateSortMenu(sortDropdown.menu);

            // Register value change with the search field so that it filters all the items everytime the input text changes.
            searchField.RegisterValueChangedCallback(OnSearchFieldValueChanged);
        }

        private void SetupSplitView()
        {
            _splitView = rootVisualElement.Q<SplitView>();
            _overlayContainer = _splitView.Q("overlayContainer");
            _collectionsView = _splitView.Q<LibraryCollectionsView>();
            _itemsView = _splitView.Q<LibraryItemsView>();

            _splitView.RegisterCallback<GeometryChangedEvent>(InitializeSplitViewState);

            // Collections TreeView
            _collectionsView.OnSelectionChanged += OnTreeViewSelectionChange;
            _collectionsView.ExpandedIds = _expandedTreeViewIds;
            _collectionsView.Q<ScrollView>().scrollOffset = _collectionsScrollPosition;

            // Items view.
            _itemsView.OnSelectionChange += items =>
            {
                Texture2D icon = null;
                string path = "";
                
                if (items.Any())
                {
                    var firstItem = items.First();
                    icon = (Texture2D)AssetDatabase.GetCachedIcon(firstItem.AssetPath);
                    path = firstItem.AssetPath;
                }

                _footerContainer.Q("toolbarInfoIcon").style.backgroundImage = icon;
                _footerContainer.Q<Label>("toolbarInfoLabel").text = path;
                    
            };
            
            _itemsView.Owner = this;
            _itemsView.LocalItemSize = _itemSize;
            _itemsView.LocalViewStyle = _isGridMode ? ItemsViewStyle.Grid : ItemsViewStyle.List;

            SetupItemsViewFooter();
            
            if (_selectedCollection == null)
            {
                _selectedCollectionViewId = -1;
            }

            // Set selection to previously selected collection.
            _collectionsView.SetSelection(_selectedCollectionViewId);
            _itemsView.SetSelectionWithoutNotify(_selectedItemIndices);

            _itemsView.ScrollPosition = _itemsScrollPosition;
        }

        private void SetupItemsViewFooter()
        {
            _footerContainer = _splitView.Q("itemsFooter");

            var refilterButton = _footerContainer.Q<Button>("toolbarRefilterButton");
            var collectionDataToggle = _footerContainer.Q<IconButton>("collectionViewLinkToggle");
            var viewToggle = _footerContainer.Q<Button>("toolbarViewToggle");
            var itemSizeSlider = _footerContainer.Q<Slider>("toolbarItemSize");

            refilterButton.clicked += RefilterCollection;
            refilterButton.Q(className: LibraryConstants.IconUssClassName).style.backgroundImage = _updateItemsIcon;

            bool useCollectionViewSettings = SelectedCollection != null && SelectedCollection.UseCollectionViewSettings;
            collectionDataToggle.Q(className: LibraryConstants.IconUssClassName).style.backgroundImage =
                useCollectionViewSettings ? _collectionLinkedIcon : _collectionUnlinkedIcon;
            collectionDataToggle.OnClicked += ToggleCollectionViewLink;

                // Grid/List toggle.
            viewToggle.Q(className: LibraryConstants.IconUssClassName).style.backgroundImage =
                _isGridMode ? _gridModeDisplayIcon : _listModeDisplayIcon;
            viewToggle.clicked += ToggleItemsViewStyle;

            // Item size slider.
            itemSizeSlider.value = _itemsView.ItemSize;
            itemSizeSlider.RegisterValueChangedCallback(OnItemSizeValueChanged);
        }

        private void CreateSortMenu(DropdownMenu menu)
        {
            menu.AppendAction(LibraryConstants.SortNameAscendingName, action => UpdateSort(ItemSortOrder.NameAscending));
            menu.AppendAction(LibraryConstants.SortNameDescendingName, action => UpdateSort(ItemSortOrder.NameDescending));
            menu.AppendAction(LibraryConstants.SortTypeAscendingName, action => UpdateSort(ItemSortOrder.TypeAscending));
            menu.AppendAction(LibraryConstants.SortTypeDescendingName, action => UpdateSort(ItemSortOrder.TypeDescending));
        }

        private void UpdateSort(ItemSortOrder sortOrder)
        {
            _sortOrder = sortOrder;
            _itemsView.SortOrder = sortOrder;

            switch (sortOrder)
            {
                case ItemSortOrder.NameAscending:
                    _headerContainer.Q<ToolbarMenu>("toolbarSortMenu").text = LibraryConstants.ActiveSortNameAscendingName;
                    break;
                case ItemSortOrder.NameDescending:
                    _headerContainer.Q<ToolbarMenu>("toolbarSortMenu").text = LibraryConstants.ActiveSortNameDescendingName;
                    break;
                case ItemSortOrder.TypeAscending:
                    _headerContainer.Q<ToolbarMenu>("toolbarSortMenu").text = LibraryConstants.ActiveSortTypeAscendingName;
                    break;
                case ItemSortOrder.TypeDescending:
                    _headerContainer.Q<ToolbarMenu>("toolbarSortMenu").text = LibraryConstants.ActiveSortTypeDescendingName;
                    break;
            }
        }

        private void OnSearchFieldValueChanged(ChangeEvent<string> evt)
        {
            _itemsView.Filter = evt.newValue;
        }

        private void OnItemSizeValueChanged(ChangeEvent<float> evt)
        {
            _itemsView.ItemSize = evt.newValue;
            _itemSize = evt.newValue;
        }

        private void RefilterCollection()
        {
            if (_selectedCollection != null)
                _selectedCollection.UpdateItems();
        }

        private void OnUndoRedo()
        {
            _collectionsView.Rebuild();
            _selectedCollection = LibraryDatabase.FindCollectionByID(_selectedCollectionId);
            
            if (_selectedCollection != null)
                _itemsView.SetTargetItems(_selectedCollection, _selectedCollection.ID);
            else if (_selectedCollectionViewId != -1)
                _collectionsView.SetSelection(-1);
        }

        private void OnCollectionRenamed(LibraryCollection collection, string newName)
        {
            if (collection == _selectedCollection)
                titleContent.text = newName;
        }

        public void ToggleCollectionViewLink()
        {
            if (SelectedCollection != null)
                SelectedCollection.UseCollectionViewSettings = !SelectedCollection.UseCollectionViewSettings;
            
            UpdateCollectionViewLinkDisplay();
        }

        private void UpdateCollectionViewLinkDisplay()
        {
            var collectionDataToggle = _footerContainer.Q<IconButton>("collectionViewLinkToggle");
            var displayModeDisplayElement = _footerContainer.Q<Button>("toolbarViewToggle").Q(className: LibraryConstants.IconUssClassName);
            var itemSizeSlider = _footerContainer.Q<Slider>("toolbarItemSize");

            bool useCollectionViewSettings = SelectedCollection != null && SelectedCollection.UseCollectionViewSettings;
            
            if (SelectedCollection == null)
            {
                collectionDataToggle.Q(className: LibraryConstants.IconUssClassName).SetEnabled(false);
                collectionDataToggle.Q(className: LibraryConstants.IconUssClassName).style.backgroundImage = _collectionUnlinkedIcon;
            }
            else
            {
                collectionDataToggle.Q(className: LibraryConstants.IconUssClassName).SetEnabled(true);
                collectionDataToggle.Q(className: LibraryConstants.IconUssClassName).style.backgroundImage =
                    useCollectionViewSettings ? _collectionLinkedIcon : _collectionUnlinkedIcon;
            }
            
            displayModeDisplayElement.style.backgroundImage = _itemsView.IsGridViewStyle ? _gridModeDisplayIcon : _listModeDisplayIcon;
            itemSizeSlider.SetValueWithoutNotify(_itemsView.ItemSize);
        }

        /// <summary>
        /// Toggle the items view between list and grid view styles.
        /// </summary>
        public void ToggleItemsViewStyle()
        {
            // Toggle the current display mode. If it is grid then set it to list, and vice versa.
            _itemsView.ViewStyle = _itemsView.IsGridViewStyle ? ItemsViewStyle.List : ItemsViewStyle.Grid;

            // Get the elements that need to be updated after the ItemsView display mode has changed.
            var displayModeDisplayElement = _footerContainer.Q<Button>("toolbarViewToggle").Q(className: LibraryConstants.IconUssClassName);
            var itemSizeSlider = _footerContainer.Q<Slider>("toolbarItemSize");

            displayModeDisplayElement.style.backgroundImage = _itemsView.IsGridViewStyle ? _gridModeDisplayIcon : _listModeDisplayIcon;

            itemSizeSlider.SetValueWithoutNotify(_itemsView.ItemSize);
        }

        private void InitializeSplitViewState(GeometryChangedEvent evt)
        {
            _splitView.Init(0, _treeViewWidth, SplitView.Orientation.Horizontal);

            _splitView.UnregisterCallback<GeometryChangedEvent>(InitializeSplitViewState);
            
            // We register here as sometimes the panel would get a width of 0 and I suspect
            // that this callback was being called before the initializer.
            _splitView.ElementAt(0).RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (_splitView.ElementAt(0).style.display == DisplayStyle.Flex)
                    _treeViewWidth = Mathf.Max(evt.newRect.width, 125);
            });
        }

        private void EnableCollectionsPanel(bool doEnable)
        {
            _splitView.Q(className: SplitView.HandleDragLineAnchorClassName).SetDisplay(doEnable);

            _splitView.ElementAt(0).SetDisplay(doEnable);
            _showCollectionsPanel = doEnable;

            if (doEnable)
            {
                _splitView.Init(0, _treeViewWidth, SplitView.Orientation.Horizontal);
            }
        }

        private void OnTreeViewSelectionChange(IEnumerable<BTreeViewItem> selectedItems)
        {
            var treeViewItem = selectedItems.FirstOrDefault();
            
            if (treeViewItem != null && treeViewItem is CollectionTreeViewItem collectionTreeViewItem)
            {
                SetCurrentCollection(collectionTreeViewItem);
            }
            else
            {
                SetCurrentCollection(null);
            }
        }

        private void SetCurrentCollection(CollectionTreeViewItem collectionTreeViewItem)
        {
            _selectedCollection = collectionTreeViewItem?.Collection;

            string title = LibraryConstants.LibraryWindowTitle;

            if (_selectedCollection != null)
            {
                _selectedCollectionViewId = collectionTreeViewItem.Id;
                _selectedCollectionId = _selectedCollection.ID;

                if (!string.IsNullOrWhiteSpace(_selectedCollection.name))
                    title = _selectedCollection.name;

                _itemsView.SetTargetItems(_selectedCollection, _selectedCollection.ID);
            }
            else
            {
                _selectedCollectionViewId = -1;
                _selectedCollectionId = UniqueID.Empty;
                _collectionsView.SetSelectionWithoutNotify(new int[] { -1 });
                _itemsView.SetTargetItems(LibraryDatabase.AllItems, UniqueID.Empty);
            }

            titleContent.text = title;
            _footerContainer.Q("toolbarInfoIcon").style.backgroundImage = null;
            _footerContainer.Q<Label>("toolbarInfoLabel").text = "";
            UpdateCollectionViewLinkDisplay();

            // Selecting a collection does not change inspector selection normally,
            // so it can be confusing when one collection is shown in the inspector but another in the Library window.
            // To prevent this confusion we clear the inspector if a collection is currently selected.
            if (Selection.activeObject is LibraryCollection)
                Selection.activeObject = null;
            
            // We repaint the hierarchy so that the DefaultParent indicator will update to show the current collection.
            EditorApplication.RepaintHierarchyWindow();
            
            // We ping the default parent if enabled and one exists so that it is easy to find.
            if (_useDefaultParent)
            {
                if (_selectedCollection.DefaultSceneParent != null)
                {
                    EditorGUIUtility.PingObject(_selectedCollection.DefaultSceneParent);
                }
            }
        }

        private void SetOverlayContent(VisualElement element)
        {
            _overlayContainer.Clear();
            _overlayContainer.Add(element);
            SetOverlayDisplay(true);
        }

        private void SetOverlayDisplay(bool doDIsplay)
        {
            _overlayContainer.SetDisplay(doDIsplay);
        }

        public void SetItemSelection(IEnumerable<int> indices)
        {
            _itemsView.SetSelection(indices);
        }

        public void ClearItemSelection()
        {
            _itemsView.ClearSelection();
        }
        
        internal static void HandleContextualItemMenu(DropdownMenu menu, LibraryCollection collection, LibraryItem item)
        {
            ContextualItemMenu?.Invoke(menu, collection, item);
        }

        internal static void HandleContextualCollectionMenu(DropdownMenu menu, LibraryCollection collection)
        {
            ContextualCollectionMenu?.Invoke(menu, collection);
        }
    } 
}
