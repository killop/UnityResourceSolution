using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    internal static class GenericMenuExtensions
    {
        private static FieldInfo _menuItemsInfo;

        private static Type _menuItemType;
        
        public static void InsertItem(this GenericMenu menu, int index, GUIContent content, GenericMenu.MenuFunction func)
        {
            var menuItems = GetMenuItems(menu);
            
            var newMenuItem = Activator.CreateInstance(_menuItemType, new object[] { content, false, false, func });
            menuItems.Insert(index, newMenuItem);
        }
        
        public static void InsertItem(this GenericMenu menu, int index, GUIContent content, GenericMenu.MenuFunction2 func, object userData)
        {
            var menuItems = GetMenuItems(menu);
            
            var newMenuItem = Activator.CreateInstance(_menuItemType, new object[] { content, false, false, func, userData });
            menuItems.Insert(index, newMenuItem);
        }

        private static IList GetMenuItems(GenericMenu menu)
        {
            if (_menuItemsInfo == null)
            {
                // Unity changed from `private ArrayList menuItems;` to `private List<MenuItem> m_MenuItems;`
                // in a minor version of 2020.3 so we try to get the old one as a fallback if it can't get the new format.
                _menuItemsInfo = TypeAccessor.GetField(typeof(GenericMenu), "m_MenuItems");
                if (_menuItemsInfo == null)
                    _menuItemsInfo = TypeAccessor.GetField(typeof(GenericMenu), "menuItems");
                
                _menuItemType = typeof(GenericMenu).GetNestedType("MenuItem", BindingFlags.NonPublic);
            }

            var menuItems = (IList) _menuItemsInfo.GetValue(menu);
            return menuItems;
        }
        
    }
}
