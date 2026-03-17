#if UNITY_EDITOR || PACKAGE_DOCS_GENERATION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
#if UNITY_EDITOR
using UnityEngine.UIElements;
#endif

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

#if UNITY_EDITOR
        /// <summary>
        /// Builds a UI Toolkit row for editing the control path (picker, optional text mode, manual path toggle).
        /// </summary>
        /// <param name="property">Optional property to bind; defaults to the constructor <see cref="pathProperty"/>.</param>
        /// <param name="modifiedCallback">Optional callback when the path changes; defaults to <see cref="onModified"/>.</param>
        public VisualElement CreateVisualElement(SerializedProperty property = null, Action modifiedCallback = null)
        {
            var prop = property ?? pathProperty;
            var modified = modifiedCallback ?? onModified;

            var row = new VisualElement { name = "input-control-path-editor" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = EditorGUIUtility.singleLineHeight;
            row.style.marginTop = 1;
            row.style.marginBottom = 1;

            var pathLabel = new Label(m_PathLabel.text)
            {
                tooltip = m_PathLabel.tooltip
            };
            pathLabel.AddToClassList("unity-base-field__label");
            pathLabel.style.flexShrink = 0;
            pathLabel.style.marginRight = 4;
            pathLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            var pathArea = new VisualElement
            {
                name = "path-area"
            };
            pathArea.style.flexGrow = 1;
            pathArea.style.flexShrink = 1;
            pathArea.style.minWidth = 40;
            pathArea.style.flexDirection = FlexDirection.Row;

            var manualToggle = new Toggle
            {
                tooltip = "Toggle direct text editing of the control path."
            };
            manualToggle.text = "T";
            manualToggle.style.flexShrink = 0;
            manualToggle.style.marginLeft = 2;
            manualToggle.SetValueWithoutNotify(m_PickerState.manualPathEditMode);

            Button pathButton = null;
            TextField pathTextField = null;

            void RefreshCachedDisplay(string path)
            {
                if (!string.Equals(path, m_CachedPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    m_CachedPath = path;
                    m_CachedDisplayName = InputControlPath.ToHumanReadableString(path);
                }
            }

            void RebuildPathArea()
            {
                pathArea.Clear();
                string path;
                try
                {
                    path = prop.stringValue ?? string.Empty;
                }
                catch
                {
                    return;
                }

                RefreshCachedDisplay(path);

                if (m_PickerState.manualPathEditMode)
                {
                    pathTextField = new TextField { isDelayed = true };
                    pathTextField.style.flexGrow = 1;
                    pathTextField.value = path;

                    void CommitTextPath()
                    {
                        try
                        {
                            var newPath = pathTextField.value ?? string.Empty;
                            if (newPath == prop.stringValue)
                                return;
                            prop.stringValue = newPath;
                            prop.serializedObject.ApplyModifiedProperties();
                            modified();
                            RefreshCachedDisplay(newPath);
                        }
                        catch
                        {
                            // SerializedProperty may be invalid (e.g. stale after reload).
                        }
                    }

                    pathTextField.RegisterCallback<FocusOutEvent>(_ => CommitTextPath());
                    pathTextField.RegisterCallback<KeyDownEvent>(evt =>
                    {
                        if (evt.keyCode == KeyCode.Return)
                            CommitTextPath();
                    });

                    pathArea.Add(pathTextField);
                }
                else
                {
                    pathButton = new Button(() =>
                    {
                        try
                        {
                            _ = prop.stringValue;
                        }
                        catch
                        {
                            return;
                        }

                        SetExpectedControlLayoutFromAttribute(prop);
                        ShowDropdown(pathButton, prop, modified, RebuildPathArea);
                    })
                    {
                        text = string.IsNullOrEmpty(m_CachedDisplayName) ? "(none)" : m_CachedDisplayName
                    };
                    pathButton.style.flexGrow = 1;
                    pathButton.style.unityTextAlign = TextAnchor.MiddleLeft;
                    pathArea.Add(pathButton);
                }
            }

            manualToggle.RegisterValueChangedCallback(evt =>
            {
                m_PickerState.manualPathEditMode = evt.newValue;
                RebuildPathArea();
            });

            RebuildPathArea();

            row.Add(pathLabel);
            row.Add(pathArea);
            row.Add(manualToggle);

            return row;
        }

        /// <summary>
        /// Screen-space anchor rect for <see cref="AdvancedDropdownWindow.ShowAsDropDown"/>.
        /// Editor panels cannot use <see cref="RuntimePanelUtils.ScreenToPanel"/> (runtime-only; throws InvalidCastException).
        /// </summary>
        internal static Rect GetScreenSpaceButtonRect(VisualElement element)
        {
            if (element?.panel == null)
                return Rect.zero;

            var bounds = element.worldBound;
            var panel = element.panel;

            // Editor UITK (Inspector, Input Actions window, etc.)
            if (panel.contextType == ContextType.Editor)
                return GetEditorPanelPickerAnchorRect();

            float sx1 = 100f;
            float sy1 = 100f;
            float sx2 = Mathf.Clamp(Screen.width * 0.5f, sx1 + 200f, Screen.width - 50f);
            float sy2 = Mathf.Clamp(Screen.height * 0.5f, sy1 + 200f, Screen.height - 50f);

            var s1 = new Vector2(sx1, sy1);
            var s2 = new Vector2(sx2, sy1);
            var s3 = new Vector2(sx1, sy2);

            var p1 = RuntimePanelUtils.ScreenToPanel(panel, s1);
            var p2 = RuntimePanelUtils.ScreenToPanel(panel, s2);
            var p3 = RuntimePanelUtils.ScreenToPanel(panel, s3);

            var dx = s2.x - s1.x;
            var dy = s3.y - s1.y;
            if (dx < 1f)
                dx = 1f;
            if (dy < 1f)
                dy = 1f;

            var dPdsx = (p2 - p1) / dx;
            var dPdsy = (p3 - p1) / dy;
            var det = dPdsx.x * dPdsy.y - dPdsx.y * dPdsy.x;

            Vector2 PanelPointToScreen(Vector2 panelPoint)
            {
                if (Mathf.Abs(det) < 1e-10f)
                    return s1;
                var dp = panelPoint - p1;
                var dsx = (dPdsy.y * dp.x - dPdsy.x * dp.y) / det;
                var dsy = (-dPdsx.y * dp.x + dPdsx.x * dp.y) / det;
                return s1 + new Vector2(dsx, dsy);
            }

            var tl = PanelPointToScreen(new Vector2(bounds.xMin, bounds.yMin));
            var tr = PanelPointToScreen(new Vector2(bounds.xMax, bounds.yMin));
            var bl = PanelPointToScreen(new Vector2(bounds.xMin, bounds.yMax));
            var br = PanelPointToScreen(new Vector2(bounds.xMax, bounds.yMax));

            var minX = Mathf.Min(tl.x, tr.x, bl.x, br.x);
            var maxX = Mathf.Max(tl.x, tr.x, bl.x, br.x);
            var minY = Mathf.Min(tl.y, tr.y, bl.y, br.y);
            var maxY = Mathf.Max(tl.y, tr.y, bl.y, br.y);

            var rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            if (rect.width >= 0.5f && rect.height >= 0.5f)
                return rect;

            var hostWindow = EditorWindow.focusedWindow;
            if (hostWindow != null)
            {
                var w = hostWindow.position;
                return new Rect(w.x + 24f, w.y + 48f, Mathf.Max(bounds.width, 100f), Mathf.Max(bounds.height, EditorGUIUtility.singleLineHeight));
            }

            return rect;
        }

        static Rect GetEditorPanelPickerAnchorRect()
        {
            const float anchorW = 200f;
            const float anchorH = 22f;
            var sw = Screen.width;
            var sh = Screen.height;
            if (sw < 64 || sh < 64)
            {
                sw = (int)1280f;
                sh = (int)720f;
            }

            return new Rect((sw - anchorW) * 0.5f, (sh - anchorH) * 0.5f, anchorW, anchorH);
        }

        private void ShowDropdown(VisualElement anchor, SerializedProperty serializedProperty, Action modifiedCallback,
            Action refreshPathRowUi = null)
        {
            var screenRect = GetScreenSpaceButtonRect(anchor);
            if (screenRect.width < 1f)
                screenRect.width = 100f;
            if (screenRect.height < 1f)
                screenRect.height = EditorGUIUtility.singleLineHeight;

            void OnPathPicked(string path)
            {
                serializedProperty.stringValue = path;
                m_PickerState.manualPathEditMode = false;
                modifiedCallback();
                if (refreshPathRowUi != null)
                    EditorApplication.delayCall += () => refreshPathRowUi();
            }

            InputActionsEditorSettingsProvider.SetIMGUIDropdownVisible(true, false);
            IsShowingDropdown = true;

            if (m_PickerDropdown == null)
                m_PickerDropdown = new InputControlPickerDropdown(m_PickerState, OnPathPicked);

            m_PickerDropdown.SetPickedCallback(OnPathPicked);

            m_PickerDropdown.SetControlPathsToMatch(m_ControlPathsToMatch);
            m_PickerDropdown.SetExpectedControlLayout(m_ExpectedControlLayout);

            m_PickerDropdown.ShowFromScreenSpaceButtonRect(screenRect);

            IsShowingDropdown = false;
        }
#endif

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

            // Cache the display name per path value and only recompute when the string actually changes.
            if (!string.Equals(path, m_CachedPath, StringComparison.InvariantCultureIgnoreCase))
            {
                m_CachedPath = path;
                m_CachedDisplayName = InputControlPath.ToHumanReadableString(path);
            }

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
                if (EditorGUI.DropdownButton(bindingTextRect, new GUIContent(m_CachedDisplayName), FocusType.Keyboard))
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
            InputActionsEditorSettingsProvider.SetIMGUIDropdownVisible(true, false);
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

        private string m_CachedPath;
        private string m_CachedDisplayName;

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
#endif // UNITY_EDITOR || PACKAGE_DOCS_GENERATION
