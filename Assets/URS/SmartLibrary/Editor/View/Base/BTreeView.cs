using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class BTreeView : VisualElement
    {
        internal struct ItemWrapper
        {
            public int depth;
            public BTreeViewItem item;
            public VisualElement element;
        }

        public new class UxmlFactory : UxmlFactory<BTreeView, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlIntAttributeDescription _itemHeight = new UxmlIntAttributeDescription { name = "item-height", obsoleteNames = new[] { "itemHeight" }, defaultValue = 24 };
            private readonly UxmlEnumAttributeDescription<SelectionType> _selectionType = new UxmlEnumAttributeDescription<SelectionType> { name = "selection-type", defaultValue = SelectionType.Single };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var itemHeight = 0;
                var listView = (ListView)ve;
                // Avoid setting itemHeight unless it's explicitly defined.
                // Setting itemHeight property will activate inline property mode.
                if (_itemHeight.TryGetValueFromBag(bag, cc, ref itemHeight))
                {
#if UNITY_2021_2_OR_NEWER
                    listView.fixedItemHeight = itemHeight;
#else
                    listView.itemHeight = itemHeight;
#endif
                }
                listView.selectionType = _selectionType.GetValueFromBag(bag, cc);
            }
        }

        public static readonly string UssClassName = "bewildered-tree-view";
        public static readonly string ItemUssClassName = UssClassName + "__item";
        public static readonly string ItemToggleUssClassName = ItemUssClassName + "__toggle";

        private static readonly string ItemIndentsContainerUssClassName = ItemUssClassName + "-indents-container";
        private static readonly string ItemIndentUssClassName = ItemUssClassName + "-indent";
        private static readonly string ItemContentContainerUssClassName = ItemUssClassName + "-content";

        private ListView _listView;
        private Func<VisualElement> _makeItem;
        private Action<VisualElement, BTreeViewItem> _bindItem;
        private Action<VisualElement, BTreeViewItem> _unbindItem;

        private IList<BTreeViewItem> _rootItems = null;
        private List<ItemWrapper> _itemWrappers = new List<ItemWrapper>();
        private List<int> _expandedIds = new List<int>();
        private List<BTreeViewItem> _selectedItems = null;
        private List<int> _selectedIds = new List<int>();

        public Func<VisualElement> MakeItem
        {
            get { return _makeItem; }
            set
            {
                if (value == _makeItem)
                    return;
                _makeItem = value;
#if UNITY_2021_2_OR_NEWER
                _listView.Rebuild();
#else
                _listView.Refresh();
#endif
            }
        }

        public Action<VisualElement, BTreeViewItem> BindItem
        {
            get { return _bindItem; }
            set
            {
                if (value == _bindItem)
                    return;
                _bindItem = value;
#if UNITY_2021_2_OR_NEWER
                _listView.Rebuild();
#else
                _listView.Refresh();
#endif
            }
        }

        public Action<VisualElement, BTreeViewItem> UnbindItem 
        { 
            get { return _unbindItem; }
            set
            {
                if (value == _unbindItem)
                    return;

                _unbindItem = value;
            }
        }

        public IList<BTreeViewItem> RootItems
        {
            get { return _rootItems; }
            set
            {
                _rootItems = value;
                Refresh();
            }
        }

#if UNITY_2021_2_OR_NEWER
        public float ItemHeight
        {
            get { return _listView.fixedItemHeight; }
            set
            {
                if (value == _listView.fixedItemHeight)
                    return;

                _listView.fixedItemHeight = value;
            }
        }
#else
        public int ItemHeight
        {
            get { return _listView.itemHeight; }
            set
            {
                if (value == _listView.itemHeight)
                    return;

                _listView.itemHeight = value;
            }
        }
