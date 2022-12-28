using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    [CustomEditor(typeof(LibraryCollection), true)]
    public class LibraryCollectionEditor : Editor
    {
        public static readonly string emptyPromptTextUssClassName = "bewildered-library-empty-collection-text";
        private Button _updateItemsButton;

        public virtual string EmptyCollectionPromptText 
        {
            get { return LibraryConstants.DefaultEmptyCollectionPromptText + '"' + target.name + '"'; }
        }

        protected virtual void Awake() { }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        protected virtual void OnDestroy() { }

        protected override void OnHeaderGUI()
        {
            var collection = (LibraryCollection)target;
            
            using (new GUILayout.HorizontalScope("IN BigTitle"))
            {
                GUILayout.Label(collection.Icon, GUILayout.Width(32), GUILayout.Height(32));

                GUILayout.Label(collection.name, Styles.titleStyle, GUILayout.ExpandWidth(false), GUILayout.Height(32));

                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Label($"({collection.GetType().Name})", Styles.typeStyle, GUILayout.Height(32)); 
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var rootElement = new VisualElement();
            rootElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("LibraryCollection"));
            rootElement.styleSheets.Add(LibraryUtility.LoadStyleSheet($"LibraryCollection{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}"));
            rootElement.styleSheets.Add(LibraryUtility.LoadStyleSheet("Common"));
            rootElement.styleSheets.Add(LibraryUtility.LoadStyleSheet($"Common{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}"));

            var rules = new RulesView(serializedObject.FindProperty("_rules._rules"));
            rootElement.Add(rules);

            _updateItemsButton = new Button(UpdateItems);
            _updateItemsButton.name = "updateItemsButton";
            _updateItemsButton.text = "Update Items In Collection";
            _updateItemsButton.tooltip = "Update the items in the collection.";
            rootElement.Add(_updateItemsButton);

            return rootElement;
        }

        public virtual VisualElement CreateEmptyCollectionPrompt()
        {
            var label = new Label(EmptyCollectionPromptText);
            label.AddToClassList(emptyPromptTextUssClassName);
            return label;
        }

        private void UpdateItems()
        {
            ((LibraryCollection)target).UpdateItems();
        }

        private static class Styles
        {
            public static readonly GUIStyle titleStyle;
            public static readonly GUIStyle typeStyle;

            static Styles()
            {
                titleStyle = new GUIStyle(EditorStyles.boldLabel);
                titleStyle.fontSize = 16;
                titleStyle.alignment = TextAnchor.MiddleLeft;

                typeStyle = new GUIStyle(EditorStyles.miniLabel);
                typeStyle.alignment = TextAnchor.MiddleLeft;
            }
        }
    } 
}
