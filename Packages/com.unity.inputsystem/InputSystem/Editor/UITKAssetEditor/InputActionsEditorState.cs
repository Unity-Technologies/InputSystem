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
        public SerializedObject serializedObject { get; }
        private List<CutElement> cutElements => m_CutElements;

        internal struct CutElement
        {
            internal int actionMapIndex;
            internal int actionOrBindingIndex;
            internal Type type;

            public CutElement(int actionMapIndex)
            {
                this.actionMapIndex = actionMapIndex;
                actionOrBindingIndex = -1;
                type = typeof(InputActionMap);
            }

            public CutElement(int actionMapIndex, int actionOrBindingIndex, Type type)
            {
                this.actionMapIndex = actionMapIndex;
                this.actionOrBindingIndex = actionOrBindingIndex;
                this.type = type;
            }
        }

        // Control schemes
        public int selectedControlSchemeIndex { get { return m_selectedControlSchemeIndex; } }
        public int selectedDeviceRequirementIndex { get  {return m_selectedDeviceRequirementIndex; } }
        public InputControlScheme selectedControlScheme => m_ControlScheme;

        [SerializeField] int m_selectedActionMapIndex;
        [SerializeField] int m_selectedActionIndex;
        [SerializeField] int m_selectedBindingIndex;
        [SerializeField] SelectionType m_selectionType;
        [SerializeField] int m_selectedControlSchemeIndex;
        [SerializeField] int m_selectedDeviceRequirementIndex;
        private List<CutElement> m_CutElements;

        public InputActionsEditorState(
            SerializedObject inputActionAsset,
            int selectedActionMapIndex = 0,
            int selectedActionIndex = 0,
            int selectedBindingIndex = 0,
            SelectionType selectionType = SelectionType.Action,
            Dictionary<(string, string), HashSet<int>> expandedBindingIndices = null,
            InputControlScheme selectedControlScheme = default,
            int selectedControlSchemeIndex = -1,
            int selectedDeviceRequirementIndex = -1,
            List<CutElement> cutElementses = null)
        {
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
            m_CutElements = cutElementses;
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

            // Selected ControlScheme index is serialized but we have to recreated actual object after domain reload
            if (m_selectedControlSchemeIndex != -1)
            {
                var controlSchemeSerializedProperty = serializedObject
                    .FindProperty(nameof(InputActionAsset.m_ControlSchemes))
                    .GetArrayElementAtIndex(m_selectedControlSchemeIndex);

                m_ControlScheme = new InputControlScheme(controlSchemeSerializedProperty);
            }
            else
                m_ControlScheme = new InputControlScheme();

            // Editor may leave these as null after domain reloads, so recreate them
            m_ExpandedCompositeBindings = (other.m_ExpandedCompositeBindings == null)
                ? new Dictionary<(string, string), HashSet<int>>()
                : other.m_ExpandedCompositeBindings;
            m_CutElements = other.cutElements;
        }

        public InputActionsEditorState With(
            int? selectedActionMapIndex = null,
            int? selectedActionIndex = null,
            int? selectedBindingIndex = null,
            SelectionType? selectionType = null,
            InputControlScheme? selectedControlScheme = null,
            int? selectedControlSchemeIndex = null,
            int? selectedDeviceRequirementIndex = null,
            Dictionary<(string, string), HashSet<int>> expandedBindingIndices = null,
            List<CutElement> cutElements = null)
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
                selectedDeviceRequirementIndex ?? this.selectedDeviceRequirementIndex,

                cutElements ?? m_CutElements
            );
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

        public InputActionsEditorState CutActionOrBinding()
        {
            m_CutElements = new List<CutElement>();
            var actionOrBindingIndex = selectionType == SelectionType.Action ? selectedActionIndex : selectedBindingIndex;
            var type = selectionType == SelectionType.Action ? typeof(InputAction) : typeof(InputBinding);
            cutElements.Add(new CutElement(selectedActionMapIndex, actionOrBindingIndex, type));
            return With(cutElements: cutElements);
        }

        public InputActionsEditorState CutActionMaps()
        {
            m_CutElements = new List<CutElement> { new(selectedActionMapIndex) };
            return With(cutElements: cutElements);
        }

        public IEnumerable<string> GetDisabledActionMaps(List<string> allActionMaps)
        {
            if (cutElements == null || cutElements == null)
                return Enumerable.Empty<string>();
            var cutActionMaps = cutElements.Where(cut => cut.type == typeof(InputActionMap));
            return allActionMaps.Where(actionMapName =>
            {
                return cutActionMaps.Any(am => am.actionMapIndex == allActionMaps.IndexOf(actionMapName));
            });
        }

        public bool IsBindingCut(int actionMapIndex, int bindingIndex)
        {
            if (cutElements == null)
                return false;
            return cutElements.Any(cutElement => cutElement.actionMapIndex == actionMapIndex && cutElement.actionOrBindingIndex == bindingIndex && cutElement.type == typeof(InputBinding));
        }

        public bool IsActionCut(int actionMapIndex, int actionIndex)
        {
            if (cutElements == null)
                return false;
            return cutElements.Any(cutElement => cutElement.actionMapIndex == actionMapIndex && cutElement.actionOrBindingIndex == actionIndex && cutElement.type == typeof(InputAction));
        }

        public bool IsActionMapCut(int actionMapIndex)
        {
            if (cutElements == null)
                return false;
            return cutElements.Any(cutElement => cutElement.actionMapIndex == actionMapIndex && cutElement.type == typeof(InputActionMap));
        }

        public List<CutElement> GetCutElements()
        {
            return m_CutElements;
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
        None,
        Action,
        Binding
    }
}

#endif
