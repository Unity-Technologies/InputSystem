#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Allows persisting a parameter override associated with a binding.
    /// </summary>
    [CustomEditor(typeof(RebindActionParameterUI))]
    public class RebindActionParameterUIEditor : UnityEditor.Editor
    {
        protected void OnEnable()
        {
            m_Binding = new BindingUI(serializedObject);
            m_DefaultValueProperty = serializedObject.FindProperty("m_DefaultValue");
            m_PreferenceKeyProperty = serializedObject.FindProperty("m_PreferenceKey");
            m_SliderProperty = serializedObject.FindProperty("m_Slider");

            Refresh();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            // Binding section.
            m_Binding.Draw();

            // UI section
            EditorGUILayout.LabelField("UI");
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.ObjectField(m_SliderProperty);
            }

            // Parameter section.
            EditorGUILayout.LabelField("Parameter");
            using (new EditorGUI.IndentLevelScope())
            {
                var key = EditorGUILayout.TextField("Preference Key", m_PreferenceKeyProperty.stringValue);
                if (key != m_PreferenceKeyProperty.stringValue)
                    m_PreferenceKeyProperty.stringValue = key;

                var defaultValue = EditorGUILayout.FloatField("Default Value", m_DefaultValueProperty.floatValue);
                if (!Mathf.Approximately(defaultValue, m_DefaultValueProperty.floatValue))
                    m_DefaultValueProperty.floatValue = defaultValue;

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    Refresh();
                }
            }
        }

        private void Refresh()
        {
            m_Binding.Refresh();
        }

        private struct ParameterValue
        {
            public string bindingId;
            public string name;
        }

        private SerializedProperty m_PreferenceKeyProperty;
        private SerializedProperty m_DefaultValueProperty;
        private SerializedProperty m_SliderProperty;

        private BindingUI m_Binding;
    }
}

#endif
