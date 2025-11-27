#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    [System.Serializable]

    internal class CutElement
    {
        private Guid id;
        internal Type type;

        public CutElement(Guid id, Type type)
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
                    ?.FirstOrDefault(s => InputActionSerializationHelpers.GetId(s).Equals(id));
                return actionMap.GetIndexOfArrayElement();
            }

            if (type == typeof(InputAction))
            {
                var action = Selectors.GetActionMapAtIndex(state, actionMapIndex(state))?.wrappedProperty.FindPropertyRelative("m_Actions").FirstOrDefault(a => InputActionSerializationHelpers.GetId(a).Equals(id));
                return action.GetIndexOfArrayElement();
            }

            if (type == typeof(InputBinding))
            {
                var binding = Selectors.GetBindingForId(state, id.ToString(),
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
                cutActionMapIndex =  actionMaps.FirstOrDefault(map => map.FindPropertyRelative("m_Bindings").Select(InputActionSerializationHelpers.GetId).Contains(id)).GetIndexOfArrayElement();
            else if (type == typeof(InputAction))
                cutActionMapIndex =  actionMaps.FirstOrDefault(map => map.FindPropertyRelative("m_Actions").Select(InputActionSerializationHelpers.GetId).Contains(id)).GetIndexOfArrayElement();
            return cutActionMapIndex ?? -1;
        }
    }
    internal struct InputActionsEditorState
    {
        public int selectedActionMapIndex { get {return m_selectedActionMapIndex; } }
        public int selectedActionIndex { get {return m_selectedActionIndex; } }
        public int selectedBindingIndex { get {return m_selectedBindingIndex; } }
        public SelectionType selectionType { get {return m_selectionType; } }
        public SerializedObject serializedObject { get; } // Note that state doesn't own this disposable object
        private readonly List<CutElement> cutElements => m_CutElements;

        // Control schemes
        public int selectedControlSchemeIndex { get { return m_selectedControlSchemeIndex; } }
        public int selectedDeviceRequirementIndex { get  {return m_selectedDeviceRequirementIndex; } }
        public InputControlScheme selectedControlScheme => m_ControlScheme; // TODO Bad this either po

        internal InputActionsEditorSessionAnalytic m_Analytics;

        [SerializeField] int m_selectedActionMapIndex;
        [SerializeField] int m_selectedActionIndex;
        [SerializeField] int m_selectedBindingIndex;
        [SerializeField] SelectionType m_selectionType;
        [SerializeField] int m_selectedControlSchemeIndex;
        [SerializeField] int m_selectedDeviceRequirementIndex;
        private List<CutElement> m_CutElements;
        internal bool hasCutElements => m_CutElements != null && m_CutElements.Count > 0;

        public InputActionsEditorState(
            InputActionsEditorSessionAnalytic analytics,
            SerializedObject inputActionAsset,
            int selectedActionMapIndex = 0,
            int selectedActionIndex = 0,
            int selectedBindingIndex = 0,
            SelectionType selectionType = SelectionType.Action,
            InputControlScheme selectedControlScheme = default,
            int selectedControlSchemeIndex = -1,
            int selectedDeviceRequirementIndex = -1,
            List<CutElement> cutElements = null)
        {
            Debug.Assert(inputActionAsset != null);

            m_Analytics = analytics;

            serializedObject = inputActionAsset;

            m_selectedActionMapIndex = selectedActionMapIndex;
            m_selectedActionIndex = selectedActionIndex;
            m_selectedBindingIndex = selectedBindingIndex;
            m_selectionType = selectionType;
            m_ControlScheme = selectedControlScheme;
            m_selectedControlSchemeIndex = selectedControlSchemeIndex;
            m_selectedDeviceRequirementIndex = selectedDeviceRequirementIndex;

            m_CutElements = cutElements;
        }

        public InputActionsEditorState(InputActionsEditorState other, SerializedObject asset)
        {
            m_Analytics = other.m_Analytics;

            // Assign serialized object, not that this might be equal to other.serializedObject,
            // a slight variation of it with any kind of changes or a completely different one.
            // Hence, we do our best here to keep any selections consistent by remapping objects
            // based on GUIDs (IDs) and when it fails, attempt to select first object and if that
            // fails revert to not having a selection. This would even be true for domain reloads
            // if the asset would be modified during domain reload.
            serializedObject = asset;

            if (other.Equals(default(InputActionsEditorState)))
            {
                // This instance was created by default constructor and thus is missing some appropriate defaults:
                other.m_selectionType = SelectionType.Action;
                other.m_selectedControlSchemeIndex = -1;
                other.m_selectedDeviceRequirementIndex = -1;
            }

            // Attempt to preserve action map selection by GUID, otherwise select first or last resort none
            var otherSelectedActionMap = other.GetSelectedActionMap();
            var actionMapCount = Selectors.GetActionMapCount(asset);
            m_selectedActionMapIndex = otherSelectedActionMap != null
                ? Selectors.GetActionMapIndexFromId(asset,
                InputActionSerializationHelpers.GetId(otherSelectedActionMap))
                : actionMapCount > 0 ? 0 : -1;
            var selectedActionMap = m_selectedActionMapIndex >= 0
                ? Selectors.GetActionMapAtIndex(asset, m_selectedActionMapIndex)?.wrappedProperty : null;

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
            List<CutElement> cutElements = null)
        {
            return new InputActionsEditorState(
                m_Analytics,
                serializedObject,
                selectedActionMapIndex ?? this.selectedActionMapIndex,
                selectedActionIndex ?? this.selectedActionIndex,
                selectedBindingIndex ?? this.selectedBindingIndex,
                selectionType ?? this.selectionType,

                // Control schemes
                selectedControlScheme ?? this.selectedControlScheme,
                selectedControlSchemeIndex ?? this.selectedControlSchemeIndex,
                selectedDeviceRequirementIndex ?? this.selectedDeviceRequirementIndex,

                cutElements ?? m_CutElements
            );
        }

        public InputActionsEditorState ClearCutElements()
        {
            return new InputActionsEditorState(
                m_Analytics,
                serializedObject,
                selectedActionMapIndex,
                selectedActionIndex,
                selectedBindingIndex,
                selectionType,
                selectedControlScheme,
                selectedControlSchemeIndex,
                selectedDeviceRequirementIndex,
                cutElements: null);
        }

        public SerializedProperty GetActionMapByName(string actionMapName)
        {
            return serializedObject
                .FindProperty(nameof(InputActionAsset.m_ActionMaps))
                .FirstOrDefault(p => p.FindPropertyRelative(nameof(InputActionMap.m_Name)).stringValue == actionMapName);
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
            return With(selectedBindingIndex: index, selectionType: SelectionType.Binding);
        }

        public InputActionsEditorState SelectAction(int index)
        {
            //if no action selected (no actions available) set selection type to none
            if (index == -1)
                return With(selectedActionIndex: index, selectionType: SelectionType.None);
            return With(selectedActionIndex: index, selectionType: SelectionType.Action);
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
            cutElements.Add(new CutElement(InputActionSerializationHelpers.GetId(property), type));
            return With(cutElements: cutElements);
        }

        public InputActionsEditorState CutActionMaps()
        {
            m_CutElements = new List<CutElement> { new(InputActionSerializationHelpers.GetId(Selectors.GetSelectedActionMap(this)?.wrappedProperty), typeof(InputActionMap)) };
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

        private SerializedProperty GetSelectedActionMap()
        {
            return Selectors.GetActionMapAtIndex(serializedObject, selectedActionMapIndex)?.wrappedProperty;
        }

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
