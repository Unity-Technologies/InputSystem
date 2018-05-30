#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

////REVIEW: Ideally we'd have a view that can be switched dynamically between going by action and going by device/controls.
////        The second view would ideally look something like Steam's binding overlay where you see a graphical representation
////        of the device and can just assign actions. This would also make it much easier to deal with controls that act as
////        as interactions; this could simply be displayed as layers of actions on the controller.
////        Also, ideally the InputActionAsset importer inspector stuff would also allow creating binding overrides from the UI
////        such that you can create multiple binding profiles and each gets stored on disk as well (maybe in the same asset?)

////FIXME: ATM there is a bug in the ScriptedImporter feature where if you edit the .inputactions asset outside of Unity,
////       it will correctly re-import but the InputActionAssetEditor will not refresh

////FIXME: undo when editing bindings does not work properly

namespace UnityEngine.Experimental.Input.Editor
{
    // Custom inspector that allows modifying action sets in InputActionAssets.
    [CustomEditor(typeof(InputActionAsset))]
    public class InputActionAssetEditor : UnityEditor.Editor
    {
        [NonSerialized] private int m_ActionMapCount;
        [NonSerialized] private SerializedProperty m_ActionMapArrayProperty;
        [NonSerialized] internal Action m_ApplyAction;

        private static List<InputActionAssetEditor> s_EnabledEditors;

        internal static InputActionAssetEditor FindFor(InputActionAsset asset)
        {
            if (s_EnabledEditors != null)
            {
                foreach (var editor in s_EnabledEditors)
                    if (editor.target == asset)
                        return editor;
            }
            return null;
        }

        public void OnEnable()
        {
            m_ActionMapArrayProperty = serializedObject.FindProperty("m_ActionMaps");
            m_ActionMapCount = m_ActionMapArrayProperty.arraySize;

            if (m_ActionMapCount > 0)
                InitializeActionTreeView();

            if (s_EnabledEditors == null)
                s_EnabledEditors = new List<InputActionAssetEditor>();
            s_EnabledEditors.Add(this);
        }

        public void OnDisable()
        {
            if (s_EnabledEditors != null)
                s_EnabledEditors.Remove(this);
        }

        public void Reload()
        {
            serializedObject.Update();
            if (m_ActionTreeView != null)
                m_ActionTreeView.Reload();
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
            if (m_ActionTreeView != null)
                m_ActionTreeView.OnGUI(treeViewRect);
        }

        protected void DrawToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            ////REVIEW: should this work the same as adding actions and just have an "<Add Action Map...>" entry?
            if (GUILayout.Button(Contents.addNewMap, EditorStyles.toolbarButton))
                AddActionSet();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        protected void AddActionSet()
        {
            InputActionSerializationHelpers.AddActionMap(serializedObject);
            ++m_ActionMapCount;

            Apply();

            if (m_ActionTreeView == null)
                InitializeActionTreeView();
            else
                m_ActionTreeView.Reload();
        }

        private void InitializeActionTreeView()
        {
            m_ActionTreeView = InputActionTreeView.Create(serializedObject.FindProperty("m_ActionMaps"), Apply,
                    ref m_ActionTreeViewState, ref m_ActionTreeViewHeaderState);
        }

        private void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            if (m_ApplyAction != null)
                m_ApplyAction.Invoke();
        }

        [SerializeField] private TreeViewState m_ActionTreeViewState;
        [SerializeField] private MultiColumnHeaderState m_ActionTreeViewHeaderState;

        [NonSerialized] private InputActionTreeView m_ActionTreeView;

        private static class Contents
        {
            public static GUIContent addNewMap = new GUIContent("Add New Action Map");
        }
    }
}
#endif // UNITY_EDITOR
