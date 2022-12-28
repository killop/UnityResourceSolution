using UnityEngine.UIElements;
using UnityEditor;

namespace Bewildered.SmartLibrary.UI
{
    [CustomPropertyDrawer(typeof(RuleSet))]
    internal class RuleSetPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var rulesElement = new RulesView(property.FindPropertyRelative("_rules"));

            return rulesElement;
        }
    }
}
