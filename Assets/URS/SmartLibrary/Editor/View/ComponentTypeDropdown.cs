using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace Bewildered.SmartLibrary
{
    internal class ComponentTypeDropdown : AdvancedDropdown
    {
        internal class ComponentItem : AdvancedDropdownItem
        {
            public string Command { get; }

            public Type Type
            {
                get { return GetTypeFromCommand(); }
            }

            public ComponentItem(string command, string name) : base(name)
            {
                Command = command;
                if (command.StartsWith("SCRIP"))
                {
                    int instanceID = int.Parse(command.Substring(6));
                    var obj = (MonoScript)EditorUtility.InstanceIDToObject(instanceID);
                    icon = AssetPreview.GetMiniThumbnail(obj);
                }
                else
                {
                    int classID = int.Parse(command);
                    icon = AssetPreviewRef.GetMiniTypeThumbnailFromClassID(classID);
                }
            }

            private Type GetTypeFromCommand()
            {
                Type type;
                if (Command.StartsWith("SCRIP"))
                {
                    int instanceID = int.Parse(Command.Substring(6));
                    var obj = (MonoScript)EditorUtility.InstanceIDToObject(instanceID);
                    type = obj.GetClass();
                }
                else
                {
                    int classID = int.Parse(Command);
                    var unityType = _findTypeByPersistentTypeIDInfo.Invoke(null, new object[] { classID });
                    string qualifiedName = (string)_qualifiedNameInfo.GetValue(unityType);

                    var engineAssembly = typeof(MonoBehaviour).Assembly;
                    type = engineAssembly.GetType("UnityEngine." + qualifiedName);
                    if (type == null)
                    {
                        type = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == qualifiedName);
                    }
                }

                return type;
            }
        }
        
        private struct MenuItemData
        {
            public string path;
            public string command;
        }

        private static Type _unityTypeType;
        private static MethodInfo _findTypeByPersistentTypeIDInfo;
        private static PropertyInfo _qualifiedNameInfo;

        public event Action<Type> OnSelected;

        static ComponentTypeDropdown()
        {
            _unityTypeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.UnityType");
            _findTypeByPersistentTypeIDInfo = TypeAccessor.GetMethod(_unityTypeType, "FindTypeByPersistentTypeID");
            _qualifiedNameInfo = TypeAccessor.GetProperty(_unityTypeType, "qualifiedName");
        }

        public ComponentTypeDropdown(AdvancedDropdownState state) : base(state)
        {
            minimumSize = new Vector2(200, 270);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var rootItem = new AdvancedDropdownItem("Component");
            var addedItems = new Dictionary<string, AdvancedDropdownItem>();
            addedItems.Add("Component", rootItem);

            var menuItemsData = GetSortedMenuItems();
            foreach (var menuItemData in menuItemsData)
            {
                if (menuItemData.command == "ADD")
                    continue;

                string[] splitPath = menuItemData.path.Split('/');
                AdvancedDropdownItem parentItem = rootItem;
                for (int i = 0; i < splitPath.Length - 1; i++)
                {
                    string name = splitPath[i];

                    if (!addedItems.TryGetValue(name, out AdvancedDropdownItem item))
                    {
                        item = new AdvancedDropdownItem(name);
                        parentItem.AddChild(item);
                        addedItems.Add(name, item);
                    }
                        
                    parentItem = item;
                }
                
                var typeItem = new ComponentItem(menuItemData.command, splitPath[splitPath.Length-1]);
                parentItem.AddChild(typeItem);
            }
            
            return rootItem;
        }

        private static List<MenuItemData> GetSortedMenuItems()
        {
            var componentMenus =  Unsupported.GetSubmenus("Component");
            var componentCommands =  Unsupported.GetSubmenusCommands("Component");

            var menuData = new List<MenuItemData>();
            
            for (int i = 0; i < componentMenus.Length; i++)
            {
                menuData.Add(new MenuItemData() {path = componentMenus[i], command = componentCommands[i]});
            }
            
            menuData.Sort((data, otherData) => string.CompareOrdinal(data.path, otherData.path));

            return menuData;
        }
        

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var componentItem = (ComponentItem)item;
            OnSelected?.Invoke(componentItem.Type);
        }
    }
}
