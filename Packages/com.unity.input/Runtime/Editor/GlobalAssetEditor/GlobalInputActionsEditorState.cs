using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal struct GlobalInputActionsEditorState
    {
        private Dictionary<(string, string), HashSet<int>> m_ExpandedCompositeBindings;

        public ReactiveProperty<int> selectedActionMapIndex { get; private set; }
        public ReactiveProperty<int> selectedActionIndex { get; private set; }
        public ReactiveProperty<int> selectedBindingIndex { get; private set; }
        public ReactiveProperty<SelectionType> selectionType { get; private set; }

        public SerializedObject serializedObject { get; private set; }
        public SerializedProperty actionMaps { get; private set; }


        public GlobalInputActionsEditorState(SerializedObject inputActionAsset)
        {
            serializedObject = inputActionAsset;
            actionMaps = serializedObject.FindProperty(nameof(InputActionAsset.m_ActionMaps));

            selectedActionMapIndex = new ReactiveProperty<int>();
            selectedActionIndex = new ReactiveProperty<int>();
            selectedBindingIndex = new ReactiveProperty<int>();
            selectionType = new ReactiveProperty<SelectionType>();

            m_ExpandedCompositeBindings = new Dictionary<(string, string), HashSet<int>>();

            selectedActionMapIndex.value = 0;
            selectedActionIndex.value = 0;
            selectedBindingIndex.value = 0;
        }

        public SerializedProperty GetActionMapByName(string actionMapName)
        {
            return serializedObject
                .FindProperty(nameof(InputActionAsset.m_ActionMaps))
                .FirstOrDefault(p => p.FindPropertyRelative(nameof(InputActionMap.m_Name)).stringValue == actionMapName);
        }

        public void ExpandCompositeBinding(SerializedInputBinding binding)
        {
            var expandedStates = GetOrCreateExpandedState();
            expandedStates.Add(binding.indexOfBinding);
        }

        public void CollapseCompositeBinding(SerializedInputBinding binding)
        {
            var key = GetSelectedActionMapAndActionKey();

            if (m_ExpandedCompositeBindings.ContainsKey(key) == false)
                throw new InvalidOperationException("Trying to collapse a composite binding tree that was never expanded.");

            m_ExpandedCompositeBindings[key].Remove(binding.indexOfBinding);
        }

        public void SelectAction(string actionName)
        {
            var actionMap = GetSelectedActionMap();
            var actions = actionMap.FindPropertyRelative(nameof(InputActionMap.m_Actions));

            for (var i = 0; i < actions.arraySize; i++)
            {
                if (actions.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(InputAction.m_Name)).stringValue !=
                    actionName) continue;

                selectedActionIndex.value = i;
                selectionType.value = SelectionType.Action;
                break;
            }
        }

        public HashSet<int> GetOrCreateExpandedState()
        {
            var key = GetSelectedActionMapAndActionKey();

            if (m_ExpandedCompositeBindings.TryGetValue(key, out var expandedStates))
                return expandedStates;

            expandedStates = new HashSet<int>();
            m_ExpandedCompositeBindings.Add(key, expandedStates);
            return expandedStates;
        }

        public void SelectActionMap(string actionMapName)
        {
            var actionMap = GetActionMapByName(actionMapName);
            selectedBindingIndex.SetValueWithoutChangeNotification(0);
            selectedActionMapIndex.value = actionMap.GetIndexOfArrayElement();
        }

        private (string, string) GetSelectedActionMapAndActionKey()
        {
            var selectedActionMap = GetSelectedActionMap();

            var selectedAction = selectedActionMap
                .FindPropertyRelative(nameof(InputActionMap.m_Actions))
                .GetArrayElementAtIndex(selectedActionIndex.value);

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
                .GetArrayElementAtIndex(selectedActionMapIndex.value);
        }
    }

    internal enum SelectionType
    {
        Action,
        Binding
    }
}
