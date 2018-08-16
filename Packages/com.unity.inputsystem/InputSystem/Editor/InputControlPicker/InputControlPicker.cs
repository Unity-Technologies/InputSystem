#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor.InputControlPicker
{
    // Popup window that allows selecting controls to target in a binding. Will generate
    // a path string as a result and store it in the given "path" property.
    //
    // At the moment, the interface is pretty simplistic. You can either select a usage
    // or select a specific control on a specific base device layout.
    //
    // Usages are discovered from all layouts that are registered with the system.
    public class InputControlPickerPopup : PopupWindowContent
    {
        public Action<SerializedProperty> onPickCallback;
        public float width;
        internal SearchField m_SearchField;

        SerializedProperty m_PathProperty;
        InputControlTree m_PathTree;
        TreeViewState m_PathTreeState;
        bool m_FirstRenderCompleted;

        public InputControlPickerPopup(SerializedProperty pathProperty, TreeViewState treeViewState = null)
        {
            if (pathProperty == null)
                throw new ArgumentNullException("pathProperty");
            m_PathProperty = pathProperty;
            m_PathTreeState = treeViewState ?? new TreeViewState();

            m_SearchField = new SearchField();
            m_SearchField.SetFocus();
            m_SearchField.downOrUpArrowKeyPressed += OnDownOrUpArrowKeyPressed;
        }

        void OnDownOrUpArrowKeyPressed()
        {
            m_PathTree.SetFocusAndEnsureSelectedItem();
        }

        public override Vector2 GetWindowSize()
        {
            var s = base.GetWindowSize();
            if (width > s.x)
                s.x = width;
            return s;
        }

        public override void OnGUI(Rect rect)
        {
            if (m_PathTree == null)
            {
                m_PathTree = new InputControlTree(m_PathTreeState, this, OnSelected);
            }

            DrawToolbar();

            var toolbarRect = GUILayoutUtility.GetLastRect();
            var listRect = new Rect(rect.x, rect.y + toolbarRect.height, rect.width, rect.height - toolbarRect.height);

            m_PathTree.OnGUI(listRect);
            m_FirstRenderCompleted = true;
        }

        void OnSelected(string path)
        {
            if (path != null)
            {
                m_PathProperty.stringValue = path;
                m_PathProperty.serializedObject.ApplyModifiedProperties();

                if (onPickCallback != null)
                    onPickCallback(m_PathProperty);
            }
            editorWindow.Close();
        }

        void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Controls", GUILayout.MinWidth(75), GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            var searchRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.toolbarSearchField, GUILayout.MinWidth(70));
            m_PathTree.searchString = m_SearchField.OnToolbarGUI(searchRect, m_PathTree.searchString);
            GUILayout.EndHorizontal();
        }

        static class Styles
        {
            public static GUIStyle toolbarSearchField = new GUIStyle("ToolbarSeachTextField");
        }
    }
}

#endif // UNITY_EDITOR