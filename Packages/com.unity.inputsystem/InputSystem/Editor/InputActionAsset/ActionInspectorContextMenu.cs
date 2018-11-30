#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    [Serializable]
    class ActionInspectorContextMenu
    {
        private static readonly GUIContent m_AddBindingGUI = EditorGUIUtility.TrTextContent("Create Binding");
        private static readonly GUIContent m_AddBindingContextGUI = EditorGUIUtility.TrTextContent("Create/Binding");
        private static readonly GUIContent m_AddActionMapGUI = EditorGUIUtility.TrTextContent("Create Action map");
        private static readonly GUIContent m_AddActionMapContextGUI = EditorGUIUtility.TrTextContent("Create Action map");

        AssetInspectorWindow m_AssetInspectorWindow;
        InputActionAssetManager m_ActionAssetManager;
        InputActionWindowToolbar m_Toolbar;

        ActionsTree m_ActionsTree
        {
            get { return m_AssetInspectorWindow.m_ActionsTree; }
        }

        ActionMapsTree m_ActionMapsTree
        {
            get { return m_AssetInspectorWindow.m_ActionMapsTree; }
        }

        public ActionInspectorContextMenu(AssetInspectorWindow window, InputActionAssetManager assetManager, InputActionWindowToolbar toolbar)
        {
            SetReferences(window, assetManager, toolbar);
        }

        public void SetReferences(AssetInspectorWindow window, InputActionAssetManager assetManager, InputActionWindowToolbar toolbar)
        {
            m_AssetInspectorWindow = window;
            m_ActionAssetManager = assetManager;
            m_Toolbar = toolbar;
        }

        public void OnActionMapContextClick(SerializedProperty property)
        {
            var menu = new GenericMenu();
            AddActionMapOptionsToMenu(menu, true);
            m_AssetInspectorWindow.m_CopyPasteUtility.AddOptionsToMenu(menu);
            menu.ShowAsContext();
        }

        public void OnActionsContextClick(SerializedProperty property)
        {
            var menu = new GenericMenu();
            var actionLine = GetSelectedActionLine();
            AddActionsOptionsToMenu(menu, actionLine, true);
            m_AssetInspectorWindow.m_CopyPasteUtility.AddOptionsToMenu(menu);
            menu.ShowAsContext();
        }

        public void ShowAddActionsMenu(TreeViewItem treeViewItem)
        {
            var menu = new GenericMenu();
            AddActionsOptionsToMenu(menu, treeViewItem, false);
            menu.ShowAsContext();
        }

        private void AddActionMapOptionsToMenu(GenericMenu menu, bool isContextMenu)
        {
            menu.AddItem(isContextMenu ?  m_AddActionMapContextGUI : m_AddActionMapGUI, false, OnAddActionMap);
        }

        private void AddActionsOptionsToMenu(GenericMenu menu, TreeViewItem action, bool isContextMenu)
        {
            bool canAddBinding = action != null;
            if (canAddBinding)
            {
                menu.AddItem(isContextMenu ? m_AddBindingContextGUI : m_AddBindingGUI, false, OnAddBinding, action);
            }
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(m_AddBindingGUI);
            }

            var compositeString = isContextMenu ? EditorGUIUtility.TrTextContent("Create/Composite") : EditorGUIUtility.TrTextContent("Create Composite");
            if (canAddBinding)
            {
                foreach (var composite in InputBindingComposite.s_Composites.names)
                {
                    menu.AddItem(new GUIContent(compositeString.text + " " + composite), false, OnAddCompositeBinding, new object[] {action, composite});
                }
            }
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(new GUIContent(compositeString), false);
            }
        }

        internal void OnAddCompositeBinding(object objs)
        {
            var actionLine = ((object[])objs)[0] as ActionTreeItem;
            var compositeName = ((object[])objs)[1] as string;
            if (actionLine == null)
                return;
            actionLine.AddCompositeBinding(compositeName, m_Toolbar.selectedControlSchemeBindingGroup);
            m_AssetInspectorWindow.Apply();
            m_AssetInspectorWindow.m_ActionsTree.SelectNewBindingRow(actionLine);
        }

        internal void OnAddBinding(object actionLineObj)
        {
            var actionLine = actionLineObj as ActionTreeItem;
            if (actionLine == null)
                return;
            actionLine.AddBinding(m_Toolbar.selectedControlSchemeBindingGroup);
            m_AssetInspectorWindow.Apply();
            m_AssetInspectorWindow.m_ActionsTree.SelectNewBindingRow(actionLine);
        }

        public void OnAddAction()
        {
            var actionMapLine = GetSelectedActionMapLine();
            if (actionMapLine == null)
                return;
            actionMapLine.AddAction();
            m_AssetInspectorWindow.Apply();
            m_AssetInspectorWindow.m_ActionsTree.SelectNewActionRow();
        }

        public void OnAddActionMap()
        {
            InputActionSerializationHelpers.AddActionMap(m_ActionAssetManager.serializedObject);
            m_AssetInspectorWindow.Apply();
            m_AssetInspectorWindow.m_ActionMapsTree.SelectNewActionMapRow();
        }

        private ActionTreeItem GetSelectedActionLine()
        {
            TreeViewItem selectedRow = m_ActionsTree.GetSelectedRow();
            do
            {
                if (selectedRow is ActionTreeItem)
                    return (ActionTreeItem)selectedRow;
                selectedRow = selectedRow.parent;
            }
            while (selectedRow.parent != null);

            return null;
        }

        private ActionMapTreeItem GetSelectedActionMapLine()
        {
            TreeViewItem selectedRow = m_ActionMapsTree.GetSelectedRow();
            if (selectedRow == null)
                return null;
            do
            {
                if (selectedRow is ActionMapTreeItem)
                    return (ActionMapTreeItem)selectedRow;
                selectedRow = selectedRow.parent;
            }
            while (selectedRow.parent != null);

            return null;
        }
    }
}
#endif // UNITY_EDITOR
