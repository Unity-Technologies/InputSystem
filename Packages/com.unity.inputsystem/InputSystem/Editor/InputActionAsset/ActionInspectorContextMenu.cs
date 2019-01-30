#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    [Serializable]
    internal class ActionInspectorContextMenu
    {
        private static readonly GUIContent m_AddBindingGUI = EditorGUIUtility.TrTextContent("Create Binding");
        private static readonly GUIContent m_AddBindingContextGUI = EditorGUIUtility.TrTextContent("Create/Binding");
        private static readonly GUIContent m_AddActionMapGUI = EditorGUIUtility.TrTextContent("Create Action Map");
        private static readonly GUIContent m_AddActionMapContextGUI = EditorGUIUtility.TrTextContent("Create Action Map");

        private AssetInspectorWindow m_AssetInspectorWindow;
        private InputActionAssetManager m_ActionAssetManager;
        private InputActionWindowToolbar m_Toolbar;

        private ActionsTree m_ActionsTree => m_AssetInspectorWindow.m_ActionsTree;
        private ActionMapsTree m_ActionMapsTree => m_AssetInspectorWindow.m_ActionMapsTree;

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
            var canAddBinding = action != null;
            if (canAddBinding)
            {
                menu.AddItem(isContextMenu ? m_AddBindingContextGUI : m_AddBindingGUI, false, OnAddBinding, action);
            }
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(m_AddBindingGUI);
            }

            var compositePrefix = isContextMenu ? EditorGUIUtility.TrTextContent("Create/") : EditorGUIUtility.TrTextContent("Create ");
            var compositeSuffix = EditorGUIUtility.TrTextContent("Composite");
            if (canAddBinding)
            {
                foreach (var composite in InputBindingComposite.s_Composites.internedNames.Where(x =>
                    !InputBindingComposite.s_Composites.aliases.Contains(x)))
                {
                    var name = ObjectNames.NicifyVariableName(composite);
                    menu.AddItem(new GUIContent(compositePrefix.text + name + " " + compositeSuffix.text), false,
                        OnAddCompositeBinding, new KeyValuePair<TreeViewItem, string>(action, composite));
                }
            }
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(new GUIContent(compositePrefix.text + compositeSuffix.text), false);
            }
        }

        internal void OnAddCompositeBinding(object actionAndComposite)
        {
            var actionLine = ((KeyValuePair<TreeViewItem, string>)actionAndComposite).Key as ActionTreeItem;
            var compositeName = ((KeyValuePair<TreeViewItem, string>)actionAndComposite).Value;
            if (actionLine == null)
                return;
            actionLine.AddCompositeBinding(compositeName, m_Toolbar.selectedControlSchemeBindingGroup);
            m_AssetInspectorWindow.ApplyAndReload();
            m_AssetInspectorWindow.m_ActionsTree.SelectNewBindingRow(actionLine);
        }

        internal void OnAddBinding(object actionLineObj)
        {
            if (!(actionLineObj is ActionTreeItem actionLine))
                return;

            actionLine.AddBinding(m_Toolbar.selectedControlSchemeBindingGroup);
            m_AssetInspectorWindow.ApplyAndReload();
            m_AssetInspectorWindow.m_ActionsTree.SelectNewBindingRow(actionLine);
        }

        public void OnAddAction()
        {
            var actionMapLine = GetSelectedActionMapLine();
            if (actionMapLine == null)
                return;
            actionMapLine.AddAction();
            m_AssetInspectorWindow.ApplyAndReload();
            m_AssetInspectorWindow.m_ActionsTree.SelectNewActionRow();
        }

        public void OnAddActionMap()
        {
            InputActionSerializationHelpers.AddActionMap(m_ActionAssetManager.serializedObject);
            m_AssetInspectorWindow.ApplyAndReload();
            m_AssetInspectorWindow.m_ActionMapsTree.SelectNewActionMapRow();
        }

        private ActionTreeItem GetSelectedActionLine()
        {
            TreeViewItem selectedRow = m_ActionsTree.GetSelectedRow();
            do
            {
                if (selectedRow is ActionTreeItem row)
                    return row;
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
                if (selectedRow is ActionMapTreeItem row)
                    return row;
                selectedRow = selectedRow.parent;
            }
            while (selectedRow.parent != null);

            return null;
        }
    }
}
#endif // UNITY_EDITOR
