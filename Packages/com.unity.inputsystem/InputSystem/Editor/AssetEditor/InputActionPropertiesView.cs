#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        public static FourCC k_PropertiesChanged = new FourCC("PROP");

        public InputActionPropertiesView(SerializedProperty actionProperty, Action<FourCC> onChange = null)
            : base("Action", actionProperty, onChange, actionProperty.FindPropertyRelative("m_ExpectedControlLayout").stringValue)
        {
            m_ExpectedControlLayoutProperty = actionProperty.FindPropertyRelative("m_ExpectedControlLayout");
            m_FlagsProperty = actionProperty.FindPropertyRelative("m_Flags");
            BuildControlTypeList();

            m_SelectedControlType = Array.IndexOf(m_ControlTypeList, m_ExpectedControlLayoutProperty.stringValue);
            if (m_SelectedControlType == -1)
                m_SelectedControlType = 0;

            if (s_TypeLabel == null)
                s_TypeLabel = EditorGUIUtility.TrTextContent("Type", m_ExpectedControlLayoutProperty.tooltip);
        }

        protected override void DrawGeneralProperties()
        {
            EditorGUI.BeginChangeCheck();
            m_SelectedControlType = EditorGUILayout.Popup(s_TypeLabel, m_SelectedControlType, m_ControlTypeOptions);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_SelectedControlType == 0)
                    m_ExpectedControlLayoutProperty.stringValue = string.Empty;
                else
                    m_ExpectedControlLayoutProperty.stringValue = m_ControlTypeList[m_SelectedControlType];
                onChange(k_PropertiesChanged);
            }

            var flags = (InputAction.ActionFlags)m_FlagsProperty.intValue;
            var initialStateCheckOld = (flags & InputAction.ActionFlags.InitialStateCheck) != 0;
            var isContinuousOld = (flags & InputAction.ActionFlags.Continuous) != 0;
            var isPassThroughOld = (flags & InputAction.ActionFlags.PassThrough) != 0;

            var initialStateCheckNew = EditorGUILayout.Toggle(s_InitialStateCheck, initialStateCheckOld);
            var isContinuousNew = EditorGUILayout.Toggle(s_ContinuousLabel, isContinuousOld);
            var isPassThroughNew = EditorGUILayout.Toggle(s_PassThroughLabel, isPassThroughOld);

            if (isContinuousOld != isContinuousNew || isPassThroughOld != isPassThroughNew || initialStateCheckOld != initialStateCheckNew)
            {
                flags = InputAction.ActionFlags.None;

                if (isContinuousNew)
                    flags |= InputAction.ActionFlags.Continuous;
                if (isPassThroughNew)
                    flags |= InputAction.ActionFlags.PassThrough;
                if (initialStateCheckNew)
                    flags |= InputAction.ActionFlags.InitialStateCheck;

                m_FlagsProperty.intValue = (int)flags;
                m_FlagsProperty.serializedObject.ApplyModifiedProperties();

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

                ////TODO: skip aliases

                if (typeof(InputControl).IsAssignableFrom(allLayouts.layoutTypes[layoutName]) &&
                    !typeof(InputDevice).IsAssignableFrom(allLayouts.layoutTypes[layoutName]))
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

        private readonly SerializedProperty m_ExpectedControlLayoutProperty;
        private readonly SerializedProperty m_FlagsProperty;

        private string m_ExpectedControlLayout;
        private string[] m_ControlTypeList;
        private GUIContent[] m_ControlTypeOptions;
        private int m_SelectedControlType;

        private static GUIContent s_TypeLabel;
        private static readonly GUIContent s_ContinuousLabel = EditorGUIUtility.TrTextContent("Continuous",
            "If enabled, the action will trigger every update while controls are actuated even if the controls do not change value in a given frame.");
        private static readonly GUIContent s_PassThroughLabel = EditorGUIUtility.TrTextContent("Pass Through",
            "If enabled, the action will not gate value changes on controls but will instead perform for every value change on any bound control. " +
            "This is especially useful when binding multiple controls concurrently and not wanting the action to single out any one of multiple " +
            "concurrent inputs.");
        private static readonly GUIContent s_InitialStateCheck = EditorGUIUtility.TrTextContent("Initial State Check",
            "If enabled, the action will perform an initial state check on all bound controls when the action is enabled. This means that " +
            "if, for example, a button is held when the action is enabled, the action will be triggered right away. By default, controls " +
            "that are already actuated when an action is enabled do not cause the action to be triggered.");
    }
}
#endif // UNITY_EDITOR
