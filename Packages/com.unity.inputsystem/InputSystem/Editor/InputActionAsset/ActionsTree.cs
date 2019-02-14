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

        private string m_NameFilter;
        private string m_SchemeBindingGroupFilter;
        private string m_DeviceFilter;

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

        private ActionsTree(Action applyAction, TreeViewState state)
            : base(applyAction, state)
        {
            foldoutOverride += OnFoldoutDraw;
            Reload();
        }

        static bool OnFoldoutDraw(Rect position, bool expandedState, GUIStyle style)
        {
            var indent = (int)(position.x / 15);
            position.x = 6 * indent + 8;
            return EditorGUI.Foldout(position, expandedState, GUIContent.none, style);
        }

        public void SetSchemeBindingGroupFilter(string schemeBindingGroupName)
        {
            m_SchemeBindingGroupFilter = schemeBindingGroupName;
            Reload();
        }

        public void SetNameFilter(string filter)
        {
            m_NameFilter = string.IsNullOrEmpty(filter) ? null : filter.ToLower();
            Reload();
        }

        public void SetDeviceFilter(string deviceId)
        {
            m_DeviceFilter = deviceId;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                id = 0,
                depth = -1,
                children = new List<TreeViewItem>()
            };

            if (actionMapProperty != null)
            {
                ParseActionMap(root, actionMapProperty, 0);
                // is searching
                if (!string.IsNullOrEmpty(m_NameFilter)
                    || !string.IsNullOrEmpty(m_SchemeBindingGroupFilter)
                    || !string.IsNullOrEmpty(m_DeviceFilter))
                {
                    FilterResultsByGroup(root);
                    FilterResultsByDevice(root);
                    FilterResults(root, MatchNameFilter);
                }
            }
            return root;
        }

        private void ParseActionMap(TreeViewItem parentTreeItem, SerializedProperty actionMapProperty, int depth)
        {
            var actionsArrayProperty = actionMapProperty.FindPropertyRelative("m_Actions");
            for (var i = 0; i < actionsArrayProperty.arraySize; i++)
            {
                ParseAction(parentTreeItem, actionMapProperty, actionsArrayProperty, i, depth);
            }
        }

        private static void ParseAction(TreeViewItem parentTreeItem, SerializedProperty actionMapProperty, SerializedProperty actionsArrayProperty, int index, int depth)
        {
            var bindingsArrayProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
            var actionMapName = actionMapProperty.FindPropertyRelative("m_Name").stringValue;
            var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(index);

            var actionItem = new ActionTreeItem(actionMapProperty, actionProperty, index) {depth = depth};
            var actionName = actionItem.actionName;

            ParseBindings(actionItem, actionMapName, actionName, bindingsArrayProperty, depth + 1);
            parentTreeItem.AddChild(actionItem);
        }

        private static void ParseBindings(TreeViewItem parent, string actionMapName, string actionName, SerializedProperty bindingsArrayProperty, int depth)
        {
            var bindingsCount = InputActionSerializationHelpers.GetBindingCount(bindingsArrayProperty, actionName);
            CompositeGroupTreeItem compositeGroupTreeItem = null;
            for (var j = 0; j < bindingsCount; j++)
            {
                var bindingProperty = InputActionSerializationHelpers.GetBinding(bindingsArrayProperty, actionName, j);
                var bindingsItem = new BindingTreeItem(actionMapName, bindingProperty, j) {depth = depth};
                if (bindingsItem.isComposite)
                {
                    compositeGroupTreeItem =
                        new CompositeGroupTreeItem(actionMapName, bindingProperty, j) {depth = depth};
                    parent.AddChild(compositeGroupTreeItem);
                    continue;
                }
                if (bindingsItem.isPartOfComposite)
                {
                    var compositeItem = new CompositeTreeItem(actionMapName, bindingProperty, j) {depth = depth + 1};
                    compositeGroupTreeItem?.AddChild(compositeItem);
                    continue;
                }
                compositeGroupTreeItem = null;
                parent.AddChild(bindingsItem);
            }
        }

        // Return true is the child node should be removed from the parent
        private bool FilterResultsByGroup(TreeViewItem item)
        {
            if (item.hasChildren)
            {
                var listToRemove = new List<TreeViewItem>();
                foreach (var child in item.children)
                {
                    if (!MatchGroupFilter(child))
                    {
                        listToRemove.Add(child);
                    }
                    FilterResultsByGroup(child);
                }
                foreach (var itemToRemove in listToRemove)
                {
                    item.children.Remove(itemToRemove);
                }

                return !item.hasChildren;
            }
            return true;
        }

        // Return true is the child node should be removed from the parent
        private bool FilterResultsByDevice(TreeViewItem item)
        {
            if (item.hasChildren)
            {
                var listToRemove = new List<TreeViewItem>();
                foreach (var child in item.children)
                {
                    if (!MatchDeviceFilter(child))
                    {
                        listToRemove.Add(child);
                    }
                    FilterResultsByDevice(child);
                }
                foreach (var itemToRemove in listToRemove)
                {
                    item.children.Remove(itemToRemove);
                }

                return !item.hasChildren;
            }
            return true;
        }

        // Return true is the child node should be removed from the parent
        private static bool FilterResults(TreeViewItem item, Func<TreeViewItem, bool> filterMatch)
        {
            if (item.hasChildren)
            {
                var listToRemove = new List<TreeViewItem>();
                foreach (var child in item.children)
                {
                    if (filterMatch(child))
                    {
                        continue;
                    }

                    if (FilterResults(child, filterMatch))
                    {
                        listToRemove.Add(child);
                    }
                }
                foreach (var itemToRemove in listToRemove)
                {
                    item.children.Remove(itemToRemove);
                }

                return !item.hasChildren;
            }
            return true;
        }

        private bool MatchNameFilter(TreeViewItem item)
        {
            if (string.IsNullOrEmpty(m_NameFilter))
                return true;
            if (item.displayName == null)
                return false;
            return item.displayName.ToLower().Contains(m_NameFilter);
        }

        private bool MatchGroupFilter(TreeViewItem item)
        {
            if (string.IsNullOrEmpty(m_SchemeBindingGroupFilter))
                return true;

            if (item is ActionTreeItem)
                return true;

            if (item is CompositeGroupTreeItem)
                return !FilterResults(item, MatchGroupFilter);

            if (item is BindingTreeItem binding)
            {
                if (string.IsNullOrEmpty(binding.path))
                    return true;
                return binding.groups.Contains(m_SchemeBindingGroupFilter);
            }
            return false;
        }

        private bool MatchDeviceFilter(TreeViewItem item)
        {
            if (string.IsNullOrEmpty(m_DeviceFilter))
                return true;

            if (item is ActionTreeItem)
                return true;

            if (item is CompositeGroupTreeItem)
                return !FilterResults(item, MatchDeviceFilter);

            if (item is BindingTreeItem binding)
            {
                if (string.IsNullOrEmpty(binding.path))
                    return true;
                return binding.path.StartsWith(m_DeviceFilter);
            }
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
            OnRowGUI?.Invoke(args.item, args.rowRect);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition != DragAndDropPosition.BetweenItems)
                return DragAndDropVisualMode.None;

            var id = int.Parse(DragAndDrop.paths.First());
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

        private void MoveAction(DragAndDropArgs args, ActionTreeViewItem row)
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

        private void MoveBinding(DragAndDropArgs args, ActionTreeViewItem row)
        {
            var compositeChildrenCount = 0;
            for (var i = 0; i < args.insertAtIndex; i++)
            {
                var item = args.parentItem.children[i];
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

        public void SelectNewBindingRow(ActionTreeItem actionLine)
        {
            // Since the tree is rebuilt, we need to find action line with matching id of the current tree
            var action = (ActionTreeItem)FindItem(actionLine.id, rootItem);
            var newRow = action.children.Last();
            if (newRow.hasChildren)
                newRow = newRow.children.First();
            SetSelection(new List<int>() { newRow.id });
            var selectedRow = FindItem(newRow.id, rootItem);
            while (selectedRow.parent != null)
            {
                SetExpanded(selectedRow.id, true);
                selectedRow = selectedRow.parent;
            }
            OnSelectionChanged();
        }

        public void SelectNewActionRow()
        {
            var newRow = rootItem.children.Last();
            SetSelection(new List<int>() { newRow.id });
            OnSelectionChanged();
            EndRename();
            BeginRename(newRow);
        }
    }
}
#endif // UNITY_EDITOR
