using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    /// <summary>
    /// Derive from this class to create a <see cref="PropertyDrawer"/> for a <see cref="LibraryRuleBase"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(LibraryRuleBase), true)]
    public class LibraryRuleBasePropertyDrawer : PropertyDrawer
    {
        public static readonly string ussClassName = "bewildered-library-rule";
        public static readonly string OperandPopupUssClassName = ussClassName + "__operand-popup";

        private static readonly string _typeLabelUssClassName = ussClassName + "__type-label";

        /// <summary>
        /// Override  this method to create your GUI for this <see cref="LibraryRuleBase"/> property. 
        /// <see cref="VisualElement"/>s should be added to the <c>base.CreatePropertyGUI(property)</c> element.
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> to make the GUI for.</param>
        /// <returns>The <see cref="VisualElement"/> containing the GUI for the <see cref="LibraryRuleBase"/> <see cref="SerializedProperty"/>.</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var rootElement = new VisualElement();
            rootElement.AddToClassList(ussClassName);

            if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
                return rootElement;

            var operandField = new EnumField();
            operandField.BindProperty(property.FindPropertyRelative("_operand"));
            operandField.AddToClassList(OperandPopupUssClassName);
            rootElement.Add(operandField);

            var typeName = property.managedReferenceFullTypename.Substring(property.managedReferenceFullTypename.LastIndexOf('.') + 1);
            typeName = typeName.Replace("Rule", "");
            var typeLabel = new Label(ObjectNames.NicifyVariableName(typeName));
            typeLabel.AddToClassList(_typeLabelUssClassName);
            rootElement.Add(typeLabel);

            return rootElement;
        }

        // These methods are overridden so they can be sealed to make the API cleaner.
        // They cannot be used because the Elements in the CreatePropertyGUI are required for rules to be used properly.
        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
        }

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }
    } 
}
