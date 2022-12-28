using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class FolderField : BaseField<string>
    {
        public new static readonly string ussClassName = "bewildered-folder-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public static readonly string folderUssClassName = ussClassName + "__folder";
        public static readonly string selectorUssClassName = ussClassName + "__selector";

        private class FolderFieldDisplay : VisualElement
        {
            public static readonly string ussClassName = "bewildered-folder-field-display";
            public static readonly string iconUssClassName = ussClassName + "__icon";
            public static readonly string labelUssClassName = ussClassName + "__label";

            private readonly FolderField _folderField;

            private readonly Image _folderIcon;
            private readonly Label _folderLabel;

            public FolderFieldDisplay(FolderField folderField)
            {
                _folderField = folderField;

                AddToClassList(ussClassName);

                _folderIcon = new Image();
                _folderIcon.pickingMode = PickingMode.Ignore;
                _folderIcon.image = EditorGUIUtility.IconContent("Folder Icon").image;
                _folderIcon.AddToClassList(iconUssClassName);

                _folderLabel = new Label();
                _folderLabel.pickingMode = PickingMode.Ignore;
                _folderLabel.AddToClassList(labelUssClassName);

                Add(_folderIcon);
                Add(_folderLabel);
            }

            public void Update()
            {
                //_folderLabel.text = AssetDatabase.GUIDToAssetPath(_folderField.value);
                _folderLabel.text = _folderField.value;
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
                if (evt.clickCount == 1 && !string.IsNullOrEmpty(_folderField.value))
                {
                    var folderInstanceID = AssetUtility.GetMainAssetInstanceIDFromGUID(AssetDatabase.AssetPathToGUID(_folderField.value));
                    if (folderInstanceID != 0)
                        EditorGUIUtility.PingObject(folderInstanceID);

                    evt.StopPropagation();
                }
            }

            private string ValidateDnD()
            {
                string[] paths = DragAndDrop.paths;
                if (paths.Length > 0)
                {
                    // Is folder.
                    if (!Path.HasExtension(paths[0]))
                        return paths[0];
                }

                return "";
            }

            private void OnDragUpdated(EventBase evt)
            {
                string validatedPath = ValidateDnD();
                if (!string.IsNullOrEmpty(validatedPath))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                    evt.StopPropagation();
                }
            }

            private void OnDragPerform(EventBase evt)
            {
                string validatedPath = ValidateDnD();
                if (!string.IsNullOrEmpty(validatedPath))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    _folderField.value = validatedPath;// AssetDatabase.AssetPathToGUID(validatedPath);
                    DragAndDrop.AcceptDrag();

                    evt.StopPropagation();
                }
            }
        }

        private class FolderFieldSelector : Image
        {
            private readonly FolderField _folderField;

            public FolderFieldSelector(FolderField folderField)
            {
                _folderField = folderField;
                image = (Texture2D)EditorGUIUtility.IconContent("Icon Dropdown").image;
            }

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    _folderField.ShowFolderSelector();
            }
        }

        private FolderFieldDisplay _fieldDisplay;


        public FolderField() : base(null, null)
        {
            var visualInput = this.Q(className: BaseField<string>.inputUssClassName);

            visualInput.focusable = false;
            labelElement.focusable = false;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(ussClassName);
            visualInput.AddToClassList(inputUssClassName);

            _fieldDisplay = new FolderFieldDisplay(this) { focusable = true };
            _fieldDisplay.AddToClassList(folderUssClassName);
            visualInput.Add(_fieldDisplay);

            var folderSelector = new FolderFieldSelector(this);
            folderSelector.AddToClassList(selectorUssClassName);
            visualInput.Add(folderSelector);
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            _fieldDisplay.Update();
        }

        internal void ShowFolderSelector()
        {
          //  FolderSelector.Open(OnFolderSelected, AssetDatabase.GUIDToAssetPath(value));
            FolderSelector.Open(OnFolderSelected, value);
        }

        private void OnFolderSelected(string path)
        {
            value = path;// AssetDatabase.AssetPathToGUID(path);
        }
    } 
}
