#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
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
        public SerializedObject serializedObject { get; }

        // Control schemes
        public int selectedControlSchemeIndex { get {return m_selectedControlSchemeIndex; } }
        public int selectedDeviceRequirementIndex { get {return m_selectedDeviceRequirementIndex; } }
        public InputControlScheme selectedControlScheme => m_ControlScheme;

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
            serializedObject = inputActionAsset;

            this.m_selectedActionMapIndex = selectedActionMapIndex;
            this.m_selectedActionIndex = selectedActionIndex;
            this.m_selectedBindingIndex = selectedBindingIndex;
            this.m_selectionType = selectionType;
            m_ControlScheme = selectedControlScheme;
            this.m_selectedControlSchemeIndex = selectedControlSchemeIndex;
            this.m_selectedDeviceRequirementIndex = selectedDeviceRequirementIndex;

            m_ExpandedCompositeBindings = expandedBindingIndices == null ?
                new Dictionary<(string, string), HashSet<int>>() :
                new Dictionary<(string, string), HashSet<int>>(expandedBindingIndices);
        }

        public InputActionsEditorState(InputActionsEditorState other, SerializedObject asset)
        {
            serializedObject = asset;

            m_selectedActionMapIndex = other.m_selectedActionMapIndex;
            m_selectedActionIndex = other.m_selectedActionIndex;
            m_selectedBindingIndex = other.m_selectedBindingIndex;
            m_selectionType = other.m_selectionType;
            m_ControlScheme = other.m_ControlScheme;
            m_selectedControlSchemeIndex = other.m_selectedControlSchemeIndex;
            m_selectedDeviceRequirementIndex = other.m_selectedDeviceRequirementIndex;

            // Editor may leave these as null after domain reloads, so recreate them
            m_ExpandedCompositeBindings = (other.m_ExpandedCompositeBindings == null)
                ? new Dictionary<(string, string), HashSet<int>>()
                : other.m_ExpandedCompositeBindings;
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

            throw new InvalidOperationException($"Couldn't find an action map with name '{actionName}'.");
        }

        public InputActionsEditorState SelectAction(SerializedProperty state)
        {
            var index = state.GetIndexOfArrayElement();
            return With(selectedActionIndex: index, selectionType: SelectionType.Action);
        }

        public InputActionsEditorState SelectActionMap(SerializedProperty state)
        {
            var index = state.GetIndexOfArrayElement();
            return With(selectedBindingIndex: 0, selectedActionMapIndex: index, selectedActionIndex: 0);
        }

        public InputActionsEditorState SelectActionMap(string actionMapName)
        {
            var actionMap = GetActionMapByName(actionMapName);
            return With(selectedBindingIndex: 0,
                selectedActionMapIndex: actionMap.GetIndexOfArrayElement(),
                selectedActionIndex: 0);
        }

        public InputActionsEditorState SelectBinding(int index)
        {
            return With(selectedBindingIndex: index);
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

        private (string, string) GetSelectedActionMapAndActionKey()
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

        private SerializedProperty GetSelectedActionMap()
        {
            return serializedObject
                .FindProperty(nameof(InputActionAsset.m_ActionMaps))
                .GetArrayElementAtIndex(selectedActionMapIndex);
        }

        private readonly Dictionary<(string, string), HashSet<int>> m_ExpandedCompositeBindings;

        /// <summary>
        /// Expanded states for the actions tree view. These are stored per InputActionMap
        /// </summary>
        private readonly InputControlScheme m_ControlScheme;
    }

    internal enum SelectionType
    {
        Action,
        Binding
    }
}

#endif
