using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class ReorderableList : VisualElement
    {
        public static readonly string ussClassName = "bewildered-reorderable-list";
        public static readonly string itemUssClassName = ussClassName + "-item";
        public static readonly string selectedItemUssClassName = itemUssClassName + "--selected";
        public static readonly string itemContainerUssClassName = ussClassName + "__container";
        private static readonly string footerUssClassName = ussClassName + "__footer";
        private static readonly string buttonUssClassName = ussClassName + "__button";

        public static readonly string headerUssClassName = ussClassName + "__header-container";

        private static readonly string emptyUssClassName = ussClassName + "-empty";

        internal class ListItemElement : VisualElement
        {
            public ListItemElement(bool draggable)
            {
                AddToClassList(itemUssClassName);
            }

            public void SetSelected(bool selected)
            {
                EnableInClassList(selectedItemUssClassName, selected);
            }
        }

        private bool _hasInitialized = false;
        private int _selectedIndex = -1;

        private Clickable _addManipulator;
        private Clickable _removeManipulator;

        /// <summary>
        /// The <see cref="SerializedProperty"/> array that contains the elements to display in the <see cref="ReorderableList"/>.
        /// </summary>
        public SerializedProperty ListProperty
        {
            get;
            private set;
        }

        public IList List
        {
            get;
            private set;
        }

        /// <summary>
        /// The number of items in the <see cref="ReorderableList"/>.
        /// </summary>
        public int Count
        {
            get { return ListProperty != null ? ListProperty.arraySize : List.Count; }
        }

        /// <summary>
        /// Whether the items in the <see cref="ReorderableList"/> can be reordered.
        /// </summary>
        public bool IsReorderable
        {
            get;
            set;
        } = true;

        /// <summary>
        /// If the list is nested then when an UndoRedo occurs the list will not refresh itself but instead let the root list do it.
        /// When a nested list has been moved and an undo occurs the nested list property path may now be incorrect so refreshing would cause errors.
        /// </summary>
        public bool IsNestedList
        {
            get;
            set;
        }

        public bool ShowDropdownIcon
        {
            set { this.Q<Image>("addButton").image = LibraryUtility.LoadLibraryIcon("list_add" + (value ? "_dropdown" : "")); }
        }

        internal ScrollView ScrollView { get; private set; }

        public event Func<VisualElement> MakeItem;
        public event Action<ReorderableList, VisualElement, int> BindItem;
        public event Action<ReorderableList, VisualElement, int> UnbindItem;

        public event Action<int> OnSelectionChanged;
        /// <summary>
        /// Used to add an item to the item source of the <see cref="ReorderableList"/>.
        /// </summary>
        public event Action<ReorderableList> AddItem;

        /// <summary>
        /// Used to remove an item from the item source of the <see cref="ReorderableList"/>.
        /// </summary>
        public event Action<ReorderableList, int> RemoveItem;

        /// <summary>
        /// Used to reorder items of the source of the <see cref="ReorderableList"/>. Params: <c>list, from, to, position</c>.
        /// </summary>
        public event Action<ReorderableList, int, int, ReletiveDragPosition> Reorder;

        public ReorderableList(SerializedProperty listProperty)
        {
            ListProperty = listProperty.Copy();

            Initialize();

            this.Q<Label>("headerLabel").text = listProperty.displayName;
        }

        private void Initialize()
        {
            LibraryUtility.LoadVisualTree(nameof(ReorderableList)).CloneTree(this);
            AddToClassList(ussClassName);
            styleSheets.Add(LibraryUtility.LoadStyleSheet("Common"));
            styleSheets.Add(LibraryUtility.LoadStyleSheet($"Common{(EditorGUIUtility.isProSkin ? "dark" : "Light")}"));

            ScrollView = this.Q<ScrollView>(className: itemContainerUssClassName);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            VisualElement footer = this.Q(className: footerUssClassName);

            _addManipulator = new Clickable(HandleAddItem);

            var addElement = new Image();
            addElement.name = "addButton";
            addElement.image = LibraryUtility.LoadLibraryIcon("list_add");
            addElement.AddToClassList(buttonUssClassName);
            addElement.AddManipulator(_addManipulator);

            _removeManipulator = new Clickable(HandleRemoveItem);

            var removeElement = new Image();
            removeElement.name = "removeButton";
            removeElement.image = LibraryUtility.LoadLibraryIcon("list_remove");
            removeElement.AddToClassList(buttonUssClassName);
            removeElement.AddManipulator(_removeManipulator);
            removeElement.SetEnabled(false);

            footer.Add(addElement);
            footer.Add(removeElement);

            this.AddManipulator(new ReorderListManipulator(this));
        }


        public void Refresh()
        {
            if (ListProperty != null)
                ListProperty.serializedObject.Update();

            // The empty list element is added to the scroll view, so we need to remove it if we are adding items so that MakeItem will be called.
            if (Count >= 1 && ScrollView.childCount == 1 && ScrollView[0].ClassListContains(emptyUssClassName))
                ScrollView.RemoveAt(0);

            while (Count > ScrollView.childCount)
            {
                ScrollView.Add(MakeListItem());
            }

            while (Count < ScrollView.childCount)
            {
                ScrollView.RemoveAt(ScrollView.childCount - 1);
            }

            for (int i = 0; i < ScrollView.childCount; i++)
            {
                UnbindListItem(ScrollView[i], i);
                BindListItem(ScrollView[i], i);
            }

            if (Count == 0)
            {
                ScrollView.Add(CreateEmptyListElement());
            }
        }

        private ListItemElement MakeListItem()
        {
            var item = new ListItemElement(true);
            if (MakeItem != null)
            {
                item.Add(MakeItem());
            }
            else if (ListProperty != null)
            {
                item.Add(new PropertyField());
            }
            
            return item;
        }

        private void BindListItem(VisualElement element, int index)
        {
            element.userData = index;
            element.RegisterCallback<MouseDownEvent>(OnItemMouseDown);
            element.EnableInClassList(selectedItemUssClassName, index == _selectedIndex);

            if (BindItem != null)
                BindItem(this, element[0], index);
            else if (ListProperty != null)
                ((PropertyField)element[0]).BindProperty(ListProperty.GetArrayElementAtIndex(index));
        }

        private void UnbindListItem(VisualElement element, int index)
        {
            element.UnregisterCallback<MouseDownEvent>(OnItemMouseDown);
            element.EnableInClassList(selectedItemUssClassName, false);

            UnbindItem?.Invoke(this, ScrollView[index], index);
        }

        private void OnItemMouseDown(MouseDownEvent evt)
        {
            int index =  (int)((ListItemElement)evt.currentTarget).userData;
            SetSelection(index);
            evt.StopPropagation();
        }

        private VisualElement CreateEmptyListElement()
        {
            var label = new Label("List is Empty");
            label.AddToClassList(emptyUssClassName);
            return label;
        }

        internal void Swap(int fromIndex, int toIndex, ReletiveDragPosition position)
        {
            if (fromIndex == toIndex)
                return;

            if (position == ReletiveDragPosition.OverItem && fromIndex < toIndex)
                toIndex--;

            // Update the selection.
            if (fromIndex == _selectedIndex)
                _selectedIndex = toIndex;

            if (Reorder != null)
            {
                Reorder(this, fromIndex, toIndex, position);
            }
            else if (ListProperty != null)
            {
                ListProperty.MoveArrayElement(fromIndex, toIndex);
                ListProperty.serializedObject.ApplyModifiedProperties();
            }

            Refresh();
            SetSelection(toIndex);
        }

        /// <summary>
        /// Set the selected item in the <see cref="ReorderableList"/>.
        /// </summary>
        /// <param name="index">The index of the item to select.</param>
        public void SetSelection(int index)
        {
            // Exit early if the index is already selected.
            if (index == _selectedIndex)
                return;

            // We deselect before the out of range checks so that even if the index is out of range the item is still deselected.
            Deselect();

            _selectedIndex = index;
            
            // Exit early if the index is out of range.
            if (index < 0 || index >= Count)
                return;

            GetItemAtIndex(index).SetSelected(true);

            // Enable the manipulator element that is used to remove the selected element when clicked.
            _removeManipulator.target.SetEnabled(true);

            OnSelectionChanged?.Invoke(_selectedIndex);
        }

        /// <summary>
        /// Deselects the currently selected item.
        /// </summary>
        private void Deselect()
        {
            _removeManipulator.target.SetEnabled(false);

            if (_selectedIndex >= 0)
            {
                if (_selectedIndex < Count)
                    GetItemAtIndex(_selectedIndex).SetSelected(false);

                _selectedIndex = -1;
            }
        }

        private void HandleAddItem()
        {
            if (AddItem != null)
            {
                AddItem(this);
            }
            else if (ListProperty != null)
            {
                ListProperty.arraySize++;
                ListProperty.serializedObject.ApplyModifiedProperties();
            }

            Refresh();
        }

        private void HandleRemoveItem()
        {
            if (_selectedIndex >= 0 && _selectedIndex < Count)
            {
                if (RemoveItem != null)
                {
                    RemoveItem(this, _selectedIndex);
                }
                else if (ListProperty != null)
                {
                    ListProperty.DeleteArrayElementAtIndex(_selectedIndex);
                    ListProperty.serializedObject.ApplyModifiedProperties();
                }

                if (_selectedIndex >= Count || Count == 0)
                    SetSelection(Count - 1);

                Refresh();
            }
        }

        private ListItemElement GetItemAtIndex(int index)
        {
            ListItemElement item = null;
            if (index >= 0 && index < ScrollView.childCount)
            {
                item = ScrollView[index] as ListItemElement;
            }

            return item;
        }

        private void OnUndoRedo()
        {
            if (!IsNestedList)
                Refresh();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null || _hasInitialized)
                return;

            Refresh();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            Undo.undoRedoPerformed -= OnUndoRedo;
        }
    }
}
