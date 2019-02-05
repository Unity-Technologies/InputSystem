#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputActionPropertiesView : PropertiesView
    {
        public static FourCC k_PropertiesChanged = new FourCC("PROP");

        public InputActionPropertiesView(SerializedProperty actionProperty, Action<FourCC> onChange)
            : base("Action", actionProperty, onChange)
        {
            m_ExpectedControlLayoutProperty = actionProperty.FindPropertyRelative("m_ExpectedControlLayout");
            m_FlagsProperty = actionProperty.FindPropertyRelative("m_Flags");
            m_ControlTypeList = BuildControlTypeList();

            m_SelectedControlType = Array.IndexOf(m_ControlTypeList, m_ExpectedControlLayoutProperty.stringValue);
            if (m_SelectedControlType == -1)
                m_SelectedControlType = 0;

            m_ActionTypeLabel =
                EditorGUIUtility.TrTextContent("Type", m_ExpectedControlLayoutProperty.tooltip);
        }

        protected override void DrawGeneralProperties()
        {
            EditorGUI.BeginChangeCheck();
            m_SelectedControlType = EditorGUILayout.Popup(m_ActionTypeLabel, m_SelectedControlType, m_ControlTypeList);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_SelectedControlType == 0)
                    m_ExpectedControlLayoutProperty.stringValue = string.Empty;
                else
                    m_ExpectedControlLayoutProperty.stringValue = m_ControlTypeList[m_SelectedControlType];
                onChange(k_PropertiesChanged);
            }

            var flags = (InputAction.ActionFlags)m_FlagsProperty.intValue;
            var isContinuousOld = (flags & InputAction.ActionFlags.Continuous) != 0;
            var isContinuousNew = EditorGUILayout.Toggle(s_ContinuousLabel, isContinuousOld);

            if (isContinuousOld != isContinuousNew)
            {
                flags = InputAction.ActionFlags.None;
                if (isContinuousNew)
                    flags |= InputAction.ActionFlags.Continuous;

                m_FlagsProperty.intValue = (int)flags;
                m_FlagsProperty.serializedObject.ApplyModifiedProperties();

                onChange(k_PropertiesChanged);
            }
        }

        private static string[] BuildControlTypeList()
        {
            var types = new List<string>();
            foreach (var layoutName in InputSystem.s_Manager.m_Layouts.layoutTypes.Keys)
            {
                if (typeof(InputControl).IsAssignableFrom(InputSystem.s_Manager.m_Layouts.layoutTypes[layoutName]) &&
                    !typeof(InputDevice).IsAssignableFrom(InputSystem.s_Manager.m_Layouts.layoutTypes[layoutName]))
                {
                    types.Add(layoutName);
                }
            }
            // Sort alphabetically.
            types.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
            // Make sure "Any" is always topmost entry.
            types.Insert(0, "Any");
            return types.ToArray();
        }

        private readonly SerializedProperty m_ExpectedControlLayoutProperty;
        private readonly SerializedProperty m_FlagsProperty;

        private string m_ExpectedControlLayout;
        private readonly string[] m_ControlTypeList;
        private int m_SelectedControlType;
        private readonly GUIContent m_ActionTypeLabel;

        private static readonly GUIContent s_ContinuousLabel = EditorGUIUtility.TrTextContent("Continuous");
    }
}
#endif // UNITY_EDITOR
