#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem
{
    internal class InputSettingsiOSProvider
    {
        [NonSerialized] private SerializedProperty m_MotionUsageEnabled;
        [NonSerialized] private SerializedProperty m_MotionUsageDescription;
        [NonSerialized] private VisualElement m_Container;
        [NonSerialized] private Toggle m_MotionUsageToggle;
        [NonSerialized] private TextField m_MotionUsageDescriptionField;

        private GUIContent m_MotionUsageContent;
        private GUIContent m_MotionUsageDescriptionContent;

        public InputSettingsiOSProvider(SerializedObject parent)
        {
            Update(parent);

            m_MotionUsageContent = new GUIContent("Motion Usage", "Enables Motion Usage for the app, required for sensors like Step Counter. This also adds 'Privacy - Motion Usage Description' entry to Info.plist");
            m_MotionUsageDescriptionContent = new GUIContent("  Description", "Describe why the app wants to access the device's Motion Usage sensor.");
        }

        public void Update(SerializedObject parent)
        {
            var prefix = "m_iOSSettings.m_MotionUsage";
            m_MotionUsageEnabled = parent.FindProperty(prefix + ".m_Enabled");
            m_MotionUsageDescription = parent.FindProperty(prefix + ".m_Description");
        }

        public void OnGUI()
        {
            EditorGUILayout.PropertyField(m_MotionUsageEnabled, m_MotionUsageContent);
            EditorGUI.BeginDisabledGroup(!m_MotionUsageEnabled.boolValue);
            EditorGUILayout.PropertyField(m_MotionUsageDescription, m_MotionUsageDescriptionContent);
            EditorGUI.EndDisabledGroup();
        }

        public void CreateGUI(VisualElement parent, Action onValueChanged)
        {
            if (parent == null)
                return;

            m_Container = new VisualElement();
            var titleLabel = new Label("iOS");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginTop = 12;
            m_Container.Add(titleLabel);

            m_MotionUsageToggle = new Toggle(m_MotionUsageContent.text)
            {
                tooltip = m_MotionUsageContent.tooltip
            };
            m_MotionUsageToggle.RegisterValueChangedCallback(evt =>
            {
                if (m_MotionUsageEnabled == null || m_MotionUsageEnabled.boolValue == evt.newValue)
                    return;

                m_MotionUsageEnabled.boolValue = evt.newValue;
                onValueChanged?.Invoke();
            });
            m_Container.Add(m_MotionUsageToggle);

            m_MotionUsageDescriptionField = new TextField("Description")
            {
                tooltip = m_MotionUsageDescriptionContent.tooltip
            };
            m_MotionUsageDescriptionField.RegisterValueChangedCallback(evt =>
            {
                if (m_MotionUsageDescription == null || m_MotionUsageDescription.stringValue == evt.newValue)
                    return;

                m_MotionUsageDescription.stringValue = evt.newValue;
                onValueChanged?.Invoke();
            });
            m_Container.Add(m_MotionUsageDescriptionField);

            parent.Add(m_Container);
        }

        public void RefreshUIToolkitState(bool canEditSettings)
        {
            if (m_Container == null)
                return;

            if (m_MotionUsageToggle != null)
            {
                m_MotionUsageToggle.SetEnabled(canEditSettings && m_MotionUsageEnabled != null);
                m_MotionUsageToggle.SetValueWithoutNotify(m_MotionUsageEnabled?.boolValue ?? false);
            }

            if (m_MotionUsageDescriptionField != null)
            {
                m_MotionUsageDescriptionField.SetEnabled(canEditSettings && m_MotionUsageEnabled != null && m_MotionUsageEnabled.boolValue);
                m_MotionUsageDescriptionField.SetValueWithoutNotify(m_MotionUsageDescription?.stringValue ?? string.Empty);
            }
        }
    }
}

#endif
