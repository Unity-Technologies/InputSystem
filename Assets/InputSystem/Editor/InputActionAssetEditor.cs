#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ISX.Editor
{
    // Custom inspector that allows modifying action sets in InputActionAssets.
    [CustomEditor(typeof(InputActionAsset))]
    public class InputActionAssetEditor : UnityEditor.Editor
    {
        [NonSerialized] private int m_ActionSetCount;
        [NonSerialized] private SerializedProperty m_ActionSetProperty;

        public void OnEnable()
        {
            m_ActionSetProperty = serializedObject.FindProperty("m_ActionSets");
            m_ActionSetCount = m_ActionSetProperty.arraySize;

            if (m_ActionSetCount > 0)
                InitializeActionTreeView();
        }

        protected override void OnHeaderGUI()
        {
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        public override void OnInspectorGUI()
        {
            //one set after the other
            //can add and remove sets (reorder too?)
            //each set shows list of actions
            //bindings can be filtered by their group
            //new groups can be added

            // Toolbar.
            DrawToolbarGUI();

            // Action tree view.
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            var treeViewRect = GUILayoutUtility.GetLastRect();
            m_ActionTreeView?.OnGUI(treeViewRect);
        }

        protected void DrawToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(Contents.addNewSet, EditorStyles.toolbarButton))
                AddActionSet();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        protected void AddActionSet()
        {
            var index = m_ActionSetCount;
            ////FIXME: duplicates the last action set which is annoying; make it produce a clean action set with nothing in it
            m_ActionSetProperty.InsertArrayElementAtIndex(index);
            ++m_ActionSetCount;

            ////TODO: assign unique name
            var name = "default";
            var nameProperty = m_ActionSetProperty.GetArrayElementAtIndex(index).FindPropertyRelative("m_Name");
            nameProperty.stringValue = name;

            serializedObject.ApplyModifiedProperties();

            if (m_ActionTreeView == null)
                InitializeActionTreeView();
            else
                m_ActionTreeView.Reload();
        }

        private void InitializeActionTreeView()
        {
            m_ActionTreeView = InputActionTreeView.Create(serializedObject.FindProperty("m_ActionSets"),
                    ref m_ActionTreeViewState, ref m_ActionTreeViewHeaderState);
        }

        [SerializeField] private TreeViewState m_ActionTreeViewState;
        [SerializeField] private MultiColumnHeaderState m_ActionTreeViewHeaderState;

        [NonSerialized] private TreeView m_ActionTreeView;

        private static class Styles
        {
            public static GUIStyle box = "Box";
        }

        private static class Contents
        {
            public static GUIContent addNewSet = new GUIContent("Add New Set");
        }
    }
}
#endif // UNITY_EDITOR
