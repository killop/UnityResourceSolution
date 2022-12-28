using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    [CustomEditor(typeof(SmartCollection))]
    internal class SmartCollectionEditor : LibraryCollectionEditor
    {
        private SmartCollection _smartCollection;

        protected override void Awake()
        {
            _smartCollection = (SmartCollection) target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = base.CreateInspectorGUI();

            ReorderableList folders = new ReorderableList(serializedObject.FindProperty("_folders"));
            folders.IsReorderable = true;
            folders.AddItem += list =>
            {
                list.ListProperty.arraySize++;
                list.ListProperty.GetArrayElementAtIndex(list.ListProperty.arraySize - 1).FindPropertyRelative("_doInclude").boolValue = true;
                list.ListProperty.serializedObject.ApplyModifiedProperties();
            };
            
            // Add a label to the folders header to indicate what folders will be searched.
            // Mostly to indicate that even if no folders are set, it will still search everything.
            var searchingScopeLabel = new Label();
            searchingScopeLabel.tooltip = "Has little impact on the search speed.";
            searchingScopeLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            searchingScopeLabel.schedule.Execute(() =>
            {
                var targetCollection = (SmartCollection)target;
                if (targetCollection.Folders.Count == 0)
                {
                    searchingScopeLabel.text = "Searches all folders";
                    return;
                }

                bool containsInclude = targetCollection.Folders.Any(folder => folder.DoInclude);
                searchingScopeLabel.text = containsInclude ? "Searches only specified folders" : "Searches all folders excluding specified";
            }).Every(100);
            
            folders.Q("header").Add(searchingScopeLabel);
            
            root.Insert(0, folders);

            var spinnerContainer = new VisualElement();
            spinnerContainer.style.alignItems = Align.Center;
            var spinner = new ProcessSpinner();
            spinner.schedule.Execute(() => spinner.SetDisplay(_smartCollection.IsSearching)).Every(100);
            spinnerContainer.Add(spinner);
            root.Add(spinnerContainer);

            return root;
        }

        public override VisualElement CreateEmptyCollectionPrompt()
        {
            var targetCollection = (SmartCollection)target;

            var rootElement = new VisualElement();
            rootElement.Add(base.CreateEmptyCollectionPrompt());

            var instructionlabel = new Label();
            instructionlabel.AddToClassList(emptyPromptTextUssClassName);
            rootElement.Add(instructionlabel);

            if (targetCollection.Rules.Count == 0 && targetCollection.Folders.Count == 0)
            {
                instructionlabel.text = LibraryConstants.SmartCollectionEmptyNoFiltersFolders;
                var settingsButton = new Button(() => Selection.activeObject = targetCollection);
                settingsButton.text = "Settings";
                settingsButton.style.alignSelf = Align.Center;
                rootElement.Add(settingsButton);
            }
            else
            {
                instructionlabel.text = LibraryConstants.SmartCollectionEmpty;
            }

            return rootElement;
        }
    } 
}
