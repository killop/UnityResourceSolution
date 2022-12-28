using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    [CustomPropertyDrawer(typeof(PrefabRule))]
    internal class PrefabRulePropertyDrawer : LibraryRuleBasePropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = base.CreatePropertyGUI(property);

            var enumField = new EnumField();
            enumField.style.flexShrink = 1;
            enumField.BindProperty(property.FindPropertyRelative("_prefabType"));
            root.Add(enumField);

            return root;
        }
    }
}
