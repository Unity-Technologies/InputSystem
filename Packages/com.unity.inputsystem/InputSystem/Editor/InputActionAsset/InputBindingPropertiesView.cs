#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Editor.Lists;

namespace UnityEngine.Experimental.Input.Editor
{
    class InputBindingPropertiesView
    {
        static class Styles
        {
            public static GUIStyle foldoutBackgroundStyle = new GUIStyle("Label");
            public static GUIStyle foldoutStyle = new GUIStyle("foldout");

            static string ResourcesPath
            {
                get
                {
                    var path = "Packages/com.unity.inputsystem/InputSystem/Editor/InputActionAsset/Resources/";
                    if (EditorGUIUtility.isProSkin)
                        return path + "pro/";
                    return path + "personal/";
                }
            }

            static Styles()
            {
                var darkGreyBackgroundWithBorderTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "foldoutBackground.png");
                foldoutBackgroundStyle.normal.background = darkGreyBackgroundWithBorderTexture;
                foldoutBackgroundStyle.border = new RectOffset(3, 3, 3, 3);
                foldoutBackgroundStyle.margin = new RectOffset(1, 1, 3, 3);
            }
        }

        SerializedProperty m_InteractionsProperty;
        InteractionsReorderableReorderableList m_InteractionsReorderableReorderableList;

        ProcessorsReorderableReorderableList m_ProcessorsReorderableReorderableListView;
        SerializedProperty m_ProcessorsProperty;

        SerializedProperty m_BindingProperty;
        Action m_ReloadTree;
        ////REVIEW: when we start with a blank tree view state, we should initialize the control picker to select the control currently
        ////        selected by the path property
        TreeViewState m_ControlPickerTreeViewState;
        bool m_GeneralFoldout = true;
        bool m_InteractionsFoldout = true;
        bool m_ProcessorsFoldout = true;

        static GUIContent s_ProcessorsContent = EditorGUIUtility.TrTextContent("Processors");
        static GUIContent s_InteractionsContent = EditorGUIUtility.TrTextContent("Interactions");
        static GUIContent s_GeneralContent = EditorGUIUtility.TrTextContent("General");
        static GUIContent s_BindingGUI = EditorGUIUtility.TrTextContent("Binding");

        bool m_ManualPathEditMode;

        public InputBindingPropertiesView(SerializedProperty bindingProperty, Action reloadTree, TreeViewState controlPickerTreeViewState)
        {
            m_ControlPickerTreeViewState = controlPickerTreeViewState;
            m_BindingProperty = bindingProperty;
            m_ReloadTree = reloadTree;
            m_InteractionsProperty = bindingProperty.FindPropertyRelative("interactions");
            m_ProcessorsProperty = bindingProperty.FindPropertyRelative("processors");
            m_InteractionsReorderableReorderableList = new InteractionsReorderableReorderableList(bindingProperty.FindPropertyRelative("interactions"), ApplyModifiers);
            m_ProcessorsReorderableReorderableListView = new ProcessorsReorderableReorderableList(bindingProperty.FindPropertyRelative("processors"), ApplyModifiers);
        }

        void ApplyModifiers()
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

                var pathProperty = m_BindingProperty.FindPropertyRelative("path");
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
        internal static void DrawBindingGUI(SerializedProperty pathProperty, ref bool manualPathEditMode, TreeViewState pickerTreeViewState, Action<SerializedProperty> onModified)
        {
            EditorGUILayout.BeginHorizontal();

            var lineRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            var labelRect = lineRect;
            labelRect.width = 60;
            EditorGUI.LabelField(labelRect, s_BindingGUI);
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

        static void ShowInputControlPicker(Rect rect, SerializedProperty pathProperty, TreeViewState pickerTreeViewState,
            Action<SerializedProperty> onPickCallback)
        {
            var w = new InputControlPicker(pathProperty, pickerTreeViewState)
            {
                onPickCallback = onPickCallback
            };
            w.width = rect.width;
            PopupWindow.Show(rect, w);
        }

        static bool DrawFoldout(GUIContent content, bool folded)
        {
            var bgRect = GUILayoutUtility.GetRect(s_ProcessorsContent, Styles.foldoutBackgroundStyle);
            EditorGUI.LabelField(bgRect, GUIContent.none, Styles.foldoutBackgroundStyle);
            return EditorGUI.Foldout(bgRect, folded, content, Styles.foldoutStyle);
        }

        void OnBindingModified(SerializedProperty obj)
        {
            var importerEditor = InputActionImporterEditor.FindFor(m_BindingProperty.serializedObject);
            if (importerEditor != null)
                importerEditor.OnAssetModified();
            m_ReloadTree();
        }
    }

    class CompositeGroupPropertiesView : InputBindingPropertiesView
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
