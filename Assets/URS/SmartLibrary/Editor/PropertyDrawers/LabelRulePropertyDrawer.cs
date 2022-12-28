using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    [CustomPropertyDrawer(typeof(LabelRule))]
    internal class LabelRulePropertyDrawer : LibraryRuleBasePropertyDrawer
    {
        public new static readonly string ussClassName = "bewildered-library-label-rule";

        private static readonly string _labelUssClassName = ussClassName + "__label";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var rootElement = base.CreatePropertyGUI(property);
            rootElement.AddToClassList(ussClassName);

            var text = new TextField();
            text.AddToClassList(_labelUssClassName);
            text.BindProperty(property.FindPropertyRelative("_label"));
            rootElement.Add(text);

            return rootElement;
        }
    } 
}
