using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

//probably something like...
//  one dimension is usage (may want to bring back InputUsage)
//  the other dimension is template
//  the third one is custom where the user can just enter a string

namespace ISX
{
    // Popup window that allows selecting controls to target in a binding. Will generate
    // a path string as a result and store it in the given "path" property.
    //
    // At the moment, the interface is pretty simplistic. You can either select a usage
    // or select a specific control on a specific template.
    //
    // Usages are discovered from all templates that are registered with the system.
    public class InputBindingPathSelector : PopupWindowContent
    {
        private string m_SearchString;
        private SerializedProperty m_PathProperty;

        public InputBindingPathSelector(SerializedProperty pathProperty)
        {
            if (pathProperty == null)
                throw new ArgumentNullException(nameof(pathProperty));
            m_PathProperty = pathProperty;

            m_PathTreeState = new TreeViewState();
            m_PathTree = new PathTreeView(m_PathTreeState);
        }

        public override void OnGUI(Rect rect)
        {
            DrawToolbar();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Controls", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));

            var searchRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.toolbarSearchField, GUILayout.MinWidth(80));
            m_SearchString = EditorGUI.TextField(searchRect, m_SearchString, Styles.toolbarSearchField);
            if (GUILayout.Button(
                    GUIContent.none,
                    m_SearchString == string.Empty ? Styles.toolbarSearchFieldCancelEmpty : Styles.toolbarSearchFieldCancel))
            {
                m_SearchString = string.Empty;
                EditorGUIUtility.keyboardControl = 0;
            }

            GUILayout.EndHorizontal();
        }

        private PathTreeView m_PathTree;
        private TreeViewState m_PathTreeState;

        private static class Styles
        {
            public static GUIStyle toolbarSearchField = new GUIStyle("ToolbarSeachTextField");
            public static GUIStyle toolbarSearchFieldCancel = new GUIStyle("ToolbarSeachCancelButton");
            public static GUIStyle toolbarSearchFieldCancelEmpty = new GUIStyle("ToolbarSeachCancelButtonEmpty");
        }

        private class PathTreeView : TreeView
        {
            public PathTreeView(TreeViewState state)
                : base(state)
            {
            }

            protected override TreeViewItem BuildRoot()
            {
                throw new NotImplementedException();
            }

            private TreeViewItem BuildItemFromUsage(string usage)
            {
                throw new NotImplementedException();
            }

            private TreeViewItem BuildItemFromControlTemplate(InputTemplate.ControlTemplate template)
            {
                throw new NotImplementedException();
            }
        }
    }
}
