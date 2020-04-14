#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine.InputSystem.Utilities;

////TODO: better method for creating display names than InputControlPath.TryGetDeviceLayout

////FIXME: Device requirements list in control scheme popup must mention explicitly that that is what it is

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Toolbar in input action asset editor.
    /// </summary>
    /// <remarks>
    /// Allows editing and selecting from the set of control schemes as well as selecting from the
    /// set of device requirements within the currently selected control scheme.
    ///
    /// Also controls saving and has the global search text field.
    /// </remarks>
    /// <seealso cref="InputActionEditorWindow"/>
    [Serializable]
    internal class InputActionEditorToolbar
    {
        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawSchemeSelection();
            DrawDeviceFilterSelection();
            if (!InputEditorUserSettings.autoSaveInputActionAssets)
                DrawSaveButton();
            GUILayout.FlexibleSpace();
            DrawAutoSaveToggle();
            GUILayout.Space(5);
            DrawSearchField();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSchemeSelection()
        {
            var buttonGUI = m_ControlSchemes.LengthSafe() > 0
                ? new GUIContent(selectedControlScheme?.name ?? "All Control Schemes")
                : new GUIContent("No Control Schemes");

            var buttonRect = GUILayoutUtility.GetRect(buttonGUI, EditorStyles.toolbarPopup, GUILayout.MinWidth(k_MinimumButtonWidth));

            if (GUI.Button(buttonRect, buttonGUI, EditorStyles.toolbarPopup))
            {
                // PopupWindow.Show already takes the current OnGUI EditorWindow context into account for window coordinates.
                // However, on macOS, menu commands are performed asynchronously, so we don't have a current OnGUI context.
                // So in that case, we need to translate the rect to screen coordinates. Don't do that on windows, as we will
                // overcompensate otherwise.
                if (Application.platform == RuntimePlatform.OSXEditor)
                    buttonRect = new Rect(EditorGUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y)), Vector2.zero);

                var menu = new GenericMenu();

                // Add entries to select control scheme, if we have some.
                if (m_ControlSchemes.LengthSafe() > 0)
                {
                    menu.AddItem(s_AllControlSchemes, m_SelectedControlSchemeIndex == -1, OnControlSchemeSelected, null);
                    var selectedControlSchemeName = m_SelectedControlSchemeIndex == -1
                        ? null : m_ControlSchemes[m_SelectedControlSchemeIndex].name;
                    foreach (var controlScheme in m_ControlSchemes.OrderBy(x => x.name))
                        menu.AddItem(new GUIContent(controlScheme.name),
                            controlScheme.name == selectedControlSchemeName, OnControlSchemeSelected,
                            controlScheme.name);

                    menu.AddSeparator(string.Empty);
                }

                // Add entries to add/edit/duplicate/delete control schemes.
                menu.AddItem(s_AddControlSchemeLabel, false, OnAddControlScheme, buttonRect);
                if (m_SelectedControlSchemeIndex >= 0)
                {
                    menu.AddItem(s_EditControlSchemeLabel, false, OnEditSelectedControlScheme, buttonRect);
                    menu.AddItem(s_DuplicateControlSchemeLabel, false, OnDuplicateControlScheme, buttonRect);
                    menu.AddItem(s_DeleteControlSchemeLabel, false, OnDeleteControlScheme);
                }
                else
                {
                    menu.AddDisabledItem(s_EditControlSchemeLabel, false);
                    menu.AddDisabledItem(s_DuplicateControlSchemeLabel, false);
                    menu.AddDisabledItem(s_DeleteControlSchemeLabel, false);
                }

                menu.ShowAsContext();
            }
        }

        private void DrawDeviceFilterSelection()
        {
            // Lazy-initialize list of GUIContents that represent each individual device requirement.
            if (m_SelectedSchemeDeviceRequirementNames == null && m_ControlSchemes.LengthSafe() > 0 && m_SelectedControlSchemeIndex >= 0)
            {
                m_SelectedSchemeDeviceRequirementNames = m_ControlSchemes[m_SelectedControlSchemeIndex]
                    .deviceRequirements.Select(x => new GUIContent(DeviceRequirementToDisplayString(x)))
                    .ToArray();
            }

            EditorGUI.BeginDisabledGroup(m_SelectedControlSchemeIndex < 0);
            if (m_SelectedSchemeDeviceRequirementNames.LengthSafe() == 0)
            {
                GUILayout.Button(s_AllDevicesLabel, EditorStyles.toolbarPopup, GUILayout.MinWidth(k_MinimumButtonWidth));
            }
            else if (GUILayout.Button(m_SelectedDeviceRequirementIndex < 0 ? s_AllDevicesLabel : m_SelectedSchemeDeviceRequirementNames[m_SelectedDeviceRequirementIndex],
                EditorStyles.toolbarPopup, GUILayout.MinWidth(k_MinimumButtonWidth)))
            {
                var menu = new GenericMenu();
                menu.AddItem(s_AllDevicesLabel, m_SelectedControlSchemeIndex == -1, OnSelectedDeviceChanged, -1);
                for (var i = 0; i < m_SelectedSchemeDeviceRequirementNames.Length; i++)
                    menu.AddItem(m_SelectedSchemeDeviceRequirementNames[i], m_SelectedDeviceRequirementIndex == i, OnSelectedDeviceChanged, i);
                menu.ShowAsContext();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawSaveButton()
        {
            EditorGUI.BeginDisabledGroup(!m_IsDirty);
            EditorGUILayout.Space();
            if (GUILayout.Button(s_SaveAssetLabel, EditorStyles.toolbarButton))
                onSave();
            EditorGUI.EndDisabledGroup();
        }

        private void DrawAutoSaveToggle()
        {
            ////FIXME: Using a normal Toggle style with a miniFont, I can't get the "Auto-Save" label to align properly on the vertical.
            ////       The workaround here splits it into a toggle with an empty label plus an extra label.
            ////       Not using EditorStyles.toolbarButton here as it makes it hard to tell that it's a toggle.
            if (s_MiniToggleStyle == null)
            {
                s_MiniToggleStyle = new GUIStyle("Toggle")
                {
                    font = EditorStyles.miniFont,
                    margin = new RectOffset(0, 0, 1, 0),
                    padding = new RectOffset(0, 16, 0, 0)
                };
                s_MiniLabelStyle = new GUIStyle("Label")
                {
                    font = EditorStyles.miniFont,
                    margin = new RectOffset(0, 0, 3, 0)
                };
            }

            var autoSaveNew = GUILayout.Toggle(InputEditorUserSettings.autoSaveInputActionAssets, "",
                s_MiniToggleStyle);
            GUILayout.Label(s_AutoSaveLabel, s_MiniLabelStyle);
            if (autoSaveNew != InputEditorUserSettings.autoSaveInputActionAssets && autoSaveNew && m_IsDirty)
            {
                // If it changed from disabled to enabled, perform an initial save.
                onSave();
            }

            InputEditorUserSettings.autoSaveInputActionAssets = autoSaveNew;

            GUILayout.Space(5);
        }

        private void DrawSearchField()
        {
            if (m_SearchField == null)
                m_SearchField = new SearchField();

            EditorGUI.BeginChangeCheck();
            m_SearchText = m_SearchField.OnToolbarGUI(m_SearchText, GUILayout.MaxWidth(250));
            if (EditorGUI.EndChangeCheck())
                onSearchChanged?.Invoke();
        }

        private void OnControlSchemeSelected(object nameObj)
        {
            var index = -1;
            var name = (string)nameObj;
            if (name != null)
            {
                index = ArrayHelpers.IndexOf(m_ControlSchemes,
                    x => x.name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                Debug.Assert(index != -1, $"Cannot find control scheme {name}");
            }

            m_SelectedControlSchemeIndex = index;
            m_SelectedDeviceRequirementIndex = -1;
            m_SelectedSchemeDeviceRequirementNames = null;

            onSelectedSchemeChanged?.Invoke();
        }

        private void OnSelectedDeviceChanged(object indexObj)
        {
            Debug.Assert(m_SelectedControlSchemeIndex >= 0, "Control scheme must be selected");

            m_SelectedDeviceRequirementIndex = (int)indexObj;
            onSelectedDeviceChanged?.Invoke();
        }

        private void OnAddControlScheme(object position)
        {
            var uniqueName = MakeUniqueControlSchemeName("New control scheme");
            ControlSchemePropertiesPopup.Show((Rect)position,
                new InputControlScheme(uniqueName),
                (s, _) => AddAndSelectControlScheme(s));
        }

        private void OnDeleteControlScheme()
        {
            Debug.Assert(m_SelectedControlSchemeIndex >= 0, "Control scheme must be selected");

            var name = m_ControlSchemes[m_SelectedControlSchemeIndex].name;
            var bindingGroup = m_ControlSchemes[m_SelectedControlSchemeIndex].bindingGroup;

            // Ask for confirmation.
            if (!EditorUtility.DisplayDialog("Delete scheme?", $"Do you want to delete control scheme '{name}'?",
                "Delete", "Cancel"))
                return;

            ArrayHelpers.EraseAt(ref m_ControlSchemes, m_SelectedControlSchemeIndex);
            m_SelectedControlSchemeIndex = -1;
            m_SelectedSchemeDeviceRequirementNames = null;

            if (m_SelectedDeviceRequirementIndex >= 0)
            {
                m_SelectedDeviceRequirementIndex = -1;
                onSelectedDeviceChanged?.Invoke();
            }

            onControlSchemesChanged?.Invoke();
            onSelectedSchemeChanged?.Invoke();
            onControlSchemeDeleted?.Invoke(name, bindingGroup);
        }

        ////REVIEW: this does nothing to bindings; should this ask to duplicate bindings as well?
        private void OnDuplicateControlScheme(object position)
        {
            Debug.Assert(m_SelectedControlSchemeIndex >= 0, "Control scheme must be selected");

            var scheme = m_ControlSchemes[m_SelectedControlSchemeIndex];
            scheme = new InputControlScheme(MakeUniqueControlSchemeName(scheme.name),
                devices: scheme.deviceRequirements);

            ControlSchemePropertiesPopup.Show((Rect)position, scheme,
                (s, _) => AddAndSelectControlScheme(s));
        }

        private void OnEditSelectedControlScheme(object position)
        {
            Debug.Assert(m_SelectedControlSchemeIndex >= 0, "Control scheme must be selected");

            ControlSchemePropertiesPopup.Show((Rect)position,
                m_ControlSchemes[m_SelectedControlSchemeIndex],
                UpdateControlScheme,
                m_SelectedControlSchemeIndex);
        }

        private void AddAndSelectControlScheme(InputControlScheme scheme)
        {
            // Ensure scheme has a name.
            if (string.IsNullOrEmpty(scheme.name))
                scheme.m_Name = "New control scheme";

            // Make sure name is unique.
            scheme.m_Name = MakeUniqueControlSchemeName(scheme.name);

            var index = ArrayHelpers.Append(ref m_ControlSchemes, scheme);
            onControlSchemesChanged?.Invoke();

            SelectControlScheme(index);
        }

        private void UpdateControlScheme(InputControlScheme scheme, int index)
        {
            Debug.Assert(index >= 0 && index < m_ControlSchemes.LengthSafe(), "Control scheme index out of range");

            var renamed = false;
            string oldBindingGroup = null;
            string newBindingGroup = null;

            // If given scheme has no name, preserve the existing one on the control scheme.
            if (string.IsNullOrEmpty(scheme.name))
                scheme.m_Name = m_ControlSchemes[index].name;

            // If name is changing, make sure it's unique.
            else if (scheme.name != m_ControlSchemes[index].name)
            {
                renamed = true;
                oldBindingGroup = m_ControlSchemes[index].bindingGroup;
                m_ControlSchemes[index].m_Name = ""; // Don't want this to interfere with finding a unique name.
                var newName = MakeUniqueControlSchemeName(scheme.name);
                m_ControlSchemes[index].SetNameAndBindingGroup(newName);
                newBindingGroup = m_ControlSchemes[index].bindingGroup;
            }

            m_ControlSchemes[index] = scheme;
            onControlSchemesChanged?.Invoke();

            if (renamed)
                onControlSchemeRenamed?.Invoke(oldBindingGroup, newBindingGroup);
        }

        private void SelectControlScheme(int index)
        {
            Debug.Assert(index >= 0 && index < m_ControlSchemes.LengthSafe(), "Control scheme index out of range");

            m_SelectedControlSchemeIndex = index;
            m_SelectedSchemeDeviceRequirementNames = null;
            onSelectedSchemeChanged?.Invoke();

            // Reset device selection.
            if (m_SelectedDeviceRequirementIndex != -1)
            {
                m_SelectedDeviceRequirementIndex = -1;
                onSelectedDeviceChanged?.Invoke();
            }
        }

        private string MakeUniqueControlSchemeName(string name)
        {
            return StringHelpers.MakeUniqueName(name, m_ControlSchemes, x => x.name);
        }

        private static string DeviceRequirementToDisplayString(InputControlScheme.DeviceRequirement requirement)
        {
            ////TODO: need something more flexible to produce correct results for more than the simple string we produce here
            var deviceLayout = InputControlPath.TryGetDeviceLayout(requirement.controlPath);
            var deviceLayoutText = !string.IsNullOrEmpty(deviceLayout)
                ? EditorInputControlLayoutCache.GetDisplayName(deviceLayout)
                : string.Empty;
            var usages = InputControlPath.TryGetDeviceUsages(requirement.controlPath);

            if (usages != null && usages.Length > 0)
                return $"{deviceLayoutText} {string.Join("}{", usages)}";

            return deviceLayoutText;
        }

        // Notifications.
        public Action onSearchChanged;
        public Action onSelectedSchemeChanged;
        public Action onSelectedDeviceChanged;
        public Action onControlSchemesChanged;
        public Action<string, string> onControlSchemeRenamed;
        public Action<string, string> onControlSchemeDeleted;
        public Action onSave;

        [SerializeField] private bool m_IsDirty;
        [SerializeField] private int m_SelectedControlSchemeIndex = -1;
        [SerializeField] private int m_SelectedDeviceRequirementIndex = -1;
        [SerializeField] private InputControlScheme[] m_ControlSchemes;
        [SerializeField] private string m_SearchText;

        private GUIContent[] m_SelectedSchemeDeviceRequirementNames;
        private SearchField m_SearchField;

        private static readonly GUIContent s_AllControlSchemes = EditorGUIUtility.TrTextContent("All Control Schemes");
        private static readonly GUIContent s_AddControlSchemeLabel = new GUIContent("Add Control Scheme...");
        private static readonly GUIContent s_EditControlSchemeLabel = EditorGUIUtility.TrTextContent("Edit Control Scheme...");
        private static readonly GUIContent s_DuplicateControlSchemeLabel = EditorGUIUtility.TrTextContent("Duplicate Control Scheme...");
        private static readonly GUIContent s_DeleteControlSchemeLabel = EditorGUIUtility.TrTextContent("Delete Control Scheme...");
        private static readonly GUIContent s_SaveAssetLabel = EditorGUIUtility.TrTextContent("Save Asset");
        private static readonly GUIContent s_AutoSaveLabel = EditorGUIUtility.TrTextContent("Auto-Save");
        private static readonly GUIContent s_AllDevicesLabel = EditorGUIUtility.TrTextContent("All Devices");

        private static GUIStyle s_MiniToggleStyle;
        private static GUIStyle s_MiniLabelStyle;

        private const float k_MinimumButtonWidth = 110f;

        public ReadOnlyArray<InputControlScheme> controlSchemes
        {
            get => m_ControlSchemes;
            set
            {
                m_ControlSchemes = value.ToArray();
                m_SelectedSchemeDeviceRequirementNames = null;
            }
        }

        /// <summary>
        /// The control scheme currently selected in the toolbar or null if none is selected.
        /// </summary>
        public InputControlScheme? selectedControlScheme => m_SelectedControlSchemeIndex >= 0
        ? new InputControlScheme ? (m_ControlSchemes[m_SelectedControlSchemeIndex])
        : null;

        /// <summary>
        /// The device requirement of the currently selected control scheme which is currently selected
        /// in the toolbar or null if none is selected.
        /// </summary>
        public InputControlScheme.DeviceRequirement? selectedDeviceRequirement => m_SelectedDeviceRequirementIndex >= 0
        ? new InputControlScheme.DeviceRequirement ? (m_ControlSchemes[m_SelectedControlSchemeIndex]
            .deviceRequirements[m_SelectedDeviceRequirementIndex])
        : null;

        /// <summary>
        /// The search text currently entered in the toolbar or null.
        /// </summary>
        public string searchText => m_SearchText;

        internal void ResetSearchFilters()
        {
            m_SearchText = null;
            m_SelectedControlSchemeIndex = -1;
            m_SelectedDeviceRequirementIndex = -1;
        }

        public bool isDirty
        {
            get => m_IsDirty;
            set => m_IsDirty = value;
        }

        /// <summary>
        /// Popup window content for editing control schemes.
        /// </summary>
        private class ControlSchemePropertiesPopup : PopupWindowContent
        {
            public static void Show(Rect position, InputControlScheme controlScheme, Action<InputControlScheme, int> onApply,
                int controlSchemeIndex = -1)
            {
                var popup = new ControlSchemePropertiesPopup
                {
                    m_ControlSchemeIndex = controlSchemeIndex,
                    m_ControlScheme = controlScheme,
                    m_OnApply = onApply,
                    m_SetFocus = true,
                };

                // We're calling here from a callback, so we need to manually handle ExitGUIException.
                try
                {
                    PopupWindow.Show(position, popup);
                }
                catch (ExitGUIException) {}
            }

            public override Vector2 GetWindowSize()
            {
                return m_ButtonsAndLabelsHeights > 0 ? new Vector2(300, m_ButtonsAndLabelsHeights) : s_DefaultSize;
            }

            public override void OnOpen()
            {
                m_DeviceList = m_ControlScheme.deviceRequirements.Select(a => new DeviceEntry(a)).ToList();
                m_DeviceView = new ReorderableList(m_DeviceList, typeof(InputControlScheme.DeviceRequirement));
                m_DeviceView.headerHeight = 2;
                m_DeviceView.onAddCallback += list =>
                {
                    var dropdown = new InputControlPickerDropdown(
                        new InputControlPickerState(),
                        path =>
                        {
                            var requirement = new InputControlScheme.DeviceRequirement
                            {
                                controlPath = path,
                                isOptional = false
                            };

                            AddDeviceRequirement(requirement);
                        },
                        mode: InputControlPicker.Mode.PickDevice);
                    dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
                };
                m_DeviceView.onRemoveCallback += list =>
                {
                    list.list.RemoveAt(list.index);
                    list.index = -1;
                };
            }

            public override void OnGUI(Rect rect)
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    Event.current.Use();
                }

                if (Event.current.type == EventType.Repaint)
                    m_ButtonsAndLabelsHeights = 0;

                GUILayout.BeginArea(rect);
                DrawTopBar();
                EditorGUILayout.BeginVertical(EditorStyles.label);
                DrawSpace();
                DrawNameEditTextField();
                DrawSpace();
                DrawDeviceList();
                DrawConfirmationButton();
                EditorGUILayout.EndVertical();
                GUILayout.EndArea();
            }

            private void DrawConfirmationButton()
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true)))
                {
                    editorWindow.Close();
                }
                if (GUILayout.Button("Save", GUILayout.ExpandWidth(true)))
                {
                    // Don't allow control scheme name to be empty.
                    if (string.IsNullOrEmpty(m_ControlScheme.name))
                    {
                        ////FIXME: On 2019.1 this doesn't display properly in the window; check 2019.3
                        editorWindow.ShowNotification(new GUIContent("Control scheme must have a name"));
                    }
                    else
                    {
                        m_ControlScheme = new InputControlScheme(m_ControlScheme.name,
                            devices: m_DeviceList.Select(a => a.deviceRequirement));

                        editorWindow.Close();
                        m_OnApply(m_ControlScheme, m_ControlSchemeIndex);
                    }
                }
                if (Event.current.type == EventType.Repaint)
                    m_ButtonsAndLabelsHeights += GUILayoutUtility.GetLastRect().height;
                EditorGUILayout.EndHorizontal();
            }

            private void DrawDeviceList()
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.label);
                var requirementsLabelSize = EditorStyles.label.CalcSize(s_RequirementsLabel);
                var deviceListRect = GUILayoutUtility.GetRect(GetWindowSize().x - requirementsLabelSize.x - 20, m_DeviceView.GetHeight());
                m_DeviceView.DoList(deviceListRect);
                var requirementsHeight = DrawRequirementsCheckboxes();
                var listHeight = m_DeviceView.GetHeight() + EditorGUIUtility.singleLineHeight * 3;
                if (Event.current.type == EventType.Repaint)
                {
                    if (listHeight < requirementsHeight)
                    {
                        m_ButtonsAndLabelsHeights += requirementsHeight;
                    }
                    else
                    {
                        m_ButtonsAndLabelsHeights += listHeight;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            private void DrawSpace()
            {
                GUILayout.Space(6f);
                if (Event.current.type == EventType.Repaint)
                    m_ButtonsAndLabelsHeights += 6f;
            }

            private void DrawTopBar()
            {
                EditorGUILayout.LabelField(s_AddControlSchemeLabel, Styles.headerLabel);

                if (Event.current.type == EventType.Repaint)
                    m_ButtonsAndLabelsHeights += GUILayoutUtility.GetLastRect().height;
            }

            private void DrawNameEditTextField()
            {
                EditorGUILayout.BeginHorizontal();
                var labelSize = EditorStyles.label.CalcSize(s_RequirementsLabel);
                EditorGUILayout.LabelField(s_ControlSchemeNameLabel, GUILayout.Width(labelSize.x));

                GUI.SetNextControlName("ControlSchemeName");
                ////FIXME: This should be a DelayedTextField but for some reason (maybe because it's in a popup?), this
                ////       will lead to the text field not working correctly. Hitting enter on the keyboard will apply the
                ////       change as expected but losing focus will *NOT*. In most cases, this makes the text field seem not
                ////       to work at all so instead we use a normal text field here and then apply the name change as part
                ////       of apply the control scheme changes as a whole. The only real downside is that if the name gets
                ////       adjusted automatically because of a naming conflict, this will only become evident *after* hitting
                ////       the "Save" button.
                m_ControlScheme.m_Name = EditorGUILayout.TextField(m_ControlScheme.m_Name);

                if (m_SetFocus)
                {
                    EditorGUI.FocusTextInControl("ControlSchemeName");
                    m_SetFocus = false;
                }

                EditorGUILayout.EndHorizontal();
            }

            private float DrawRequirementsCheckboxes()
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(s_RequirementsLabel, GUILayout.Width(200));
                var requirementHeights = GUILayoutUtility.GetLastRect().y;
                EditorGUI.BeginDisabledGroup(m_DeviceView.index == -1);
                var requirementsOption = -1;
                if (m_DeviceView.index >= 0)
                {
                    var deviceEntryForList = (DeviceEntry)m_DeviceView.list[m_DeviceView.index];
                    requirementsOption = deviceEntryForList.deviceRequirement.isOptional ? 0 : 1;
                }
                EditorGUI.BeginChangeCheck();
                requirementsOption = GUILayout.SelectionGrid(requirementsOption, s_RequiredOptionalChoices, 1, EditorStyles.radioButton);
                requirementHeights += GUILayoutUtility.GetLastRect().y;
                if (EditorGUI.EndChangeCheck())
                    m_DeviceList[m_DeviceView.index].deviceRequirement.isOptional = requirementsOption == 0;
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndVertical();
                return requirementHeights;
            }

            private void AddDeviceRequirement(InputControlScheme.DeviceRequirement requirement)
            {
                ArrayHelpers.Append(ref m_ControlScheme.m_DeviceRequirements, requirement);
                m_DeviceList.Add(new DeviceEntry(requirement));
                m_DeviceView.index = m_DeviceView.list.Count - 1;

                editorWindow.Repaint();
            }

            /// <summary>
            /// The control scheme edited by the popup.
            /// </summary>
            public InputControlScheme controlScheme => m_ControlScheme;

            private int m_ControlSchemeIndex;
            private InputControlScheme m_ControlScheme;
            private Action<InputControlScheme, int> m_OnApply;

            private ReorderableList m_DeviceView;
            private List<DeviceEntry> m_DeviceList = new List<DeviceEntry>();
            private int m_RequirementsOptionsChoice;

            private bool m_SetFocus;
            private float m_ButtonsAndLabelsHeights;

            private static Vector2 s_DefaultSize => new Vector2(300, 200);
            private static readonly GUIContent s_RequirementsLabel = EditorGUIUtility.TrTextContent("Requirements:");
            private static readonly GUIContent s_AddControlSchemeLabel = EditorGUIUtility.TrTextContent("Add control scheme");
            private static readonly GUIContent s_ControlSchemeNameLabel = EditorGUIUtility.TrTextContent("Scheme Name");
            private static readonly string[] s_RequiredOptionalChoices = { "Optional", "Required" };

            private static class Styles
            {
                public static readonly GUIStyle headerLabel = new GUIStyle(EditorStyles.toolbar)
                    .WithAlignment(TextAnchor.MiddleCenter)
                    .WithFontStyle(FontStyle.Bold)
                    .WithPadding(new RectOffset(10, 6, 0, 0));
            }

            private class DeviceEntry
            {
                public string displayText;
                public InputControlScheme.DeviceRequirement deviceRequirement;

                public DeviceEntry(InputControlScheme.DeviceRequirement requirement)
                {
                    displayText = DeviceRequirementToDisplayString(requirement);
                    deviceRequirement = requirement;
                }

                public override string ToString()
                {
                    return displayText;
                }
            }
        }
    }
}
#endif // UNITY_EDITOR
