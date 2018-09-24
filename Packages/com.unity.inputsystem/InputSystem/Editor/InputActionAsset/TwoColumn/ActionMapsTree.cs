#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class ActionMapsTree : InputActionTreeBase
    {
        private SerializedObject m_SerializedObject;
        private Action m_ApplyAction;

        public Action OnSelectionChanged;
        public Action<SerializedProperty> OnContextClick;

        public static ActionMapsTree CreateFromSerializedObject(Action applyAction, SerializedObject serializedObject, ref TreeViewState treeViewState)
        {
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();
            }
            var treeView = new ActionMapsTree(applyAction, treeViewState);
            treeView.m_SerializedObject = serializedObject;
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

        protected ActionMapsTree(Action applyAction, TreeViewState state)
            : base(state)
        {
            m_ApplyAction = applyAction;
            ////REVIEW: good enough like this for 2018.2?
            #if UNITY_2018_3_OR_NEWER
            foldoutOverride += OnFoldoutDraw;
            #endif
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                id = 0,
                depth = -1
            };
            root.children = new List<TreeViewItem>();
            if (m_SerializedObject != null)
            {
                m_SerializedObject.Update();
                BuildFromSerializedObject(root);
            }
            return root;
        }

        private void BuildFromSerializedObject(TreeViewItem root)
        {
            var actionMapArrayProperty = m_SerializedObject.FindProperty("m_ActionMaps");
            for (var i = 0; i < actionMapArrayProperty.arraySize; i++)
            {
                var actionMapProperty = actionMapArrayProperty.GetArrayElementAtIndex(i);
                var actionMapItem = new ActionMapTreeItem(actionMapProperty, i);
                root.AddChild(actionMapItem);
            }
        }

        protected override void ContextClicked()
        {
            OnContextClick(null);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (!HasSelection())
                return;
            if (OnSelectionChanged != null)
            {
                OnSelectionChanged();
            }
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return 18;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item is CompositeGroupTreeItem || item is ActionTreeViewItem && !(item is BindingTreeItem);
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item == null)
                return;
            if (item is BindingTreeItem && !(item is CompositeGroupTreeItem))
                return;
            BeginRename(item);
            ((ActionTreeViewItem)item).renaming = true;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            var actionItem = FindItem(args.itemID, rootItem) as ActionTreeViewItem;
            if (actionItem == null)
                return;

            actionItem.renaming = false;

            if (!args.acceptedRename || args.originalName == args.newName)
                return;

            if (actionItem is ActionTreeItem)
            {
                ((ActionTreeItem)actionItem).Rename(args.newName);
            }
            else if (actionItem is ActionMapTreeItem)
            {
                ((ActionMapTreeItem)actionItem).Rename(args.newName);
            }
            else if (actionItem is CompositeGroupTreeItem)
            {
                ((CompositeGroupTreeItem)actionItem).Rename(args.newName);
            }
            else
            {
                Debug.LogAssertion("Cannot rename: " + actionItem);
            }

            var newId = actionItem.GetIdForName(args.newName);
            SetSelection(new[] {newId});
            SetExpanded(newId, IsExpanded(actionItem.id));
            m_ApplyAction();

            actionItem.displayName = args.newName;
            Reload();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            // We try to predict the indentation
            var indent = (args.item.depth + 2) * 6 + 10;
            var item = (args.item as ActionTreeViewItem);
            if (item != null)
            {
                item.OnGUI(args.rowRect, args.selected, args.focused, indent);
            }
        }
    }
}
#endif // UNITY_EDITOR
