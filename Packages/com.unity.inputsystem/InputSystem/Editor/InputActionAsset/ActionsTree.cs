#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class ActionsTree : InputActionTreeBase
    {
        public SerializedProperty actionMapProperty;
        public Action<TreeViewItem, Rect> OnRowGUI;

        public static ActionsTree CreateFromSerializedObject(Action applyAction, ref TreeViewState treeViewState)
        {
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();
            }
            var treeView = new ActionsTree(applyAction, treeViewState);
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

        protected ActionsTree(Action applyAction, TreeViewState state)
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
            var root = new TreeViewItem
            {
                id = 0,
                depth = -1
            };
            root.children = new List<TreeViewItem>();
            if (actionMapProperty != null)
            {
                ParseActionMap(root, actionMapProperty, 0);
                // is searching
                if (!string.IsNullOrEmpty(m_NameFilter))
                {
                    FilterResultsByName(root);
                    // is searching
                    if (!string.IsNullOrEmpty(m_NameFilter))
                    {
                        FilterResultsByName(root);
                    }
                }
            }
            return root;
        }

        // Return true is the child node should be removed from the parent
        private bool FilterResultsByName(TreeViewItem root)
        {
            if (root.hasChildren)
            {
                var listToRemove = new List<TreeViewItem>();
                foreach (var child in root.children)
                {
                    if (root.displayName != null && root.displayName.ToLower().Contains(m_NameFilter))
                    {
                        continue;
                    }

                    if (FilterResultsByName(child))
                    {
                        listToRemove.Add(child);
                    }
                }
                foreach (var item in listToRemove)
                {
                    root.children.Remove(item);
                }

                return !root.hasChildren;
            }

            if (root.displayName == null)
                return false;
            return !root.displayName.ToLower().Contains(m_NameFilter);
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

        public TreeViewItem GetRootElement()
        {
            return rootItem;
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition != DragAndDropPosition.BetweenItems)
                return DragAndDropVisualMode.None;

            var id = Int32.Parse(DragAndDrop.paths.First());
            var item = FindItem(id, rootItem);
            var row = (ActionTreeViewItem)item;

            if (!row.isDraggable || args.parentItem != row.parent)
            {
                return DragAndDropVisualMode.None;
            }

            if (args.performDrop)
            {
                if (item is BindingTreeItem)
                {
                    MoveBinding(args, row);
                }
                else if (item is ActionTreeItem)
                {
                    MoveAction(args, row);
                }

                m_ApplyAction();
                DragAndDrop.AcceptDrag();
            }
            return DragAndDropVisualMode.Move;
        }

        void MoveAction(DragAndDropArgs args, ActionTreeViewItem row)
        {
            var action = (ActionTreeItem)row;

            var dstIndex = args.insertAtIndex;
            var srcIndex = action.index;
            if (dstIndex > srcIndex)
            {
                dstIndex--;
            }
            InputActionSerializationHelpers.MoveAction(actionMapProperty, srcIndex, dstIndex);
        }

        void MoveBinding(DragAndDropArgs args, ActionTreeViewItem row)
        {
            TreeViewItem item;
            var compositeChildrenCount = 0;
            for (var i = 0; i < args.insertAtIndex; i++)
            {
                item = args.parentItem.children[i];
                if (item.hasChildren)
                {
                    compositeChildrenCount += item.children.Count;
                }
            }

            args.insertAtIndex += compositeChildrenCount;

            var action = (ActionTreeItem)args.parentItem;

            var dstIndex = action.bindingsStartIndex + args.insertAtIndex;
            var srcIndex = action.bindingsStartIndex + row.index;
            if (dstIndex > srcIndex)
            {
                dstIndex--;
            }

            InputActionSerializationHelpers.MoveBinding(actionMapProperty, srcIndex, dstIndex);

            if (row.hasChildren)
            {
                for (var i = 0; i < row.children.Count; i++)
                {
                    if (dstIndex > srcIndex)
                    {
                        // when moving composite down
                        InputActionSerializationHelpers.MoveBinding(actionMapProperty, srcIndex, dstIndex);
                        continue;
                    }

                    // when moving composite up
                    dstIndex++;
                    srcIndex = action.bindingsStartIndex + (row.children[i] as CompositeTreeItem).index;
                    InputActionSerializationHelpers.MoveBinding(actionMapProperty, srcIndex, dstIndex);
                }
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
            if (OnRowGUI != null)
                OnRowGUI(args.item, args.rowRect);
        }
    }
}
#endif // UNITY_EDITOR
