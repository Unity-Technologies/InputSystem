#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    [CustomEditor(typeof(RebindActionParameterUI))]
    public class RebindActionParameterUIEditor : UnityEditor.Editor
    {
        protected void OnEnable()
        {
            m_Binding = new BindingUI(serializedObject);
            m_ParameterNameProperty = serializedObject.FindProperty("m_ParameterName");

            Refresh();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            //EditorGUILayout.PropertyField(m_ActionProperty);

            // Binding section.
            m_Binding.Draw();

            // Parameter section.
            var newSelectedParameter = EditorGUILayout.Popup(m_ParameterLabel, m_SelectedParameterOption, m_Parameters);
            if (newSelectedParameter != m_SelectedParameterOption)
            {
                m_SelectedParameterOption = newSelectedParameter;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Refresh();
            }
        }

        private void Refresh()
        {
            if (!m_Binding.Refresh())
                return;

            RefreshItems();
        }

        private void RefreshItems()
        {
            var action = m_Binding.action;

            var parameters = new List<GUIContent>();
            var parameterValues = new List<ParameterValue>();

            // Add action processors
            var actionItems = NameAndParameters.ParseMultiple(action.processors);
            foreach (var item in actionItems)
            {
                // TODO FIX
                //parameters.Add(new GUIContent(item.name + " (Action)"));
                //parameterValues.Add(item);
            }

            // Add binding processors
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var bindingItems = NameAndParameters.ParseMultiple(bindings[i].processors);
                foreach (var item in bindingItems)
                {
                    //parameters.Add(new GUIContent(item.name + " (Binding)"));
                    //parameterValues.Add(item.name);

                    // Only add parameters from the active binding
                    if (m_Binding.bindingIndex != i)
                        continue;

                    var uniformParameterType = true;
                    var previousType = TypeCode.Empty;
                    var processorParameters = item.parameters;
                    foreach (var parameter in processorParameters)
                    {
                        if (uniformParameterType && parameter.type != previousType && previousType != TypeCode.Empty)
                            uniformParameterType = false;
                        previousType = parameter.type;

                        // And option for individual parameter
                        var processorParameterName = item.name + "." + parameter.name;
                        parameters.Add(new GUIContent(processorParameterName));
                        parameterValues.Add(new ParameterValue
                        {
                            bindingId = m_Binding.bindingId,
                            name = parameter.name,
                        });
                    }

                    // Add parameter option for all/uniform parameter modification if all parameters are of
                    // the same type.
                    if (!uniformParameterType)
                        continue;
                    parameters.Add(new GUIContent(item.name + " (Uniform)"));
                    parameterValues.Add(new ParameterValue
                    {
                        bindingId = m_Binding.bindingId,
                        name = null
                    });
                }
            }

            m_Parameters = parameters.ToArray();
            m_ParameterValues = parameterValues.ToArray();
        }

        private struct ParameterValue
        {
            public string bindingId;
            public string name;
        }

        private SerializedProperty m_ParameterNameProperty;
        private BindingUI m_Binding;
        private GUIContent[] m_Parameters;
        private ParameterValue[] m_ParameterValues;
        private int m_SelectedParameterOption;
        private readonly GUIContent m_ParameterLabel = new GUIContent("Processor Parameter");
    }
}

#endif
