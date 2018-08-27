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
            var root = new TreeViewItem
            {
                id = 0,
                depth = -1
            };
            root.children = new List<TreeViewItem>();
            if (m_ActionMapSerializedProperty != null)
            {
                BuildFromActionMapSerializedProperty(root);
            }
            else if (m_ActionSerializedProperty != null)
            {
                BuildFromActionSerializedProperty(root);
            }
            return root;
        }

        protected override void ContextClicked()
        {
            if (m_ActionSerializedProperty != null)
            {
                OnContextClick(m_ActionSerializedProperty);
            }
        }

        void BuildFromActionSerializedProperty(TreeViewItem root)
        {
            var bindingsArrayProperty = m_ActionSerializedProperty.FindPropertyRelative("m_SingletonActionBindings");
            var actionName = m_ActionSerializedProperty.FindPropertyRelative("m_Name").stringValue;
            ParseBindings(root, "", actionName, bindingsArrayProperty, 0);
        }

        void BuildFromActionMapSerializedProperty(TreeViewItem root)
        {
            ParseActionMap(root, m_ActionMapSerializedProperty, 0);
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is InputTreeViewLine)
            {
                var bindingItem = (args.item as InputTreeViewLine);

                // We try to predict the indentation
                var indent = (args.item.depth + 2) * 6 + 10;
                bindingItem.OnGUI(args.rowRect, args.selected, args.focused, indent);

                var btnRect = args.rowRect;
                btnRect.x = btnRect.width - 20;
                btnRect.width = 20;

                if (!bindingItem.hasProperties)
                    return;

                if (GUI.Button(btnRect, "..."))
                {
                    var screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(btnRect.x, btnRect.y));
                    btnRect.x = screenPoint.x;
                    btnRect.y = screenPoint.y;
                    BindingPropertiesPopup.Show(btnRect, bindingItem, Reload);
                }
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var element = (InputTreeViewLine)FindItem(id, rootItem);
            var rect = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), Vector2.zero);
            BindingPropertiesPopup.Show(rect, element, Reload);
        }
    }
}
#endif // UNITY_EDITOR
