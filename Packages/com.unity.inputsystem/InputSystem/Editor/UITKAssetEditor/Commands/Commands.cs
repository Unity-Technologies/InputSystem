#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.IO;
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
                CopyPasteHelper.CopyActionMap(state);
                return state;
            };
        }

        public static Command CutActionMapSelection()
        {
            return (in InputActionsEditorState state) =>
            {
                CopyPasteHelper.CutActionMap(state);
                return DeleteActionMap(state.selectedActionMapIndex).Invoke(state);
            };
        }

        public static Command CopyActionBindingSelection()
        {
            return (in InputActionsEditorState state) =>
            {
                CopyPasteHelper.Copy(state);
                return state;
            };
        }

        public static Command CutActionsOrBindings()
        {
            return (in InputActionsEditorState state) =>
            {
                CopyPasteHelper.Cut(state);
                return state.CutActionOrBinding();
            };
        }

        public static Command PasteActionMaps()
        {
            return (in InputActionsEditorState state) =>
            {
                var lastPastedElement = CopyPasteHelper.PasteActionMapsFromClipboard(state);
                if (lastPastedElement != null)
                {
                    state.serializedObject.ApplyModifiedProperties();
                    return state.SelectActionMap(lastPastedElement.GetIndexOfArrayElement());
                }
                return state;
            };
        }

        public static Command PasteActionIntoActionMap(int actionMapIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var lastPastedElement = CopyPasteHelper.PasteActionsOrBindingsFromClipboard(state, true, actionMapIndex);
                if (lastPastedElement != null)
                    state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        public static Command PasteActionFromActionMap()
        {
            return (in InputActionsEditorState state) =>
            {
                var lastPastedElement = CopyPasteHelper.PasteActionsOrBindingsFromClipboard(state, true);
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
                var newIndex = DeleteCutElementsAfterPaste(state);
                var lastPastedElement = CopyPasteHelper.PasteActionsOrBindingsFromClipboard(state.With(selectedActionIndex: state.selectionType == SelectionType.Action && newIndex >= 0 ? newIndex : state.selectedActionIndex, selectedBindingIndex: state.selectionType == SelectionType.Binding && newIndex >= 0 ? newIndex : state.selectedBindingIndex));
                if (lastPastedElement != null)
                {
                    state.serializedObject.ApplyModifiedProperties();
                    if (typeOfCopiedData == typeof(InputAction))
                        return state.With(selectedActionIndex: lastPastedElement.GetIndexOfArrayElement(), cutElements: new List<InputActionsEditorState.CutElement>());
                    if (typeOfCopiedData == typeof(InputBinding))
                        return state.With(selectedBindingIndex: lastPastedElement.GetIndexOfArrayElement(), cutElements: new List<InputActionsEditorState.CutElement>());
                }
                return state.With(cutElements: new List<InputActionsEditorState.CutElement>());
            };
        }

        private static int DeleteCutElementsAfterPaste(InputActionsEditorState state)
        {
            var cutElements = state.GetCutElements();
            if (cutElements is null || cutElements.Count <= 0)
                return -1;
            var index = cutElements[0].type == typeof(InputAction)
                ? state.selectedActionIndex
                : cutElements[0].type == typeof(InputBinding) ?
                state.selectedBindingIndex : state.selectedActionMapIndex;
            foreach (var cutElement in cutElements)
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, cutElement.actionMapIndex)?.wrappedProperty;

                if (cutElement.type == typeof(InputBinding) || cutElement.type == typeof(InputAction))
                {
                    if (cutElement.type == typeof(InputAction))
                    {
                        var action = Selectors.GetActionForIndex(actionMap, cutElement.actionOrBindingIndex);
                        var id = InputActionSerializationHelpers.GetId(action);
                        InputActionSerializationHelpers.DeleteActionAndBindings(actionMap, id);
                    }
                    else
                    {
                        var binding = Selectors.GetCompositeOrBindingInMap(actionMap, cutElement.actionOrBindingIndex).wrappedProperty;
                        InputActionSerializationHelpers.DeleteBinding(binding, actionMap);
                    }

                    if (cutElement.actionOrBindingIndex <= index && cutElement.actionMapIndex == state.selectedActionMapIndex)
                        index--;
                }
                else if (cutElement.type == typeof(InputActionMap))
                {
                    InputActionSerializationHelpers.DeleteActionMap(state.serializedObject, InputActionSerializationHelpers.GetId(actionMap));
                    if (cutElement.actionMapIndex <= index)
                        index--;
                }
            }
            return index;
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
            var index = 0;
            if (count != null && count.Value > 0)
                index = Math.Max(state.selectedActionMapIndex - 1, 0);
            return state.SelectActionMap(index);
        }

        public static Command ReorderActionMap(int oldIndex, int newIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                InputActionSerializationHelpers.MoveActionMap(state.serializedObject, oldIndex, newIndex);
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectActionMap(newIndex);
            };
        }

        public static Command MoveAction(int oldIndex, int newIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
                InputActionSerializationHelpers.MoveAction(actionMap, oldIndex, newIndex);
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectAction(newIndex);
            };
        }

        public static Command MoveBinding(int oldIndex, int actionIndex, int childIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var newBindingIndex = MoveBindingOrComposite(state, oldIndex, actionIndex, childIndex);
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectBinding(newBindingIndex);
            };
        }

        public static Command MoveComposite(int oldIndex, int actionIndex, int childIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
                var compositeBindings = CopyPasteHelper.GetBindingsForComposite(actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Bindings)), oldIndex);
                //move the composite element
                var newBindingIndex = MoveBindingOrComposite(state, oldIndex, actionIndex, childIndex);
                var actionTo = Selectors.GetActionForIndex(actionMap, actionIndex).FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
                var toIndex = newBindingIndex;
                foreach (var compositePart in compositeBindings)
                {
                    // the index of the composite part stays the same if composite was moved down as previous elements are shifted down (the index seems to update async so it's safer to use the oldIndex)
                    // if the composite was moved up, the index of the composite part is not changing so we are safe to use it
                    var from = oldIndex < newBindingIndex ? oldIndex : compositePart.GetIndexOfArrayElement();
                    // if added below the old position the array changes as composite parts are added on top (increase the index)
                    // if added above the oldIndex, the index does not change
                    var to = oldIndex < newBindingIndex ? newBindingIndex : ++toIndex;
                    InputActionSerializationHelpers.MoveBinding(actionMap, from, to);
                    Selectors.GetCompositeOrBindingInMap(actionMap, to).wrappedProperty.FindPropertyRelative("m_Action").stringValue = actionTo;
                }
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectBinding(newBindingIndex);
            };
        }

        private static int MoveBindingOrComposite(InputActionsEditorState state, int oldIndex, int actionIndex, int childIndex)
        {
            var actionMap = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
            var bindingsForAction = Selectors.GetBindingsForAction(state, actionMap, actionIndex);
            var allBindings = actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
            var actionTo = Selectors.GetActionForIndex(actionMap, actionIndex).FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
            var actionFrom = Selectors.GetCompositeOrBindingInMap(actionMap, oldIndex).wrappedProperty.FindPropertyRelative("m_Action");
            int newBindingIndex;
            if (bindingsForAction.Count == 0) //if there are no bindings for an action retrieve the first binding index of a binding before (iterate previous actions)
                newBindingIndex = Selectors.GetBindingIndexBeforeAction(allBindings, actionIndex, allBindings);
            else
            {
                var toSkip = GetNumberOfCompositePartItemsToSkip(bindingsForAction, childIndex, oldIndex); //skip composite parts if there are - avoid moving into a composite
                newBindingIndex = bindingsForAction[0].GetIndexOfArrayElement() + Math.Clamp(childIndex + toSkip, 0, bindingsForAction.Count);
                newBindingIndex -= newBindingIndex > oldIndex && !actionTo.Equals(actionFrom.stringValue) ? 1 : 0; // reduce index by one in case the moved binding will be shifted underneath to another action
            }

            actionFrom.stringValue = actionTo;
            InputActionSerializationHelpers.MoveBinding(actionMap, oldIndex, newBindingIndex);
            return newBindingIndex;
        }

        private static int GetNumberOfCompositePartItemsToSkip(List<SerializedProperty> bindings, int childIndex, int oldIndex)
        {
            var toSkip = 0;
            var normalBindings = 0;
            foreach (var binding in bindings)
            {
                if (binding.GetIndexOfArrayElement() == oldIndex)
                    continue;
                if (normalBindings > childIndex)
                    break;
                if (binding.FindPropertyRelative(nameof(InputBinding.m_Flags)).intValue ==
                    (int)InputBinding.Flags.PartOfComposite)
                    toSkip++;
                else
                    normalBindings++;
            }
            return toSkip;
        }

        public static Command MovePartOfComposite(int oldIndex, int newIndex, int compositeIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetSelectedActionMap(state)?.wrappedProperty;
                var actionTo = actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Bindings)).GetArrayElementAtIndex(compositeIndex).FindPropertyRelative("m_Action").stringValue;
                InputActionSerializationHelpers.MoveBinding(actionMap, oldIndex, newIndex);
                Selectors.GetCompositeOrBindingInMap(actionMap, newIndex).wrappedProperty.FindPropertyRelative("m_Action").stringValue = actionTo;
                state.serializedObject.ApplyModifiedProperties();
                return state.SelectBinding(newIndex);
            };
        }

        public static Command DeleteAction(int actionMapIndex, string actionName)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, actionMapIndex)?.wrappedProperty;
                var action = Selectors.GetActionInMap(state, actionMapIndex, actionName).wrappedProperty;
                var actionID = InputActionSerializationHelpers.GetId(action);
                InputActionSerializationHelpers.DeleteActionAndBindings(actionMap, actionID);
                state.serializedObject.ApplyModifiedProperties();

                // ActionsTreeView will dispatch a separate command to select the previous Action
                return state;
            };
        }

        public static Command DeleteBinding(int actionMapIndex, int bindingIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMap = Selectors.GetActionMapAtIndex(state, actionMapIndex)?.wrappedProperty;
                var binding = Selectors.GetCompositeOrBindingInMap(actionMap, bindingIndex).wrappedProperty;
                InputActionSerializationHelpers.DeleteBinding(binding, actionMap);
                state.serializedObject.ApplyModifiedProperties();

                // ActionsTreeView will dispatch a separate command to select the previous Binding
                return state;
            };
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
                InputActionSerializationHelpers.SetBindingPartName(bindingProperty.wrappedProperty, partName);
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
                InputActionAssetManager.SaveAsset(state.serializedObject.targetObject as InputActionAsset);
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
                        InputActionAssetManager.SaveAsset(state.serializedObject.targetObject as InputActionAsset);
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

        // Removes all action maps and their content from the associated serialized InputActionAsset.
        public static Command ClearActionMaps()
        {
            return (in InputActionsEditorState state) =>
            {
                InputActionSerializationHelpers.DeleteAllActionMaps(state.serializedObject);
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }

        // Replaces all action maps of the associated serialized InputActionAsset with the action maps contained in
        // the given source asset.
        public static Command ReplaceActionMaps(string inputActionAssetJsonContent)
        {
            return (in InputActionsEditorState state) =>
            {
                // First delete all existing data
                InputActionSerializationHelpers.DeleteAllActionMaps(state.serializedObject);
                InputActionSerializationHelpers.DeleteAllControlSchemes(state.serializedObject);

                // Create new data based on source
                var temp = InputActionAsset.FromJson(inputActionAssetJsonContent);
                using (var tmp = new SerializedObject(temp))
                {
                    InputActionSerializationHelpers.AddControlSchemes(state.serializedObject, tmp);
                    InputActionSerializationHelpers.AddActionMaps(state.serializedObject, tmp);
                }
                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }
    }
}

#endif