#endif

        public BTreeViewItem SelectedItem
        {
            get 
            {
                if (_selectedItems == null)
                    return null;
                return _selectedItems.Count == 0 ? null : _selectedItems.First(); 
            }
        }

        public IEnumerable<BTreeViewItem> SelectedItems
        {
            get 
            {
                if (_selectedItems != null)
                    return _selectedItems;

                foreach (int selectedIndex in _listView.selectedIndices)
                    _selectedItems.Add(_itemWrappers[selectedIndex].item);

                return _selectedItems;
            }
        }

        public SelectionType SelectionType
        {
            get { return _listView.selectionType; }
            set { _listView.selectionType = value; }
        }

        public List<int> ExpandedIds
        {
            get { return _expandedIds; }
            set 
            {
                _expandedIds = value;
                Refresh();
            }
        }

        internal List<ItemWrapper> ItemWrappers
        {
            get { return _itemWrappers; }
        }

        public event Action<IEnumerable<BTreeViewItem>> OnSelectionChanged;

        public event Action<BTreeViewItem> OnItemChosen;

        public BTreeView()
        {
            AddToClassList(UssClassName);

            _listView = new ListView();
            _listView.itemsSource = _itemWrappers;
            _listView.makeItem = MakeTreeItem;
            _listView.bindItem = BindTreeItem;
            _listView.unbindItem = UnbindTreeItem;
            _listView.style.flexGrow = 1;
            hierarchy.Add(_listView);
            
            _listView.onSelectionChange += HandleOnSelectionChange;
            _listView.onItemsChosen += HandleItemChosen;
            _listView.Q<ScrollView>().contentContainer.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public BTreeView(IList<BTreeViewItem> rootItems, int itemHeight, Func<VisualElement> makeItem, Action<VisualElement, BTreeViewItem> bindItem) : this()
        {
            _rootItems = rootItems;        
#if UNITY_2021_2_OR_NEWER
            _listView.fixedItemHeight = itemHeight;
#else
    _listView.itemHeight = itemHeight;
#endif
            _makeItem = makeItem;
            _bindItem = bindItem;

            Refresh();
        }

        private VisualElement MakeTreeItem()
        {
            VisualElement itemElement = new VisualElement();
            itemElement.AddToClassList(ItemUssClassName);
            itemElement.style.flexDirection = FlexDirection.Row;

            VisualElement indentsContainer = new VisualElement();
            indentsContainer.AddToClassList(ItemIndentsContainerUssClassName);
            itemElement.Add(indentsContainer);

            Toggle toggle = new Toggle();
            toggle.AddToClassList(ItemToggleUssClassName);
            toggle.AddToClassList(Foldout.toggleUssClassName);
            toggle.RegisterValueChangedCallback(ItemToggleExpandedState);
            itemElement.Add(toggle);

            VisualElement itemContentContainer = new VisualElement();
            itemContentContainer.AddToClassList(ItemContentContainerUssClassName);
            itemContentContainer.style.flexGrow = 1;
            if (_makeItem != null)
                itemContentContainer.Add(_makeItem());
            itemElement.Add(itemContentContainer);

            return itemElement;
        }

        private void BindTreeItem(VisualElement element, int index)
        {
            var itemWrapper =_itemWrappers[index];
            itemWrapper.element = element;
            _itemWrappers[index] = itemWrapper;

            BTreeViewItem item = _itemWrappers[index].item;

            // Add indentation.
            var indentsContainer = element.Q(className: ItemIndentsContainerUssClassName);
            indentsContainer.Clear();
            for (int i = 0; i < _itemWrappers[index].depth; i++)
            {
                VisualElement indent = new VisualElement();
                indent.AddToClassList(ItemIndentUssClassName);
                indentsContainer.Add(indent);
            }

            // Set toggle data.
            Toggle toggle = element.Q<Toggle>(className: ItemToggleUssClassName);
            toggle.SetValueWithoutNotify(IsExpandedByIndex(index));
            toggle.userData = index;
            toggle.visible = item.HasChildren;
            toggle.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.altKey)
                {
                    if (IsExpandedByIndex(index))
                        CollapseAllChildren(index);
                    else
                        ExpandAllChildren(index);
                }
            });

            if (_bindItem == null)
                return;

            // Bind user content container.
            VisualElement userContentContainer = element.Q(className: ItemContentContainerUssClassName);
            _bindItem(userContentContainer.ElementAt(0), item);
        }

        private void UnbindTreeItem(VisualElement element, int index)
        {
            if (_unbindItem == null || element == null)
                return;

            var item = _itemWrappers[index].item;
            var userContent = element.Q(className: ItemContentContainerUssClassName).ElementAt(0);
            _unbindItem(userContent, item);
        }

        public void Refresh()
        {
            RegenerateWrappers();
#if UNITY_2021_2_OR_NEWER
            _listView.Rebuild();
#else
            _listView.Refresh();
#endif
        }

        protected int GetItemIndex(int id, bool expand)
        {
            var item = FindItem(id);
            if (item == null)
                throw new ArgumentOutOfRangeException();

            if (expand)
            {
                bool regenerateWrappers = false;
                var itemParent = item.Parent;
                while (itemParent != null)
                {
                    if (!_expandedIds.Contains(itemParent.Id))
                    {
                        _expandedIds.Add(itemParent.Id);
                        regenerateWrappers = true;
                    }
                    itemParent = itemParent.Parent;
                }

                if (regenerateWrappers)
                    RegenerateWrappers();
            }

            for (int i = 0; i < _itemWrappers.Count; ++i)
            {
                if (_itemWrappers[i].item.Id == id)
                    return i;
            }

            return -1;
        }

        public BTreeViewItem FindItem(int id)
        {
            foreach (var item in GetAllItems(RootItems))
            {
                if (item.Id == id)
                    return item;
            }

            return null;
        }

        public static IEnumerable<BTreeViewItem> GetAllItems(IEnumerable<BTreeViewItem> rootItems)
        {
            if (rootItems == null)
                yield break;

            var iteratorStack = new Stack<IEnumerator<BTreeViewItem>>();
            var currentIterator = rootItems.GetEnumerator();

            while (true)
            {
                bool hasNext = currentIterator.MoveNext();
                if (!hasNext)
                {
                    if (iteratorStack.Count > 0)
                    {
                        currentIterator = iteratorStack.Pop();
                        continue;
                    }

                    // We're at the end of the root items list.
                    break;
                }

                var currentItem = currentIterator.Current;
                yield return currentItem;

                if (currentItem.HasChildren)
                {
                    iteratorStack.Push(currentIterator);
                    currentIterator = currentItem.Children.GetEnumerator();
                }
            }
        }

        public void ScrollToItem(BTreeViewItem item)
        {
            int index = GetItemIndex(item.Id, true);
            _listView.ScrollToItem(index);
        }

        public void SetSelection(int id)
        {
            SetSelection(new int[] { id });
        }

        public void SetSelection(IEnumerable<int> ids)
        {
            SetSelectionInternal(ids, true);
        }

        public void SetSelectionWithoutNotify(IEnumerable<int> ids)
        {
            SetSelectionInternal(ids, false);
        }

        internal void SetSelectionInternal(IEnumerable<int> ids, bool sendNotification)
        {
            if (ids == null)
                return;

            var selectedIndices = new List<int>();
            foreach (int id in ids)
            {
                selectedIndices.Add(GetItemIndex(id, true));
            }

            if (selectedIndices.Any())
            {
                if (sendNotification)
                    _listView.SetSelection(selectedIndices);
                else
                    _listView.SetSelectionWithoutNotify(selectedIndices);
            }
        }

        public void ClearSelection()
        {
            _listView.ClearSelection();
        }

        private void HandleOnSelectionChange(IEnumerable<object> newSelectedItems)
        {
            if (_selectedItems == null)
                _selectedItems = new List<BTreeViewItem>();

            _selectedItems.Clear();
            _selectedIds.Clear();
            foreach (ItemWrapper itemWrapper in newSelectedItems)
            {
                _selectedItems.Add(itemWrapper.item);
                _selectedIds.Add(itemWrapper.item.Id);
            }

            OnSelectionChanged?.Invoke(_selectedItems);
        }

        private void HandleItemChosen(object item)
        {
            OnItemChosen?.Invoke(((ItemWrapper)item).item);
        }

        // =====================
        // Expaned State
        // =====================

        private void ItemToggleExpandedState(ChangeEvent<bool> evt)
        {
            Toggle toggle = evt.target as Toggle;
            int index = (int)toggle.userData;

            if (IsExpandedByIndex(index))
                CollapseItemByIndex(index);
            else
                ExpandItemByIndex(index);
        }

        private bool IsExpandedByIndex(int index)
        {
            return _expandedIds.Contains(_itemWrappers[index].item.Id);
        }

        public void ExpandItem(int id)
        {
            for (int i = 0; i < _itemWrappers.Count; i++)
            {
                if (_itemWrappers[i].item.Id == id && !IsExpandedByIndex(i))
                {
                    ExpandItemByIndex(i);
                    return;
                }
            }
            
            if (_expandedIds.Contains(id))
                return;

            _expandedIds.Add(id);
            Refresh();
        }

        private void ExpandItemByIndex(int index)
        {
            ItemWrapper itemWrapper = _itemWrappers[index];

            if (!itemWrapper.item.HasChildren)
                return;

            List<ItemWrapper> childItemWrappers = new List<ItemWrapper>();
            CreateWrappers(itemWrapper.item.Children, itemWrapper.depth + 1, ref childItemWrappers);

            _itemWrappers.InsertRange(index + 1, childItemWrappers);


            SetSelectionFromIds();

            if (!_expandedIds.Contains(itemWrapper.item.Id))
                _expandedIds.Add(itemWrapper.item.Id);

#if UNITY_2021_2_OR_NEWER
            _listView.Rebuild();
#else
            _listView.Refresh();
#endif
        }

        private void ExpandAllChildren(int index)
        {
            ItemWrapper itemWrapper = _itemWrappers[index];

            if (!itemWrapper.item.HasChildren)
                return;

            var items = new Stack<BTreeViewItem>();
            items.Push(itemWrapper.item);

            while (items.Count > 0)
            {
                BTreeViewItem item = items.Pop();
                if (!_expandedIds.Contains(item.Id))
                    _expandedIds.Add(item.Id);

                if (item.Children == null)
                    continue;

                foreach (BTreeViewItem child in item.Children)
                {
                    items.Push(child);
                }
            }

            ExpandItemByIndex(index);
        }

        public void CollapseItem(int id)
        {
            for (int i = 0; i < _itemWrappers.Count; i++)
            {
                if (_itemWrappers[i].item.Id == id && IsExpandedByIndex(i))
                {
                    CollapseItemByIndex(i);
                    return;
                }
            }

            if (!_expandedIds.Contains(id))
                return;

            _expandedIds.Remove(id);
            Refresh();
        }

        private void CollapseItemByIndex(int index)
        {
            if (!_itemWrappers[index].item.HasChildren)
                return;

            _expandedIds.Remove(_itemWrappers[index].item.Id);

            int recursiveChildCount = 0;
            int currentIndex = index + 1;
            int currentDepth = _itemWrappers[index].depth;
            while (currentIndex < _itemWrappers.Count && _itemWrappers[currentIndex].depth > currentDepth)
            {
                recursiveChildCount++;
                currentIndex++;
            }

            _itemWrappers.RemoveRange(index + 1, recursiveChildCount);
            // Sets the selection to none if the currently selected item is a child of the item that was collaposed (meaning it is no longer visible).
            SetSelectionFromIds();
#if UNITY_2021_2_OR_NEWER
            _listView.Rebuild();
#else
            _listView.Refresh();
#endif
        }

        private void CollapseAllChildren(int index)
        {
            if (!_itemWrappers[index].item.HasChildren)
                return;
            
            _expandedIds.Remove(_itemWrappers[index].item.Id);

            int recursiveChildCount = 0;
            int currentIndex = index + 1;
            int currentDepth = _itemWrappers[index].depth;
            while (currentIndex < _itemWrappers.Count && _itemWrappers[currentIndex].depth > currentDepth)
            {
                _expandedIds.Remove(_itemWrappers[currentIndex].item.Id);
                recursiveChildCount++;
                currentIndex++;
            }

            _itemWrappers.RemoveRange(index + 1, recursiveChildCount);

            // Sets the selection to none if the currently selected item is a child of the item that was collaposed (meaning it is no longer visible).
            SetSelectionFromIds();

#if UNITY_2021_2_OR_NEWER
            _listView.Rebuild();
#else
            _listView.Refresh();
#endif
        }

        private void SetSelectionFromIds()
        {
            List<int> selectedIndices = new List<int>();
            foreach (var id in _selectedIds)
            {
                int selectedIndex = GetItemIndex(id, false);
                if (selectedIndex >= 0)
                    selectedIndices.Add(selectedIndex);
            }

            _listView.SetSelectionWithoutNotify(selectedIndices);
        }

        private void RegenerateWrappers()
        {
            _itemWrappers.Clear();

            if (_rootItems != null)
                CreateWrappers(_rootItems, 0, ref _itemWrappers);
        }

        private void CreateWrappers(IEnumerable<BTreeViewItem> treeViewItems, int depth, ref List<ItemWrapper> wrappers)
        {
            foreach (var item in treeViewItems)
            {
                ItemWrapper wrapper = new ItemWrapper()
                {
                    depth = depth,
                    item = item
                };

                wrappers.Add(wrapper);
                if (_expandedIds.Contains(item.Id) && item.HasChildren)
                    CreateWrappers(item.Children, depth + 1, ref wrappers);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            var index = _listView.selectedIndex;

            bool shouldStopPropagation = true;

            switch (evt.keyCode)
            {
                case KeyCode.RightArrow:
                    if (!IsExpandedByIndex(index))
                        ExpandItemByIndex(index);
                    break;
                case KeyCode.LeftArrow:
                    if (IsExpandedByIndex(index))
                        CollapseItemByIndex(index);
                    break;
                default:
                    shouldStopPropagation = false;
                    break;
            }

            if (shouldStopPropagation)
                evt.StopPropagation();
        }
    }
}