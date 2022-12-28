using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    internal class CollectionField : BaseField<Object>
    {
        public new static readonly string ussClassName = "bewildered-collection-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public static readonly string collectionUssClassName = ussClassName + "__collection";
        public static readonly string selectorUssClassName = ussClassName + "__selector";

        private class CollectionFieldDisplay : VisualElement
        {
            public static readonly string ussClassName = "bewildered-collection-field-display";
            public static readonly string iconUssClassName = ussClassName + "__icon";
            public static readonly string labelUssClassName = ussClassName + "__label";

            private readonly CollectionField _collectionField;

            private readonly Image _collectionIcon;
            private readonly Label _collectionLabel;

            public CollectionFieldDisplay(CollectionField collectionField)
            {
                _collectionField = collectionField;

                AddToClassList(ussClassName);

                _collectionIcon = new Image();
                _collectionIcon.pickingMode = PickingMode.Ignore;
                _collectionIcon.image = LibraryConstants.DefaultCollectionIcon;
                _collectionIcon.AddToClassList(iconUssClassName);

                _collectionLabel = new Label();
                _collectionLabel.pickingMode = PickingMode.Ignore;
                _collectionLabel.AddToClassList(labelUssClassName);

                Add(_collectionIcon);
                Add(_collectionLabel);
            }

            public void Update()
            {
                if (_collectionField.value is LibraryCollection collection)
                {
                    _collectionIcon.image = collection.Icon;
                    _collectionLabel.text = collection.name + " (" + collection.GetType().Name + ")";
                }
                else
                {
                    _collectionIcon.image = LibraryConstants.DefaultCollectionIcon;
                    _collectionLabel.text = string.Empty;
                }
            }

            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                if (evt == null)
                    return;

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    OnMouseDown(evt as MouseDownEvent);
                else if (evt is DragUpdatedEvent)
                    OnDragUpdated(evt);
                else if (evt is DragPerformEvent)
                    OnDragPerform(evt);
            }

            private void OnMouseDown(MouseDownEvent evt)
            {
                if (evt.clickCount == 2 && _collectionField.value)
                {
                    Selection.activeObject = _collectionField.value;

                    evt.StopPropagation();
                }
            }

            private void OnDragUpdated(EventBase evt)
            {
                LibraryCollection validCollection = ValidateDnD();
                if (validCollection)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                    evt.StopPropagation();
                }
            }

            private void OnDragPerform(EventBase evt)
            {
                LibraryCollection validCollection = ValidateDnD();
                if (validCollection)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    _collectionField.value = validCollection;
                    DragAndDrop.AcceptDrag();

                    evt.StopPropagation();
                }
            }

            private LibraryCollection ValidateDnD()
            {
                Object[] objects = DragAndDrop.objectReferences;
                if (objects.Length > 0)
                {
                    if (objects[0] is LibraryCollection collection)
                        return collection;
                }

                return null;
            }
        }

        private class CollectionFieldSelector : Image
        {
            private readonly CollectionField _collectionField;

            public CollectionFieldSelector(CollectionField collectionField)
            {
                _collectionField = collectionField;
                image = (Texture2D)EditorGUIUtility.IconContent("Icon Dropdown").image;
            }

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    _collectionField.ShowFolderSelector();
            }
        }

        private CollectionFieldDisplay _fieldDisplay;


        public CollectionField() : base(null, null)
        {
            var visualInput = this.Q(className: BaseField<LibraryCollection>.inputUssClassName);

            visualInput.focusable = false;
            labelElement.focusable = false;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(ussClassName);
            visualInput.AddToClassList(inputUssClassName);

            _fieldDisplay = new CollectionFieldDisplay(this) { focusable = true };
            _fieldDisplay.AddToClassList(collectionUssClassName);
            visualInput.Add(_fieldDisplay);

            var folderSelector = new CollectionFieldSelector(this);
            folderSelector.AddToClassList(selectorUssClassName);
            visualInput.Add(folderSelector);
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            base.SetValueWithoutNotify(newValue);
            _fieldDisplay.Update();
        }

        internal void ShowFolderSelector()
        {
            CollectionSelector.Open(OnFolderSelected, (LibraryCollection)value);
        }

        private void OnFolderSelected(LibraryCollection collection)
        {
            value = collection;
        }
    }
}