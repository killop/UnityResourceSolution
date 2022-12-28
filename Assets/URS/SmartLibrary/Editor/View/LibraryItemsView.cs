using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Search;

namespace Bewildered.SmartLibrary.UI
{
    public enum ItemSortOrder { NameAscending, NameDescending, TypeAscending, TypeDescending }
    
    public enum ItemsViewStyle { Grid, List }

    internal class LibraryItemsView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<LibraryItemsView, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public static readonly string UssClassName = "bewildered-library-items-container";
        private static readonly string _emptyOverlayContainerUssClassName = "bewildered-library-items__empty-overlay-container";
        private static readonly string _notificationLabelUssClassName = "bewildered-library-items__notification-label";
        private static readonly string _notificationContainerUssClassName = "bewildered-library-items__notification-container";

        private static readonly int _gridNameLineCount = 2;
        private static readonly float _iconPadding = 3.0f;
        private static readonly float _gridLabelPadding = 2.0f;

        private static readonly Color _hoverLabelBackgroundColorDark = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        private static readonly Color _hoverLabelBackgroundColorLight = new Color(0.8f, 0.8f, 0.8f, 0.7f);

        protected const int minDistanceToActive = 5;

        private SmartLibraryWindow _owner;
        private GridViewControl _gridView;
        private ListViewControl _listView;
        private ItemsViewControlBase _currentView;
        private IMGUIContainer _viewContainer;
        private VisualElement _emptyOverlayContainer;
        private VisualElement _notificationOverlayContainer;
        private Label _notificationLabel;

        private LibraryCollectionEditor _collectionEditor;
        private ItemsViewStyle _viewStyle = ItemsViewStyle.Grid;
        private float _itemSize = 100;
        private Vector2 _startPosition;
        private List<LibraryItem> _draggableItems = new List<LibraryItem>();
        private ItemSortOrder _sortOrder = ItemSortOrder.NameAscending;
        private FilteredSet<LibraryItem> _filteredItems = new FilteredSet<LibraryItem>();
        private bool _changeObjectSelection = true;
        private bool _drawingShowingGirdNames = LibraryPreferences.ShowNamesInGridView;

        private Dictionary<string, string> _cachedNames = new Dictionary<string, string>();
        private List<LibraryItem> _lastDrawnAssetItems = new List<LibraryItem>();
        private List<int> _lastDrawnAssetDirtyCounts = new List<int>();

        private bool UseCollectionSettings
        {
            get { return Owner != null && Owner.SelectedCollection != null && Owner.SelectedCollection.UseCollectionViewSettings; }
        }
        
        internal SmartLibraryWindow Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        public bool IsGridViewStyle
        {
            get
            {
                if (UseCollectionSettings)
                    return Owner.SelectedCollection.ViewStyle == ItemsViewStyle.Grid;
                
                return _viewStyle == ItemsViewStyle.Grid;
            }
        }

        public ItemsViewStyle ViewStyle
        {
            get
            {
                if (UseCollectionSettings)
                    return Owner.SelectedCollection.ViewStyle;
                
                return _viewStyle;
            }
            set { SetViewStyle(value); }
        }

        public ItemsViewStyle LocalViewStyle
        {
            get { return _viewStyle; }
            set { _viewStyle = value; }
        }

        public float ItemSize
        {
            get
            {
                if (UseCollectionSettings)
                    return Owner.SelectedCollection.ItemDisplaySize;
                
                return _itemSize;
            }
            set
            {
                if (UseCollectionSettings)
                    _owner.SelectedCollection.ItemDisplaySize = value;
                else
                    _itemSize = value;

                UpdateViewsItemSize();

                _cachedNames.Clear();
            }
        }

        public float LocalItemSize
        {
            get { return _itemSize; }
            set
            {
                _itemSize = value;
                UpdateViewsItemSize();
                _cachedNames.Clear();
            }
        }

        public IEnumerable<int> SelectedIndices
        {
            get { return _currentView.SelectedIndices; }
        }

        public IEnumerable<LibraryItem> SelectedItems
        {
            get { return _currentView.SelectedItems.Cast<FilterEntry<LibraryItem>>().Select(e => e.Item); }
        }

