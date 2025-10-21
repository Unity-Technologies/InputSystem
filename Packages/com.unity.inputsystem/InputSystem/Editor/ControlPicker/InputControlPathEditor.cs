#if UNITY_EDITOR || PACKAGE_DOCS_GENERATION
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
            m_PathLabel = label ?? new GUIContent(pathProperty.displayName, pathProperty.GetTooltip());
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

        public void OnGUI(Rect rect, GUIContent label = null, SerializedProperty property = null, Action modifiedCallback = null)
        {
            var pathLabel = label ?? m_PathLabel;
            var serializedProperty = property ?? pathProperty;

            var lineRect = rect;
            var labelRect = lineRect;
            labelRect.width = EditorStyles.label.CalcSize(pathLabel).x + 20; // Fit to label with some padding
            EditorGUI.LabelField(labelRect, pathLabel);
            lineRect.x += labelRect.width;
            lineRect.width -= labelRect.width;

            var bindingTextRect = lineRect;
            var editButtonRect = lineRect;

            bindingTextRect.x = labelRect.x + labelRect.width; // Place directly after labelRect
            editButtonRect.x += lineRect.width - 20; // Place at the edge of the window to appear after bindingTextRect
            bindingTextRect.width = editButtonRect.x - bindingTextRect.x; // bindingTextRect fills remaining space between label and editButton
            editButtonRect.width = 20;
            editButtonRect.height = 15;

            var path = String.Empty;
            try
            {
                path = serializedProperty.stringValue;
            }
            catch
            {
                // This try-catch block is a temporary fix for ISX-1436
                // The plan is to convert InputControlPathEditor entirely to UITK and therefore this fix will
                // no longer be required.
                return;
            }

            ////TODO: this should be cached; generates needless GC churn
            var displayName = InputControlPath.ToHumanReadableString(path);

            // Either show dropdown control that opens path picker or show path directly as
            // text, if manual path editing is toggled on.
            if (m_PickerState.manualPathEditMode)
            {
                ////FIXME: for some reason the text field does not fill all the rect but rather adds large padding on the left
                bindingTextRect.x -= 15;
                bindingTextRect.width += 15;

                EditorGUI.BeginChangeCheck();
                path = EditorGUI.DelayedTextField(bindingTextRect, path);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedProperty.stringValue = path;
                    serializedProperty.serializedObject.ApplyModifiedProperties();
                    (modifiedCallback ?? onModified).Invoke();
                }
            }
            else
            {
                // Dropdown that shows binding text and allows opening control picker.
                if (EditorGUI.DropdownButton(bindingTextRect, new GUIContent(displayName), FocusType.Keyboard))
                {
                    SetExpectedControlLayoutFromAttribute(serializedProperty);
                    ////TODO: for bindings that are part of composites, use the layout information from the [InputControl] attribute on the field
                    ShowDropdown(bindingTextRect, serializedProperty, modifiedCallback ?? onModified);
                }
            }

            // Button to toggle between text edit mode.
            m_PickerState.manualPathEditMode = GUI.Toggle(editButtonRect, m_PickerState.manualPathEditMode, "T",
                EditorStyles.miniButton);
        }

        private void ShowDropdown(Rect rect, SerializedProperty serializedProperty, Action modifiedCallback)
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            InputActionsEditorSettingsProvider.SetIMGUIDropdownVisible(true, false);
#endif
            IsShowingDropdown = true;

            if (m_PickerDropdown == null)
            {
                m_PickerDropdown = new InputControlPickerDropdown(
                    m_PickerState,
                    path =>
                    {
                        serializedProperty.stringValue = path;
                        m_PickerState.manualPathEditMode = false;
                        modifiedCallback();
                    });
            }

            m_PickerDropdown.SetPickedCallback(path =>
            {
                serializedProperty.stringValue = path;
                m_PickerState.manualPathEditMode = false;
                modifiedCallback();
            });

            m_PickerDropdown.SetControlPathsToMatch(m_ControlPathsToMatch);
            m_PickerDropdown.SetExpectedControlLayout(m_ExpectedControlLayout);

            m_PickerDropdown.Show(rect);

            IsShowingDropdown = false;
        }

        private void SetExpectedControlLayoutFromAttribute(SerializedProperty property)
        {
            var field = property.GetField();
            if (field == null)
                return;

            var attribute = field.GetCustomAttribute<InputControlAttribute>();
            if (attribute != null)
                SetExpectedControlLayout(attribute.layout);
        }

        public SerializedProperty pathProperty { get; }
        public Action onModified { get; }

        private GUIContent m_PathLabel;
        private string m_ExpectedControlLayout;
        private string[] m_ControlPathsToMatch;

        private InputControlPickerDropdown m_PickerDropdown;
        private readonly InputControlPickerState m_PickerState;

        /// <summary>
        /// This property is only set from this class in order to communicate that we're showing the dropdown at the moment
        /// It's employed to skip auto-saving, because that complicates updating the internal SerializedProperties.
        /// Unfortunately, we can't use IMGUIDropdownVisible from the setings provider because of the early-out logic in there.
        /// </summary>
        internal static bool IsShowingDropdown { get; private set; }
    }
}
 #endif // UNITY_EDITOR
