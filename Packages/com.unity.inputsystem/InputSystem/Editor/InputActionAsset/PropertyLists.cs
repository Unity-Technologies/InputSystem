#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor.Lists
{
    ////TODO: rename to InteractionsListView
    internal class InteractionsReorderableReorderableList : PropertiesReorderableList
    {
        public InteractionsReorderableReorderableList(SerializedProperty property, Action applyAction)
            : base(property, applyAction)
        {
        }

        protected override TypeTable GetOptions()
        {
            return InputSystem.s_Manager.interactions;
        }
    }

    ////TODO: rename to ProcessorsListView
    internal class ProcessorsReorderableReorderableList : PropertiesReorderableList
    {
        public ProcessorsReorderableReorderableList(SerializedProperty property, Action applyAction)
            : base(property, applyAction)
        {
        }

        protected override TypeTable GetOptions()
        {
            return InputSystem.s_Manager.processors;
        }
    }

    internal abstract class PropertiesReorderableList
    {
        protected PropertiesReorderableList(SerializedProperty property, Action applyAction)
        {
            m_Property = property;
            m_Apply = applyAction;
            m_ListItems = new List<string>();
            m_ListOptions = GetOptions();
            m_EditableParametersForSelectedItem = new ParameterListView {onChange = OnParametersChanged};
            m_ParametersForEachListItem = InputControlLayout.ParseNameAndParameterList(m_Property.stringValue)
                ?? new InputControlLayout.NameAndParameters[0];

            foreach (var nameAndParams in m_ParametersForEachListItem)
                m_ListItems.Add(nameAndParams.name);

            m_ListView = new ReorderableList(m_ListItems, typeof(string))
            {
                headerHeight = 3,
                onAddDropdownCallback = (rect, list) =>
                {
                    var menu = new GenericMenu();
                    foreach (var name in m_ListOptions.names)
                        menu.AddItem(new GUIContent(name), false, OnAddElement, name);
                    menu.ShowAsContext();
                },
                onRemoveCallback = list =>
                {
                    var index = list.index;
                    list.list.RemoveAt(index);
                    ArrayHelpers.EraseAt(ref m_ParametersForEachListItem, index);
                    m_EditableParametersForSelectedItem.Clear();
                    m_Apply();
                    list.index = -1;
                },
                onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                {
                    MemoryHelpers.Swap(ref m_ParametersForEachListItem[oldIndex],
                        ref m_ParametersForEachListItem[newIndex]);
                    OnSelection(list);
                    m_Apply();
                },
                onSelectCallback = OnSelection
            };
        }

        protected abstract TypeTable GetOptions();

        private void OnAddElement(object data)
        {
            var name = (string)data;

            if (m_ListItems.Count == 1 && m_ListItems[0] == "")
                m_ListItems.Clear();

            m_ListItems.Add(name);
            ArrayHelpers.Append(ref m_ParametersForEachListItem,
                new InputControlLayout.NameAndParameters {name = name});
            m_Apply();
        }

        private void OnParametersChanged()
        {
            var selected = m_ListView.index;
            if (selected < 0)
                return;

            m_ParametersForEachListItem[selected] = new InputControlLayout.NameAndParameters
            {
                name = m_ListItems[selected],
                parameters = m_EditableParametersForSelectedItem.GetParameters(),
            };

            m_Apply();
        }

        private void OnSelection(ReorderableList list)
        {
            var index = list.index;
            if (index < 0)
            {
                m_EditableParametersForSelectedItem.Clear();
                return;
            }

            var typeName = m_ListItems[index];
            var rowType =  m_ListOptions.LookupTypeRegistration(typeName);
            Debug.Assert(rowType != null);

            m_EditableParametersForSelectedItem.Initialize(rowType, m_ParametersForEachListItem[index].parameters);
        }

        public void OnGUI()
        {
            // Use for debugging
            // EditorGUILayout.LabelField(m_Property.stringValue);

            var listRect = GUILayoutUtility.GetRect(200, m_ListView.GetHeight());
            listRect = EditorGUI.IndentedRect(listRect);
            m_ListView.DoList(listRect);
            m_EditableParametersForSelectedItem.OnGUI();
        }

        public string ToSerializableString()
        {
            if (m_ParametersForEachListItem == null)
                return string.Empty;

            return string.Join(InputControlLayout.kSeparatorString,
                m_ParametersForEachListItem.Select(x => x.ToString()).ToArray());
        }

        private List<string> m_ListItems;
        private ReorderableList m_ListView;
        private SerializedProperty m_Property;
        private TypeTable m_ListOptions;

        private InputControlLayout.NameAndParameters[] m_ParametersForEachListItem;
        private ParameterListView m_EditableParametersForSelectedItem;
        private Action m_Apply;
    }
}
#endif // UNITY_EDITOR
