using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    [CustomEditor(typeof(CompoundCollection))]
    internal class CompoundCollectionEditor : LibraryCollectionEditor
    {
        private EnumField _autoSourceField;
        private ReorderableList _autoSourcesList;

        protected override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var rootElement = base.CreateInspectorGUI();

            _autoSourceField = new EnumField("Auto Source", ((CompoundCollection)target).AutoSource);
            _autoSourceField.RegisterValueChangedCallback(evt =>
            {
                var groupTarget = (CompoundCollection)target;
                groupTarget.AutoSource = (AutoSourceOption)evt.newValue;
                _autoSourcesList.style.display = groupTarget.AutoSource == AutoSourceOption.None ? DisplayStyle.None : DisplayStyle.Flex;
            });
            rootElement.Insert(0, _autoSourceField);

            _autoSourcesList = new ReorderableList(serializedObject.FindProperty("_autoSourceCollections"));
            _autoSourcesList.SetEnabled(false);
            rootElement.Insert(1, _autoSourcesList);

            if (((CompoundCollection)target).AutoSource == AutoSourceOption.None)
                _autoSourcesList.style.display = DisplayStyle.None;

            var sourceCollectionsElement = new ReorderableList(serializedObject.FindProperty("_sourceCollections"));
            rootElement.Insert(2, sourceCollectionsElement);
           

            return rootElement;
        }

        private void OnUndoRedo()
        {
            if (target is CompoundCollection groupTarget && _autoSourceField != null)
            {
                _autoSourceField.SetValueWithoutNotify(groupTarget.AutoSource);
                _autoSourcesList.style.display = groupTarget.AutoSource == AutoSourceOption.None ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    } 
}
