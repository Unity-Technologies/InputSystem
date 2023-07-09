#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal delegate InputActionsEditorState Command(in InputActionsEditorState state);

    internal static class Commands
    {
        public static Command SelectAction(string actionName)
        {
            return (in InputActionsEditorState state) => state.SelectAction(actionName);
        }

        public static Command SelectActionMap(string actionMapName)
        {
            return (in InputActionsEditorState state) => state.SelectActionMap(actionMapName);
        }

        public static Command AddActionMap()
        {
            return (in InputActionsEditorState state) =>
            {
                var newMap = InputActionSerializationHelpers.AddActionMap(state.serializedObject);
                var actionProperty = InputActionSerializationHelpers.AddAction(newMap);
                InputActionSerializationHelpers.AddBinding(actionProperty, newMap);
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectActionMap(newMap);
            };
        }

        public static Command AddAction()
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
                if (actionMap == null)
                {
                    Debug.LogError("Cannot add action without an action map selected");
                    return state;
                }
                var newAction = InputActionSerializationHelpers.AddAction(actionMap);
                InputActionSerializationHelpers.AddBinding(newAction, actionMap);
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectAction(newAction);
            };
        }

        public static Command AddBinding()
        {
            return (in InputActionsEditorState state) =>
            {
                var action = Selectors.GetSelectedAction(state)?.wrappedProperty;
                var map = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
                if (action == null || map == null)
                {
                    Debug.LogError("Cannot add binding without an action and action map selected");
                    return state;
                }
                var binding = InputActionSerializationHelpers.AddBinding(action, map);
                var bindingIndex = new SerializedInputBinding(binding).indexOfBinding;
                state.serializedObject.ApplyModifiedProperties();
                return state.With(selectedBindingIndex: bindingIndex, selectionType: SelectionType.Binding);
            };
        }

        public static Command DeleteActionMap(int actionMapIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, actionMapIndex)?.wrappedProperty;
                var actionMapID = InputActionSerializationHelpers.GetId(actionMap);
                InputActionSerializationHelpers.DeleteActionMap(state.serializedObject, actionMapID);
                state.serializedObject.ApplyModifiedProperties();
                if (state.selectedActionMapIndex == actionMapIndex)
                    return SelectPrevActionMap(state);
                return state.SelectActionMap(state.selectedActionMapIndex > actionMapIndex ? state.selectedActionMapIndex - 1 : state.selectedActionMapIndex);
            };
        }

        private static InputActionsEditorState SelectPrevActionMap(InputActionsEditorState state)
        {
            var count = Selectors.GetActionMapCount(state);
            int index = -1;
            if (count != null && count.Value > 0)
                index = Math.Max(state.selectedActionMapIndex - 1, 0);
            return state.SelectActionMap(index);
        }

        public static Command DeleteAction(int actionMapIndex, string actionName)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, actionMapIndex)?.wrappedProperty;
                var action = Selectors.GetActionInMap(state, actionMapIndex, actionName).wrappedProperty;
                var actionIndex = action.GetIndexOfArrayElement();
                var actionID = InputActionSerializationHelpers.GetId(action);
                InputActionSerializationHelpers.DeleteActionAndBindings(actionMap, actionID);
                state.serializedObject.ApplyModifiedProperties();
                if (state.selectedActionIndex >= actionIndex)
                    return SelectPrevAction(state, actionMap);
                return state.SelectAction(state.selectedActionIndex);
            };
        }

        private static InputActionsEditorState SelectPrevAction(InputActionsEditorState state, SerializedProperty actionMap)
        {
            var count = Selectors.GetActionCount(actionMap);
            int index = -1;
            if (count != null && count.Value > 0)
                index = Math.Max(state.selectedActionIndex - 1, 0);
            return state.SelectAction(index);
        }

        public static Command DeleteBinding(int actionMapIndex, int bindingIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, actionMapIndex)?.wrappedProperty;
                var binding = Selectors.GetCompositeOrBindingInMap(actionMap, bindingIndex).wrappedProperty;
                InputActionSerializationHelpers.DeleteBinding(binding, actionMap);
                state.serializedObject.ApplyModifiedProperties();
                if (state.selectedBindingIndex >= bindingIndex)
                    return SelectPrevBinding(state, actionMap);
                return state.SelectBinding(state.selectedBindingIndex);
            };
        }

        private static InputActionsEditorState SelectPrevBinding(InputActionsEditorState state, SerializedProperty actionMap)
        {
            var count = Selectors.GetBindingCount(actionMap);
            var index = -1;
            if (count != null && count.Value > 0)
                index = Math.Max(state.selectedBindingIndex - 1, 0);
            return state.SelectBinding(index);
        }

        public static Command ExpandCompositeBinding(SerializedInputBinding binding)
        {
            return (in InputActionsEditorState state) => state.ExpandCompositeBinding(binding);
        }

        public static Command CollapseCompositeBinding(SerializedInputBinding binding)
        {
            return (in InputActionsEditorState state) => state.CollapseCompositeBinding(binding);
        }

        public static Command SelectBinding(int bindingIndex)
        {
            return (in InputActionsEditorState state) =>
                state.With(selectedBindingIndex: bindingIndex, selectionType: SelectionType.Binding);
        }

        public static Command UpdatePathNameAndValues(NamedValue[] parameters, SerializedProperty pathProperty)
        {
            return (in InputActionsEditorState state) =>
            {
                var path = pathProperty.stringValue;
                var nameAndParameters = NameAndParameters.Parse(path);
                nameAndParameters.parameters = parameters;

                pathProperty.stringValue = nameAndParameters.ToString();
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command SetCompositeBindingType(SerializedInputBinding bindingProperty, IEnumerable<string> compositeTypes,
            ParameterListView parameterListView, int selectedCompositeTypeIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var nameAndParameters = new NameAndParameters
                {
                    name = compositeTypes.ElementAt(selectedCompositeTypeIndex),
                    parameters = parameterListView.GetParameters()
                };
                InputActionSerializationHelpers.ChangeCompositeBindingType(bindingProperty.wrappedProperty, nameAndParameters);
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command SetCompositeBindingPartName(SerializedInputBinding bindingProperty, string partName)
        {
            return (in InputActionsEditorState state) =>
            {
                InputActionSerializationHelpers.ChangeBinding(bindingProperty.wrappedProperty, partName);
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command ChangeActionType(SerializedInputAction inputAction, InputActionType newValue)
        {
            return (in InputActionsEditorState state) =>
            {
                inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Type)).intValue = (int)newValue;
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command ChangeInitialStateCheck(SerializedInputAction inputAction, bool value)
        {
            return (in InputActionsEditorState state) =>
            {
                var property = inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Flags));
                if (value)
                    property.intValue |= (int)InputAction.ActionFlags.WantsInitialStateCheck;
                else
                    property.intValue &= ~(int)InputAction.ActionFlags.WantsInitialStateCheck;
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command ChangeActionControlType(SerializedInputAction inputAction, int controlTypeIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var controlTypes = Selectors.BuildSortedControlList(inputAction.type).ToList();
                inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_ExpectedControlType)).stringValue = controlTypes[controlTypeIndex];
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        /// <summary>
        /// Exists to integrate with some existing UI stuff, like InputControlPathEditor
        /// </summary>
        /// <returns></returns>
        public static Command ApplyModifiedProperties()
        {
            return (in InputActionsEditorState state) =>
            {
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command SaveAsset()
        {
            return (in InputActionsEditorState state) =>
            {
                InputActionsEditorWindow.SaveAsset(state.serializedObject);
                return state;
            };
        }

        public static Command ToggleAutoSave(bool newValue)
        {
            return (in InputActionsEditorState state) =>
            {
                if (newValue != InputEditorUserSettings.autoSaveInputActionAssets)
                {
                    // If it changed from disabled to enabled, perform an initial save.
                    if (newValue)
                        InputActionsEditorWindow.SaveAsset(state.serializedObject);

                    InputEditorUserSettings.autoSaveInputActionAssets = newValue;
                }

                return state;
            };
        }

        public static Command ChangeActionMapName(int index, string newName)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, index)?.wrappedProperty;
                InputActionSerializationHelpers.RenameActionMap(actionMap, newName);
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command ChangeActionName(int actionMapIndex, string oldName, string newName)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, actionMapIndex)?.wrappedProperty;
                var action = Selectors.GetActionInMap(state, actionMapIndex, oldName).wrappedProperty;
                InputActionSerializationHelpers.RenameAction(action, actionMap, newName);
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command ChangeCompositeName(int actionMapIndex, int bindingIndex, string newName)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, actionMapIndex)?.wrappedProperty;
                var binding = Selectors.GetCompositeOrBindingInMap(actionMap, bindingIndex).wrappedProperty;
                InputActionSerializationHelpers.RenameComposite(binding, newName);
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }
    }
}

#endif
