using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Audio;

using UnityObject = UnityEngine.Object;

namespace Bewildered.SmartLibrary.UI
{
    internal class UnityTypeDropdown : EditorWindow
    {
        private class TypeTreeItem : BTreeViewItem
        {
            public Type Type { get; }

            public TypeTreeItem(int id, Type type) : base(id)
            {
                Type = type;
            }
        }

        private class GroupTreeItem : BTreeViewItem
        {
            public string Name { get; }

            public GroupTreeItem(int id, string name) : base(id)
            {
                Name = name;
            }
        }

        private class TypComparer : IComparer<Type>
        {
            public int Compare(Type x, Type y)
            {
                var xSplit = x.FullName.Split('.');
                var ySplit = y.FullName.Split('.');

                int count = xSplit.Length > ySplit.Length ? xSplit.Length : ySplit.Length;
                for (int i = 0; i < count; i++)
                {
                    if (i >= xSplit.Length)
                        return -1;

                    if (i >= ySplit.Length)
                        return 1;

                    if (xSplit[i].CompareTo(ySplit[i]) != 0)
                    {
                        if (xSplit.Length > ySplit.Length)
                            return 1;
                        else if (xSplit.Length < ySplit.Length)
                            return -1;
                        else
                            return xSplit[i].CompareTo(ySplit[i]);
                    }
                }

                return x.FullName.CompareTo(y.FullName);
            }
        }

        private static readonly Type[] _commonTypes = new Type[]
        {
            typeof(GameObject), typeof(Texture), typeof(Material), typeof(Sprite), typeof(Shader), typeof(AnimationClip),
            typeof(UnityEditor.Animations.AnimatorController), typeof(AudioClip), typeof(AudioMixer), typeof(SceneAsset),
            typeof(PhysicMaterial), typeof(Font), typeof(StyleSheet), typeof(MonoScript)
        };

        private static readonly Type[] _types = TypeCache.GetTypesDerivedFrom<UnityObject>()
                .Where(t =>
                t.IsVisible &&
                !t.IsSubclassOf(typeof(Editor)) &&
                !t.IsSubclassOf(typeof(EditorWindow)) &&
                !t.IsSubclassOf(typeof(AssetImporter)) &&
                !t.IsSubclassOf(typeof(Component))).OrderBy(t => t, new TypComparer()).ToArray();

        private Action<Type> _onTypeSelect;

        public static void Open(Rect rect, Action<Type> onTypeSelect, Type currentType = null)
        {
            var window = CreateInstance<UnityTypeDropdown>();
            window._onTypeSelect = onTypeSelect;
            window.ShowAsDropDown(rect, new Vector2(250, 300));
        }

        private void OnEnable()
        {
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("Common"));
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("DropdownWindow"));
            rootVisualElement.AddToClassList("bewildered-dropdown");

            var tree = new BTreeView(CreateItems(), 18, MakeItem, BindItem);
            tree.ExpandItem(1); // Expand "Common" item since those types are most likely going to be selected.
            tree.OnSelectionChanged += items =>
            {
                var item = items.FirstOrDefault();
                if (item != null)
                {
                    if (item is TypeTreeItem typeItem)
                    {
                        _onTypeSelect?.Invoke(typeItem.Type);
                        Close();
                    }
                }
            };
            
            tree.style.flexGrow = 1;
            rootVisualElement.Add(tree);
        }

        private IList<BTreeViewItem> CreateItems()
        {
            List<BTreeViewItem> rootItems = new List<BTreeViewItem>();
            int nextId = 0;

            var noneItem = new TypeTreeItem(nextId++, null);
            rootItems.Add(noneItem);

            GroupTreeItem commonTypesItem = new GroupTreeItem(nextId++, "Common");
            foreach (var type in _commonTypes)
            {
                commonTypesItem.AddChild(new TypeTreeItem(nextId++, type));
            }
            rootItems.Add(commonTypesItem);

            var itemGroups = new Dictionary<string, GroupTreeItem>();
            var miscGroupItem = new GroupTreeItem(nextId++, "Other");

            foreach (var type in _types)
            {
                GroupTreeItem parentItem = miscGroupItem;

                CreateGroups(type, rootItems, ref nextId, itemGroups, ref parentItem);

                parentItem.AddChild(new TypeTreeItem(nextId++, type));
            }

            if (miscGroupItem.ChildCount > 0)
                rootItems.Add(miscGroupItem);

            return rootItems;
        }

        private void CreateGroups(Type type, List<BTreeViewItem> rootItems, ref int nextId, Dictionary<string, GroupTreeItem> itemGroups, ref GroupTreeItem parentItem)
        {
            string[] splitName = type.FullName.Split('.');
            string pathName = "";

            for (int i = 0; i < splitName.Length - 1; i++)
            {
                string previousPathName = pathName;
                pathName += i > 0 ? '.' + splitName[i] : splitName[i];

                if (itemGroups.TryGetValue(pathName, out GroupTreeItem parent))
                {
                    parentItem = parent;
                }
                else
                {
                    parentItem = new GroupTreeItem(nextId++, splitName[i]);
                    if (i == 0)
                        rootItems.Add(parentItem);
                    else
                        itemGroups[previousPathName].AddChild(parentItem);

                    itemGroups.Add(pathName, parentItem);
                }
            }
        }

        private VisualElement MakeItem()
        {
            var itemElement = new VisualElement();
            itemElement.style.flexDirection = FlexDirection.Row;

            var icon = new Image();
            icon.AddToClassList(LibraryConstants.IconUssClassName);
            itemElement.Add(icon);

            itemElement.Add(new Label());

            return itemElement;
        }

        private void BindItem(VisualElement element, BTreeViewItem item)
        {
            if (item is GroupTreeItem groupItem)
            {
                if (groupItem.Id == 1)
                    element.Q<Image>().image = EditorGUIUtility.IconContent("FolderFavorite Icon").image;
                else
                    element.Q<Image>().image = EditorGUIUtility.IconContent("Folder Icon").image;

                element.Q<Label>().text = groupItem.Name;
            }
            else if (item is TypeTreeItem typeItem)
            {
                if (typeItem.Type == null)
                {
                    element.Q<Image>().image = null;
                    element.Q<Label>().text = "(None)";
                }
                else
                {
                    element.Q<Image>().image = GetTextureForType(typeItem.Type);
                    element.Q<Label>().text = typeItem.Type.Name;
                }
            }
        }

        private static Texture2D _scriptableObjectIcon;
        private static Texture2D _defaultIcon;

        private Texture2D GetTextureForType(Type type)
        {
            if (_scriptableObjectIcon == null)
                _scriptableObjectIcon = (Texture2D)EditorGUIUtility.IconContent("ScriptableObject Icon").image;

            if (_defaultIcon == null)
                _defaultIcon = (Texture2D)EditorGUIUtility.IconContent("DefaultAsset Icon").image;

            var icon = AssetUtility.LoadBuiltinTypeIcon(type);

            if (icon == null)
                icon = AssetPreview.GetMiniTypeThumbnail(type);

            if (icon == _defaultIcon)
                icon = _scriptableObjectIcon;

            return icon;
        }
    }

}