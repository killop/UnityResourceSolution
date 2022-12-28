using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    internal static class LibraryConstants
    {
        public const string Ellipsis = "\u2026"; // 'horizontal ellipsis' (U+2026) is: ...;

        public const string CollectionFileExtension = ".collect";
        
        public const string ItemDragDataName = "BewilderedCollectionLibraryItems";
        public const string DragItemFromCollectionName = "BewilderedLibraryCollectionItems";

        public static readonly string LibraryWindowTitle = "Smart Library";
        public static readonly string DefaultLibraryDataPath = "Assets/Smart Library.asset";
        public static readonly string CollectionsPath = "SmartLibrarySettings/Collections";
        public static readonly string DefaultEmptyCollectionPromptText = "No items in collection ";
        public static readonly string SmartCollectionEmpty = "Check that the rule and folder settings are properly setup?";
        public static readonly string SmartCollectionEmptyNoFiltersFolders = "Add at least one rule or folder setting to add items to the collection.";

        public static readonly Texture DefaultCollectionIcon = EditorGUIUtility.IconContent("Folder Icon").image;
        public static readonly Texture CollectionSettingsIcon = EditorGUIUtility.IconContent("SettingsIcon").image;
        public static readonly Texture FallbackAssetIcon = EditorGUIUtility.IconContent("DefaultAsset Icon").image;

        internal static readonly Texture ScriptableObjectIcon = EditorGUIUtility.IconContent("ScriptableObject Icon").image;

        // Sort menu
        public static readonly string SortNameAscendingName = "Name ↓";
        public static readonly string SortNameDescendingName = "Name ↑";
        public static readonly string SortTypeAscendingName = "Type ↓";
        public static readonly string SortTypeDescendingName = "Type ↑";

        public static readonly string ActiveSortNameAscendingName = "Sort: " + SortNameAscendingName;
        public static readonly string ActiveSortNameDescendingName = "Sort: " + SortNameDescendingName;
        public static readonly string ActiveSortTypeAscendingName = "Sort: " + SortTypeAscendingName;
        public static readonly string ActiveSortTypeDescendingName = "Sort: " + SortTypeDescendingName;

        public static readonly string IconUssClassName = "bewildered-library-icon";


        public static readonly string EmptyRulesText = "No rules for collection.";

        internal static readonly List<System.Type> OrderedRuleTypes = new List<System.Type>() 
            { typeof(NameRule), typeof(TypeRule), typeof(ExtensionRule), typeof(PrefabRule) };
    } 
}
