#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputActionListTreeView : TreeView
    {
        private SerializedObject m_SerializedObject;
        private string m_GroupFilter;
        private string m_NameFilter;
        private Action m_ApplyAction;

        public Action OnSelectionChanged;
        public Action<SerializedProperty> OnContextClick;

        public static InputActionListTreeView CreateFromSerializedObject(Action applyAction, SerializedObject serializedObject, ref TreeViewState treeViewState)
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            var treeView = new InputActionListTreeView(applyAction, treeViewState);
            treeView.m_SerializedObject = serializedObject;
            treeView.Reload();
            treeView.ExpandAll();
            return treeView;
        }

        public ActionMapTreeItem FindActionMapTreeViewItem(string mapName)
        {
            if (!rootItem.hasChildren)
                return null;

            foreach (var child in rootItem.children)
            {
                var mapItem = child as ActionMapTreeItem;
                if (mapItem == null)
                    continue;

                if (string.Compare(mapItem.displayName, mapName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return mapItem;
            }

            return null;
        }

        public ActionTreeViewItem FindActionTreeViewItem(string mapName, string actionName)
        {
            var mapItem = FindActionMapTreeViewItem(mapName);
            if (mapItem == null)
                return null;

            if (!mapItem.hasChildren)
                return null;

            foreach (var child in mapItem.children)
            {
                var actionItem = child as ActionTreeViewItem;
                if (actionItem == null)
                    continue;

                if (string.Compare(actionItem.displayName, actionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return actionItem;
            }

            return null;
        }

        static bool OnFoldoutDraw(Rect position, bool expandedstate, GUIStyle style)
        {
            var indent = (int)(position.x / 15);
            position.x = 6 * indent + 8;
            return EditorGUI.Foldout(position, expandedstate, GUIContent.none, style);
        }

        protected InputActionListTreeView(Action applyAction, TreeViewState state)
            : base(state)
        {
            m_ApplyAction = applyAction;
            ////REVIEW: good enough like this for 2018.2?
            #if UNITY_2018_3_OR_NEWER
            foldoutOverride += OnFoldoutDraw;
            #endif
            Reload();
        }

        public void SetGroupFilter(string filter)
        {
            m_GroupFilter = filter;
            Reload();
        }

        public void SetNameFilter(string filter)
        {
            m_NameFilter = filter;
            Reload();
        }

        public bool IsSearching()
        {
            return !string.IsNullOrEmpty(m_NameFilter);
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
                BuildFromSerializedObject(root);
            }
            return root;
        }

        private void BuildFromSerializedObject(TreeViewItem root)
        {
            m_SerializedObject.Update();
            var actionMapArrauProperty = m_SerializedObject.FindProperty("m_ActionMaps");
            for (var i = 0; i < actionMapArrauProperty.arraySize; i++)
            {
                var actionMapProperty = actionMapArrauProperty.GetArrayElementAtIndex(i);
                var actionMapItem = new ActionMapTreeItem(actionMapProperty, i);
                ParseActionMap(actionMapItem, actionMapProperty, 1);
                root.AddChild(actionMapItem);
            }
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

            bool actionSearchMatched = IsSearching() && actionName.ToLower().Contains(m_NameFilter.ToLower());
            if (actionSearchMatched || IsSearching() && actionItem.children != null && actionItem.children.Any())
            {
                parentTreeItem.AddChild(actionItem);
            }
            else if (!IsSearching())
            {
                parentTreeItem.AddChild(actionItem);
            }
        }

        protected void ParseBindings(TreeViewItem parent, string actionMapName, string actionName, SerializedProperty bindingsArrayProperty, int depth)
        {
            var actionSearchMatched = IsSearching() && actionName.ToLower().Contains(m_NameFilter.ToLower());
            var bindingsCount = InputActionSerializationHelpers.GetBindingCount(bindingsArrayProperty, actionName);
            CompositeGroupTreeItem compositeGroupTreeItem = null;
            for (var j = 0; j < bindingsCount; j++)
            {
                var bindingProperty = InputActionSerializationHelpers.GetBinding(bindingsArrayProperty, actionName, j);
                var bindingsItem = new BindingTreeItem(actionMapName, bindingProperty, j);
                bindingsItem.depth = depth;
                if (!string.IsNullOrEmpty(m_GroupFilter) && !bindingsItem.groups.Split(';').Contains(m_GroupFilter))
                {
                    continue;
                }
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
                if (!actionSearchMatched && IsSearching() && !bindingsItem.path.ToLower().Contains(m_NameFilter.ToLower()))
                {
                    continue;
                }
                parent.AddChild(bindingsItem);
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

        public ActionTreeViewItem GetSelectedRow()
        {
            if (!HasSelection())
                return null;

            return (ActionTreeViewItem)FindItem(GetSelection().First(), rootItem);
        }

        public IEnumerable<ActionTreeViewItem> GetSelectedRows()
        {
            return FindRows(GetSelection()).Cast<ActionTreeViewItem>();
        }

        public ActionTreeItem GetSelectedAction()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);

            while (!(item is ActionTreeItem) && item != null && item.parent != null)
            {
                item = item.parent;
            }

            return item as ActionTreeItem;
        }

        public ActionMapTreeItem GetSelectedActionMap()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);

            while (!(item is ActionMapTreeItem) && item != null && item.parent != null)
            {
                item = item.parent;
            }

            return item as ActionMapTreeItem;
        }

        public SerializedProperty GetSelectedProperty()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);

            if (item == null)
                return null;

            return (item as ActionTreeViewItem).elementProperty;
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
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            var item = FindItem(args.itemID, rootItem);
            var actionItem = item as ActionTreeViewItem;
            if (actionItem == null)
                return;

            if (!args.acceptedRename)
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
                Debug.Assert(false, "Cannot rename: " + actionItem);
                return;
            }

            m_ApplyAction();

            item.displayName = args.newName;
            Reload();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as ActionTreeViewItem;
            if (item != null)
                item.OnGUI(args.rowRect, args.selected, args.focused);
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            if (args.draggedItemIDs.Count > 1)
                return false;
            var item = FindItem(args.draggedItemIDs[0], rootItem) as ActionTreeViewItem;
            return item.isDraggable;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var row = FindItem(args.draggedItemIDs.First(), rootItem);
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = args.draggedItemIDs.Select(i => "" + i).ToArray();
            DragAndDrop.StartDrag(row.displayName);
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
                var map = (ActionMapTreeItem)args.parentItem.parent;

                var dstIndex = action.bindingsStartIndex + args.insertAtIndex;
                var srcIndex = action.bindingsStartIndex + row.index;
                if (dstIndex > srcIndex)
                {
                    dstIndex--;
                }

                InputActionSerializationHelpers.MoveBinding(map.elementProperty, srcIndex, dstIndex);

                if (row.hasChildren)
                {
                    for (var i = 0; i < row.children.Count; i++)
                    {
                        if (dstIndex > srcIndex)
                        {
                            // when moving composite down
                            InputActionSerializationHelpers.MoveBinding(map.elementProperty, srcIndex, dstIndex);
                            continue;
                        }

                        // when moving composite up
                        dstIndex++;
                        srcIndex = action.bindingsStartIndex + (row.children[i] as CompositeTreeItem).index;
                        InputActionSerializationHelpers.MoveBinding(map.elementProperty, srcIndex, dstIndex);
                    }
                }

                m_ApplyAction();
                DragAndDrop.AcceptDrag();
                //since there is no easy way to know the id of the element after reordering
                //instead of updating the selected item, we leave the UI without selection
                if (OnSelectionChanged != null)
                    OnSelectionChanged();
            }
            return DragAndDropVisualMode.Move;
        }
    }
}
#endif // UNITY_EDITOR
