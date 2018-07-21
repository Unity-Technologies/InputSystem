#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

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
        InteractionsList m_InteractionsList;

        ProcessorsList m_ProcessorsListView;
        SerializedProperty m_ProcessorsProperty;

        SerializedProperty m_BindingProperty;
        Action m_ReloadTree;
        TreeViewState m_TreeViewState;
        bool m_GeneralFoldout = true;
        bool m_InteractionsFoldout = true;
        bool m_ProcessorsFoldout = true;

        GUIContent m_ProcessorsContent = EditorGUIUtility.TrTextContent("Processors");
        GUIContent m_InteractionsContent = EditorGUIUtility.TrTextContent("Interactions");
        GUIContent m_GeneralContent = EditorGUIUtility.TrTextContent("General");
        GUIContent m_BindingGUI = EditorGUIUtility.TrTextContent("Binding");
        bool m_ManualEditMode;

        public InputBindingPropertiesView(SerializedProperty bindingProperty, Action reloadTree, ref TreeViewState treeViewState)
        {
            m_TreeViewState = treeViewState;
            m_BindingProperty = bindingProperty;
            m_ReloadTree = reloadTree;
            m_InteractionsProperty = bindingProperty.FindPropertyRelative("interactions");
            m_ProcessorsProperty = bindingProperty.FindPropertyRelative("processors");
            m_InteractionsList = new InteractionsList(bindingProperty.FindPropertyRelative("interactions"), ApplyModifiers);
            m_ProcessorsListView = new ProcessorsList(bindingProperty.FindPropertyRelative("processors"), ApplyModifiers);
        }

        void ApplyModifiers()
        {
            m_InteractionsProperty.stringValue = m_InteractionsList.ToSerializableString();
            m_InteractionsProperty.serializedObject.ApplyModifiedProperties();
            m_ProcessorsProperty.stringValue = m_ProcessorsListView.ToSerializableString();
            m_ProcessorsProperty.serializedObject.ApplyModifiedProperties();
            m_ReloadTree();
        }

        public void OnGUI()
        {
            if (m_BindingProperty == null)
                return;

            EditorGUILayout.BeginVertical();

            m_GeneralFoldout = DrawFoldout(m_GeneralContent, m_GeneralFoldout);

            if (m_GeneralFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();

                var lineRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                var labelRect = lineRect;
                labelRect.width = 60;
                EditorGUI.LabelField(labelRect, m_BindingGUI);
                lineRect.x += 65;
                lineRect.width -= 65;

                var btnRect = lineRect;
                var editBtn = lineRect;
                btnRect.width -= 20;
                editBtn.x += btnRect.width;
                editBtn.width = 20;
                editBtn.height = 15;

                var pathProperty = m_BindingProperty.FindPropertyRelative("path");
                DrawBindingField(btnRect, editBtn, pathProperty);

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            m_InteractionsFoldout = DrawFoldout(m_InteractionsContent, m_InteractionsFoldout);

            if (m_InteractionsFoldout)
            {
                EditorGUI.indentLevel++;
                m_InteractionsList.OnGUI();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            m_ProcessorsFoldout = DrawFoldout(m_ProcessorsContent, m_ProcessorsFoldout);

            if (m_ProcessorsFoldout)
            {
                EditorGUI.indentLevel++;
                m_ProcessorsListView.OnGUI();
                EditorGUI.indentLevel--;
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();
        }

        void DrawBindingField(Rect rect, Rect editBtn, SerializedProperty pathProperty)
        {
            var path = pathProperty.stringValue;

            if (m_ManualEditMode || string.IsNullOrEmpty(BindingTreeItem.ParseName(path)))
            {
                EditorGUI.BeginChangeCheck();
                path = EditorGUI.DelayedTextField(rect, path);
                if (EditorGUI.EndChangeCheck())
                {
                    pathProperty.stringValue = path;
                    OnBindingModified(pathProperty);
                }
                if (GUI.Button(editBtn, "˅"))
                {
                    rect.x += editBtn.width;
                    ShowInputControlPicker(rect, pathProperty);
                }
            }
            else
            {
                var parsedPath = BindingTreeItem.ParseName(path);
                if (EditorGUI.DropdownButton(rect, new GUIContent(parsedPath), FocusType.Keyboard))
                {
                    ShowInputControlPicker(rect, pathProperty);
                }
                if (GUI.Button(editBtn, "..."))
                {
                    m_ManualEditMode = true;
                }
            }
        }

        void ShowInputControlPicker(Rect rect, SerializedProperty pathProperty)
        {
            var w = new InputControlPicker(pathProperty, ref m_TreeViewState)
            {
                onPickCallback = s =>
                {
                    m_ManualEditMode = false;
                    OnBindingModified(s);
                }
            };
            w.width = rect.width;
            PopupWindow.Show(rect, w);
        }

        bool DrawFoldout(GUIContent content, bool folded)
        {
            var bgRect = GUILayoutUtility.GetRect(m_ProcessorsContent, Styles.foldoutBackgroundStyle);
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
}
#endif // UNITY_EDITOR
