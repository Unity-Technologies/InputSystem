#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    [System.Serializable]
    internal struct InputActionsEditorState
    {
        public int selectedActionMapIndex { get {return m_selectedActionMapIndex; } }
        public int selectedActionIndex { get {return m_selectedActionIndex; } }
        public int selectedBindingIndex { get {return m_selectedBindingIndex; } }
        public SelectionType selectionType { get {return m_selectionType; } }
        public SerializedObject serializedObject { get; } // Note that state doesn't own this disposable object

        // Control schemes
        public int selectedControlSchemeIndex { get { return m_selectedControlSchemeIndex; } }
        public int selectedDeviceRequirementIndex { get  {return m_selectedDeviceRequirementIndex; } }
        public InputControlScheme selectedControlScheme => m_ControlScheme; // TODO Bad this either po

        [SerializeField] int m_selectedActionMapIndex;
        [SerializeField] int m_selectedActionIndex;
        [SerializeField] int m_selectedBindingIndex;
        [SerializeField] SelectionType m_selectionType;
        [SerializeField] int m_selectedControlSchemeIndex;
        [SerializeField] int m_selectedDeviceRequirementIndex;

        public InputActionsEditorState(
            SerializedObject inputActionAsset,
            int selectedActionMapIndex = 0,
            int selectedActionIndex = 0,
            int selectedBindingIndex = 0,
            SelectionType selectionType = SelectionType.Action,
            Dictionary<(string, string), HashSet<int>> expandedBindingIndices = null,
            InputControlScheme selectedControlScheme = default,
            int selectedControlSchemeIndex = -1,
            int selectedDeviceRequirementIndex = -1)
        {
            Debug.Assert(inputActionAsset != null);

            serializedObject = inputActionAsset;

            m_selectedActionMapIndex = selectedActionMapIndex;
            m_selectedActionIndex = selectedActionIndex;
            m_selectedBindingIndex = selectedBindingIndex;
            m_selectionType = selectionType;
            m_ControlScheme = selectedControlScheme;
            m_selectedControlSchemeIndex = selectedControlSchemeIndex;
            m_selectedDeviceRequirementIndex = selectedDeviceRequirementIndex;

            m_ExpandedCompositeBindings = expandedBindingIndices == null ?
                new Dictionary<(string, string), HashSet<int>>() :
                new Dictionary<(string, string), HashSet<int>>(expandedBindingIndices);
        }

        private static int AdjustSelection(SerializedObject serializedObject, string propertyName, int index)
        {
            if (index < 0)
                return index;
            var controlSchemesArrayProperty = serializedObject.FindProperty(propertyName);
            if (index >= controlSchemesArrayProperty.arraySize)
                return 0;
            return index;
        }

        public InputActionsEditorState(InputActionsEditorState other, SerializedObject asset)
        {
            // Assign serialized object, not that this might be equal to other.serializedObject,
            // a slight variation of it with any kind of changes or a completely different one.
            // Hence, we do our best here to keep any selections consistent by remapping objects
            // based on GUIDs (IDs) and when it fails, attempt to select first object and if that
            // fails revert to not having a selection. This would even be true for domain reloads
            // if the asset would be modified during domain reload.
            serializedObject = asset;

            // Attempt to preserve action map selection by GUID, otherwise select first or last resort none
            var otherSelectedActionMap = other.GetSelectedActionMap();
            var actionMapCount = Selectors.GetActionMapCount(asset);
            m_selectedActionMapIndex = otherSelectedActionMap != null
                ? Selectors.GetActionMapIndexFromId(asset,
                InputActionSerializationHelpers.GetId(otherSelectedActionMap))
                : actionMapCount > 0 ? 0 : -1;
            var selectedActionMap = m_selectedActionMapIndex >= 0
                ? GetActionMap(asset, m_selectedActionMapIndex) : null;

            // Attempt to preserve action selection by GUID, otherwise select first or last resort none
            var otherSelectedAction = m_selectedActionMapIndex >= 0 ?
                Selectors.GetSelectedAction(other) : null;
            m_selectedActionIndex = selectedActionMap != null && otherSelectedAction.HasValue
                ? Selectors.GetActionIndexFromId(selectedActionMap,
                InputActionSerializationHelpers.GetId(otherSelectedAction.Value.wrappedProperty))
                : Selectors.GetActionCount(selectedActionMap) > 0 ? 0 : -1;

            // Attempt to preserve binding selection by GUID, otherwise select first or none
            m_selectedBindingIndex = -1;
            if (m_selectedActionMapIndex >= 0)
            {
                var otherSelectedBinding = Selectors.GetSelectedBinding(other);
                if (otherSelectedBinding != null)
                {
                    var otherSelectedBindingId =
                        InputActionSerializationHelpers.GetId(otherSelectedBinding.Value.wrappedProperty);
                    var binding = Selectors.GetBindingForId(asset, otherSelectedBindingId.ToString(), out _);
                    if (binding != null)
                        m_selectedBindingIndex = binding.GetIndexOfArrayElement();
                }
            }

            // Sanity check selection type and override any previous selection if not valid given indices
            // since we have remapped GUIDs to selection indices for another asset (SerializedObject)
            if (other.m_selectionType == SelectionType.Binding && m_selectedBindingIndex < 0)
                m_selectionType = SelectionType.Action;
            else
                m_selectionType = other.m_selectionType;

            m_selectedControlSchemeIndex = other.m_selectedControlSchemeIndex;
            m_selectedDeviceRequirementIndex = other.m_selectedDeviceRequirementIndex;

            // Selected ControlScheme index is serialized but we have to recreated actual object after domain reload.
            // In case asset is different from from others asset the index might not even be valid range so we need
            // to reattempt to preserve selection but range adapt.
            // Note that control schemes and device requirements currently lack any GUID/ID to be uniquely identified.
            var controlSchemesArrayProperty = serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
            if (m_selectedControlSchemeIndex >= 0 && controlSchemesArrayProperty.arraySize > 0)
            {
                if (m_selectedControlSchemeIndex >= controlSchemesArrayProperty.arraySize)
                    m_selectedControlSchemeIndex = 0;
                m_ControlScheme = new InputControlScheme(
                    controlSchemesArrayProperty.GetArrayElementAtIndex(other.m_selectedControlSchemeIndex));
                // TODO Preserve device requirement index
            }
            else
            {
                m_selectedControlSchemeIndex = -1;
                m_selectedDeviceRequirementIndex = -1;
                m_ControlScheme = new InputControlScheme();
            }

            // Editor may leave these as null after domain reloads, so recreate them in that case.
            // If they exist, we attempt to just preserve the same expanded items based on name for now for simplicity.
            m_ExpandedCompositeBindings = other.m_ExpandedCompositeBindings == null ?
                new Dictionary<(string, string), HashSet<int>>() :
                new Dictionary<(string, string), HashSet<int>>(other.m_ExpandedCompositeBindings);
        }

        public InputActionsEditorState With(
            int? selectedActionMapIndex = null,
            int? selectedActionIndex = null,
            int? selectedBindingIndex = null,
            SelectionType? selectionType = null,
            InputControlScheme? selectedControlScheme = null,
            int? selectedControlSchemeIndex = null,
            int? selectedDeviceRequirementIndex = null,
            Dictionary<(string, string), HashSet<int>> expandedBindingIndices = null)
        {
            return new InputActionsEditorState(
                serializedObject,
                selectedActionMapIndex ?? this.selectedActionMapIndex,
                selectedActionIndex ?? this.selectedActionIndex,
                selectedBindingIndex ?? this.selectedBindingIndex,
                selectionType ?? this.selectionType,
                expandedBindingIndices ?? m_ExpandedCompositeBindings,

                // Control schemes
                selectedControlScheme ?? this.selectedControlScheme,
                selectedControlSchemeIndex ?? this.selectedControlSchemeIndex,
                selectedDeviceRequirementIndex ?? this.selectedDeviceRequirementIndex);
        }

        public SerializedProperty GetActionMapByName(string actionMapName)
        {
            return serializedObject
                .FindProperty(nameof(InputActionAsset.m_ActionMaps))
                .FirstOrDefault(p => p.FindPropertyRelative(nameof(InputActionMap.m_Name)).stringValue == actionMapName);
        }

        public InputActionsEditorState ExpandCompositeBinding(SerializedInputBinding binding)
        {
            var key = GetSelectedActionMapAndActionKey();

            var expandedCompositeBindings = new Dictionary<(string, string), HashSet<int>>(m_ExpandedCompositeBindings);
            if (!expandedCompositeBindings.TryGetValue(key, out var expandedStates))
            {
                expandedStates = new HashSet<int>();
                expandedCompositeBindings.Add(key, expandedStates);
            }

            expandedStates.Add(binding.indexOfBinding);

            return With(expandedBindingIndices: expandedCompositeBindings);
        }

        public InputActionsEditorState CollapseCompositeBinding(SerializedInputBinding binding)
        {
            var key = GetSelectedActionMapAndActionKey();

            if (m_ExpandedCompositeBindings.ContainsKey(key) == false)
                throw new InvalidOperationException("Trying to collapse a composite binding tree that was never expanded.");

            // do the dance of C# immutability
            var oldExpandedCompositeBindings = m_ExpandedCompositeBindings;
            var expandedCompositeBindings = oldExpandedCompositeBindings.Keys.Where(dictKey => dictKey != key)
                .ToDictionary(dictKey => dictKey, dictKey => oldExpandedCompositeBindings[dictKey]);
            var newHashset = new HashSet<int>(m_ExpandedCompositeBindings[key].Where(index => index != binding.indexOfBinding));
            expandedCompositeBindings.Add(key, newHashset);

            return With(expandedBindingIndices: expandedCompositeBindings);
        }

        public InputActionsEditorState SelectAction(string actionName)
        {
            var actionMap = GetSelectedActionMap();
            var actions = actionMap.FindPropertyRelative(nameof(InputActionMap.m_Actions));

            for (var i = 0; i < actions.arraySize; i++)
            {
                if (actions.GetArrayElementAtIndex(i)
                    .FindPropertyRelative(nameof(InputAction.m_Name)).stringValue != actionName) continue;

                return With(selectedActionIndex: i, selectionType: SelectionType.Action);
            }

            // If we cannot find the desired map we should return invalid index
            return With(selectedActionIndex: -1, selectionType: SelectionType.Action);
        }

        public InputActionsEditorState SelectAction(SerializedProperty state)
        {
            var index = state.GetIndexOfArrayElement();
            return With(selectedActionIndex: index, selectionType: SelectionType.Action);
        }

        public InputActionsEditorState SelectActionMap(SerializedProperty actionMap)
        {
            var index = actionMap.GetIndexOfArrayElement();
            return With(selectedBindingIndex: 0, selectedActionMapIndex: index, selectedActionIndex: 0);
        }

        public InputActionsEditorState SelectActionMap(string actionMapName)
        {
            var actionMap = GetActionMapByName(actionMapName);
            return With(selectedBindingIndex: 0,
                selectedActionMapIndex: actionMap.GetIndexOfArrayElement(),
                selectedActionIndex: 0, selectionType: SelectionType.Action);
        }

        public InputActionsEditorState SelectBinding(int index)
        {
            //if no binding selected (due to no bindings in list) set selection type to action
            if (index == -1)
                return With(selectedBindingIndex: index, selectionType: SelectionType.Action);
            return With(selectedBindingIndex: index);
        }

        public InputActionsEditorState SelectAction(int index)
        {
            //if no action selected (no actions available) set selection type to none
            if (index == -1)
                return With(selectedActionIndex: index, selectionType: SelectionType.None);
            return With(selectedActionIndex: index);
        }

        public InputActionsEditorState SelectActionMap(int index)
        {
            if (index == -1)
                return With(selectedActionMapIndex: index, selectionType: SelectionType.None);
            return With(selectedBindingIndex: 0,
                selectedActionMapIndex: index,
                selectedActionIndex: 0, selectionType: SelectionType.Action);
        }

        public ReadOnlyCollection<int> GetOrCreateExpandedState()
        {
            return new ReadOnlyCollection<int>(GetOrCreateExpandedStateInternal().ToList());
        }

        private HashSet<int> GetOrCreateExpandedStateInternal()
        {
            var key = GetSelectedActionMapAndActionKey();

            if (m_ExpandedCompositeBindings.TryGetValue(key, out var expandedStates))
                return expandedStates;

            expandedStates = new HashSet<int>();
            m_ExpandedCompositeBindings.Add(key, expandedStates);
            return expandedStates;
        }

        internal (string, string) GetSelectedActionMapAndActionKey()
        {
            var selectedActionMap = GetSelectedActionMap();

            var selectedAction = selectedActionMap
                .FindPropertyRelative(nameof(InputActionMap.m_Actions))
                .GetArrayElementAtIndex(selectedActionIndex);

            var key = (
                selectedActionMap.FindPropertyRelative(nameof(InputActionMap.m_Name)).stringValue,
                selectedAction.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue
            );
            return key;
        }

        private static SerializedProperty GetActionMap(SerializedObject serializedObject, int index)
        {
            if (serializedObject == null)
                return null;
            var property = serializedObject.FindProperty(nameof(InputActionAsset.m_ActionMaps));
            if (index >= 0 && index < property.arraySize)
                return property.GetArrayElementAtIndex(index);
            return null;
        }

        private SerializedProperty GetSelectedActionMap()
        {
            return GetActionMap(serializedObject, selectedActionMapIndex);
        }

        private static SerializedProperty GetSelectedAction(SerializedProperty map, int actionIndex)
        {
            return map?.FindPropertyRelative(nameof(InputActionMap.m_Actions))
                .GetArrayElementAtIndex(actionIndex);
        }

        /// <summary>
        /// Expanded states for the actions tree view. These are stored per InputActionMap
        /// </summary>
        private readonly Dictionary<(string, string), HashSet<int>> m_ExpandedCompositeBindings;

        private readonly InputControlScheme m_ControlScheme;
    }

    internal enum SelectionType
    {
        None,
        Action,
        Binding
    }
}

#endif
