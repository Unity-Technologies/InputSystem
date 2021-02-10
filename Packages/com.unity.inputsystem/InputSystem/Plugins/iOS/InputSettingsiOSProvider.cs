#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.InputSystem
{
    internal class InputSettingsiOSProvider
    {
        [NonSerialized] private SerializedProperty m_MotionUsageEnabled;
        [NonSerialized] private SerializedProperty m_MotionUsageDescription;

        private GUIContent m_MotionUsageContent;
        private GUIContent m_MotionUsageDescriptionContent;

        public InputSettingsiOSProvider(SerializedObject parent)
        {
            var prefix = "m_iOSSettings.m_MotionUsage";
            m_MotionUsageEnabled = parent.FindProperty(prefix + ".m_Enabled");
            m_MotionUsageDescription = parent.FindProperty(prefix + ".m_Description");

            m_MotionUsageContent = new GUIContent("Motion Usage", "Enables Motion Usage for the app, required for sensors like Step Counter. This also adds 'Privacy - Motion Usage Description' entry to Info.plist");
            m_MotionUsageDescriptionContent = new GUIContent("  Description", "Describe why the app wants to access the device's Motion Usage sensor.");
        }

        public void OnGUI()
        {
            EditorGUILayout.PropertyField(m_MotionUsageEnabled, m_MotionUsageContent);
            EditorGUI.BeginDisabledGroup(!m_MotionUsageEnabled.boolValue);
            EditorGUILayout.PropertyField(m_MotionUsageDescription, m_MotionUsageDescriptionContent);
            EditorGUI.EndDisabledGroup();
        }
    }
}

#endif
