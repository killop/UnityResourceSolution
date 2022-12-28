using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Bewildered.SmartLibrary.UI
{
    internal class ComponentTypeField : TypeField
    {
        protected override void OnFieldSelected(Rect buttonRect, Action<Type> onTypeSelected)
        {
            ComponentTypeDropdown dropdown = new ComponentTypeDropdown(new AdvancedDropdownState());
            dropdown.OnSelected += onTypeSelected;
            buttonRect.width = 225;
            dropdown.Show(buttonRect);
        }
    }
}
