#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static partial class Selectors
    {
        public static IEnumerable<string> GetActionMapNames(InputActionsEditorState state)
        {
            return state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                ?.Select(m => m.FindPropertyRelative(nameof(InputActionMap.m_Name))?.stringValue)
                ?? Enumerable.Empty<string>();
        }

        public static IEnumerable<SerializedInputAction> GetActionsForSelectedActionMap(InputActionsEditorState state)
        {
            var actionMap = GetActionMapAtIndex(state, state.selectedActionMapIndex);

            if (!actionMap.HasValue)
                return Enumerable.Empty<SerializedInputAction>();

            return actionMap.Value.wrappedProperty
                .FindPropertyRelative(nameof(InputActionMap.m_Actions))
                .Select(serializedProperty => new SerializedInputAction(serializedProperty));
        }

        public static SerializedInputActionMap? GetSelectedActionMap(InputActionsEditorState state)
        {
            return GetActionMapAtIndex(state, state.selectedActionMapIndex);
        }

        public static SerializedInputActionMap? GetActionMapAtIndex(InputActionsEditorState state, int index)
        {
            var actionMaps = state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps));
            if (actionMaps == null || index < 0 || index > actionMaps.arraySize - 1)
                return null;
            return new SerializedInputActionMap(actionMaps?.GetArrayElementAtIndex(index));
        }

        public static int? GetBindingCount(SerializedProperty actionMap)
        {
            return actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Bindings))?.arraySize;
        }

        public static int? GetActionCount(SerializedProperty actionMap)
        {
            return actionMap?.FindPropertyRelative(nameof(InputActionMap.m_Actions))?.arraySize;
        }

        public static int? GetActionMapCount(InputActionsEditorState state)
        {
            return state.serializedObject?.FindProperty(nameof(InputActionAsset.m_ActionMaps))?.arraySize;
        }

        public static SerializedInputAction GetActionInMap(InputActionsEditorState state, int mapIndex, string name)
        {
            return new SerializedInputAction(state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))?.GetArrayElementAtIndex(mapIndex)
                ?.FindPropertyRelative(nameof(InputActionMap.m_Actions))
                ?.FirstOrDefault(p => p.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue == name));
        }

        public static SerializedInputBinding GetCompositeOrBindingInMap(SerializedProperty actionMap, int bindingIndex)
        {
            return new SerializedInputBinding(actionMap
                ?.FindPropertyRelative(nameof(InputActionMap.m_Bindings))
                ?.GetArrayElementAtIndex(bindingIndex));
        }

        public static SerializedProperty GetSelectedBindingPath(InputActionsEditorState state)
        {
            var selectedBinding = GetSelectedBinding(state);
            return selectedBinding?.wrappedProperty.FindPropertyRelative("m_Path");
        }

        public static SerializedInputBinding? GetSelectedBinding(InputActionsEditorState state)
        {
            var actionMapSO = GetActionMapAtIndex(state, state.selectedActionMapIndex);
            var bindings = actionMapSO?.wrappedProperty.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
            if (bindings == null || bindings.arraySize - 1 < state.selectedBindingIndex || state.selectedBindingIndex < 0)
                return null;
            return new SerializedInputBinding(bindings.GetArrayElementAtIndex(state.selectedBindingIndex));
        }

        public static SerializedInputAction? GetRelatedInputAction(InputActionsEditorState state)
        {
            var binding = GetSelectedBinding(state);
            if (binding == null)
                return null;
            var actionName = binding.Value.wrappedProperty.FindPropertyRelative("m_Action").stringValue;
            return GetActionInMap(state, state.selectedActionMapIndex, actionName);
        }

        public static IEnumerable<string> GetCompositeTypes(string path, string expectedControlLayout)
        {
            // Find name of current composite.
            var compositeNameAndParameters = NameAndParameters.Parse(path);
            var compositeName = compositeNameAndParameters.name;
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);

            // Collect all possible composite types.
            var selectedCompositeIndex = -1;
            var currentIndex = 0;
            foreach (var composite in InputBindingComposite.s_Composites.internedNames.Where(x =>
                !InputBindingComposite.s_Composites.aliases.Contains(x)).OrderBy(x => x))
            {
                if (!string.IsNullOrEmpty(expectedControlLayout))
                {
                    var valueType = InputBindingComposite.GetValueType(composite);
                    if (valueType != null &&
                        !InputControlLayout.s_Layouts.ValueTypeIsAssignableFrom(
                            new InternedString(expectedControlLayout), valueType))
                        continue;
                }

                if (InputBindingComposite.s_Composites.LookupTypeRegistration(composite) == compositeType)
                    selectedCompositeIndex = currentIndex;

                yield return composite;
                ++currentIndex;
            }

            // If the current composite type isn't a registered type, add it to the list as
            // an extra option.
            if (selectedCompositeIndex == -1)
                yield return compositeName;
        }

        public static IEnumerable<string> GetCompositePartOptions(string bindingName, string compositeName)
        {
            var currentIndex = 0;
            var selectedPartNameIndex = -1;
            foreach (var partName in InputBindingComposite.GetPartNames(compositeName))
            {
                if (partName.Equals(bindingName, StringComparison.OrdinalIgnoreCase))
                    selectedPartNameIndex = currentIndex;
                yield return partName;
                ++currentIndex;
            }

            // If currently selected part is not in list, add it as an option.
            if (selectedPartNameIndex == -1)
                yield return bindingName;
        }

        public static SerializedInputAction? GetSelectedAction(InputActionsEditorState state)
        {
            var actions = GetActionMapAtIndex(state, state.selectedActionMapIndex)
                ?.wrappedProperty.FindPropertyRelative(nameof(InputActionMap.m_Actions));
            if (actions == null || actions.arraySize - 1 < state.selectedActionIndex || state.selectedActionIndex < 0)
                return null;
            return new SerializedInputAction(actions.GetArrayElementAtIndex(state.selectedActionIndex));
        }

        public static IEnumerable<string> BuildSortedControlList(InputActionType selectedActionType)
        {
            return BuildControlTypeList(selectedActionType)
                .OrderBy(typeName => typeName, StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<string> BuildControlTypeList(InputActionType selectedActionType)
        {
            var allLayouts = InputSystem.s_Manager.m_Layouts;

            yield return "Any";
            foreach (var layoutName in allLayouts.layoutTypes.Keys)
            {
                if (EditorInputControlLayoutCache.TryGetLayout(layoutName).hideInUI)
                    continue;

                // If the action type is InputActionType.Value, skip button controls.
                var type = allLayouts.layoutTypes[layoutName];
                if (selectedActionType == InputActionType.Value && typeof(ButtonControl).IsAssignableFrom(type))
                    continue;

                ////TODO: skip aliases

                if (typeof(InputControl).IsAssignableFrom(type) && !typeof(InputDevice).IsAssignableFrom(type))
                    yield return layoutName;
            }
        }

        public static IEnumerable<ParameterListView> GetInteractionsAsParameterListViews(InputActionsEditorState state, SerializedInputAction? inputAction)
        {
            Type expectedValueType = null;
            if (inputAction.HasValue && !string.IsNullOrEmpty(inputAction.Value.expectedControlType))
                expectedValueType = EditorInputControlLayoutCache.GetValueType(inputAction.Value.expectedControlType);

            var interactions = string.Empty;
            if (inputAction.HasValue && state.selectionType == SelectionType.Action)
                interactions = inputAction.Value.interactions;
            else if (state.selectionType == SelectionType.Binding && GetSelectedBinding(state).HasValue)
                interactions = GetSelectedBinding(state)?.interactions;

            return CreateParameterListViews(
                interactions,
                expectedValueType,
                InputInteraction.s_Interactions.LookupTypeRegistration,
                InputInteraction.GetValueType);
        }

        public static IEnumerable<ParameterListView> GetProcessorsAsParameterListViews(InputActionsEditorState state, SerializedInputAction? inputAction)
        {
            var processors = string.Empty;
            Type expectedValueType = null;

            if (inputAction.HasValue && !string.IsNullOrEmpty(inputAction.Value.expectedControlType))
                expectedValueType = EditorInputControlLayoutCache.GetValueType(inputAction.Value.expectedControlType);

            if (inputAction.HasValue && state.selectionType == SelectionType.Action)
                processors = inputAction.Value.processors;
            else if (state.selectionType == SelectionType.Binding && GetSelectedBinding(state).HasValue)
                processors = GetSelectedBinding(state)?.processors;

            return CreateParameterListViews(
                processors,
                expectedValueType,
                InputProcessor.s_Processors.LookupTypeRegistration,
                InputProcessor.GetValueTypeFromType);
        }

        private static IEnumerable<ParameterListView> CreateParameterListViews(string interactions, Type expectedValueType,
            Func<string, Type> typeLookup, Func<Type, Type> getGenericArgumentType)
        {
            return NameAndParameters.ParseMultiple(interactions)
                .Select(p => (interaction: p, rowType: typeLookup(p.name)))
                .Select(t =>
                {
                    var(parameter, rowType) = t;

                    var parameterListView = new ParameterListView();
                    parameterListView.Initialize(rowType, parameter.parameters);

                    parameterListView.name = ObjectNames.NicifyVariableName(parameter.name);

                    if (rowType == null)
                    {
                        parameterListView.name += " (Obsolete)";
                    }
                    else if (expectedValueType != null)
                    {
                        var valueType = getGenericArgumentType(rowType);
                        if (valueType != null && !expectedValueType.IsAssignableFrom(valueType))
                            parameterListView.name += " (Incompatible Value Type)";
                    }

                    return parameterListView;
                });
        }
    }
}

#endif
