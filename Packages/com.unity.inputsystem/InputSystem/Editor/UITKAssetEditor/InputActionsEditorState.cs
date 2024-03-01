#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    [System.Serializable]

    internal class CutElement
    {
        private string id;
        internal Type type;

        public CutElement(string id, Type type)
        {
            this.id = id;
            this.type = type;
        }

        public int GetIndexOfProperty(InputActionsEditorState state)
        {
            if (type == typeof(InputActionMap))
            {
                var actionMap = state.serializedObject
                    ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                    ?.FirstOrDefault(s => s.FindPropertyRelative("m_Id").stringValue.Equals(id));
                return actionMap.GetIndexOfArrayElement();
            }

            if (type == typeof(InputAction))
            {
                var action = Selectors.GetActionMapAtIndex(state, actionMapIndex(state))?.wrappedProperty.FindPropertyRelative("m_Actions").FirstOrDefault(a => a.FindPropertyRelative("m_Id").stringValue.Equals(id));
                return action.GetIndexOfArrayElement();
            }

            if (type == typeof(InputBinding))
            {
                var binding = Selectors.GetBindingForId(state, id,
                    out _);
                return binding.GetIndexOfArrayElement();
            }
            return -1;
        }

        public int actionMapIndex(InputActionsEditorState state) => type == typeof(InputActionMap) ? GetIndexOfProperty(state) : GetActionMapIndex(state);

        private int GetActionMapIndex(InputActionsEditorState state)
        {
            var actionMaps = state.serializedObject?.FindProperty(nameof(InputActionAsset.m_ActionMaps));
            var cutActionMapIndex = state.serializedObject
                ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                ?.FirstOrDefault(s => s.FindPropertyRelative("m_Id").stringValue.Equals(id)).GetIndexOfArrayElement();
            if (type == typeof(InputBinding))
                cutActionMapIndex =  actionMaps.FirstOrDefault(map => map.FindPropertyRelative("m_Bindings").Select(a => a.FindPropertyRelative("m_Id").stringValue).Contains(id)).GetIndexOfArrayElement();
            else if (type == typeof(InputAction))
                cutActionMapIndex =  actionMaps.FirstOrDefault(map => map.FindPropertyRelative("m_Actions").Select(a => a.FindPropertyRelative("m_Id").stringValue).Contains(id)).GetIndexOfArrayElement();
            return cutActionMapIndex ?? -1;
        }
    }
    internal struct InputActionsEditorState
    {
        public int selectedActionMapIndex { get {return m_selectedActionMapIndex; } }
        public int selectedActionIndex { get {return m_selectedActionIndex; } }
        public int selectedBindingIndex { get {return m_selectedBindingIndex; } }
        public SelectionType selectionType { get {return m_selectionType; } }
        public SerializedObject serializedObject { get; }
        private readonly List<CutElement> cutElements => m_CutElements;

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
        internal bool hasCutElements => m_CutElements != null && m_CutElements.Count > 0;

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
            List<CutElement> cutElements = null)
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
            m_CutElements = cutElements;
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
            if (m_selectedControlSchemeIndex >= 0 && m_selectedControlSchemeIndex < serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes)).arraySize)
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
            var type = selectionType == SelectionType.Action ? typeof(InputAction) : typeof(InputBinding);
            var property = selectionType == SelectionType.Action ? Selectors.GetSelectedAction(this)?.wrappedProperty : Selectors.GetSelectedBinding(this)?.wrappedProperty;
            cutElements.Add(new CutElement(property?.FindPropertyRelative("m_Id").stringValue, type));
            return With(cutElements: cutElements);
        }

        public InputActionsEditorState CutActionMaps()
        {
            m_CutElements = new List<CutElement> { new(Selectors.GetSelectedActionMap(this)?.wrappedProperty?.FindPropertyRelative("m_Id").stringValue, typeof(InputActionMap)) };
            return With(cutElements: cutElements);
        }

        public IEnumerable<string> GetDisabledActionMaps(List<string> allActionMaps)
        {
            if (cutElements == null || cutElements == null)
                return Enumerable.Empty<string>();
            var cutActionMaps = cutElements.Where(cut => cut.type == typeof(InputActionMap));
            var state = this;
            return allActionMaps.Where(actionMapName =>
            {
                return cutActionMaps.Any(am => am.GetIndexOfProperty(state) == allActionMaps.IndexOf(actionMapName));
            });
        }

        public readonly bool IsBindingCut(int actionMapIndex, int bindingIndex)
        {
            if (cutElements == null)
                return false;

            var state = this;
            return cutElements.Any(cutElement => cutElement.actionMapIndex(state) == actionMapIndex &&
                cutElement.GetIndexOfProperty(state) == bindingIndex &&
                cutElement.type == typeof(InputBinding));
        }

        public readonly bool IsActionCut(int actionMapIndex, int actionIndex)
        {
            if (cutElements == null)
                return false;

            var state = this;
            return cutElements.Any(cutElement => cutElement.actionMapIndex(state) == actionMapIndex &&
                cutElement.GetIndexOfProperty(state) == actionIndex &&
                cutElement.type == typeof(InputAction));
        }

        public readonly bool IsActionMapCut(int actionMapIndex)
        {
            if (cutElements == null)
                return false;
            var state = this;
            return cutElements.Any(cutElement => cutElement.GetIndexOfProperty(state) == actionMapIndex && cutElement.type == typeof(InputActionMap));
        }

        public readonly List<CutElement> GetCutElements()
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
