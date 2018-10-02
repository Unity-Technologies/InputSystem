#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    class InputActionComponentListTreeView : InputActionListTreeView
    {
        SerializedProperty m_ActionMapSerializedProperty;
        SerializedProperty m_ActionSerializedProperty;

        protected InputActionComponentListTreeView(Action applyAction, TreeViewState state)
            : base(applyAction, state) {}

        public static InputActionListTreeView CreateFromActionProperty(Action applyAction, SerializedProperty actionProperty)
        {
            var treeView = new InputActionComponentListTreeView(applyAction, new TreeViewState());
            treeView.m_ActionSerializedProperty = actionProperty;
            treeView.Reload();
            treeView.ExpandAll();
            return treeView;
        }

        public static InputActionListTreeView CreateFromActionMapProperty(Action applyAction, SerializedProperty actionMapProperty)
        {
            var treeView = new InputActionComponentListTreeView(applyAction, new TreeViewState());
            treeView.m_ActionMapSerializedProperty = actionMapProperty;
            treeView.Reload();
            treeView.ExpandAll();
            return treeView;
        }

        protected override TreeViewItem BuildRoot()
        {
            if (m_ActionMapSerializedProperty != null)
            {
                return BuildFromActionMapSerializedProperty();
            }
            if (m_ActionSerializedProperty != null)
            {
                return BuildFromActionSerializedProperty();
            }
            var dummyRoot = new TreeViewItem();
            dummyRoot.children = new List<TreeViewItem>();
            dummyRoot.depth = -1;
            return dummyRoot;
        }

        protected override void ContextClicked()
        {
            if (OnContextClick == null)
                return;
            if (m_ActionSerializedProperty != null)
            {
                OnContextClick(m_ActionSerializedProperty);
            }
            else
            {
                OnContextClick(m_ActionMapSerializedProperty);
            }
        }

        TreeViewItem BuildFromActionSerializedProperty()
        {
            var bindingsArrayProperty = m_ActionSerializedProperty.FindPropertyRelative("m_SingletonActionBindings");
            var actionName = m_ActionSerializedProperty.FindPropertyRelative("m_Name").stringValue;
            var actionItem = new ActionTreeItem(null, m_ActionSerializedProperty, 0);
            actionItem.children = new List<TreeViewItem>();
            actionItem.depth = -1;
            ParseBindings(actionItem, "", actionName, bindingsArrayProperty, 0);
            return actionItem;
        }

        TreeViewItem BuildFromActionMapSerializedProperty()
        {
            var actionMapItem = new ActionMapTreeItem(m_ActionMapSerializedProperty, 0);
            actionMapItem.depth = -1;
            actionMapItem.children = new List<TreeViewItem>();
            ParseActionMap(actionMapItem, m_ActionMapSerializedProperty, 0);
            return actionMapItem;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return false;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return false;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as ActionTreeViewItem;
            if (item == null)
                return;

            item.OnGUI(args.rowRect, args.selected, args.focused);

            var btnRect = args.rowRect;
            btnRect.x = btnRect.width - 20;
            btnRect.width = 20;
        }

        protected override void DoubleClickedItem(int id)
        {
            var element = (ActionTreeViewItem)FindItem(id, rootItem);
            var rect = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), Vector2.zero);
            BindingPropertiesPopup.Show(rect, element, Reload);
        }
    }
}
#endif // UNITY_EDITOR
