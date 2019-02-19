#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    /// <summary>
    /// Right-most pane in action editor when action is selected. Shows properties of action.
    /// </summary>
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

            if (s_TypeLabel == null)
                s_TypeLabel = EditorGUIUtility.TrTextContent("Type", m_ExpectedControlLayoutProperty.tooltip);
        }

        protected override void DrawGeneralProperties()
        {
            EditorGUI.BeginChangeCheck();
            m_SelectedControlType = EditorGUILayout.Popup(s_TypeLabel, m_SelectedControlType, m_ControlTypeList);
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
            var isPassThroughOld = (flags & InputAction.ActionFlags.PassThrough) != 0;

            var isContinuousNew = EditorGUILayout.Toggle(s_ContinuousLabel, isContinuousOld);
            var isPassThroughNew = EditorGUILayout.Toggle(s_PassThroughLabel, isPassThroughOld);

            if (isContinuousOld != isContinuousNew || isPassThroughOld != isPassThroughNew)
            {
                flags = InputAction.ActionFlags.None;

                if (isContinuousNew)
                    flags |= InputAction.ActionFlags.Continuous;
                if (isPassThroughNew)
                    flags |= InputAction.ActionFlags.PassThrough;

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

        private static GUIContent s_TypeLabel;
        private static readonly GUIContent s_ContinuousLabel = EditorGUIUtility.TrTextContent("Continuous",
            "If enabled, the action will trigger every update while controls are actuated even if the controls do not change value.");
        private static readonly GUIContent s_PassThroughLabel = EditorGUIUtility.TrTextContent("Pass Through",
            "If enabled, the action will not gate value changes on controls but will instead perform for every value change on any bound control. " +
            "This is especially useful when binding multiple controls concurrently and not wanting the action to single out any one of multiple " +
            "concurrent inputs.");
    }
}
#endif // UNITY_EDITOR
