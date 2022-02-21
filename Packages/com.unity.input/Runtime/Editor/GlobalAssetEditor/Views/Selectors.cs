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
    internal static class Selectors
    {
        public static IEnumerable<string> GetActionMapNames(GlobalInputActionsEditorState state)
        {
            return state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                ?.Select(m => m.FindPropertyRelative(nameof(InputActionMap.m_Name))?.stringValue)
                ?? Enumerable.Empty<string>();
        }

        public static IEnumerable<SerializedInputAction> GetActionsForSelectedActionMap(GlobalInputActionsEditorState state)
        {
            var actionMapIndex = state.selectedActionMapIndex.value;
            var actionMaps = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ActionMaps));

            var actionMap = actionMapIndex == -1 ?
                actionMaps.GetArrayElementAtIndex(0) :
                actionMaps.GetArrayElementAtIndex(actionMapIndex);

            if (actionMap == null)
                return Enumerable.Empty<SerializedInputAction>();

            return actionMap
                .FindPropertyRelative(nameof(InputActionMap.m_Actions))
                .Select(serializedProperty => new SerializedInputAction(serializedProperty));
        }

        public static SerializedInputActionMap GetSelectedActionMap(GlobalInputActionsEditorState state)
        {
            return new SerializedInputActionMap(state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                ?.GetArrayElementAtIndex(state.selectedActionMapIndex.value));
        }

        public struct BindingViewState
        {
            public SerializedInputBinding binding { get; }
            public bool isExpanded { get; }

            public BindingViewState(SerializedInputBinding binding, bool isExpanded)
            {
                this.binding = binding;
                this.isExpanded = isExpanded;
            }
        }

        /// <summary>
        /// Return a collection of the bindings that should be rendered in the view based on the selected action map, selected action,
        /// and expanded state.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static IEnumerable<BindingViewState> GetVisibleBindingsForSelectedAction(GlobalInputActionsEditorState state)
        {
            var actionMap = state.serializedObject
                .FindProperty(nameof(InputActionAsset.m_ActionMaps))
                .GetArrayElementAtIndex(state.selectedActionMapIndex.value);
            var selectedAction = new SerializedInputAction(
                actionMap.FindPropertyRelative(nameof(InputActionMap.m_Actions))
                    .GetArrayElementAtIndex(state.selectedActionIndex.value));

            var bindings = actionMap
                .FindPropertyRelative(nameof(InputActionMap.m_Bindings))
                .Select(sp => new SerializedInputBinding(sp))
                .Where(sp => sp.action == selectedAction.name);

            var expandedStates = state.GetOrCreateExpandedState();
            var indexOfPreviousComposite = -1;
            foreach (var binding in bindings)
            {
                if (binding.isComposite)
                {
                    indexOfPreviousComposite = binding.indexOfBinding;
                    yield return new BindingViewState(binding, expandedStates.Contains(indexOfPreviousComposite));
                }
                else
                {
                    if (binding.isPartOfComposite)
                    {
                        if (expandedStates.Contains(indexOfPreviousComposite) == false)
                            continue;

                        yield return new BindingViewState(binding, false);
                    }
                    else
                    {
                        yield return new BindingViewState(binding, false);
                    }
                }
            }
        }

        public static SerializedProperty GetSelectedBindingPath(GlobalInputActionsEditorState state)
        {
            var actionMapSO = state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                ?.GetArrayElementAtIndex(state.selectedActionMapIndex.value);

            return actionMapSO?.FindPropertyRelative(nameof(InputActionMap.m_Bindings))
                ?.GetArrayElementAtIndex(state.selectedBindingIndex.value)
                ?.FindPropertyRelative("m_Path");
        }

        public static SerializedInputBinding GetSelectedBinding(GlobalInputActionsEditorState state)
        {
            var actionMapSO = state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                ?.GetArrayElementAtIndex(state.selectedActionMapIndex.value);
            return new SerializedInputBinding(actionMapSO?.FindPropertyRelative(nameof(InputActionMap.m_Bindings))
                ?.GetArrayElementAtIndex(state.selectedBindingIndex.value));
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

        public static SerializedInputAction GetSelectedAction(GlobalInputActionsEditorState state)
        {
            return new SerializedInputAction(state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                ?.GetArrayElementAtIndex(state.selectedActionMapIndex.value)
                ?.FindPropertyRelative(nameof(InputActionMap.m_Actions))
                ?.GetArrayElementAtIndex(state.selectedActionIndex.value));
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

        public static IEnumerable<ParameterListView> GetInteractionsAsParameterListViews(GlobalInputActionsEditorState state)
        {
            var inputAction = GetSelectedAction(state);

            Type expectedValueType = null;
            if (!string.IsNullOrEmpty(inputAction.expectedControlType))
                expectedValueType = EditorInputControlLayoutCache.GetValueType(inputAction.expectedControlType);

            var interactions = string.Empty;
            if (state.selectionType.value == SelectionType.Action)
                interactions = inputAction.interactions;
            else if (state.selectionType.value == SelectionType.Binding)
                interactions = GetSelectedBinding(state).interactions;

            return CreateParameterListViews(
                interactions,
                expectedValueType,
                InputInteraction.s_Interactions.LookupTypeRegistration,
                InputInteraction.GetValueType);
        }

        public static IEnumerable<ParameterListView> GetProcessorsAsParameterListViews(GlobalInputActionsEditorState state)
        {
            var processors = string.Empty;
            Type expectedValueType = null;

            var inputAction = GetSelectedAction(state);

            if (!string.IsNullOrEmpty(inputAction.expectedControlType))
                expectedValueType = EditorInputControlLayoutCache.GetValueType(inputAction.expectedControlType);

            if (state.selectionType.value == SelectionType.Action)
                processors = inputAction.processors;
            else if (state.selectionType.value == SelectionType.Binding)
                processors = GetSelectedBinding(state).processors;

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
