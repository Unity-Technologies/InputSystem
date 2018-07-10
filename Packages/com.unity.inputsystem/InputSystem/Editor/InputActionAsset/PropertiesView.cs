#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    class PropertiesView
    {
        static class Styles
        {
            public static GUIStyle foldoutBackgroundStyle = new GUIStyle("Label");
            public static GUIStyle foldoutStyle = new GUIStyle("foldout");

            static Styles()
            {
                Initialize();
                EditorApplication.playModeStateChanged += s =>
                    {
                        if (s == PlayModeStateChange.ExitingPlayMode)
                            Initialize();
                    };
            }

            static void Initialize()
            {
                var darkGreyBackgroundWithBorderTexture = StyleHelpers.CreateTextureWithBorder(new Color32(221, 223, 221, 255));
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
        
        GUIContent m_ProcessorsContent = new GUIContent("Processors");
        GUIContent m_InteractionsContent = new GUIContent("Interactions");
        GUIContent m_GeneralContent = new GUIContent("General");

        public PropertiesView(SerializedProperty bindingProperty, Action reloadTree, ref TreeViewState treeViewState)
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

                var pathProperty = m_BindingProperty.FindPropertyRelative("path");
                var path = BindingTreeItem.ParseName(pathProperty.stringValue);

                var btnRect = GUILayoutUtility.GetRect(0, EditorStyles.miniButton.lineHeight);
                btnRect = EditorGUI.IndentedRect(btnRect);
                if (EditorGUI.DropdownButton(btnRect, new GUIContent(path), FocusType.Keyboard))
                {
                    PopupWindow.Show(btnRect,
                        new InputControlPicker(pathProperty, ref m_TreeViewState) { onPickCallback = OnBindingModified });
                }
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
