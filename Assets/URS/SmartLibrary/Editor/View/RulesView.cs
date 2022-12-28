using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class RulesView : VisualElement
    {
        public static readonly string ussClassName = "bewildered-rules-list";

        private static readonly string ruleListItemUssClassName = ussClassName + "__item";
        private static readonly string inScopeUssClassName = ruleListItemUssClassName + "-in-scope";
        private static readonly string openScopeUssClassName = ruleListItemUssClassName + "-open-scope";
        private static readonly string closeScopeUssClassName = ruleListItemUssClassName + "-close-scope";

        private ReorderableList _list;
        private bool _isInScope = false;

        public RulesView(SerializedProperty rulesProperty)
        {
            AddToClassList(ussClassName);

            _list = new ReorderableList(rulesProperty);
            _list.ShowDropdownIcon = true;

            _list.MakeItem += MakeItem;
            _list.BindItem += BindItem;
            _list.UnbindItem += UnbindItem;
            _list.AddItem += AddItem;
            _list.RemoveItem += RemoveItem;
            _list.Reorder += Reorder;

            Add(_list);
        }

        private VisualElement MakeItem()
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(ruleListItemUssClassName);
            element.AddManipulator(new ContextualMenuManipulator(ContextClick));

            element.Add(new PropertyField());

            var options = new Image();
            options.AddToClassList("bewildered-library-rule-options");
            options.image = LibraryUtility.LoadLibraryIcon("options");

            // UITK doesn't have a way to show a menu on its own,
            // so we need to use the menu manupulator with the button set to left mouse button in order to show the menu.
            var menuManip = new ContextualMenuManipulator(ContextClick);
            menuManip.activators.Add(new ManipulatorActivationFilter() { button = 0 });
            options.AddManipulator(menuManip);

            element.Add(options);
            return element;
        }

        private void BindItem(ReorderableList list, VisualElement element, int index)
        {
            element.userData = index;

            var propertyElement = element.Q<PropertyField>();
            var property = list.ListProperty.GetArrayElementAtIndex(index);
            propertyElement.BindProperty(property);

            if (index == 0)
            {
                var operand = propertyElement.Q<EnumField>();
                operand.SetEnabled(false);
                operand.Unbind();
                operand.Q<TextElement>().text = "If";
            }

            bool isOpenScope = HasScopeState(index, ConditionScopeState.Open);
            bool isCloseScope = HasScopeState(index, ConditionScopeState.Close);

            if (isOpenScope)
            {
                _isInScope = true;
            }

            element.EnableInClassList(openScopeUssClassName, isOpenScope);
            element.EnableInClassList(inScopeUssClassName, _isInScope);
            element.EnableInClassList(closeScopeUssClassName, isCloseScope);

            if (isCloseScope)
            {
                _isInScope = false;
            }

            // Reset state if it is the last item so that the first item does not think it is in scope.
            if (index == list.Count - 1)
                _isInScope = false;
        }

        private void UnbindItem(ReorderableList list, VisualElement element, int index)
        {
            element.RemoveFromClassList(inScopeUssClassName);
            element.RemoveFromClassList(openScopeUssClassName);
            element.RemoveFromClassList(closeScopeUssClassName);
        }

        private void AddItem(ReorderableList list)
        {
            var menu = new GenericMenu();
            var ruleTypes = TypeCache.GetTypesDerivedFrom<LibraryRuleBase>();

            foreach (var type in LibraryConstants.OrderedRuleTypes)
            {
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, AddRule, type);
            }

            foreach (var type in ruleTypes)
            {
                if (!LibraryConstants.OrderedRuleTypes.Contains(type))
                    menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, AddRule, type);
            }
            menu.ShowAsContext();

            void AddRule(object typeData)
            {
                list.ListProperty.arraySize++;
                list.ListProperty.GetArrayElementAtIndex(list.ListProperty.arraySize - 1).managedReferenceValue = Activator.CreateInstance((Type)typeData);
                list.ListProperty.serializedObject.ApplyModifiedProperties();
                list.Refresh();
            }
        }

        private void RemoveItem(ReorderableList list, int index)
        {
            if (list.ListProperty.arraySize > 1)
            {
                SerializedProperty scopeStateProperty = GetScopeStateProperty(index);
                ConditionScopeState scopeState = (ConditionScopeState)scopeStateProperty.intValue;

                SerializedProperty nextScopeStateProperty = GetScopeStateProperty(index == list.ListProperty.arraySize - 1 ? index - 1 : index + 1);
                ConditionScopeState nextScopeState = (ConditionScopeState)nextScopeStateProperty.intValue;

                if (scopeState.HasFlag(ConditionScopeState.Open))
                    nextScopeState |= ConditionScopeState.Open;

                if (scopeState.HasFlag(ConditionScopeState.Close))
                    nextScopeState |= ConditionScopeState.Close;

                if (nextScopeState.HasFlag(ConditionScopeState.Open | ConditionScopeState.Close))
                    nextScopeState = ConditionScopeState.None;

                nextScopeStateProperty.intValue = (int)nextScopeState;
            }

            list.ListProperty.DeleteArrayElementAtIndex(index);
            list.ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void Reorder(ReorderableList list, int sourceIndex, int destinationIndex, ReletiveDragPosition position)
        {
            if (sourceIndex < 0 || sourceIndex > list.ListProperty.arraySize - 1)
                return;

            ConditionScopeState sourceScopeState = GetScopeState(sourceIndex);

            // Remove the element from its current position.

            if (sourceScopeState.HasFlag(ConditionScopeState.Open))
            {
                RemoveScopeState(sourceIndex, ConditionScopeState.Open);
                
                // We add the "Open" scope state to next element that is in the scope so that the scope stays intact.
                AddScopeState(sourceIndex + 1, ConditionScopeState.Open);
                // Instead of doing checks before adding we just clear after we set if there is both open and close on the same one.
                ClearSoloScopeState(sourceIndex + 1);
            }

            if (sourceScopeState.HasFlag(ConditionScopeState.Close))
            {
                RemoveScopeState(sourceIndex, ConditionScopeState.Close);

                // We add the "Close" scope state to next element that is in the scope so that the scope stays intact.
                AddScopeState(sourceIndex - 1, ConditionScopeState.Close);
                // Instead of doing checks before adding we just clear after we set if there is both add and remove on the same one.
                ClearSoloScopeState(sourceIndex - 1);
            }

            list.ListProperty.MoveArrayElement(sourceIndex, destinationIndex);
            list.ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void ContextClick(ContextualMenuPopulateEvent evt)
        {
            VisualElement target = evt.target as VisualElement;
            int index = (int)target.FindAncestorUserData();

            evt.menu.AppendAction("Scope with Above", a => ScopeWithAbove(index), CanScopeWithAbove(index) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Scope with Below", a => ScopeWithBelow(index), CanScopeWithBelow(index) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Remove from Scope", a => RemoveFromScope(index), IsInScope(index) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private bool CanScopeWithBelow(int index)
        {
            if (index >= _list.ListProperty.arraySize - 1)
                return false;

            ConditionScopeState scopeState = GetScopeState(index);

            if (scopeState.HasFlag(ConditionScopeState.Open))
                return false;

            if (IsInScope(index) && scopeState == ConditionScopeState.None)
                return false;

            return true;
        }

        private bool CanScopeWithAbove(int index)
        {
            if (index == 0)
                return false;

            if (HasScopeState(index, ConditionScopeState.Close))
                return false;

            if (IsInScope(index) && GetScopeState(index) == ConditionScopeState.None)
                return false;

            return true;
        }

        private void ScopeWithBelow(int index)
        {
            if (HasScopeState(index, ConditionScopeState.Close))
            {
                RemoveScopeState(index, ConditionScopeState.Close);
            }
            else
            {
                AddScopeState(index, ConditionScopeState.Open);
            }

            // If the target rule is the closing of a scope, and the rule it is scoping with is the open of a scope we merge the two scopes together.
            // Otherwise we just add the closing scope to the next one instead.
            if (HasScopeState(index + 1, ConditionScopeState.Open))
                RemoveScopeState(index + 1, ConditionScopeState.Open);
            else
                AddScopeState(index + 1, ConditionScopeState.Close);

            _list.ListProperty.serializedObject.ApplyModifiedProperties();
            _list.Refresh();
        }

        private void ScopeWithAbove(int index)
        {
            if (HasScopeState(index, ConditionScopeState.Open))
            {
                RemoveScopeState(index, ConditionScopeState.Open);
            }
            else
            {
                AddScopeState(index, ConditionScopeState.Close);
            }

            // If the target rule is the closing of a scope, and the rule it is scoping with is the open of a scope we merge the two scopes together.
            // Otherwise we just add the closing scope to the next one instead.
            if (HasScopeState(index - 1, ConditionScopeState.Close))
                RemoveScopeState(index - 1, ConditionScopeState.Close);
            else
                AddScopeState(index - 1, ConditionScopeState.Open);

            _list.ListProperty.serializedObject.ApplyModifiedProperties();
            _list.Refresh();
        }

        private void RemoveFromScope(int index)
        {
            ConditionScopeState scopeState = GetScopeState(index);

            if (scopeState.HasFlag(ConditionScopeState.Open))
            {
                RemoveScopeState(index, ConditionScopeState.Open);

                // We add the "Open" scope state to next element that is in the scope so that the scope stays intact.
                AddScopeState(index + 1, ConditionScopeState.Open);
                // Instead of doing checks before adding we just clear after we set if there is both open and close on the same one.
                ClearSoloScopeState(index + 1);
            }
            else if (scopeState.HasFlag(ConditionScopeState.Close))
            {
                RemoveScopeState(index, ConditionScopeState.Close);

                // We add the "Close" scope state to previous element that is in the scope so that the scope stays intact.
                AddScopeState(index - 1, ConditionScopeState.Close);
                // Instead of doing checks before adding we just clear after we set if there is both add and remove on the same one.
                ClearSoloScopeState(index - 1);
            }
            else if (IsInScope(index))
            {
                AddScopeState(index + 1, ConditionScopeState.Open);
                AddScopeState(index - 1, ConditionScopeState.Close);

                ClearSoloScopeState(index + 1);
                ClearSoloScopeState(index - 1);
            }

            _list.ListProperty.serializedObject.ApplyModifiedProperties();
            _list.Refresh();
        }

        private bool IsInScope(int index)
        {
            bool inScope = false;
            for (int i = 0; i < _list.ListProperty.arraySize; i++)
            {
                var scopeState = GetScopeState(i);
                if (scopeState.HasFlag(ConditionScopeState.Open))
                    inScope = true;

                if (i == index)
                    return inScope;

                if (scopeState.HasFlag(ConditionScopeState.Close) && i != _list.ListProperty.arraySize - 1)
                    inScope = false;
            }

            return false;
        }

        private bool HasScopeState(int index, ConditionScopeState scopeState)
        {
            return GetScopeState(index).HasFlag(scopeState);
        }

        private ConditionScopeState GetScopeState(int index)
        {
            return (ConditionScopeState)GetScopeStateProperty(index).intValue;
        }

        private void RemoveScopeState(int index, ConditionScopeState scopeState)
        {
            SerializedProperty scopeStateProperty = GetScopeStateProperty(index);
            ConditionScopeState currentScopeState = (ConditionScopeState)scopeStateProperty.intValue;

            currentScopeState &= ~scopeState;
            scopeStateProperty.intValue = (int)currentScopeState;
        }

        private void AddScopeState(int index, ConditionScopeState scopeState)
        {
            SerializedProperty scopeStateProperty = GetScopeStateProperty(index);
            ConditionScopeState currentScopeState = (ConditionScopeState)scopeStateProperty.intValue;

            currentScopeState |= scopeState;
            scopeStateProperty.intValue = (int)currentScopeState;
        }

        private SerializedProperty GetScopeStateProperty(int index)
        {
            return _list.ListProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_scopeState");
        }

        private void ClearSoloScopeState(int index)
        {
            SerializedProperty scopeStateProperty = GetScopeStateProperty(index);
            ConditionScopeState scopeState = (ConditionScopeState)scopeStateProperty.intValue;

            if (scopeState.HasFlag(ConditionScopeState.Open | ConditionScopeState.Close))
                scopeStateProperty.intValue = (int)ConditionScopeState.None;
        }
    }
}
