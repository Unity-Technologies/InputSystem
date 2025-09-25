#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
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
            m_ParameterOverridesProperty = serializedObject.FindProperty("m_ParameterOverrides");

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
                EditorGUILayout.PropertyField(m_PreferenceKeyProperty);
                EditorGUILayout.PropertyField(m_DefaultValueProperty);
                EditorGUILayout.PropertyField(m_ParameterOverridesProperty, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Refresh();
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
        private SerializedProperty m_ParameterOverridesProperty;

        private BindingUI m_Binding;
    }
}

#endif
