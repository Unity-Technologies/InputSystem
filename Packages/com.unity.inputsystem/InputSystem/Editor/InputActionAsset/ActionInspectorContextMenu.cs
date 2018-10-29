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

        ActionsTree m_ActionsTree
        {
            get { return m_AssetInspectorWindow.m_ActionsTree; }
        }

        ActionMapsTree m_ActionMapsTree
        {
            get { return m_AssetInspectorWindow.m_ActionMapsTree; }
        }

        public ActionInspectorContextMenu(AssetInspectorWindow window, InputActionAssetManager assetManager)
        {
            SetReferences(window, assetManager);
        }

        public void SetReferences(AssetInspectorWindow window, InputActionAssetManager assetManager)
        {
            m_AssetInspectorWindow = window;
            m_ActionAssetManager = assetManager;
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

        public void ShowAddActionMapMenu()
        {
            var menu = new GenericMenu();
            AddActionMapOptionsToMenu(menu, false);
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

        private void AddActionsOptionsToMenu(GenericMenu menu, TreeViewItem treeViewItem, bool isContextMenu)
        {
            var hasSelection = m_ActionMapsTree.HasSelection();
            var canAddBinding = false;
            var action = m_ActionsTree.GetSelectedAction();
            if (action != null && hasSelection)
            {
                canAddBinding = true;
            }
            if (canAddBinding)
            {
                menu.AddItem(isContextMenu ? m_AddBindingContextGUI : m_AddBindingGUI, false, OnAddBinding, treeViewItem);
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
                    menu.AddItem(new GUIContent(compositeString.text + " " + composite), false, OnAddCompositeBinding, new object[] {treeViewItem, composite});
                }
            }
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(new GUIContent(compositeString), false);
            }
        }

        private void OnAddCompositeBinding(object objs)
        {
            var actionLine = ((object[])objs)[0] as ActionTreeItem;
            var compositeName = ((object[])objs)[1] as string;
            if (actionLine == null)
                return;
            actionLine.AddCompositeBinding((string)compositeName);
            m_AssetInspectorWindow.Apply();
        }

        private void OnAddBinding(object actionLineObj)
        {
            var actionLine = actionLineObj as ActionTreeItem;
            if (actionLine == null)
                return;
            actionLine.AddBinding();
            m_AssetInspectorWindow.Apply();
        }

        public void OnAddAction()
        {
            var actionMapLine = GetSelectedActionMapLine();
            if (actionMapLine == null)
                return;
            actionMapLine.AddAction();
            m_AssetInspectorWindow.Apply();
        }

        public void OnAddActionMap()
        {
            InputActionSerializationHelpers.AddActionMap(m_ActionAssetManager.serializedObject);
            m_AssetInspectorWindow.Apply();
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
