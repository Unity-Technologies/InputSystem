#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
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

        public static Command AddComposite(string compositeName)
        {
            return (in InputActionsEditorState state) =>
            {
                var action = Selectors.GetSelectedAction(state)?.wrappedProperty;
                var map = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
                var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
                var composite = InputActionSerializationHelpers.AddCompositeBinding(action, map, compositeName, compositeType);
                var index = new SerializedInputBinding(composite).indexOfBinding;
                state.serializedObject.ApplyModifiedProperties();
                return state.With(selectedBindingIndex: index, selectionType: SelectionType.Binding);
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

        public static Command CopyActionMapSelection()
        {
            return (in InputActionsEditorState state) =>
            {
                CopyPasteHelper.CopySelectedTreeViewItemsToClipboard(new List<SerializedProperty> {Selectors.GetSelectedActionMap(state)?.wrappedProperty}, typeof(InputActionMap));
                return state;
            };
        }

        public static Command CutActionMapSelection()
        {
            return (in InputActionsEditorState state) =>
            {
                CopyPasteHelper.CopySelectedTreeViewItemsToClipboard(new List<SerializedProperty> {Selectors.GetSelectedActionMap(state)?.wrappedProperty}, typeof(InputActionMap));
                return DeleteActionMap(state.selectedActionMapIndex).Invoke(state);
            };
        }

        public static Command CopyActionBindingSelection(bool isAction) //TODO remove bool for multiselection
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
                if (isAction)
                    CopyPasteHelper.CopySelectedTreeViewItemsToClipboard(new List<SerializedProperty> {Selectors.GetSelectedAction(state)?.wrappedProperty}, typeof(InputAction), actionMap);
                else
                    CopyPasteHelper.CopySelectedTreeViewItemsToClipboard(new List<SerializedProperty> {Selectors.GetSelectedBinding(state)?.wrappedProperty}, typeof(InputBinding), actionMap);
                return state;
            };
        }
        
        public static Command CutActionsOrBindings(bool isAction) //TODO remove bool for multiselection
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
                if (isAction)
                {
                    var selectedAction = Selectors.GetSelectedAction(state)?.wrappedProperty;
                    CopyPasteHelper.CopySelectedTreeViewItemsToClipboard(new List<SerializedProperty> { selectedAction }, typeof(InputAction), actionMap);
                    return DeleteAction(state.selectedActionMapIndex, selectedAction.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue).Invoke(state);
                }
                CopyPasteHelper.CopySelectedTreeViewItemsToClipboard(new List<SerializedProperty> {Selectors.GetSelectedBinding(state)?.wrappedProperty}, typeof(InputBinding), actionMap);
                return DeleteBinding(state.selectedActionMapIndex, state.selectedBindingIndex).Invoke(state);
            };
        }

        public static Command PasteActionMaps()
        {
            return (in InputActionsEditorState state) =>
            {
                var typeOfCopiedData = CopyPasteHelper.GetCopiedClipboardType();
                if (typeOfCopiedData != typeof(InputActionMap)) return state;
                var actionMapArray = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ActionMaps));
                var lastPastedElement = CopyPasteHelper.PasteFromClipboard(new[] { state.selectedActionMapIndex }, actionMapArray, state);
                if (lastPastedElement != null)
                {
                    state.serializedObject.ApplyModifiedProperties();
                    return state.SelectActionMap(lastPastedElement.GetIndexOfArrayElement());
                }
                return state;
            };
        }

        public static Command PasteActionFromActionMap()
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, state.selectedActionMapIndex)?.wrappedProperty;
                var actionArray = actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Actions));
                var index = actionArray.arraySize - 1;
                var lastPastedElement = CopyPasteHelper.PasteFromClipboard(new[] { index }, actionArray, state);
                if (lastPastedElement != null)
                {
                    state.serializedObject.ApplyModifiedProperties();
                    return state.SelectAction(lastPastedElement.GetIndexOfArrayElement());
                }
                return state;
            };
        }

        public static Command PasteActionsOrBindings()
        {
            return (in InputActionsEditorState state) =>
            {
                var typeOfCopiedData = CopyPasteHelper.GetCopiedClipboardType();
                if (typeOfCopiedData == typeof(InputAction))
                {
                    var actionMap = Selectors.GetActionMapAtIndex(state, state.selectedActionMapIndex)?.wrappedProperty;
                    var actionArray = actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Actions));
                    var lastPastedElement = CopyPasteHelper.PasteFromClipboard(new[] { state.selectedActionIndex }, actionArray, state);
                    if (lastPastedElement != null)
                    {
                        state.serializedObject.ApplyModifiedProperties();
                        return state.SelectAction(lastPastedElement.GetIndexOfArrayElement());
                    }
                }
                else
                {
                    var actionMap = Selectors.GetActionMapAtIndex(state, state.selectedActionMapIndex)?.wrappedProperty;
                    var bindingsArray = actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
                    var index = state.selectionType == SelectionType.Action ? Selectors.GetLastBindingIndexForSelectedAction(state) : state.selectedBindingIndex;
                    var lastPastedElement = CopyPasteHelper.PasteFromClipboard(new[] { index }, bindingsArray, state);
                    if (lastPastedElement != null)
                    {
                        state.serializedObject.ApplyModifiedProperties();
                        return state.SelectBinding(lastPastedElement.GetIndexOfArrayElement());
                    }
                }
                return state;
            };
        }

        public static Command DuplicateActionMap(int actionMapIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMapArray = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ActionMaps));
                var actionMap = Selectors.GetActionMapAtIndex(state, actionMapIndex)?.wrappedProperty;
                var name = actionMap?.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
                var newMap = CopyPasteHelper.DuplicateElement(actionMapArray, actionMap, name, actionMap.GetIndexOfArrayElement() + 1);
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectActionMap(newMap.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue);
            };
        }

        public static Command DuplicateAction()
        {
            return (in InputActionsEditorState state) =>
            {
                var action = Selectors.GetSelectedAction(state)?.wrappedProperty;
                var actionMap = Selectors.GetActionMapAtIndex(state, state.selectedActionMapIndex)?.wrappedProperty;
                var actionArray = actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Actions));
                CopyPasteHelper.DuplicateAction(actionArray, action, actionMap, state);
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectAction(state.selectedActionIndex + 1);
            };
        }

        public static Command DuplicateBinding()
        {
            return (in InputActionsEditorState state) =>
            {
                var binding = Selectors.GetSelectedBinding(state)?.wrappedProperty;
                var actionName = binding?.FindPropertyRelative("m_Action").stringValue;
                var actionMap = Selectors.GetActionMapAtIndex(state, state.selectedActionMapIndex)?.wrappedProperty;
                var bindingsArray = actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
                var newIndex = CopyPasteHelper.DuplicateBinding(bindingsArray, binding, actionName, binding.GetIndexOfArrayElement() + 1);
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectBinding(newIndex);
            };
        }

        private static InputActionsEditorState SelectPrevActionMap(InputActionsEditorState state)
        {
            var count = Selectors.GetActionMapCount(state);
            int index = 0;
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
                var controlTypes = Selectors.BuildControlTypeList(inputAction.type).ToList();

                // ISX-1650: "Any" (in index 0) should not be put into an InputAction.expectedControlType. It's expected to be null in this case.
                var controlType = (controlTypeIndex == 0) ? string.Empty : controlTypes[controlTypeIndex];
                inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_ExpectedControlType)).stringValue = controlType;
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

        public static Command SaveAsset(Action postSaveAction)
        {
            return (in InputActionsEditorState state) =>
            {
                InputActionsEditorWindowUtils.SaveAsset(state.serializedObject);
                postSaveAction?.Invoke();
                return state;
            };
        }

        public static Command ToggleAutoSave(bool newValue, Action postSaveAction)
        {
            return (in InputActionsEditorState state) =>
            {
                if (newValue != InputEditorUserSettings.autoSaveInputActionAssets)
                {
                    // If it changed from disabled to enabled, perform an initial save.
                    if (newValue)
                    {
                        InputActionsEditorWindowUtils.SaveAsset(state.serializedObject);
                        postSaveAction?.Invoke();
                    }

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

        public static Command ResetGlobalInputAsset(Action<InputActionAsset> postResetAction)
        {
            return (in InputActionsEditorState state) =>
            {
                var asset = ProjectWideActionsAsset.CreateNewActionAsset();
                postResetAction?.Invoke(asset);
                return state;
            };
        }
    }
}

#endif
