using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    [CustomPropertyDrawer(typeof(FolderReference))]
    internal class FolderReferencePropertyDrawer : PropertyDrawer
    {
        public static readonly string ussClassName = "bewildered-library-folder-reference";
        private static readonly string _matchOptionUssClassName = ussClassName + "__match-option";
        private static readonly string _folderUssClassName = ussClassName + "__folder";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var rootElement = new VisualElement();
            rootElement.AddToClassList(ussClassName);

            var stateLabel = new Label("Include");

            var includeToggle = new Toggle();
            includeToggle.tooltip = "Whether to include or exclude files from this folder.";
            includeToggle.BindProperty(property.FindPropertyRelative("_doInclude"));
            rootElement.Add(includeToggle);

            rootElement.Add(stateLabel);

            var filterTypeField = new EnumField();
            filterTypeField.AddToClassList(_matchOptionUssClassName);
            filterTypeField.BindProperty(property.FindPropertyRelative("_matchOption"));
            rootElement.Add(filterTypeField);

            var field = new FolderField();
            field.AddToClassList(_folderUssClassName);
            field.BindProperty(property.FindPropertyRelative("_path"));
            rootElement.Add(field);

            return rootElement;
        }
    }

}