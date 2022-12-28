using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    [CustomPropertyDrawer(typeof(ComponentRule))]
    internal class ComponentRulePropertyDrawer : LibraryRuleBasePropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = base.CreatePropertyGUI(property);
            var field = new ComponentTypeField();
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.BindProperty(property.FindPropertyRelative("_type._typeName"));
            root.Add(field);

            return root;
        }
    } 
}
