using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bewildered.SmartLibrary
{
    internal static class ElementExtensions
    {
        private static PropertyInfo _pseudoStatesProperty;
        private static Type _unityPseudoStatesType;

        /// <summary>
        /// Sets the element's display style.
        /// </summary>
        public static void SetDisplay(this VisualElement element, bool doDisplay)
        {
            element.style.display = doDisplay ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetPseudoStates(this VisualElement element, PseudoStates pseudoState)
        {
            if (_pseudoStatesProperty == null)
                _pseudoStatesProperty = typeof(VisualElement).GetProperty("pseudoStates", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (_unityPseudoStatesType == null)
                _unityPseudoStatesType = _pseudoStatesProperty.PropertyType;


            _pseudoStatesProperty.SetValue(element, Enum.Parse(_unityPseudoStatesType, pseudoState.ToString()));
        }

        public static PseudoStates GetPseudoStates(this VisualElement element)
        {
            if (_pseudoStatesProperty == null)
                _pseudoStatesProperty = typeof(VisualElement).GetProperty("pseudoStates", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var value = _pseudoStatesProperty.GetValue(element);

            return (PseudoStates)Enum.Parse(typeof(PseudoStates), value.ToString());
        }
    }

    [Flags]
    internal enum PseudoStates
    {
        Active = 1 << 0,     // control is currently pressed in the case of a button
        Hover = 1 << 1,     // mouse is over control, set and removed from dispatcher automatically
        Checked = 1 << 3,     // usually associated with toggles of some kind to change visible style
        Disabled = 1 << 5,     // control will not respond to user input
        Focus = 1 << 6,     // control has the keyboard focus. This is activated deactivated by the dispatcher automatically
        Root = 1 << 7,     // set on the root visual element
    }
}
