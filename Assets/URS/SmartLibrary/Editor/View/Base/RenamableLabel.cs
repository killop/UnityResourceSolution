using UnityEngine;
using UnityEngine.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    /// <summary>
    /// A label that can be renamed when double-clicked.
    /// </summary>
    internal class RenamableLabel : TextElement
    {
        public new static readonly string ussClassName = "bewildered-renamable-label";

        /// <summary>
        /// The field used for renaming the label.
        /// </summary>
        public TextField fieldElement { get; }

        public bool canBeRenamed { get; set; }

        public RenamableLabel()
        {
            AddToClassList(ussClassName);
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            fieldElement = new TextField
            {
                isDelayed = true,
            };
            fieldElement.SetDisplay(false);
            fieldElement.RegisterValueChangedCallback(OnFieldValueChange);
            fieldElement.RegisterCallback<FocusOutEvent>(evt => EndRenaming());
            fieldElement.Q(TextField.textInputUssName).RegisterCallback<KeyDownEvent>(OnInputFieldKeyDown);
            
            Add(fieldElement);
        }
        
        public void BeginRenaming()
        {
            if (canBeRenamed)
            {
                fieldElement.SetValueWithoutNotify(text);
                fieldElement.SetDisplay(true);
                fieldElement.Q(TextField.textInputUssName).Focus();
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 2)
                BeginRenaming();
        }

        private void EndRenaming()
        {
            fieldElement.SetDisplay(false);
        }

        private void OnFieldValueChange(ChangeEvent<string> evt)
        {
            text = evt.newValue;
        }

        private void OnInputFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return)
                EndRenaming();
        }
    }
}
