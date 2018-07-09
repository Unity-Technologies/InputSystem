using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    public class InteractionsList
    {
        ReorderableList m_InteractionsListView;
        SerializedProperty m_InteractionsProperty;
        TypeTable m_InteractionChoices;
        string m_SelectedInteractionName;

        InputControlLayout.NameAndParameters[] m_InteractionNamesAndParams;
        InputControlLayout.ParameterValue[] m_SelectedInteractionParameterList;
        Action m_Apply;

        public InteractionsList(SerializedProperty bindingProperty, Action applyAction)
        {
            m_InteractionsProperty = bindingProperty.FindPropertyRelative("interactions");
            m_Apply = applyAction;
            m_InteractionChoices = InputSystem.s_Manager.interactions;
            m_InteractionsListView = new ReorderableList(new List<string>(), typeof(string));
            
            m_InteractionNamesAndParams = InputControlLayout.ParseNameAndParameterList(m_InteractionsProperty.stringValue);
            if (m_InteractionNamesAndParams == null)
            {
                m_InteractionNamesAndParams = new InputControlLayout.NameAndParameters[0];
            }
            foreach (var nameAndParams in m_InteractionNamesAndParams)
            {
                m_InteractionsListView.list.Add(nameAndParams.name);
            }
            
            m_InteractionsListView.headerHeight = 3;
            m_InteractionsListView.onAddDropdownCallback =
                (rect, list) =>
                {
                    var menu = new GenericMenu();
                    for (var i = 0; i < m_InteractionChoices.names.Count(); ++i)
                        menu.AddItem(new GUIContent(m_InteractionChoices.names.ElementAt(i)), false, AddModifier, m_InteractionChoices.names.ElementAt(i));
                    menu.ShowAsContext();
                };
            m_InteractionsListView.onRemoveCallback =
                (list) =>
                {
                    list.list.RemoveAt(list.index);
                    m_Apply();
                    list.index = -1;
                };
            m_InteractionsListView.onReorderCallback = list => { m_Apply(); };
            m_InteractionsListView.onSelectCallback = OnInteractionSelection;
        }

        void AddModifier(object modifierNameString)
        {
            if (m_InteractionsListView.list.Count == 1 && m_InteractionsListView.list[0] == "")
            {
                m_InteractionsListView.list.Clear();
            }
            m_InteractionsListView.list.Add((string)modifierNameString);
            m_Apply();
        }

        void OnInteractionSelection(ReorderableList list)
        {
            if (list.index < 0)
            {
                m_SelectedInteractionName = null;
                return;
            }
            m_SelectedInteractionName = (string)list.list[list.index];
            m_InteractionNamesAndParams = InputControlLayout.ParseNameAndParameterList(m_InteractionsProperty.stringValue);
            m_SelectedInteractionParameterList = GetFieldsFromClass();
        }
        
        InputControlLayout.ParameterValue[] GetFieldsFromClass()
        {
            var resultParameters = new List<InputControlLayout.ParameterValue>();
            var serializedParameters = new List<InputControlLayout.ParameterValue>();

            int idx = Array.FindIndex(m_InteractionNamesAndParams, a => a.name == m_SelectedInteractionName);
            if (idx >= 0)
            {
                serializedParameters.AddRange(m_InteractionNamesAndParams[idx].parameters);
            } 
            
            var interactionType =  m_InteractionChoices.LookupTypeRegistration(m_SelectedInteractionName);
            var fields = interactionType.GetFields(BindingFlags.Public | BindingFlags.Instance);
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
                    else if(field.FieldType == typeof(int))
                        paramValue.type = InputControlLayout.ParameterType.Integer;
                    else if(field.FieldType == typeof(float))
                        paramValue.type = InputControlLayout.ParameterType.Float;

                    resultParameters.Add(paramValue);
                }
            }
            return resultParameters.ToArray();
        }

        public void OnGUI()
        {
            var listRect = GUILayoutUtility.GetRect(200, m_InteractionsListView.GetHeight());
            listRect = EditorGUI.IndentedRect(listRect);
            m_InteractionsListView.DoList(listRect);
            
            if (m_InteractionsListView.index >= 0)
            {
                for (int i = 0; i < m_SelectedInteractionParameterList.Length; i++)
                {
                    var parameterValue = m_SelectedInteractionParameterList[i];
                    EditorGUI.BeginChangeCheck();

                    string result = null;
                    if (parameterValue.type == InputControlLayout.ParameterType.Integer)
                    {
                        var intValue = int.Parse(parameterValue.GetValueAsString());
                        result = EditorGUILayout.IntField(parameterValue.name, intValue).ToString();
                    }
                    else if (parameterValue.type == InputControlLayout.ParameterType.Float)
                    {
                        var floatValue = float.Parse(parameterValue.GetValueAsString());
                        result = EditorGUILayout.FloatField(parameterValue.name, floatValue).ToString();
                        
                    }
                    else if (parameterValue.type == InputControlLayout.ParameterType.Boolean)
                    {
                        var boolValue = bool.Parse(parameterValue.GetValueAsString());
                        result = EditorGUILayout.Toggle(parameterValue.name, boolValue).ToString();
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SelectedInteractionParameterList[i].SetValue(result);
                        m_Apply();
                    }
                }
            }
        }

        public string ToSerializableString()
        {
            var resultList = new List<string>();
            foreach (string listElement in m_InteractionsListView.list)
            {
                var idx = Array.FindIndex(m_InteractionNamesAndParams, a => a.name == listElement);
                if (idx >= 0)
                {
                    var param = m_InteractionNamesAndParams[idx];
                    var fieldWithValuesList = param.parameters.Select(a => string.Format("{0}={1}", a.name, a.GetValueAsString()));
                    if (m_SelectedInteractionName == m_InteractionNamesAndParams[idx].name)
                    {
                        fieldWithValuesList = m_SelectedInteractionParameterList.Select(a => string.Format("{0}={1}", a.name, a.GetValueAsString()));
                    }
                    var fieldWithValues = string.Join(",", fieldWithValuesList.ToArray());
                    resultList.Add(string.Format("{0}({1})", param.name, fieldWithValues));
                }
                else
                {
                    resultList.Add(listElement + "()");
                }

            }
            return string.Join(",", resultList.ToArray());
        }
    }
}