        public IEnumerable<LibraryItem> Items
        {
            get { return _filteredItems.ItemsSource; }
        }

        /// <summary>
        /// The method to determine the order to display the <see cref="LibraryItem"/>s in.
        /// If there is <see cref="Filter"/> text, the entry score is used then the sort order if two scores are the same.
        /// </summary>
        public ItemSortOrder SortOrder
        {
            get { return _sortOrder; }
            set 
            {
                if (value == _sortOrder)
                    return;

                _sortOrder = value;
                _filteredItems.Comparer = new LibraryItemEntryComparer(_sortOrder);
            }
        }

        /// <summary>
        /// The id of the <see cref="LibraryCollection"/> whose items are being displayed by the <see cref="LibraryItemsView"/>. 
        /// <c>UniqueID.Empty</c> if the items are not from a <see cref="LibraryCollection"/> or no items are set yet.
        /// </summary>
        public UniqueID CollectionId { get; private set; } = UniqueID.Empty;

        public string Filter
        {
            get { return _filteredItems.Filter; }
            set { _filteredItems.Filter = value; }
        }

        public Vector2 ScrollPosition
        {
            get { return _currentView.ScrollPosition; }
            set { _currentView.ScrollPosition = value; }
        }

        public event Action<IEnumerable<LibraryItem>> OnSelectionChange;
        
        public LibraryItemsView()
        {
            AddToClassList(UssClassName);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            this.AddManipulator(new LibrarySetManipulator());
            focusable = true;

            _filteredItems = new FilteredSet<LibraryItem>(new LibraryItem[0], ItemFilter);
            _filteredItems.Comparer = new LibraryItemEntryComparer(_sortOrder);

            SetupItemViews();

            _viewContainer = new IMGUIContainer(DrawItemsView);
            _viewContainer.focusable = false;
            _viewContainer.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
            _viewContainer.style.flexGrow = 1;
            _viewContainer.schedule.Execute(ForceRegenerateDirtyAssetPreviews).Every(750);
            hierarchy.Add(_viewContainer);

            SetupOverlays();
        }

        private void SetupItemViews()
        {
            // Setup GridView.
            _gridView = new GridViewControl();
            _gridView.Items = _filteredItems;
            _gridView.OnDrawItem += DrawGridItem;
            _gridView.ItemHoverColor = EditorGUIUtility.isProSkin ? new Color(0.148f, 0.148f, 0.148f) : new Color(0.6f, 0.6f, 0.6f);
            _gridView.ItemLostFocusColor = new Color(0.24f, 0.24f, 0.24f);
            _gridView.OnSelectionChange += SelectionChangedHandler;
            _gridView.OnItemsChosen += HandleOnItemsChosen;

            // Setup ListView.
            _listView = new ListViewControl();
            _listView.Items = _filteredItems;
            _listView.OnDrawItem += DrawListItem;
            _listView.ItemHoverColor = EditorGUIUtility.isProSkin ? new Color(0.148f, 0.148f, 0.148f) : new Color(0.6f, 0.6f, 0.6f);
            _listView.ItemLostFocusColor = new Color(0.24f, 0.24f, 0.24f);
            _listView.OnSelectionChange += SelectionChangedHandler;
            _listView.OnItemsChosen += HandleOnItemsChosen;
            
            UpdateViewsItemSize();

            _currentView = _gridView;
            SetViewStyle(ViewStyle);
        }

        private void SetupOverlays()
        {
            // Empty collection overlay setup.
            _emptyOverlayContainer = new VisualElement();
            _emptyOverlayContainer.pickingMode = PickingMode.Ignore;
            _emptyOverlayContainer.AddToClassList(_emptyOverlayContainerUssClassName);
            hierarchy.Add(_emptyOverlayContainer);

            // Notification overlay setup.
            _notificationOverlayContainer = new VisualElement();
            _notificationOverlayContainer.pickingMode = PickingMode.Ignore;
            _notificationOverlayContainer.AddToClassList(_notificationContainerUssClassName);
            hierarchy.Add(_notificationOverlayContainer);

            _notificationLabel = new Label();
            _notificationLabel.pickingMode = PickingMode.Ignore;
            _notificationLabel.style.opacity = 0;
            _notificationLabel.AddToClassList(_notificationLabelUssClassName);
            _notificationOverlayContainer.Add(_notificationLabel);
        }

