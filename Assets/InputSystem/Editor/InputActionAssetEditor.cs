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
        [NonSerialized] private SerializedProperty m_ActionSetArrayProperty;
        [NonSerialized] internal Action m_ApplyAction;

        public void OnEnable()
        {
            m_ActionSetArrayProperty = serializedObject.FindProperty("m_ActionSets");
            m_ActionSetCount = m_ActionSetArrayProperty.arraySize;

            if (m_ActionSetCount > 0)
                InitializeActionTreeView();
        }

        public void Reload()
        {
            serializedObject.Update();
            m_ActionTreeView?.Reload();
            Repaint();
        }

        // Disable the header that isn't providing a lot of value for us and somewhat
        // doubles up with the header we already get from the importer itself.
        protected override void OnHeaderGUI()
        {
        }

        // We want all the space we can get.
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
            ////REVIEW: should this work the same as adding actions and just have an "<Add Action Set...>" entry?
            if (GUILayout.Button(Contents.addNewSet, EditorStyles.toolbarButton))
                AddActionSet();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        protected void AddActionSet()
        {
            InputActionSerializationHelpers.AddActionSet(serializedObject);
            ++m_ActionSetCount;

            Apply();

            if (m_ActionTreeView == null)
                InitializeActionTreeView();
            else
                m_ActionTreeView.Reload();
        }

        private void InitializeActionTreeView()
        {
            m_ActionTreeView = InputActionTreeView.Create(serializedObject.FindProperty("m_ActionSets"), Apply,
                    ref m_ActionTreeViewState, ref m_ActionTreeViewHeaderState);
        }

        private void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            m_ApplyAction?.Invoke();
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
