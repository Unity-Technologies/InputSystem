#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Experimental.Input.Editor.Lists;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputBindingPropertiesView : PropertiesView
    {
        public bool isInteractivelyPicking => m_RebindingOperation != null && m_RebindingOperation.started;
        public string expectedControlLayout => m_ExpectedControlLayout;

        public string compositeType
        {
            get
            {
                if (!m_IsComposite)
                    return null;
                if (m_CompositeTypes == null)
                    InitializeCompositeProperties();
                return m_CompositeTypes[m_SelectedCompositeType];
            }
        }

        public static FourCC k_GroupsChanged => new FourCC("GRPS");
        public static FourCC k_PathChanged => new FourCC("PATH");
        public static FourCC k_CompositeTypeChanged => new FourCC("COMP");

        public InputBindingPropertiesView(SerializedProperty bindingProperty, Action<FourCC> onChange,
                                          InputControlPickerState controlPickerState, InputActionWindowToolbar toolbar,
                                          bool isCompositeBinding = false,
                                          string expectedControlLayout = null)
            : base(isCompositeBinding ? "Composite" : "Binding", bindingProperty, onChange, expectedControlLayout)
        {
            m_ControlPickerState = controlPickerState;
            m_BindingProperty = bindingProperty;
            m_GroupsProperty = bindingProperty.FindPropertyRelative("m_Groups");
            m_PathProperty = bindingProperty.FindPropertyRelative("m_Path");
            m_Toolbar = toolbar;
            if (m_Toolbar != null)
                m_ControlSchemes = toolbar.controlSchemes;
            m_BindingGroups = m_GroupsProperty.stringValue.Split(InputBinding.kSeparator).ToList();
            m_ExpectedControlLayout = expectedControlLayout;
            m_IsComposite = isCompositeBinding;
        }

        public void CancelInteractivePicking()
        {
            m_RebindingOperation?.Cancel();
        }

        protected override void DrawGeneralProperties()
        {
            if (m_IsComposite)
            {
                if (m_CompositeParameters == null)
                    InitializeCompositeProperties();

                // Composite type dropdown.
                var selectedCompositeType = EditorGUILayout.Popup(s_CompositeTypeLabel, m_SelectedCompositeType, m_CompositeTypeOptions);
                if (selectedCompositeType != m_SelectedCompositeType)
                {
                    m_SelectedCompositeType = selectedCompositeType;
                    OnCompositeTypeChanged();
                }

                // Composite parameters.
                m_CompositeParameters.OnGUI();
            }
            else
            {
                // Path.
                DrawBindingGUI(m_PathProperty, ref m_ManualPathEditMode, m_ControlPickerState,
                    () =>
                    {
                        m_ManualPathEditMode = false;
                        OnPathChanged();
                    });

                // Control scheme matrix.
                DrawUseInControlSchemes();
            }
        }

        private void DrawUseInControlSchemes()
        {
            if (m_Toolbar == null || m_Toolbar.controlSchemes.Count == 0)
                return;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_UseInControlSchemesLAbel, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical();
            foreach (var scheme in m_ControlSchemes)
            {
                EditorGUI.BeginChangeCheck();
                var result = EditorGUILayout.Toggle(scheme.name, m_BindingGroups.Contains(scheme.bindingGroup));
                if (EditorGUI.EndChangeCheck())
                {
                    if (result)
                    {
                        m_BindingGroups.Add(scheme.bindingGroup);
                    }
                    else
                    {
                        m_BindingGroups.Remove(scheme.bindingGroup);
                    }
                    OnBindingGroupsChanged();
                }
            }
            EditorGUILayout.EndVertical();
        }

        ////TODO: interactive picker; if more than one control makes it through the filters, present list of
        ////      candidates for user to choose from

        ////REVIEW: refactor this out of here; this should be a public API that allows anyone to have an inspector field to select a control binding
        internal void DrawBindingGUI(SerializedProperty pathProperty, ref bool manualPathEditMode, InputControlPickerState pickerState, Action onModified)
        {
            EditorGUILayout.BeginHorizontal();

            var lineRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            var labelRect = lineRect;
            labelRect.width = 60;
            EditorGUI.LabelField(labelRect, s_PathLabel);
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

            var path = pathProperty.stringValue;
            ////TODO: this should be cached; generates needless GC churn
            var displayName = InputControlPath.ToHumanReadableString(path);

            if (manualPathEditMode || (!string.IsNullOrEmpty(path) && string.IsNullOrEmpty(displayName)))
            {
                EditorGUI.BeginChangeCheck();
                path = EditorGUI.DelayedTextField(bindingTextRect, path);
                if (EditorGUI.EndChangeCheck())
                {
                    pathProperty.stringValue = path;
                    pathProperty.serializedObject.ApplyModifiedProperties();
                    onModified();
                }
                DrawInteractivePickButton(interactivePickButtonRect, pathProperty, onModified);
                if (GUI.Button(editButtonRect, "˅"))
                {
                    bindingTextRect.x += editButtonRect.width;
                    ShowInputControlPicker(bindingTextRect, pathProperty, pickerState, onModified);
                }
            }
            else
            {
                // Dropdown that shows binding text and allows opening control picker.
                if (EditorGUI.DropdownButton(bindingTextRect, new GUIContent(displayName), FocusType.Keyboard))
                {
                    ////TODO: pass expectedControlLayout filter on to control picker
                    ////TODO: for bindings that are part of composites, use the layout information from the [InputControl] attribute on the field
                    ShowInputControlPicker(bindingTextRect, pathProperty, pickerState, onModified);
                }

                // Button to bind interactively.
                DrawInteractivePickButton(interactivePickButtonRect, pathProperty, onModified);

                // Button that switches binding into text edit mode.
                if (GUI.Button(editButtonRect, "...", EditorStyles.miniButton))
                {
                    manualPathEditMode = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInteractivePickButton(Rect rect, SerializedProperty pathProperty, Action onModified)
        {
            ////FIXME: need to suppress triggering shortcuts in the editor while doing rebinds
            ////TODO: need to have good way to cancel binding

            var toggleRebind = GUI.Toggle(rect,
                m_RebindingOperation != null && m_RebindingOperation.started, "0", EditorStyles.miniButton);
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
                    ////REVIEW: the delay makes it more robust but doesn't feel good
                    // Give us a buffer of 0.25 seconds to see if a better match comes along.
                    .OnMatchWaitForAnother(0.25f)
                    ////REVIEW: should we exclude only the system's active pointing device?
                    // With the mouse operating the UI, its cursor control is too fickle a thing to
                    // bind to. Ignore mouse position and delta.
                    // NOTE: We go for all types of pointers here, not just mice.
                    .WithControlsExcluding("<Pointer>/position")
                    .WithControlsExcluding("<Pointer>/delta")
                    .OnApplyBinding(
                        (operation, newPath) =>
                        {
                            pathProperty.stringValue = newPath;
                            pathProperty.serializedObject.ApplyModifiedProperties();
                            onModified();
                        });

                // For all control schemes that the binding is part of, constrain what we pick
                // by the device paths we have in the control scheme.
                var bindingIsPartOfControlScheme = false;
                foreach (var controlScheme in m_ControlSchemes)
                {
                    if (m_BindingGroups.Contains(controlScheme.bindingGroup))
                    {
                        foreach (var deviceRequirement in controlScheme.deviceRequirements)
                            m_RebindingOperation.WithControlsHavingToMatchPath(deviceRequirement.controlPath);
                        bindingIsPartOfControlScheme = true;
                    }
                }
                if (!bindingIsPartOfControlScheme)
                {
                    // Not part of a control scheme. Remove all path constraints.
                    m_RebindingOperation.WithoutControlsHavingToMatchPath();
                }

                m_RebindingOperation.Start();
            }
            else if (!toggleRebind && m_RebindingOperation != null && m_RebindingOperation.started)
            {
                m_RebindingOperation.Cancel();
            }
        }

        private void ShowInputControlPicker(Rect rect, SerializedProperty pathProperty, InputControlPickerState pickerState,
            Action onPickCallback)
        {
            if (m_InputControlPickerDropdown == null)
            {
                m_InputControlPickerDropdown = new InputControlPickerDropdown(pickerState.state,
                    path =>
                    {
                        pathProperty.stringValue = path;
                        onPickCallback();
                    });
            }

            var haveDeviceFilterFromControlScheme = false;
            if (m_Toolbar != null)
            {
                if (m_Toolbar.selectedDevice != null)
                {
                    // Single device selected from set of devices in control scheme.
                    m_InputControlPickerDropdown.SetDeviceFilter(new[] {m_Toolbar.selectedDevice});
                    haveDeviceFilterFromControlScheme = true;
                }
                else
                {
                    var allDevices = m_Toolbar.allDevices;
                    if (allDevices.Length > 0)
                    {
                        // Filter by all devices in current control scheme.
                        m_InputControlPickerDropdown.SetDeviceFilter(allDevices);
                        haveDeviceFilterFromControlScheme = true;
                    }
                }
                if (m_ExpectedControlLayout != null)
                {
                    m_InputControlPickerDropdown.SetExpectedControlLayoutFilter(m_ExpectedControlLayout);
                }
            }

            // If there's no device filter coming from a control scheme, filter by supported
            // devices as given by settings .
            if (!haveDeviceFilterFromControlScheme)
                m_InputControlPickerDropdown.SetDeviceFilter(InputSystem.settings.supportedDevices.ToArray());

            m_InputControlPickerDropdown.Show(rect);
        }

        private void InitializeCompositeProperties()
        {
            // Find name of current composite.
            var path = m_PathProperty.stringValue;
            var compositeNameAndParameters = InputControlLayout.ParseNameAndParameters(path);
            var compositeName = compositeNameAndParameters.name;
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);

            // Collect all possible composite types.
            var selectedCompositeIndex = -1;
            var compositeTypeOptionsList = new List<GUIContent>();
            var compositeTypeList = new List<string>();
            var currentIndex = 0;
            foreach (var composite in InputBindingComposite.s_Composites.internedNames.Where(x =>
                !InputBindingComposite.s_Composites.aliases.Contains(x)).OrderBy(x => x))
            {
                if (InputBindingComposite.s_Composites.LookupTypeRegistration(composite) == compositeType)
                    selectedCompositeIndex = currentIndex;
                var name = ObjectNames.NicifyVariableName(composite);
                compositeTypeOptionsList.Add(new GUIContent(name));
                compositeTypeList.Add(composite);
                ++currentIndex;
            }

            // If the current composite type isn't a registered type, add it to the list as
            // an extra option.
            if (selectedCompositeIndex == -1)
            {
                selectedCompositeIndex = compositeTypeList.Count;
                compositeTypeOptionsList.Add(new GUIContent(ObjectNames.NicifyVariableName(compositeName)));
                compositeTypeList.Add(compositeName);
            }

            m_CompositeTypes = compositeTypeList.ToArray();
            m_CompositeTypeOptions = compositeTypeOptionsList.ToArray();
            m_SelectedCompositeType = selectedCompositeIndex;

            // Initialize parameters.
            m_CompositeParameters = new ParameterListView
            {
                onChange = OnCompositeParametersModified
            };
            if (compositeType != null)
                m_CompositeParameters.Initialize(compositeType, compositeNameAndParameters.parameters);
        }

        private void OnCompositeParametersModified()
        {
            Debug.Assert(m_CompositeParameters != null);

            var path = m_PathProperty.stringValue;
            var nameAndParameters = InputControlLayout.ParseNameAndParameters(path);
            nameAndParameters.parameters = m_CompositeParameters.GetParameters();

            m_PathProperty.stringValue = nameAndParameters.ToString();
            m_PathProperty.serializedObject.ApplyModifiedProperties();

            OnPathChanged();
        }

        private void OnBindingGroupsChanged()
        {
            m_GroupsProperty.stringValue = string.Join(InputBinding.kSeparatorString, m_BindingGroups.ToArray());
            m_GroupsProperty.serializedObject.ApplyModifiedProperties();

            onChange(k_GroupsChanged);
        }

        private void OnPathChanged()
        {
            m_BindingProperty.serializedObject.ApplyModifiedProperties();
            onChange(k_PathChanged);
        }

        private void OnCompositeTypeChanged()
        {
            var nameAndParameters = new InputControlLayout.NameAndParameters
            {
                name = m_CompositeTypes[m_SelectedCompositeType],
                parameters = m_CompositeParameters.GetParameters()
            };

            m_PathProperty.stringValue = nameAndParameters.ToString();
            m_PathProperty.serializedObject.ApplyModifiedProperties();
            onChange(k_CompositeTypeChanged);
        }

        private readonly bool m_IsComposite;
        private ParameterListView m_CompositeParameters;
        private int m_SelectedCompositeType;
        private GUIContent[] m_CompositeTypeOptions;
        private string[] m_CompositeTypes;

        private readonly SerializedProperty m_GroupsProperty;
        private readonly SerializedProperty m_BindingProperty;
        private readonly SerializedProperty m_PathProperty;

        ////REVIEW: when we start with a blank tree view state, we should initialize the control picker to select the control currently
        ////        selected by the path property
        private readonly InputControlPickerState m_ControlPickerState;
        private InputControlPickerDropdown m_InputControlPickerDropdown;

        private static readonly GUIContent s_PathLabel = EditorGUIUtility.TrTextContent("Path", "Path of the controls that will be bound to the action at runtime.");
        private static readonly GUIContent s_CompositeTypeLabel = EditorGUIUtility.TrTextContent("Type",
            "Type of composite. Allows changing the composite type retroactively. Doing so will modify the bindings that are part of the composite.");
        private static readonly GUIContent s_UseInControlSchemesLAbel = EditorGUIUtility.TrTextContent("Use in control scheme");

        private bool m_ManualPathEditMode;
        private readonly ReadOnlyArray<InputControlScheme> m_ControlSchemes;
        private readonly List<string> m_BindingGroups;
        private readonly InputActionWindowToolbar m_Toolbar;
        private readonly string m_ExpectedControlLayout;
        private InputActionRebindingExtensions.RebindingOperation m_RebindingOperation;
    }
}
#endif // UNITY_EDITOR
