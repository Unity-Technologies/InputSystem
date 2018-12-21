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
    internal class InputBindingPropertiesView
    {
        private static class Styles
        {
            public static GUIStyle foldoutBackgroundStyle = new GUIStyle("Label");
            public static GUIStyle foldoutStyle = new GUIStyle("foldout");

            static Styles()
            {
                var darkGreyBackgroundWithBorderTexture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.ResourcesPath + "foldoutBackground.png");
                foldoutBackgroundStyle.normal.background = darkGreyBackgroundWithBorderTexture;
                foldoutBackgroundStyle.border = new RectOffset(3, 3, 3, 3);
                foldoutBackgroundStyle.margin = new RectOffset(1, 1, 3, 3);
            }
        }

        private InteractionsReorderableReorderableList m_InteractionsList;
        private ProcessorsReorderableReorderableList m_ProcessorsList;
        private ParameterListView m_CompositeParameters;

        private SerializedProperty m_InteractionsProperty;
        private SerializedProperty m_ProcessorsProperty;
        private SerializedProperty m_GroupsProperty;
        private SerializedProperty m_BindingProperty;
        private SerializedProperty m_PathProperty;

        private Action<Change> m_OnChange;
        ////REVIEW: when we start with a blank tree view state, we should initialize the control picker to select the control currently
        ////        selected by the path property
        private InputControlPickerState m_ControlPickerState;
        private InputControlPickerDropdown m_InputControlPickerDropdown;
        private bool m_GeneralFoldout = true;
        private bool m_InteractionsFoldout = true;
        private bool m_ProcessorsFoldout = true;

        private static readonly GUIContent s_ProcessorsContent = EditorGUIUtility.TrTextContent("Processors");
        private static readonly GUIContent s_InteractionsContent = EditorGUIUtility.TrTextContent("Interactions");
        private static readonly GUIContent s_GeneralContent = EditorGUIUtility.TrTextContent("General");
        private static readonly GUIContent s_BindingGui = EditorGUIUtility.TrTextContent("Binding");
        private static readonly GUIContent s_UseInSchemesGui = EditorGUIUtility.TrTextContent("Use in control scheme");

        private bool m_ManualPathEditMode;
        private ReadOnlyArray<InputControlScheme> m_ControlSchemes;
        private List<string> m_BindingGroups;
        private InputActionWindowToolbar m_Toolbar;
        private string m_ExpectedControlLayout;
        private InputActionRebindingExtensions.RebindingOperation m_RebindingOperation;

        public bool isCompositeBinding { get; set; }

        public bool isInteractivelyPicking
        {
            get { return m_RebindingOperation != null && m_RebindingOperation.started; }
        }

        public string expectedControlLayout
        {
            get { return m_ExpectedControlLayout; }
        }

        public InputBindingPropertiesView(SerializedProperty bindingProperty, Action<Change> onChange,
                                          InputControlPickerState controlPickerState, InputActionWindowToolbar toolbar,
                                          string expectedControlLayout = null)
        {
            m_ControlPickerState = controlPickerState;
            m_BindingProperty = bindingProperty;
            m_OnChange = onChange;
            m_InteractionsProperty = bindingProperty.FindPropertyRelative("m_Interactions");
            m_ProcessorsProperty = bindingProperty.FindPropertyRelative("m_Processors");
            m_GroupsProperty = bindingProperty.FindPropertyRelative("m_Groups");
            m_PathProperty = bindingProperty.FindPropertyRelative("m_Path");
            m_InteractionsList = new InteractionsReorderableReorderableList(m_InteractionsProperty, OnInteractionsModified);
            m_ProcessorsList = new ProcessorsReorderableReorderableList(m_ProcessorsProperty, OnProcessorsModified);
            m_Toolbar = toolbar;
            if (m_Toolbar != null)
                m_ControlSchemes = toolbar.controlSchemes;
            m_BindingGroups = m_GroupsProperty.stringValue.Split(InputBinding.kSeparator).ToList();
            m_ExpectedControlLayout = expectedControlLayout;
        }

        public void CancelInteractivePicking()
        {
            if (m_RebindingOperation != null)
                m_RebindingOperation.Cancel();
        }

        public void OnGUI()
        {
            if (m_BindingProperty == null)
                return;

            EditorGUILayout.BeginVertical();
            if (isCompositeBinding)
                DrawCompositeParameters();
            else
                DrawPathPicker();
            EditorGUILayout.Space();
            DrawInteractionsPicker();
            EditorGUILayout.Space();
            DrawProcessorsPicker();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawProcessorsPicker()
        {
            m_ProcessorsFoldout = DrawFoldout(s_ProcessorsContent, m_ProcessorsFoldout);

            if (m_ProcessorsFoldout)
            {
                EditorGUI.indentLevel++;
                m_ProcessorsList.OnGUI();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawInteractionsPicker()
        {
            m_InteractionsFoldout = DrawFoldout(s_InteractionsContent, m_InteractionsFoldout);

            if (m_InteractionsFoldout)
            {
                EditorGUI.indentLevel++;
                m_InteractionsList.OnGUI();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPathPicker()
        {
            m_GeneralFoldout = DrawFoldout(s_GeneralContent, m_GeneralFoldout);

            EditorGUI.indentLevel++;
            if (m_GeneralFoldout)
            {
                DrawBindingGUI(m_PathProperty, ref m_ManualPathEditMode, m_ControlPickerState,
                    () =>
                    {
                        m_ManualPathEditMode = false;
                        OnPathModified();
                    });

                DrawUseInControlSchemes();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawCompositeParameters()
        {
            m_GeneralFoldout = DrawFoldout(s_GeneralContent, m_GeneralFoldout);

            EditorGUI.indentLevel++;
            if (m_GeneralFoldout)
            {
                if (m_CompositeParameters == null)
                    InitializeCompositeParameters();

                m_CompositeParameters.OnGUI();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawUseInControlSchemes()
        {
            if (m_Toolbar == null || m_Toolbar.controlSchemes.Count == 0)
                return;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_UseInSchemesGui, EditorStyles.boldLabel);
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
                    OnBindingGroupsModified();
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

        private static bool DrawFoldout(GUIContent content, bool folded)
        {
            var bgRect = GUILayoutUtility.GetRect(s_ProcessorsContent, Styles.foldoutBackgroundStyle);
            EditorGUI.LabelField(bgRect, GUIContent.none, Styles.foldoutBackgroundStyle);
            return EditorGUI.Foldout(bgRect, folded, content, Styles.foldoutStyle);
        }

        private void InitializeCompositeParameters()
        {
            m_CompositeParameters = new ParameterListView
            {
                onChange = OnCompositeParametersModified
            };

            var path = m_PathProperty.stringValue;
            var nameAndParameters = InputControlLayout.ParseNameAndParameters(path);

            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(nameAndParameters.name);
            if (compositeType != null)
                m_CompositeParameters.Initialize(compositeType, nameAndParameters.parameters);
        }

        private void OnCompositeParametersModified()
        {
            Debug.Assert(m_CompositeParameters != null);

            var path = m_PathProperty.stringValue;
            var nameAndParameters = InputControlLayout.ParseNameAndParameters(path);
            nameAndParameters.parameters = m_CompositeParameters.GetParameters();

            m_PathProperty.stringValue = nameAndParameters.ToString();

            OnPathModified();
        }

        private void OnProcessorsModified()
        {
            m_ProcessorsProperty.stringValue = m_ProcessorsList.ToSerializableString();
            m_ProcessorsProperty.serializedObject.ApplyModifiedProperties();
            if (m_OnChange != null)
                m_OnChange(Change.ProcessorsChanged);
        }

        private void OnInteractionsModified()
        {
            m_InteractionsProperty.stringValue = m_InteractionsList.ToSerializableString();
            m_InteractionsProperty.serializedObject.ApplyModifiedProperties();
            if (m_OnChange != null)
                m_OnChange(Change.InteractionsChanged);
        }

        private void OnBindingGroupsModified()
        {
            m_GroupsProperty.stringValue = string.Join(InputBinding.kSeparatorString, m_BindingGroups.ToArray());
            m_GroupsProperty.serializedObject.ApplyModifiedProperties();
            if (m_OnChange != null)
                m_OnChange(Change.GroupsChanged);
        }

        private void OnPathModified()
        {
            m_BindingProperty.serializedObject.ApplyModifiedProperties();
            if (m_OnChange != null)
                m_OnChange(Change.PathChanged);
        }

        public enum Change
        {
            PathChanged,
            GroupsChanged,
            InteractionsChanged,
            ProcessorsChanged,
        }
    }
}
#endif // UNITY_EDITOR
