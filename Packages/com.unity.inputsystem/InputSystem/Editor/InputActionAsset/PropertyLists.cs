#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: "properties" seems wrong; these seems to revert to "parameters" specifically

////TODO: nuke "ReorderableReorderable"

namespace UnityEngine.Experimental.Input.Editor.Lists
{
    class InteractionsReorderableReorderableList : PropertiesReorderableList
    {
        public InteractionsReorderableReorderableList(SerializedProperty property, Action applyAction) : base(property, applyAction)
        {
        }

        protected override TypeTable GetOptions()
        {
            return InputSystem.s_Manager.interactions;
        }

        protected override void AddElement(object data)
        {
            if (m_ListView.list.Count == 1 && m_ListView.list[0] == "")
            {
                m_ListView.list.Clear();
            }
            m_ListView.list.Add((string)data);
            m_Apply();
        }

        protected override string GetSeparator()
        {
            return ",";
        }
    }

    class ProcessorsReorderableReorderableList : PropertiesReorderableList
    {
        public ProcessorsReorderableReorderableList(SerializedProperty property, Action applyAction) : base(property, applyAction)
        {
        }

        protected override TypeTable GetOptions()
        {
            return InputSystem.s_Manager.processors;
        }

        protected override void AddElement(object data)
        {
            if (m_ListView.list.Count == 1 && m_ListView.list[0] == "")
            {
                m_ListView.list.Clear();
            }

            m_ListView.list.Add((string)data);
            m_Apply();
        }

        protected override string GetSeparator()
        {
            return InputBinding.kSeparatorString;
        }
    }

    abstract class PropertiesReorderableList
    {
        protected ReorderableList m_ListView;
        SerializedProperty m_Property;
        TypeTable m_ListOptions;
        string m_SelectedRow;

        InputControlLayout.NameAndParameters[] m_NamesAndParams;
        InputControlLayout.ParameterValue[] m_SelectedParameterList;
        protected Action m_Apply;

        public PropertiesReorderableList(SerializedProperty property, Action applyAction)
        {
            m_Property = property;
            m_Apply = applyAction;
            m_ListOptions = GetOptions();
            m_ListView = new ReorderableList(new List<string>(), typeof(string));

            m_NamesAndParams = InputControlLayout.ParseNameAndParameterList(m_Property.stringValue);
            if (m_NamesAndParams == null)
            {
                m_NamesAndParams = new InputControlLayout.NameAndParameters[0];
            }
            foreach (var nameAndParams in m_NamesAndParams)
            {
                m_ListView.list.Add(nameAndParams.name);
            }

            m_ListView.headerHeight = 3;
            m_ListView.onAddDropdownCallback =
                (rect, list) =>
            {
                var menu = new GenericMenu();
                for (var i = 0; i < m_ListOptions.names.Count(); ++i)
                    menu.AddItem(new GUIContent(m_ListOptions.names.ElementAt(i)), false, AddElement, m_ListOptions.names.ElementAt(i));
                menu.ShowAsContext();
            };
            m_ListView.onRemoveCallback =
                (list) =>
            {
                list.list.RemoveAt(list.index);
                m_Apply();
                list.index = -1;
            };
            m_ListView.onReorderCallback = list => { m_Apply(); };
            m_ListView.onSelectCallback = OnSelection;
        }

        protected abstract TypeTable GetOptions();
        protected abstract void AddElement(object data);
        protected abstract string GetSeparator();

        void OnSelection(ReorderableList list)
        {
            if (list.index < 0)
            {
                m_SelectedRow = null;
                return;
            }
            m_SelectedRow = (string)list.list[list.index];
            m_NamesAndParams = InputControlLayout.ParseNameAndParameterList(m_Property.stringValue);
            m_SelectedParameterList = GetFieldsFromClass();
        }

        InputControlLayout.ParameterValue[] GetFieldsFromClass()
        {
            var resultParameters = new List<InputControlLayout.ParameterValue>();
            var serializedParameters = new List<InputControlLayout.ParameterValue>();

            int idx = Array.FindIndex(m_NamesAndParams, a => a.name == m_SelectedRow);
            if (idx >= 0)
            {
                serializedParameters.AddRange(m_NamesAndParams[idx].parameters);
            }

            var rowType =  m_ListOptions.LookupTypeRegistration(m_SelectedRow);
            var fields = rowType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                idx = serializedParameters.FindIndex(a => a.name == field.Name);
                if (idx >= 0)
                {
                    resultParameters.Add(serializedParameters[idx]);
                }
                else
                {
                    var paramValue = new InputControlLayout.ParameterValue();
                    paramValue.name = field.Name;

                    if (field.FieldType == typeof(bool))
                        paramValue.type = InputControlLayout.ParameterType.Boolean;
                    else if (field.FieldType == typeof(int))
                        paramValue.type = InputControlLayout.ParameterType.Integer;
                    else if (field.FieldType == typeof(float))
                        paramValue.type = InputControlLayout.ParameterType.Float;

                    resultParameters.Add(paramValue);
                }
            }
            return resultParameters.ToArray();
        }

        public void OnGUI()
        {
            // Use for debugging
            // EditorGUILayout.LabelField(m_Property.stringValue);

            var listRect = GUILayoutUtility.GetRect(200, m_ListView.GetHeight());
            listRect = EditorGUI.IndentedRect(listRect);
            m_ListView.DoList(listRect);

            if (m_ListView.index >= 0)
            {
                for (int i = 0; i < m_SelectedParameterList.Length; i++)
                {
                    var parameterValue = m_SelectedParameterList[i];
                    EditorGUI.BeginChangeCheck();

                    string result = null;
                    if (parameterValue.type == InputControlLayout.ParameterType.Integer)
                    {
                        var intValue = int.Parse(parameterValue.GetValueAsString());
                        result = EditorGUILayout.IntField(ObjectNames.NicifyVariableName(parameterValue.name), intValue).ToString();
                    }
                    else if (parameterValue.type == InputControlLayout.ParameterType.Float)
                    {
                        var floatValue = float.Parse(parameterValue.GetValueAsString());
                        result = EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(parameterValue.name), floatValue).ToString();
                    }
                    else if (parameterValue.type == InputControlLayout.ParameterType.Boolean)
                    {
                        var boolValue = bool.Parse(parameterValue.GetValueAsString());
                        result = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(parameterValue.name), boolValue).ToString();
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SelectedParameterList[i].SetValue(result);
                        m_Apply();
                    }
                }
            }
        }

        public string ToSerializableString()
        {
            var resultList = new List<string>();
            foreach (string listElement in m_ListView.list)
            {
                var idx = Array.FindIndex(m_NamesAndParams, a => a.name == listElement);
                if (idx >= 0)
                {
                    var param = m_NamesAndParams[idx];
                    var fieldWithValuesList = param.parameters.Select(a => string.Format("{0}={1}", a.name, a.GetValueAsString()));
                    if (m_SelectedRow == m_NamesAndParams[idx].name)
                    {
                        fieldWithValuesList = m_SelectedParameterList.Where(a => !a.IsDefaultValue()).Select(a => string.Format("{0}={1}", a.name, a.GetValueAsString()));
                    }
                    var fieldWithValues = string.Join(",", fieldWithValuesList.ToArray());
                    resultList.Add(string.Format("{0}({1})", param.name, fieldWithValues));
                }
                else
                {
                    resultList.Add(listElement + "()");
                }
            }
            return string.Join(GetSeparator(), resultList.ToArray());
        }
    }
}
#endif // UNITY_EDITOR
