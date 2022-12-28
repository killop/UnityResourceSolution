using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class FolderSelector : EditorWindow
    {
        private class FolderTreeItem :BTreeViewItem
        {
            public string Path { get; private set; }

            public FolderTreeItem(int id, string path) :base(id)
            {
                Path = path;
            }
        }

        private static readonly string _spinnerContainerUssClassName = "bewildered-folder-selector__spinner-container";
        private static readonly string _folderItemUssClassName = "bewildered-folder-selector__folder";

        private Action<string> _onSelect;
        private string _currentPath;
        private int _currentId = -2;

        private string _dataPath;

        private BTreeView _folderTree;
        private VisualElement _spinnerContainer;
        private ProcessSpinner _spinner;

        public static void Open(Action<string> onSelect, string currentPath = "")
        {
            var window = EditorWindow.CreateInstance<FolderSelector>();
            window.minSize = new Vector2(250, 350);
            window._onSelect = onSelect;
            window._currentPath = currentPath;
            window.ShowAuxWindow();
        }

        private async void OnEnable()
        {
            // Setup basic window settings.
            titleContent = new GUIContent("Folder");
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("Common"));
            rootVisualElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("FolderSelector"));

            // Setup spinner.
            _spinnerContainer = new VisualElement();
            _spinnerContainer.AddToClassList(_spinnerContainerUssClassName);
            rootVisualElement.Add(_spinnerContainer);

            _spinner = new ProcessSpinner();
            _spinnerContainer.Add(_spinner);

            var loadingLabel = new Label("Fetching Project Folders...");
            _spinnerContainer.Add(loadingLabel);

            // Build folder tree.
            IList<BTreeViewItem> rootItems = await BuildTree();

            rootVisualElement.Remove(_spinnerContainer);

            _folderTree = new BTreeView(rootItems, 18, MakeItem, BindItem);
            _folderTree.OnSelectionChanged += items => _onSelect?.Invoke(((FolderTreeItem)items.First()).Path);
            _folderTree.style.flexGrow = 1;
            rootVisualElement.Add(_folderTree);
            if (_currentId > -2)
                _folderTree.SetSelection(_currentId);
        }

        private Task<IList<BTreeViewItem>> BuildTree()
        {
            _dataPath = Application.dataPath;
            var task = new Task<IList<BTreeViewItem>>(CreateTree);
            task.Start();
            return task;
        }

        private IList<BTreeViewItem> CreateTree()
        {
            var rootItems = new List<BTreeViewItem>();
            var allItems = new Dictionary<string, FolderTreeItem>();
            int nextId = 0;

            rootItems.Add(new FolderTreeItem(-1, ""));

            string[] allAssetsDirectoryPaths = Directory.GetDirectories(_dataPath, "*", SearchOption.AllDirectories);

            foreach (var directoryPath in allAssetsDirectoryPaths)
            {
                string reletivePath = GetProjectRelativePath(directoryPath);
                reletivePath = reletivePath.Replace('\\', '/');

                string folderName = reletivePath.Substring(reletivePath.LastIndexOf('/') + 1);
                var item = new FolderTreeItem(++nextId, reletivePath);
                allItems.Add(reletivePath, item);

                if (reletivePath.Equals(_currentPath, StringComparison.InvariantCultureIgnoreCase)) {
                    _currentId = nextId;
                }
                    

                if (reletivePath.Remove(0, "assets/".Length) == folderName)
                {
                    rootItems.Add(item);
                }
                else
                {
                    string parentFolder = reletivePath.Substring(0, reletivePath.LastIndexOf('/'));
                    if (allItems.ContainsKey(parentFolder))
                        allItems[parentFolder].AddChild(item);
                }
            }

            return rootItems;
        }

        private string GetProjectRelativePath(string path)
        {
            if (path.StartsWith(_dataPath))
                return "Assets" + path.Substring(_dataPath.Length);
            else
                return path;
        }

        private VisualElement MakeItem()
        {
            var itemElement = new VisualElement();
            itemElement.AddToClassList(_folderItemUssClassName);

            var icon = new Image();
            icon.AddToClassList(LibraryConstants.IconUssClassName);
            itemElement.Add(icon);

            var label = new Label();
            itemElement.Add(label);

            return itemElement;
        }

        private void BindItem(VisualElement element, BTreeViewItem item)
        {
            var folderItem = (FolderTreeItem)item;
            if (folderItem.Id == -1)
            {
                element.Q<Image>().image = EditorGUIUtility.IconContent("FolderEmpty Icon").image;
                element.Q<Label>().text = "None";
            }
            else
            {
                int lastIndex = folderItem.Path.LastIndexOf('/');
                element.Q<Image>().image = EditorGUIUtility.IconContent("Folder Icon").image;
                element.Q<Label>().text = folderItem.Path.Substring(lastIndex + 1);
            }
        }
    } 
}
