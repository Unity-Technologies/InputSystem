#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    /// <summary>
    /// Custom editor UI for editing control paths.
    /// </summary>
    /// <remarks>
    /// This is the implementation underlying <see cref="InputControlPathDrawer"/>. It is useful primarily when
    /// greater control is required than is offered by the <see cref="PropertyDrawer"/> mechanism. In particular,
    /// it allows applying additional constraints such as requiring control paths to match ...
    /// </remarks>
    public class InputControlPathEditor
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="pathProperty"><see cref="string"/> type property that will receive the picked input control path.</param>
        /// <param name="pickerState">Persistent editing state of the </param>
        /// <param name="onModified"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InputControlPathEditor(SerializedProperty pathProperty, InputControlPickerState pickerState, Action onModified)
        {
            if (pathProperty == null)
                throw new ArgumentNullException(nameof(pathProperty));

            this.pathProperty = pathProperty;

            this.onModified = onModified;
            m_PickerState = pickerState ?? new InputControlPickerState();
        }

        public void SetControlPathsToMatch(IEnumerable<string> controlPaths)
        {
            m_ControlPathsToMatch = controlPaths.ToArray();
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
            EditorGUI.LabelField(labelRect, s_PathLabel);
            lineRect.x += labelRect.width;
            lineRect.width -= labelRect.width;

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

            var path = pathProperty.stringValue;
            ////TODO: this should be cached; generates needless GC churn
            var displayName = InputControlPath.ToHumanReadableString(path);

            // Either show dropdown control that opens path picker or show path directly as
            // text, if manual path editing is toggled on.
            if (m_PickerState.manualPathEditMode)
            {
                ////FIXME: for some reason the text field does not fill all the rect but rather adds large padding on the left
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

            // Button to bind interactively.
            DrawInteractivePickButton(interactivePickButtonRect);

            // Button to toggle between text edit mode.
            m_PickerState.manualPathEditMode = GUI.Toggle(editButtonRect, m_PickerState.manualPathEditMode, "T",
                EditorStyles.miniButton);

            if (m_RebindingOperation != null && m_RebindingOperation.started)
                DrawInteractivePickingProgressBar();
        }

        private void DrawInteractivePickButton(Rect rect)
        {
            if (s_PickButtonIcon == null)
                s_PickButtonIcon = EditorInputControlLayoutCache.GetIconForLayout("Button");

            var toggleRebind = GUI.Toggle(rect,
                m_RebindingOperation != null && m_RebindingOperation.started, s_PickButtonIcon, EditorStyles.miniButton);
            if (toggleRebind && (m_RebindingOperation == null || !m_RebindingOperation.started))
            {
                // Start rebind.

                if (m_RebindingOperation == null)
                    m_RebindingOperation = new InputActionRebindingExtensions.RebindingOperation();

                ////TODO: if we have multiple candidates that we can't trivially decide between, let user choose

                m_RebindingOperation
                    .WithExpectedControlLayout(m_ExpectedControlLayout)
                    // Require minimum actuation of 0.15f. This is after deadzoning has been applied.
                    .WithMagnitudeHavingToBeGreaterThan(0.15f)
                    // After 4 seconds, cancel the operation.
                    .WithTimeout(4)
                    ////REVIEW: the delay makes it more robust but doesn't feel good
                    // Give us a buffer of 0.25 seconds to see if a better match comes along.
                    .OnMatchWaitForAnother(0.25f)
                    ////REVIEW: should we exclude only the system's active pointing device?
                    // With the mouse operating the UI, its cursor control is too fickle a thing to
                    // bind to. Ignore mouse position and delta.
                    // NOTE: We go for all types of pointers here, not just mice.
                    .WithControlsExcluding("<Pointer>/position")
                    .WithControlsExcluding("<Pointer>/delta")
                    .OnCancel(
                        operation =>
                        {
                            ////REVIEW: Is there a better way to do this? All we want is for the *current* UI to repaint but Unity's
                            ////        editor API seems to have no way to retrieve the current EditorWindow from inside an OnGUI callback.
                            ////        So we'd have to pass the EditorWindow in here all the way from the EditorWindow.OnGUI() callback
                            ////        itself.
                            InternalEditorUtility.RepaintAllViews();

                            if (m_NeedToClearProgressBar)
                                EditorUtility.ClearProgressBar();
                        })
                    .OnComplete(
                        operation =>
                        {
                            if (m_NeedToClearProgressBar)
                                EditorUtility.ClearProgressBar();
                        })
                    .OnApplyBinding(
                        (operation, newPath) =>
                        {
                            pathProperty.stringValue = newPath;
                            pathProperty.serializedObject.ApplyModifiedProperties();
                            onModified();
                        });

                // If we have control paths to match, pass them on.
                m_RebindingOperation.WithoutControlsHavingToMatchPath();
                if (m_ControlPathsToMatch.LengthSafe() > 0)
                    m_ControlPathsToMatch.Select(x => m_RebindingOperation.WithControlsHavingToMatchPath(x));

                m_RebindingOperation.Start();
            }
            else if (!toggleRebind && m_RebindingOperation != null && m_RebindingOperation.started)
            {
                m_RebindingOperation.Cancel();
            }
        }

        private void ShowDropdown(Rect rect)
        {
            if (m_PickerDropdown == null)
            {
                m_PickerDropdown = new InputControlPickerDropdown(
                    m_PickerState.advancedDropdownState,
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

        private void DrawInteractivePickingProgressBar()
        {
            var title = !string.IsNullOrEmpty(m_ExpectedControlLayout)
                ? "Waiting for " + m_ExpectedControlLayout
                : "Waiting for input";

            var percentage = (InputRuntime.s_Instance.currentTime - m_RebindingOperation.startTime) /
                m_RebindingOperation.timeout;

            ////TODO: mention action/target here
            if (EditorUtility.DisplayCancelableProgressBar(title, "Actuate control to bind to.", (float)percentage))
                m_RebindingOperation.Cancel();
            m_NeedToClearProgressBar = true;

            // We need continuous refreshes to update the progress bar. Unfortunately, we don't
            // have a good way to make them selective so just refresh the entire editor UI while
            // we're waiting. The fact that there is no RepaintCurrentView() function is aggravating.
            InternalEditorUtility.RepaintAllViews();
        }

        public SerializedProperty pathProperty { get; }
        public Action onModified { get; }

        public bool isInteractivelyPicking => m_RebindingOperation != null && m_RebindingOperation.started;

        private string m_ExpectedControlLayout;
        private string[] m_ControlPathsToMatch;
        private InputControlScheme[] m_ControlSchemes;
        private bool m_NeedToClearProgressBar;

        private InputControlPickerDropdown m_PickerDropdown;
        private readonly InputControlPickerState m_PickerState;
        private InputActionRebindingExtensions.RebindingOperation m_RebindingOperation;

        private static readonly GUIContent s_PathLabel = EditorGUIUtility.TrTextContent("Path", "Path of the controls that will be bound to the action at runtime.");
        private static GUIStyle s_WaitingForInputLabel;
        private static Texture2D s_PickButtonIcon;
    }
}
 #endif // UNITY_EDITOR
