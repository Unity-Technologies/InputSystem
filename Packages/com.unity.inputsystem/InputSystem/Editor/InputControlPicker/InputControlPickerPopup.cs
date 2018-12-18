#if UNITY_EDITOR
using System;
using UnityEditor;

////TODO: refactor to not mandate a SerializedProperty; must be able to pick anything

////TODO: make interactive pick button optional

namespace UnityEngine.Experimental.Input.Editor
{
    public class InputControlPickerPopup
    {
        private string[] m_DeviceFilter;
        private string m_ExpectedControlLayoutFilter;

        private InputControlPickerDropdown m_InputControlPickerDropdown;
        private SerializedProperty m_PathProperty;
        private Action m_OnModified;
        private InputControlPickerState m_PickerState;
        private Action<Rect, SerializedProperty, Action> m_DrawInteractivePickButton;

        private static readonly GUIContent s_BindingGui = EditorGUIUtility.TrTextContent("Binding");

        public InputControlPickerPopup(SerializedProperty pathProperty, InputControlPickerState pickerState, Action onModified, Action<Rect, SerializedProperty, Action> drawInteractivePickButton)
        {
            if (pathProperty == null)
                throw new ArgumentNullException("pathProperty");
            m_PathProperty = pathProperty;

            m_DrawInteractivePickButton = drawInteractivePickButton;
            m_OnModified = onModified;
            m_PickerState = pickerState;
        }

        public void SetDeviceFilter(string[] deviceFilter)
        {
            m_DeviceFilter = deviceFilter;
        }

        public void SetExpectedControlLayoutFilter(string expectedControlLayout)
        {
            m_ExpectedControlLayoutFilter = expectedControlLayout;
        }

        public void DrawBindingGUI(ref bool manualPathEditMode)
        {
            EditorGUILayout.BeginHorizontal();

            var lineRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            var labelRect = lineRect;
            labelRect.width = 60;
            EditorGUI.LabelField(labelRect, s_BindingGui);
            lineRect.x += 65;
            lineRect.width -= 65;

            var bindingTextRect = lineRect;
            var editButtonRect = lineRect;
            var interactivePickButtonRect = lineRect;

            bindingTextRect.width -= 42;
            editButtonRect.x += bindingTextRect.width + 21;
            editButtonRect.width = 21;
            editButtonRect.height = 15;
            interactivePickButtonRect.x += bindingTextRect.width;
            interactivePickButtonRect.width = 21;
            interactivePickButtonRect.height = 15;

            var path = m_PathProperty.stringValue;
            ////TODO: this should be cached; generates needless GC churn
            var displayName = InputControlPath.ToHumanReadableString(path);

            if (manualPathEditMode || (!string.IsNullOrEmpty(path) && string.IsNullOrEmpty(displayName)))
            {
                EditorGUI.BeginChangeCheck();
                path = EditorGUI.DelayedTextField(bindingTextRect, path);
                if (EditorGUI.EndChangeCheck())
                {
                    m_PathProperty.stringValue = path;
                    m_PathProperty.serializedObject.ApplyModifiedProperties();
                    m_OnModified();
                }
                if (m_DrawInteractivePickButton != null)
                    m_DrawInteractivePickButton(interactivePickButtonRect, m_PathProperty, m_OnModified);
                if (GUI.Button(editButtonRect, "˅"))
                {
                    bindingTextRect.x += editButtonRect.width;
                    ShowInputControlPicker(bindingTextRect);
                }
            }
            else
            {
                // Dropdown that shows binding text and allows opening control picker.
                if (EditorGUI.DropdownButton(bindingTextRect, new GUIContent(displayName), FocusType.Keyboard))
                {
                    ////TODO: pass expectedControlLayout filter on to control picker
                    ////TODO: for bindings that are part of composites, use the layout information from the [InputControl] attribute on the field
                    ShowInputControlPicker(bindingTextRect);
                }

                // Button to bind interactively.
                if (m_DrawInteractivePickButton != null)
                    m_DrawInteractivePickButton(interactivePickButtonRect, m_PathProperty, m_OnModified);

                // Button that switches binding into text edit mode.
                if (GUI.Button(editButtonRect, "...", EditorStyles.miniButton))
                {
                    manualPathEditMode = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowInputControlPicker(Rect rect)
        {
            if (m_InputControlPickerDropdown == null)
            {
                m_InputControlPickerDropdown = new InputControlPickerDropdown(m_PickerState.state,
                    path =>
                    {
                        m_PathProperty.stringValue = path;
                        m_OnModified();
                    });
            }

            if (m_DeviceFilter != null)
            {
                m_InputControlPickerDropdown.SetDeviceFilter(m_DeviceFilter);
                if (!string.IsNullOrEmpty(m_ExpectedControlLayoutFilter))
                {
                    m_InputControlPickerDropdown.SetExpectedControlLayoutFilter(m_ExpectedControlLayoutFilter);
                }
            }

            m_InputControlPickerDropdown.Show(rect);
        }
    }
}
 #endif // UNITY_EDITOR
