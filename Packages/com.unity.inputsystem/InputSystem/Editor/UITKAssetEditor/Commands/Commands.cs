#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
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
                var actionMap = Selectors.GetSelectedActionMap(state).wrappedProperty;
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
                var action = Selectors.GetSelectedAction(state).wrappedProperty;
                var map = Selectors.GetSelectedActionMap(state).wrappedProperty;
                var binding = InputActionSerializationHelpers.AddBinding(action, map);
                var bindingIndex = new SerializedInputBinding(binding).indexOfBinding;
                state.serializedObject.ApplyModifiedProperties();
                return state.With(selectedBindingIndex: bindingIndex, selectionType: SelectionType.Binding);
            };
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

        public static Command ChangeActionMapName(string newName)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetSelectedActionMap(state).wrappedProperty;
                InputActionSerializationHelpers.RenameActionMap(actionMap, newName);
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }
    }
}

#endif