        /// <summary>
        /// Handle the drawing of the IMGUI controls. Called inside of a <see cref="IMGUIContainer"/>.
        /// </summary>
        private void DrawItemsView()
        {
            if (Event.current.type == EventType.Repaint)
                ClearDirtyStateTracking();

            UpdateView();
            
            _currentView?.DrawLayout();
            HandleMouseInput();
            HandleKeyInput();

            // Resize the preview cache to be able to store previews for 2x the number of items that can be shown so that it loads smoothly.
            // The +30 is something that Unity does internally, so we do it too since they have a good reason to most likely...
            if (Event.current.type == EventType.Repaint)
                AssetPreviewManager.SetPreviewCacheSize(_currentView.MaxVisibleItems * 2 + 30, _owner.GetInstanceID());
        }

        /// <summary>
        /// Handles the mouse input within IMGUI.
        /// </summary>
        private void HandleMouseInput()
        {
            Event evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == (int)MouseButton.LeftMouse)
            {
                _startPosition = evt.mousePosition;

                // Clear selection when not clicking on an item.
                if (GetItemFromPosition(evt.mousePosition) == null)
                    _currentView.ClearSelection();

                // Dragging items needs to work on mouse down instead of mouse up or it would change the selected object.
                // And the mouse down event is different from the mouse drag event,
                // so the items need to be added to a list field outside of the method.
                _draggableItems.Clear();

                
                // If the item the mouse is over is selected, then we drag all selected items,
                // otherwise we only drag the item under the mouse.
                if (SelectedIndices.Contains(_currentView.IndexFromPosition(evt.mousePosition)))
                {
                    _draggableItems.AddRange(SelectedItems);
                }
                else
                {
                    // Ensure there is an item under the mouse that can be dragged.
                    // If we add a null item then a drag will try to be started and throw exceptions because of the null item.
                    var itemUnderMouse = GetItemFromPosition(evt.mousePosition);
                    if (itemUnderMouse != null)
                        _draggableItems.Add(itemUnderMouse);   
                }
            }

