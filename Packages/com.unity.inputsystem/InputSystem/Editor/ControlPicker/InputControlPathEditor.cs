#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom editor UI for editing control paths.
    /// </summary>
    /// <remarks>
    /// This is the implementation underlying <see cref="InputControlPathDrawer"/>. It is useful primarily when
    /// greater control is required than is offered by the <see cref="PropertyDrawer"/> mechanism. In particular,
    /// it allows applying additional constraints such as requiring control paths to match ...
    /// </remarks>
    public sealed class InputControlPathEditor : IDisposable
    {
        /// <summary>
        /// Initialize the control path editor.
        /// </summary>
        /// <param name="pathProperty"><see cref="string"/> type property that will receive the picked input control path.</param>
        /// <param name="pickerState">Persistent editing state of the path editor. Used to retain state across domain reloads.</param>
        /// <param name="onModified">Delegate that is called when the path has been modified.</param>
        /// <param name="label">Optional label to display instead of display name of <paramref name="pathProperty"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pathProperty"/> is <c>null</c>.</exception>
        public InputControlPathEditor(SerializedProperty pathProperty, InputControlPickerState pickerState, Action onModified, GUIContent label = null)
        {
            if (pathProperty == null)
                throw new ArgumentNullException(nameof(pathProperty));

            this.pathProperty = pathProperty;
            this.onModified = onModified;
            m_PickerState = pickerState ?? new InputControlPickerState();
            m_PathLabel = label ?? new GUIContent(pathProperty.displayName, pathProperty.tooltip);
        }

        public void Dispose()
        {
            m_PickerDropdown?.Dispose();
        }

        public void SetControlPathsToMatch(IEnumerable<string> controlPaths)
        {
            m_ControlPathsToMatch = controlPaths.ToArray();
            m_PickerDropdown?.SetControlPathsToMatch(m_ControlPathsToMatch);
        }

        /// <summary>
        /// Constrain the type of control layout that can be picked.
        /// </summary>
        /// <param name="expectedControlLayout">Name of the layout. This it the name as registered with
        /// <see cref="InputSystem.RegisterLayout"/>.</param>.
        /// <remarks>
        /// <example>
        /// <code>
        /// // Pick only button controls.
        /// editor.SetExpectedControlLayout("Button");
        /// </code>
        /// </example>
        /// </remarks>
        public void SetExpectedControlLayout(string expectedControlLayout)
        {
            m_ExpectedControlLayout = expectedControlLayout;
            m_PickerDropdown?.SetExpectedControlLayout(m_ExpectedControlLayout);
        }

        public void SetExpectedControlLayoutFromAttribute()
        {
            var field = pathProperty.GetField();
            if (field == null)
                return;

            var attribute = field.GetCustomAttribute<InputControlAttribute>();
            if (attribute != null)
                SetExpectedControlLayout(attribute.layout);
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            ////FIXME: for some reason, the left edge doesn't align properly in GetRect()'s result; indentation issue?
            var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            rect.x += EditorGUIUtility.standardVerticalSpacing + 2;
            rect.width -= EditorGUIUtility.standardVerticalSpacing * 2 + 4;
            OnGUI(rect);
            EditorGUILayout.EndHorizontal();
        }

        public void OnGUI(Rect rect)
        {
            var lineRect = rect;
            var labelRect = lineRect;
            labelRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(labelRect, m_PathLabel);
            lineRect.x += labelRect.width;
            lineRect.width -= labelRect.width;

            var bindingTextRect = lineRect;
            var editButtonRect = lineRect;

            bindingTextRect.width -= 20;
            editButtonRect.x += bindingTextRect.width;
            editButtonRect.width = 20;
            editButtonRect.height = 15;

            var path = pathProperty.stringValue;
            ////TODO: this should be cached; generates needless GC churn
            var displayName = InputControlPath.ToHumanReadableString(path);

            // Either show dropdown control that opens path picker or show path directly as
            // text, if manual path editing is toggled on.
            if (m_PickerState.manualPathEditMode)
            {
                ////FIXME: for some reason the text field does not fill all the rect but rather adds large padding on the left
                bindingTextRect.x -= 15;
                bindingTextRect.width += 15;

                bindingTextRect.height -= 2;
                bindingTextRect.y += 1;
                EditorGUI.BeginChangeCheck();
                path = EditorGUI.DelayedTextField(bindingTextRect, path);
                if (EditorGUI.EndChangeCheck())
                {
                    pathProperty.stringValue = path;
                    pathProperty.serializedObject.ApplyModifiedProperties();
                    onModified();
                }
            }
            else
            {
                // Dropdown that shows binding text and allows opening control picker.
                if (EditorGUI.DropdownButton(bindingTextRect, new GUIContent(displayName), FocusType.Keyboard))
                {
                    ////TODO: for bindings that are part of composites, use the layout information from the [InputControl] attribute on the field
                    ShowDropdown(bindingTextRect);
                }
            }

            // Button to toggle between text edit mode.
            m_PickerState.manualPathEditMode = GUI.Toggle(editButtonRect, m_PickerState.manualPathEditMode, "T",
                EditorStyles.miniButton);
        }

        private void ShowDropdown(Rect rect)
        {
            if (m_PickerDropdown == null)
            {
                m_PickerDropdown = new InputControlPickerDropdown(
                    m_PickerState,
                    path =>
                    {
                        pathProperty.stringValue = path;
                        m_PickerState.manualPathEditMode = false;
                        onModified();
                    });
            }

            m_PickerDropdown.SetControlPathsToMatch(m_ControlPathsToMatch);
            m_PickerDropdown.SetExpectedControlLayout(m_ExpectedControlLayout);

            m_PickerDropdown.Show(rect);
        }

        public SerializedProperty pathProperty { get; }
        public Action onModified { get; }

        private GUIContent m_PathLabel;
        private string m_ExpectedControlLayout;
        private string[] m_ControlPathsToMatch;
        private InputControlScheme[] m_ControlSchemes;
        private bool m_NeedToClearProgressBar;

        private InputControlPickerDropdown m_PickerDropdown;
        private readonly InputControlPickerState m_PickerState;
        private InputActionRebindingExtensions.RebindingOperation m_RebindingOperation;
    }
}
 #endif // UNITY_EDITOR
