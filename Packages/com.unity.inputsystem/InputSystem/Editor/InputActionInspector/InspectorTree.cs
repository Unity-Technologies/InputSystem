#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    class InspectorTree : InputActionTreeBase
    {
        private SerializedProperty m_ActionMapSerializedProperty;
        private SerializedProperty m_ActionSerializedProperty;
        private SerializedObject m_SerializedObject;

        public static InspectorTree CreateFromActionProperty(Action applyAction, SerializedProperty actionProperty)
        {
            var treeView = new InspectorTree(applyAction, new TreeViewState());
            treeView.m_ActionSerializedProperty = actionProperty;
            treeView.Reload();
            treeView.ExpandAll();
            return treeView;
        }

        public static InspectorTree CreateFromActionMapProperty(Action applyAction, SerializedProperty actionMapProperty)
        {
            var treeView = new InspectorTree(applyAction, new TreeViewState());
            treeView.m_ActionMapSerializedProperty = actionMapProperty;
            treeView.Reload();
            treeView.ExpandAll();
            return treeView;
        }

        static bool OnFoldoutDraw(Rect position, bool expandedState, GUIStyle style)
        {
            var indent = (int)(position.x / 15);
            position.x = 6 * indent + 8;
            return EditorGUI.Foldout(position, expandedState, GUIContent.none, style);
        }

        protected InspectorTree(Action applyAction, TreeViewState state)
            : base(applyAction, state)
        {
            ////REVIEW: good enough like this for 2018.2?
            #if UNITY_2018_3_OR_NEWER
            foldoutOverride += OnFoldoutDraw;
            #endif
            Reload();
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

        protected void ParseActionMap(TreeViewItem parentTreeItem, SerializedProperty actionMapProperty, int depth)
        {
            var actionsArrayProperty = actionMapProperty.FindPropertyRelative("m_Actions");
            for (var i = 0; i < actionsArrayProperty.arraySize; i++)
            {
                ParseAction(parentTreeItem, actionMapProperty, actionsArrayProperty, i, depth);
            }
        }

        private void ParseAction(TreeViewItem parentTreeItem, SerializedProperty actionMapProperty, SerializedProperty actionsArrayProperty, int index, int depth)
        {
            var bindingsArrayProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
            var actionMapName = actionMapProperty.FindPropertyRelative("m_Name").stringValue;
            var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(index);

            var actionItem = new ActionTreeItem(actionMapProperty, actionProperty, index);
            actionItem.depth = depth;
            var actionName = actionItem.actionName;

            ParseBindings(actionItem, actionMapName, actionName, bindingsArrayProperty, depth + 1);
            parentTreeItem.AddChild(actionItem);
        }

        protected void ParseBindings(TreeViewItem parent, string actionMapName, string actionName, SerializedProperty bindingsArrayProperty, int depth)
        {
            var bindingsCount = InputActionSerializationHelpers.GetBindingCount(bindingsArrayProperty, actionName);
            CompositeGroupTreeItem compositeGroupTreeItem = null;
            for (var j = 0; j < bindingsCount; j++)
            {
                var bindingProperty = InputActionSerializationHelpers.GetBinding(bindingsArrayProperty, actionName, j);
                var bindingsItem = new BindingTreeItem(actionMapName, bindingProperty, j);
                bindingsItem.depth = depth;
                if (bindingsItem.isComposite)
                {
                    compositeGroupTreeItem = new CompositeGroupTreeItem(actionMapName, bindingProperty, j);
                    compositeGroupTreeItem.depth = depth;
                    parent.AddChild(compositeGroupTreeItem);
                    continue;
                }
                if (bindingsItem.isPartOfComposite)
                {
                    var compositeItem = new CompositeTreeItem(actionMapName, bindingProperty, j);
                    compositeItem.depth = depth + 1;
                    if (compositeGroupTreeItem != null)
                        compositeGroupTreeItem.AddChild(compositeItem);
                    continue;
                }
                compositeGroupTreeItem = null;
                parent.AddChild(bindingsItem);
            }
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

            var bindingItem = (args.item as ActionTreeViewItem);

            // We try to predict the indentation
            var indent = (args.item.depth + 2) * 6 + 10;
            bindingItem.OnGUI(args.rowRect, args.selected, args.focused, indent);

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