            if (evt.type == EventType.MouseDrag && _draggableItems.Count > 0 && evt.button == (int)MouseButton.LeftMouse)
            {
                // If the mouse has not moved the min distance since the left mouse button was pressed, return.
                // This is so that a drag is not started just because the mouse moved a single pixel.
                if (Mathf.Abs(_startPosition.x - evt.mousePosition.x) < minDistanceToActive && Mathf.Abs(_startPosition.y - evt.mousePosition.y) < minDistanceToActive)
                    return;

                // A drag cannot be started if no item is under the mouse, so we return.
                if (GetItemFromPosition(evt.mousePosition) == null)
                    return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(LibraryConstants.ItemDragDataName, CollectionId);

                DragAndDrop.objectReferences = _draggableItems.Select(item => AssetDatabase.LoadMainAssetAtPath(item.AssetPath)).ToArray();
                DragAndDrop.StartDrag(LibraryConstants.DragItemFromCollectionName);
            }
        }

        private void HandleKeyInput()
        {
            Event evt = Event.current;

            // Handle deleting items.
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Delete)
            {
                // We check if the items are from a collection (could be 'all items') and if that collection supports manually removing items.
                if (LibraryDatabase.FindCollectionByID(CollectionId) is ILibrarySet librarySet)
                {
                    // Remove all selected items from the collection.
                    if (SelectedIndices.Any())
                    {
                        librarySet.ExceptWith(SelectedItems);
                        _changeObjectSelection = false;
                        ClearSelection();
                        _changeObjectSelection = true;
                    }
                }
                else if (SelectedIndices.Any()) // Prevent the notification from showing if no items are selected.
                {
                    // Provides feedback to the user to let them know why the items could not be removed.
                    ShowNotification("Collection does not support manually removing items.");
                    return;
                }
            }
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            var collection = LibraryDatabase.FindCollectionByID(CollectionId);
            ILibrarySet librarySet = collection as ILibrarySet;
            LibraryItem targetItem = GetItemFromPosition(_viewContainer.WorldToLocal(evt.mousePosition));

            if (targetItem != null)
            {
                evt.menu.AppendAction("Open", a => AssetDatabase.OpenAsset(targetItem.InstanceID));
                evt.menu.AppendAction("Remove _delete", a => librarySet.Remove(targetItem), librarySet != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Ping", a => EditorGUIUtility.PingObject(targetItem.InstanceID));
                evt.menu.AppendAction("Reveal", a => EditorUtility.RevealInFinder(targetItem.AssetPath));
                evt.menu.AppendAction("Copy Path", a => EditorGUIUtility.systemCopyBuffer = targetItem.AssetPath);
                evt.menu.AppendAction("Reimport", a => AssetDatabase.ImportAsset(targetItem.AssetPath));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Properties... _&P", a => LibraryUtility.OpenPropertyEditor(AssetDatabase.LoadMainAssetAtPath(targetItem.AssetPath))); 
            }
            SmartLibraryWindow.HandleContextualItemMenu(evt.menu, collection, targetItem);
        }

        private void DrawGridItem(Rect rect, int index, bool isActive, bool isHovering, bool isFocused)
        {
            var item = _filteredItems[index].Item;
            AddDirtyStateTracking(item);

            Rect iconRect = DrawPreview(rect, item);
            DrawMiniTypeIcon(iconRect, item, isActive, isHovering, isFocused);

            Rect labelRect = new Rect
            {
                x = rect.x + _gridLabelPadding,
                y = iconRect.yMax + 1,
                width = rect.width - (_gridLabelPadding * 2),
                height = Styles.GridLabel.lineHeight * _gridNameLineCount
            };

            if (!LibraryPreferences.ShowNamesInGridView && isHovering)
            {
                labelRect.y = iconRect.yMax - labelRect.height;
                
                Rect labelBackgroundRect = new Rect()
                {
                    x = rect.x - 2,
                    y = rect.yMax - labelRect.height - 2,
                    width = rect.width + 4,
                    height = labelRect.height + 4
                };
                EditorGUI.DrawRect(labelBackgroundRect, EditorGUIUtility.isProSkin ? _hoverLabelBackgroundColorDark : _hoverLabelBackgroundColorLight);
            }

            // Cache the truncated text so that it doesn't need to be regenerated each draw.
            if (!_cachedNames.TryGetValue(item.GUID, out string itemName))
            {
                itemName = LibraryUtility.TruncateTextWordWrap(Styles.GridLabel, item.DisplayName, labelRect, LibraryPreferences.GridTruncationPosition);
                _cachedNames.Add(item.GUID, itemName);
            }

            var labelStyle = Styles.GridLabel;
            if ((isActive && isFocused) || !LibraryPreferences.ShowNamesInGridView)
                labelStyle = Styles.SelectedGridLabel;

            if (LibraryPreferences.ShowNamesInGridView || (!LibraryPreferences.ShowNamesInGridView && isHovering))
                GUI.Label(labelRect, itemName, labelStyle);
        }

        private void DrawListItem(Rect rect, int index, bool isActive, bool isHovering, bool isFocused)
        {
            var item = _filteredItems[index].Item;
            AddDirtyStateTracking(item);

            Rect iconRect = DrawPreview(rect, item);
            DrawMiniTypeIcon(iconRect, item, isActive, isHovering, isFocused);

            bool showPath = LibraryPreferences.ShowPathInListView;

            float labelY = rect.y + (rect.height / 2);
            if (showPath)
                labelY -= EditorGUIUtility.singleLineHeight;
            else
                labelY -= EditorGUIUtility.singleLineHeight / 2; // Centers label.

            Rect labelRect = new Rect
            {
                x = iconRect.x + iconRect.width + _iconPadding,
                y = labelY,
                width = rect.width - iconRect.width,
                height = EditorGUIUtility.singleLineHeight
            };

            Rect pathRect = new Rect()
            {
                x = labelRect.x,
                y = labelRect.y + EditorGUIUtility.singleLineHeight,
                width = labelRect.width,
                height = EditorGUIUtility.singleLineHeight
            };

            var labelStyle = EditorStyles.label;
            var pathStyle = Styles.PathLabel;

            if (isActive && isFocused)
            {
                labelStyle = Styles.SelectedListLabel;
                pathStyle = Styles.SelectedPathLabel;
            }

            GUI.Label(labelRect, item.DisplayName, labelStyle);

            if (showPath)
                GUI.Label(pathRect, item.AssetPath, pathStyle);
        }
        
        private Rect DrawPreview(Rect rect, LibraryItem item)
        {
            // ItemsView (grid/list view) adds padding to the items so they are smaller than '_itemSize', and that is the rect that is passed to this method.
            // Inorder to work with both list and grid view we set the icon size from the itemSize which is unpadded, so we readd the padding to it.
            // We then pad it more for the icon padding between the icon and the border. For some reason the size is off so we need to do x3 instead of x2 to make it have even padding.
            float iconSize = ItemSize - (_currentView.Padding * 2.0f) - (_currentView.Margin * 2);// - (_iconPadding * 3);

            var iconRect = new Rect
            {
                x = rect.x,
                y = rect.y,
                width = iconSize,
                height = iconSize
            };

            var icon = AssetPreviewManager.GetAssetPreview(item.GUID, _owner.GetInstanceID(), out bool generated);
            if (icon == null)
                icon = (Texture2D)LibraryConstants.FallbackAssetIcon;

            // None generated textures look very bad and pixely when rendered with the previewMaterial in DrawPreviewTexture, so we need render them normally with DrawTexture instead.
            if (generated)
                EditorGUI.DrawPreviewTexture(iconRect, icon, LibraryUtility.PreviewGUIMaterial);
            else
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            return iconRect;
        }

        private void DrawMiniTypeIcon(Rect iconRect, LibraryItem item, bool isActive, bool isHovering, bool isFocused)
        {
            // Mini type icon.
            bool canShowAtAll = LibraryPreferences.ShowItemTypeIcon && ItemSize >= LibraryPreferences.MinItemSizeDisplayTypeIcon;
            bool canShowType = Previewer.SupportedTypes.Contains(item.Type);
            
            // SupportedTypes.Contains() will return false for subtypes, so we need to handle them separately.
            if (item.Type.IsSubclassOf(typeof(Texture)))
                canShowType = LibraryPreferences.ShowTextureTypeIcon;
            
            if (item.Type == typeof(AudioClip) && !LibraryPreferences.ShowAudioTypeIcon)
                canShowType = false;
            
            if (canShowAtAll && canShowType)
            {
                float miniSize = 16; //ItemSize / 6.0f;
                float paddedSize = 18; //ItemSize / 5.5f;

                Rect backgroundRect = new Rect()
                {
                    x = iconRect.xMax - paddedSize, // For some reason this needs to be over one less to fully cover the preview.
                    y = iconRect.yMax - paddedSize,
                    width = paddedSize,
                    height = paddedSize
                };

                Color backgroundColor = resolvedStyle.backgroundColor;

                if ((isActive && isFocused) || isHovering)
                    backgroundColor = _currentView.ItemHoverColor;

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(backgroundRect, backgroundColor);
                }

                var miniIcon = item.Type.IsSubclassOf(typeof(Texture)) ? AssetPreview.GetMiniTypeThumbnail(item.Type) : AssetDatabase.GetCachedIcon(item.AssetPath);

                var miniIconRect = new Rect()
                {
                    width = miniSize,
                    height = miniSize
                };

                miniIconRect = CenterRect(miniIconRect, backgroundRect);

                GUI.DrawTexture(miniIconRect, miniIcon, ScaleMode.ScaleToFit);
            }
        }

        private Rect CenterRect(Rect targetRect, Rect parentRect)
        {
            targetRect.x = parentRect.x + ((parentRect.width - targetRect.width) / 2.0f);
            targetRect.y = parentRect.y + ((parentRect.height - targetRect.height) / 2.0f);

            return targetRect;
        }

        private void ClearDirtyStateTracking()
        {
            _lastDrawnAssetItems.Clear();
            _lastDrawnAssetDirtyCounts.Clear();
        }

        private void AddDirtyStateTracking(LibraryItem item)
        {
            _lastDrawnAssetItems.Add(item);
            _lastDrawnAssetDirtyCounts.Add(EditorUtility.GetDirtyCount(item.InstanceID));
        }

        public void ForceRegenerateDirtyAssetPreviews()
        {
            for (int i = 0; i < _lastDrawnAssetItems.Count; i++)
            {
                int dirtyCount = EditorUtility.GetDirtyCount(_lastDrawnAssetItems[i].InstanceID);
                if (dirtyCount != _lastDrawnAssetDirtyCounts[i])
                {
                    _lastDrawnAssetDirtyCounts[i] = dirtyCount;
                    AssetPreviewManager.ForceRegenerate(_lastDrawnAssetItems[i].GUID);
                }
            }
        }

        // This is required since the Collection's UseCollectionViewSettings can be changed at any point through code.
        // LibraryPreferences.ShowNamesInGridView can also be changed at any point.
        private void UpdateView()
        {
            bool updated = false;
            
            if (IsGridViewStyle && _currentView != _gridView)
            {
                _currentView = _gridView;
                updated = true;
            }
            else if (!IsGridViewStyle && _currentView != _listView)
            {
                _currentView = _listView;
                updated = true;
            }

            if (updated)
            {
                // Sync the selection so that the same items are selected when switching between list and grid view.
                var selectedIndices = SelectedIndices;
                _currentView.SetSelectionWithoutNotify(selectedIndices);
            }
            
            UpdateViewsItemSize();
        }
        
        private void UpdateViewsItemSize()
        {
            if (_gridView.ItemSize.x != ItemSize || _drawingShowingGirdNames != LibraryPreferences.ShowNamesInGridView)
            {
                if (!LibraryPreferences.ShowNamesInGridView)
                    _gridView.ItemSize = new Vector2(ItemSize, ItemSize);
                else
                    _gridView.ItemSize = new Vector2(ItemSize, ItemSize + (13 * _gridNameLineCount));

                _drawingShowingGirdNames = LibraryPreferences.ShowNamesInGridView;
            }

            if (_listView.ItemSize.y != ItemSize)
                _listView.ItemSize = new Vector2(0, ItemSize);
        }
        
        private void SetViewStyle(ItemsViewStyle viewStyle)
        {
            if (UseCollectionSettings)
                Owner.SelectedCollection.ViewStyle = viewStyle;
            else
                _viewStyle = viewStyle;

            // Sync the selection so that the same items are selected when switching between list and grid view.
            var selectedIndices = SelectedIndices;

            if (IsGridViewStyle)
                _currentView = _gridView;
            else
                _currentView = _listView;

            _currentView.SetSelectionWithoutNotify(selectedIndices);
        }

        public void SetTargetItems(IEnumerable<LibraryItem> items, UniqueID collectionId)
        {
            _filteredItems.ItemsSource = items;
            CollectionId = collectionId;

            // Reset scroll position back to top.
            _gridView.ScrollPosition = Vector2.zero;
            _listView.ScrollPosition = Vector2.zero;

            ClearSelectionWithoutNotify();

            if (_collectionEditor != null)
                Editor.DestroyImmediate(_collectionEditor);

            if (items is LibraryCollection collection)
            {
                _collectionEditor = (LibraryCollectionEditor)Editor.CreateEditor(collection);
            }

            UpdateEmptyOverlay();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            LibraryDatabase.ItemsChanged += OnLibraryItemsChanged;
            Undo.undoRedoPerformed += _filteredItems.FilterItems;
            Undo.undoRedoPerformed += UpdateEmptyOverlay;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            LibraryDatabase.ItemsChanged -= OnLibraryItemsChanged;
            Undo.undoRedoPerformed -= _filteredItems.FilterItems;
            Undo.undoRedoPerformed -= UpdateEmptyOverlay;
        }

        private void OnLibraryItemsChanged(LibraryItemsChangedEventArgs args)
        {
            if (args.collection.ID == CollectionId)
                Refresh();
        }

        /// <summary>
        /// Filters the <see cref="LibraryItem"/>s and updates the empty overlay.
        /// </summary>
        public void Refresh()
        {
            _filteredItems.FilterItems();
            UpdateEmptyOverlay();
        }

        public void SetSelection(IEnumerable<int> indices)
        {
            _currentView.SetSelection(indices);
        }

        public void SetSelectionWithoutNotify(IEnumerable<int> indices)
        {
            _currentView.SetSelectionWithoutNotify(indices);
        }

        public void ClearSelection()
        {
            _gridView.ClearSelection();
            _listView.ClearSelection();
        }

        public void ClearSelectionWithoutNotify()
        {
            _gridView.ClearSelectionWithoutNotify();
            _listView.ClearSelectionWithoutNotify();
        }

        private void SelectionChangedHandler(IEnumerable<object> items)
        {
            var libraryItems = new List<LibraryItem>();
            var objects = new List<UnityEngine.Object>();

            foreach (var item in items)
            {
                var entry = (FilterEntry<LibraryItem>)item;
                libraryItems.Add(entry.Item);
                objects.Add(AssetUtility.LoadMainAssetFromGUID(entry.Item.GUID));
            }

            if (_changeObjectSelection)
                AssetSelection.objects = objects.ToArray();
            OnSelectionChange?.Invoke(libraryItems);
        }

        private void HandleOnItemsChosen(IEnumerable<object> items)
        {
            var assets = new List<UnityEngine.Object>();
            foreach (var item in items)
            {
                var guid = ((FilterEntry<LibraryItem>)item).Item.GUID;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                assets.Add(AssetDatabase.LoadMainAssetAtPath(path));
            }

            if (LibraryPreferences.ChooseAction == ChooseItemAction.Open)
                AssetDatabase.OpenAsset(assets.ToArray());
            else if (LibraryPreferences.ChooseAction == ChooseItemAction.Ping)
                EditorGUIUtility.PingObject(assets[0]);
        }


        /// <summary>
        /// Show a notification with the specified text that fades out after a short duration.
        /// </summary>
        /// <param name="text">The text to display in the notification.</param>
        public void ShowNotification(string text)
        {
            _notificationLabel.text = text;
            _notificationLabel.style.opacity = 1;
            _notificationLabel.schedule
                .Execute(() => _notificationLabel.style.opacity = _notificationLabel.style.opacity.value - 0.035f) // Decrease the the opacity a small amount every call.
                .Every(50)
                .Until(() => _notificationLabel.style.opacity.value <= 0)
                .ExecuteLater(600);
        }

        private void UpdateEmptyOverlay()
        {
            _emptyOverlayContainer.Clear();

            if (_collectionEditor != null)
                _emptyOverlayContainer.Add(_collectionEditor.CreateEmptyCollectionPrompt());
            else
                _emptyOverlayContainer.Add(CreateEmptyAllItemsPrompt());

            _emptyOverlayContainer.style.display = _filteredItems.Count > 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateEmptyAllItemsPrompt()
        {
            var rootElement = new VisualElement();

            if (LibraryDatabase.BaseCollections.Count == 0)
            {
                var instructionLabel = new Label("No collections in library.");
                instructionLabel.AddToClassList(LibraryCollectionEditor.emptyPromptTextUssClassName);
                rootElement.Add(instructionLabel);

                Action action = () =>
                {
                    LibraryDatabase.AddBaseCollection(LibraryCollection.CreateCollection<StandardCollection>());
                    UpdateEmptyOverlay();
                };
                var btn = new Button(action);
                btn.text = "Create Collection";
                rootElement.Add(btn);
            }
            else
            {
                var instructionLabel = new Label("No items in library.");
                instructionLabel.AddToClassList(LibraryCollectionEditor.emptyPromptTextUssClassName);
                rootElement.Add(instructionLabel);

                var instructionLabel2 = new Label("Add items to a collection in the library.");
                instructionLabel2.AddToClassList(LibraryCollectionEditor.emptyPromptTextUssClassName);
                rootElement.Add(instructionLabel2);
            }

            return rootElement;
        }

        /// <summary>
        /// Returns a <see cref="FilterResult"/> for the specified <see cref="LibraryItem"/>. Used to filter items based on search text.
        /// </summary>
        private FilterResult ItemFilter(LibraryItem item, string queryText)
        {
            var result = new FilterResult
            {
                doesMatch = true, // We want to match by default incase the query text is empty.
                itemName = item.DisplayName
            };

            // If there is no quearyText then there is no further filtering to do sowe jsut return.
            if (string.IsNullOrWhiteSpace(queryText))
                return result;

            long score = 0;
            result.doesMatch = FuzzySearch.FuzzyMatch(queryText, item.DisplayName, ref score);
            result.score = result.doesMatch ? score : 0; // NOTE: We may be able to just directly set the score regardless if it matches or not. Need to investigate.

            return result;
        }

        private LibraryItem GetItemFromPosition(Vector3 position)
        {
            int index = _currentView.IndexFromPosition(position);
            return index > -1 && index < _filteredItems.Count ? _filteredItems[index].Item : null;
        }

        private static class Styles
        {
            private static readonly GUIStyle _gridLabel;
            private static readonly GUIStyle _pathLabel;
            
            private static readonly GUIStyle _selectedGridLabelDark;
            private static readonly GUIStyle _selectedGridLabelLight;
            
            private static readonly GUIStyle _selectedListLabelDark;
            private static readonly GUIStyle _selectedListLabelLight;
            
            private static readonly GUIStyle _selectedPathLabelDark;
            private static readonly GUIStyle _selectedPathLabelLight;
            

            public static GUIStyle SelectedListLabel
            {
                get { return EditorGUIUtility.isProSkin ? _selectedListLabelDark : _selectedListLabelLight; }
            }

            public static GUIStyle PathLabel
            {
                get { return _pathLabel; }
            }

            public static GUIStyle GridLabel
            {
                get { return _gridLabel; }
            }
            
            public static GUIStyle SelectedPathLabel
            {
                get { return EditorGUIUtility.isProSkin ? _selectedPathLabelDark : _selectedPathLabelLight; }
            }
            
            public static GUIStyle SelectedGridLabel
            {
                get { return EditorGUIUtility.isProSkin ? _selectedGridLabelDark : _selectedGridLabelLight; }
            }

            static Styles()
            {
                _gridLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    clipping = TextClipping.Overflow,
                    wordWrap = true,
                    fontSize = 10,
                    padding = new RectOffset(4, 4, 0, 0)
                };

                _pathLabel = new GUIStyle(EditorStyles.miniLabel);
                var textColor = _pathLabel.normal.textColor;
                textColor.a = 0.6f;
                _pathLabel.normal.textColor = textColor;

                // Selected grid label.
                _selectedGridLabelDark = new GUIStyle(_gridLabel);
                _selectedGridLabelDark.normal.textColor = Color.white;
                
                _selectedGridLabelLight = new GUIStyle(_gridLabel);
                _selectedGridLabelLight.normal.textColor = Color.black;
                
                // Selected list label
                _selectedListLabelDark = new GUIStyle(EditorStyles.label);
                _selectedListLabelDark.normal.textColor = Color.white;
                
                _selectedListLabelLight = new GUIStyle(EditorStyles.label);
                _selectedListLabelLight.normal.textColor = Color.black;

                // Selected path label.
                _selectedPathLabelDark = new GUIStyle(EditorStyles.miniLabel);
                _selectedPathLabelDark.normal.textColor = new Color(1, 1, 1, 0.6f);
                
                _selectedPathLabelLight = new GUIStyle(EditorStyles.miniLabel);
                _selectedPathLabelLight.normal.textColor = new Color(0, 0, 0, 0.6f);
                
            }
        }
    } 
}
