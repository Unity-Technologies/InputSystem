#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Editor.Lists;

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

        private bool m_ManualPathEditMode;

        public InputBindingPropertiesView(SerializedProperty bindingProperty, Action reloadTree, TreeViewState controlPickerTreeViewState)
        {
            m_ControlPickerTreeViewState = controlPickerTreeViewState;
            m_BindingProperty = bindingProperty;
            m_ReloadTree = reloadTree;
            m_InteractionsProperty = bindingProperty.FindPropertyRelative("m_Interactions");
            m_ProcessorsProperty = bindingProperty.FindPropertyRelative("m_Processors");
            m_InteractionsReorderableReorderableList = new InteractionsReorderableReorderableList(m_InteractionsProperty, ApplyModifiers);
            m_ProcessorsReorderableReorderableListView = new ProcessorsReorderableReorderableList(m_ProcessorsProperty, ApplyModifiers);
        }

        public InputActionWindowToolbar toolbar { get; set; }

        private void ApplyModifiers()
        {
            m_InteractionsProperty.stringValue = m_InteractionsReorderableReorderableList.ToSerializableString();
            m_InteractionsProperty.serializedObject.ApplyModifiedProperties();
            m_ProcessorsProperty.stringValue = m_ProcessorsReorderableReorderableListView.ToSerializableString();
            m_ProcessorsProperty.serializedObject.ApplyModifiedProperties();
            m_ReloadTree();
        }

        public void OnGUI()
        {
            if (m_BindingProperty == null)
                return;

            EditorGUILayout.BeginVertical();
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

            if (m_GeneralFoldout)
            {
                EditorGUI.indentLevel++;

                var pathProperty = m_BindingProperty.FindPropertyRelative("m_Path");
                DrawBindingGUI(pathProperty, ref m_ManualPathEditMode, m_ControlPickerTreeViewState,
                    s =>
                    {
                        m_ManualPathEditMode = false;
                        OnBindingModified(s);
                    });

                EditorGUI.indentLevel--;
            }
        }

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

            var btnRect = lineRect;
            var editBtn = lineRect;
            btnRect.width -= 20;
            editBtn.x += btnRect.width;
            editBtn.width = 20;
            editBtn.height = 15;

            var path = pathProperty.stringValue;
            ////TODO: this should be cached; generates needless GC churn
            var displayName = InputControlPath.ToHumanReadableString(path);

            if (manualPathEditMode || (!string.IsNullOrEmpty(path) && string.IsNullOrEmpty(displayName)))
            {
                EditorGUI.BeginChangeCheck();
                path = EditorGUI.DelayedTextField(btnRect, path);
                if (EditorGUI.EndChangeCheck())
                {
                    pathProperty.stringValue = path;
                    pathProperty.serializedObject.ApplyModifiedProperties();
                    onModified(pathProperty);
                }
                if (GUI.Button(editBtn, "˅"))
                {
                    btnRect.x += editBtn.width;
                    ShowInputControlPicker(btnRect, pathProperty, pickerTreeViewState, onModified);
                }
            }
            else
            {
                if (EditorGUI.DropdownButton(btnRect, new GUIContent(displayName), FocusType.Keyboard))
                {
                    ShowInputControlPicker(btnRect, pathProperty, pickerTreeViewState, onModified);
                }
                if (GUI.Button(editBtn, "..."))
                {
                    manualPathEditMode = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowInputControlPicker(Rect rect, SerializedProperty pathProperty, TreeViewState pickerTreeViewState,
            Action<SerializedProperty> onPickCallback)
        {
            var w = new InputControlPickerPopup(pathProperty, pickerTreeViewState)
            {
                onPickCallback = onPickCallback,
                width = rect.width,
            };
            if (toolbar != null)
                w.SetDeviceFilter(toolbar.deviceFilter);
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

    internal class CompositeGroupPropertiesView : InputBindingPropertiesView
    {
        public CompositeGroupPropertiesView(SerializedProperty property, Action apply, TreeViewState state)
            : base(property, apply, state)
        {
        }

        protected override void DrawPathPicker()
        {
        }
    }
}
#endif // UNITY_EDITOR
