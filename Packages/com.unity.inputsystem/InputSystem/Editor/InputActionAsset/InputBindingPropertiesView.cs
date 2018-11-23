#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Editor.Lists;
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

        private SerializedProperty m_InteractionsProperty;
        private InteractionsReorderableReorderableList m_InteractionsReorderableReorderableList;

        private ProcessorsReorderableReorderableList m_ProcessorsReorderableReorderableListView;
        private SerializedProperty m_ProcessorsProperty;

        private SerializedProperty m_GroupsProperty;

        private SerializedProperty m_BindingProperty;

        private Action m_ReloadTree;
        ////REVIEW: when we start with a blank tree view state, we should initialize the control picker to select the control currently
        ////        selected by the path property
        private TreeViewState m_ControlPickerTreeViewState;
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

        public bool showPathAndControlSchemeSection { get; set; }

        public bool isInteractivelyPicking
        {
            get { return m_RebindingOperation != null && m_RebindingOperation.started; }
        }

        public string expectedControlLayout
        {
            get { return m_ExpectedControlLayout; }
        }

        public InputBindingPropertiesView(SerializedProperty bindingProperty, Action reloadTree,
                                          TreeViewState controlPickerTreeViewState, InputActionWindowToolbar toolbar, string expectedControlLayout = null)
        {
            m_ControlPickerTreeViewState = controlPickerTreeViewState;
            m_BindingProperty = bindingProperty;
            m_ReloadTree = reloadTree;
            m_InteractionsProperty = bindingProperty.FindPropertyRelative("m_Interactions");
            m_ProcessorsProperty = bindingProperty.FindPropertyRelative("m_Processors");
            m_GroupsProperty = bindingProperty.FindPropertyRelative("m_Groups");
            m_InteractionsReorderableReorderableList = new InteractionsReorderableReorderableList(m_InteractionsProperty, ApplyModifiers);
            m_ProcessorsReorderableReorderableListView = new ProcessorsReorderableReorderableList(m_ProcessorsProperty, ApplyModifiers);
            m_Toolbar = toolbar;
            if (m_Toolbar != null)
                m_ControlSchemes = toolbar.controlSchemes;
            m_BindingGroups = m_GroupsProperty.stringValue.Split(InputBinding.kSeparator).ToList();
            m_ExpectedControlLayout = expectedControlLayout;
            showPathAndControlSchemeSection = true;
        }

        private void ApplyModifiers()
        {
            m_InteractionsProperty.stringValue = m_InteractionsReorderableReorderableList.ToSerializableString();
            m_InteractionsProperty.serializedObject.ApplyModifiedProperties();
            m_ProcessorsProperty.stringValue = m_ProcessorsReorderableReorderableListView.ToSerializableString();
            m_ProcessorsProperty.serializedObject.ApplyModifiedProperties();
            m_GroupsProperty.stringValue = string.Join(InputBinding.kSeparatorString, m_BindingGroups.ToArray());
            m_GroupsProperty.serializedObject.ApplyModifiedProperties();
            m_ReloadTree();
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
            if (showPathAndControlSchemeSection)
                DrawPathPicker();
            EditorGUILayout.Space();
            DrawInteractionsPicker();
            EditorGUILayout.Space();
            DrawProcessorsPicker();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawProcessorsPicker()
        {
            m_ProcessorsFoldout = DrawFoldout(s_ProcessorsContent, m_ProcessorsFoldout);

            if (m_ProcessorsFoldout)
            {
                EditorGUI.indentLevel++;
                m_ProcessorsReorderableReorderableListView.OnGUI();
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void DrawInteractionsPicker()
        {
            m_InteractionsFoldout = DrawFoldout(s_InteractionsContent, m_InteractionsFoldout);

            if (m_InteractionsFoldout)
            {
                EditorGUI.indentLevel++;
                m_InteractionsReorderableReorderableList.OnGUI();
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void DrawPathPicker()
        {
            m_GeneralFoldout = DrawFoldout(s_GeneralContent, m_GeneralFoldout);

            EditorGUI.indentLevel++;
            if (m_GeneralFoldout)
            {
                var pathProperty = m_BindingProperty.FindPropertyRelative("m_Path");
                DrawBindingGUI(pathProperty, ref m_ManualPathEditMode, m_ControlPickerTreeViewState,
                    s =>
                    {
                        m_ManualPathEditMode = false;
                        OnBindingModified(s);
                    });

                DrawUseInControlSchemes();
            }
            EditorGUI.indentLevel--;
        }

        protected virtual void DrawUseInControlSchemes()
        {
            if (m_Toolbar == null)
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
                    ApplyModifiers();
                }
            }
            EditorGUILayout.EndVertical();
        }

        ////TODO: interactive picker; if more than one control makes it through the filters, present list of
        ////      candidates for user to choose from

        ////REVIEW: refactor this out of here; this should be a public API that allows anyone to have an inspector field to select a control binding
        internal void DrawBindingGUI(SerializedProperty pathProperty, ref bool manualPathEditMode, TreeViewState pickerTreeViewState, Action<SerializedProperty> onModified)
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
                    onModified(pathProperty);
                }
                DrawInteractivePickButton(interactivePickButtonRect, pathProperty, onModified);
                if (GUI.Button(editButtonRect, "˅"))
                {
                    bindingTextRect.x += editButtonRect.width;
                    ShowInputControlPicker(bindingTextRect, pathProperty, pickerTreeViewState, onModified);
                }
            }
            else
            {
                // Dropdown that shows binding text and allows opening control picker.
                if (EditorGUI.DropdownButton(bindingTextRect, new GUIContent(displayName), FocusType.Keyboard))
                {
                    ////TODO: pass expectedControlLayout filter on to control picker
                    ////TODO: for bindings that are part of composites, use the layout information from the [InputControl] attribute on the field
                    ShowInputControlPicker(bindingTextRect, pathProperty, pickerTreeViewState, onModified);
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

        private void DrawInteractivePickButton(Rect rect, SerializedProperty pathProperty, Action<SerializedProperty> onModified)
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
                            onModified(pathProperty);
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

        private void ShowInputControlPicker(Rect rect, SerializedProperty pathProperty, TreeViewState pickerTreeViewState,
            Action<SerializedProperty> onPickCallback)
        {
            var w = new InputControlPickerPopup(pathProperty, pickerTreeViewState)
            {
                onPickCallback = onPickCallback,
                width = rect.width,
            };
            if (m_Toolbar != null)
            {
                if (m_Toolbar.selectedDevice != null)
                {
                    w.SetDeviceFilter(new[] {m_Toolbar.selectedDevice});
                }
                else
                {
                    w.SetDeviceFilter(m_Toolbar.allDevices);
                }
            }
            PopupWindow.Show(rect, w);
        }

        private static bool DrawFoldout(GUIContent content, bool folded)
        {
            var bgRect = GUILayoutUtility.GetRect(s_ProcessorsContent, Styles.foldoutBackgroundStyle);
            EditorGUI.LabelField(bgRect, GUIContent.none, Styles.foldoutBackgroundStyle);
            return EditorGUI.Foldout(bgRect, folded, content, Styles.foldoutStyle);
        }

        ////FIXME: seems to nuke the property view on the right side every time a path is selected
        private void OnBindingModified(SerializedProperty obj)
        {
            m_ReloadTree();
        }
    }
}
#endif // UNITY_EDITOR
