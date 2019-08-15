#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// UI that edits the properties of an <see cref="InputAction"/>.
    /// </summary>
    /// <remarks>
    /// Right-most pane in <see cref="InputActionEditorWindow"/> when an action is selected.
    /// </remarks>
    internal class InputActionPropertiesView : PropertiesViewBase
    {
        public static FourCC k_PropertiesChanged => new FourCC("PROP");

        public InputActionPropertiesView(SerializedProperty actionProperty, Action<FourCC> onChange = null)
            : base("Action", actionProperty, onChange, actionProperty.FindPropertyRelative("m_ExpectedControlType").stringValue)
        {
            m_ExpectedControlTypeProperty = actionProperty.FindPropertyRelative("m_ExpectedControlType");
            m_ActionTypeProperty = actionProperty.FindPropertyRelative("m_Type");

            m_SelectedActionType = (InputActionType)m_ActionTypeProperty.intValue;

            BuildControlTypeList();
            m_SelectedControlType = Array.IndexOf(m_ControlTypeList, m_ExpectedControlTypeProperty.stringValue);
            if (m_SelectedControlType == -1)
                m_SelectedControlType = 0;

            if (s_ControlTypeLabel == null)
                s_ControlTypeLabel = EditorGUIUtility.TrTextContent("Control Type", m_ExpectedControlTypeProperty.tooltip);
            if (s_ActionTypeLabel == null)
                s_ActionTypeLabel = EditorGUIUtility.TrTextContent("Action Type", m_ActionTypeProperty.tooltip);
        }

        protected override void DrawGeneralProperties()
        {
            EditorGUI.BeginChangeCheck();

            m_SelectedActionType = EditorGUILayout.EnumPopup(s_ActionTypeLabel, m_SelectedActionType);
            if ((InputActionType)m_SelectedActionType != InputActionType.Button)
                m_SelectedControlType = EditorGUILayout.Popup(s_ControlTypeLabel, m_SelectedControlType, m_ControlTypeOptions);

            if (EditorGUI.EndChangeCheck())
            {
                if ((InputActionType)m_SelectedActionType == InputActionType.Button)
                    m_ExpectedControlTypeProperty.stringValue = "Button";
                else if (m_SelectedControlType == 0)
                    m_ExpectedControlTypeProperty.stringValue = string.Empty;
                else
                    m_ExpectedControlTypeProperty.stringValue = m_ControlTypeList[m_SelectedControlType];

                m_ActionTypeProperty.intValue = (int)(InputActionType)m_SelectedActionType;
                m_ActionTypeProperty.serializedObject.ApplyModifiedProperties();
                UpdateProcessors(m_ExpectedControlTypeProperty.stringValue);

                onChange(k_PropertiesChanged);
            }
        }

        private void BuildControlTypeList()
        {
            var types = new List<string>();
            var allLayouts = InputSystem.s_Manager.m_Layouts;
            foreach (var layoutName in allLayouts.layoutTypes.Keys)
            {
                if (EditorInputControlLayoutCache.TryGetLayout(layoutName).hideInUI)
                    continue;

                // If the action type is InputActionType.Value, skip button controls.
                var type = allLayouts.layoutTypes[layoutName];
                if ((InputActionType)m_SelectedActionType == InputActionType.Value &&
                    typeof(ButtonControl).IsAssignableFrom(type))
                    continue;

                ////TODO: skip aliases

                if (typeof(InputControl).IsAssignableFrom(type) &&
                    !typeof(InputDevice).IsAssignableFrom(type))
                {
                    types.Add(layoutName);
                }
            }
            // Sort alphabetically.
            types.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
            // Make sure "Any" is always topmost entry.
            types.Insert(0, "Any");

            m_ControlTypeList = types.ToArray();
            m_ControlTypeOptions = m_ControlTypeList.Select(x => new GUIContent(ObjectNames.NicifyVariableName(x)))
                .ToArray();
        }

        private readonly SerializedProperty m_ExpectedControlTypeProperty;
        private readonly SerializedProperty m_ActionTypeProperty;

        private string m_ExpectedControlLayout;
        private string[] m_ControlTypeList;
        private GUIContent[] m_ControlTypeOptions;
        private int m_SelectedControlType;
        private Enum m_SelectedActionType;

        private static GUIContent s_ActionTypeLabel;
        private static GUIContent s_ControlTypeLabel;
    }
}
#endif // UNITY_EDITOR
