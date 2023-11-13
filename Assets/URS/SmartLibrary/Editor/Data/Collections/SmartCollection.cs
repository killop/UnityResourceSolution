using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Search;
using System;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// A smart <see cref="LibraryCollection"/> that automatically adds and removes assets based on the rules and folders.
    /// </summary>
    public class SmartCollection : LibraryCollection
    {
        private SearchContext _context;
        private bool _isSearching;

        [SerializeField] private List<FolderReference> _folders = new List<FolderReference>();

        [NonSerialized]
        public AssetGUIDHashSet _items = new AssetGUIDHashSet();
        public override AssetGUIDHashSet GetGUIDHashSet()
        {
            return _items;
        }
        /// <summary>
        /// Folders to search in for valid assets to add to the <see cref="SmartCollection"/>, or to exclude.
        /// </summary>
        public ICollection<FolderReference> Folders
        {
            get { return _folders; }
        }
        /// <summary>
        /// Determines whether the <see cref="SmartCollection"/> is currently searching for assets that match its <see cref="RuleSet"/>.
        /// </summary>
        public bool IsSearching
        {
            get { return _isSearching; }
        }

        public override void UpdateItems(bool syn=false )
        {
            SearchForItems(syn);
        }

        public override bool IsAddable(LibraryItem item)
        {
            return base.IsAddable(item) && MatchesRequiredFolders(item.AssetPath) && !string.IsNullOrWhiteSpace(Rules.GetSearchQuery());
        }

        /// <summary>
        /// Updates the <see cref="SmartCollection"/> with the specified <see cref="LibraryItem"/>,
        /// either adding it if it is valid and does not arleady contain it, or remove it if it is invalid and contains it.
        /// </summary>
        /// <param name="item">The <see cref="LibraryItem"/> to try to update.</param>
        /// <returns><c>true</c> of <paramref name="item"/> was added or removed from the <see cref="SmartCollection"/>; otherwise, <c>false</c>.</returns>
        internal bool IncrementalUpdateItem(LibraryItem item)
        {
            if (Rules.Count == 0 && _folders.Count == 0)
                return false;
            
            // We don't use IsAddable() because it checks if the collection contains the item.
            if (Rules.Evaluate(item) && MatchesRequiredFolders(item.AssetPath))
            {
                return AddItem(item);
            }
            else
            {
                return RemoveItem(item);
            }
        }
        const string k_SearchProviderIdScene = "scene";
        const string k_SearchProviderIdAsset = "asset";
        const string k_SearchProviderIdAssetDatabase = "adb";
        private void SearchForItems(bool syn=false)
        {
            _context?.Dispose();
            _isSearching = true;

            string query = string.Empty;
            string folderQuery = GetFolderSearchQuery();
            string ruleQuery = Rules.GetSearchQuery();

            bool hasFolderQuery = !string.IsNullOrEmpty(folderQuery);
            bool hasRuleQuery = !string.IsNullOrEmpty(ruleQuery);

            if (hasFolderQuery && hasRuleQuery)
                query = $"p: {folderQuery} and ({ruleQuery})";
            else if (hasFolderQuery)
                query = $"p: {folderQuery}";
            else if (hasRuleQuery)
                query = $"p: {ruleQuery}";
           // for (int i = 0; i < SearchService.Providers.Count; i++)
           // {
             //   Debug.LogError("count "+ SearchService.Providers.Count+" name "+ SearchService.Providers[i].name+"  id "+ SearchService.Providers[i].id);
           // }
           // _context = SearchService.CreateContext(new string[] {  k_SearchProviderIdAsset }, query);
           // SearchService.Request(_context, OnSearchComplete);
            OnSearchComplete(GetAssetPaths());

        }

        private void OnSearchComplete(HashSet<string> paths)
        {
            Debug.Log("OnSearchComplete " + this.CollectionName + "count " + paths.Count);
            _isSearching = false;
            ClearItems();
            HashSet<LibraryItem> addedItems = new HashSet<LibraryItem>();

            foreach (var path in paths)
            {
                if (path.Contains(".DS_Store"))
                    continue;
                //Object obj = item.ToObject();

                // QuickSearch does not remove entries of deleted assets right away.
                // So we need to handle when the SearchItem may not have an asset associated with it any more.
                //if (obj == null)
                //  continue;
                var path2= path.Replace('\\', '/');
                
                LibraryItem libraryItem = LibraryItem.GetItemInstanceByPath(path2);

                // Item is added to this second list to be used when sending the items changed notification.
                addedItems.Add(libraryItem);
                //Debug.LogError("path " + path2 + " cc name " + this.CollectionName+" null"+ (libraryItem==null));
                // Now that the item has been fully validated it can be added to the collection.
                AddItem(libraryItem, false);
            }
            NotifyItemsChanged(addedItems, LibraryItemsChangeType.Added);
        }
        private void OnSearchComplete(SearchContext context, IList<SearchItem> items)
        {
            Debug.Log("OnSearchComplete "+this.CollectionName+"count "+ items.Count);
            _isSearching = false;
            ClearItems();
            HashSet<LibraryItem> addedItems = new HashSet<LibraryItem>();

            foreach (SearchItem item in items)
            {
               UnityEngine.Object obj = item.ToObject();

                // QuickSearch does not remove entries of deleted assets right away.
                // So we need to handle when the SearchItem may not have an asset associated with it any more.
                if (obj == null)
                    continue;
                
                LibraryItem libraryItem = LibraryItem.GetItemInstance(obj);

                // Item is added to this second list to be used when sending the items changed notification.
                addedItems.Add(libraryItem);
                // Now that the item has been fully validated it can be added to the collection.
                AddItem(libraryItem, false);
            }
            NotifyItemsChanged(addedItems, LibraryItemsChangeType.Added);
        }

        public HashSet<string> GetAssetPaths()
        {
             HashSet<string> paths = new HashSet<string>();
            string excludeNameContains = "";
            foreach (var rule in Rules)
            {
                if (rule is TagRule tagRule) {
                    if (tagRule.TagType == TagRule.TagRuleType.ExcludeFileNameContains)
                    {
                        excludeNameContains = tagRule.Text;
                        break;
                    }
                }
            }
            bool checkExcludeName = !string.IsNullOrEmpty(excludeNameContains);
            for (int i = 0; i < _folders.Count; i++)
            {
                var folderConfig = _folders[i];
                string folderPath = folderConfig.Path;
                if (string.IsNullOrEmpty(folderPath))
                {
                    Debug.LogError("严重警告>>> "+ CollectionName+ " <<<指定的搜索目录，guid 已经过期!!!!，这种问题，主要是非法改变文件夹guid,请重新指定搜索目录");
                    continue;
                }
                if (!System.IO.Directory.Exists(folderPath)) continue;
                var directory= UnityIO.IO.Get(folderPath);
                UnityIO.Interfaces.IFiles files;
                if (folderConfig.MatchOption == FolderMatchOption.AnyDepth)
                {
                    files = directory.GetFiles(true);
                }
                else
                {
                    files = directory.GetFiles(false);
                }
                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var path = file.path;
                        if (checkExcludeName) {
                            var fileName = System.IO.Path.GetFileName(path);
                            if (fileName.Contains(excludeNameContains)) {
                                continue;
                            }
                        }
                        if (!paths.Contains(path))
                        {
                            paths.Add(path);
                        }
                    }
                }
            }
            return paths;
        }
        private string GetFolderSearchQuery()
        {
            string includeQuery = string.Empty;
            string excludeQuery = string.Empty;

            for (int i = 0; i < _folders.Count; i++)
            {
                string path = _folders[i].Path;

                if (string.IsNullOrEmpty(path))
                    continue;

                if (_folders[i].DoInclude)
                {
                    if (!string.IsNullOrEmpty(includeQuery))
                        includeQuery += " or ";
                    includeQuery += path + "/";

                    if (_folders[i].MatchOption == FolderMatchOption.TopOnly)
                        includeQuery += "[^/]+$";
                }
                else
                {
                    if (!string.IsNullOrEmpty(excludeQuery))
                        excludeQuery += " and ";
                    excludeQuery += "-" + path + "/";

                    if (_folders[i].MatchOption == FolderMatchOption.TopOnly)
                        excludeQuery += "[^/]+$";
                }
            }

            string query = string.Empty;

            bool hasIncludeQuery = !string.IsNullOrEmpty(includeQuery);
            bool hasExcludeQuery = !string.IsNullOrEmpty(excludeQuery);

            if (hasIncludeQuery && hasExcludeQuery)
                query = $"(({includeQuery}) and ({excludeQuery}))";
            else if (hasIncludeQuery)
                query = $"({includeQuery})";
            else if (hasExcludeQuery)
                query = $"(assets/ and ({excludeQuery}))"; // Gives incorrect results if only has exclude paths.

            return query;
        }

        private bool MatchesRequiredFolders(string path)
        {
            if (_folders.Count == 0)
                return true;
            
            foreach (var folder in _folders)
            {
                bool isValidPath = folder.IsValidPath(path);
                
                // We return if the folder is exclude and the path is in the folder. A.k.a. it is an excluded path.
                if (!folder.DoInclude && !isValidPath)
                    return false;
                
                // We return if the folder is include and the path is in the folder. A.k.a. it is an included path.
                if (folder.DoInclude && isValidPath)
                    return true;
            }

            return false;
        }
    }
}