#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    class InputActionListTreeView : TreeView
    {
        InputActionAsset m_Asset;
        SerializedObject m_SerializedObject;
        string m_GroupFilter;
        string m_NameFilter;
        Action m_ApplyAction;

        public Action OnSelectionChanged;
        public Action OnContextClick;

        public static InputActionListTreeView Create(Action applyAction, InputActionAsset asset, SerializedObject serializedObject, ref TreeViewState treeViewState)
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            var treeView = new InputActionListTreeView(applyAction, asset, serializedObject, treeViewState);
            ////FIXME: this requires 2018.3 to compile
            //treeView.foldoutOverride += OnFoldoutDraw;
            treeView.ExpandAll();
            return treeView;
        }

        static bool OnFoldoutDraw(Rect position, bool expandedstate, GUIStyle style)
        {
            var indent = (int)(position.x / 15);
            position.x = 6 * indent + 8;
            return EditorGUI.Foldout(position, expandedstate, GUIContent.none, style);
        }

        protected InputActionListTreeView(Action applyAction,  InputActionAsset asset, SerializedObject serializedObject, TreeViewState state)
            : base(state)
        {
            m_ApplyAction = applyAction;
            m_Asset = asset;
            m_SerializedObject = serializedObject;
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
            m_SerializedObject.Update();
            var actionMapsProperty = m_SerializedObject.FindProperty("m_ActionMaps");
            for (var i = 0; i < m_Asset.actionMaps.Count; i++)
            {
                var actionMap = m_Asset.actionMaps[i];
                var actionMapItem = new ActionMapTreeItem(actionMapsProperty, i);
                ParseActionMap(actionMapItem, actionMapsProperty.GetArrayElementAtIndex(i), actionMap);
                root.AddChild(actionMapItem);
            }
            return root;
        }

        void ParseActionMap(ActionMapTreeItem parentTreeItem, SerializedProperty actionMapProperty, InputActionMap actionMap)
        {
            var bindingsArrayProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
            var actionsArrayProperty = actionMapProperty.FindPropertyRelative("m_Actions");
            var actionMapName = actionMapProperty.FindPropertyRelative("m_Name").stringValue;

            for (var i = 0; i < actionsArrayProperty.arraySize; i++)
            {
                var action = actionMap.actions[i];
                var actionItem = new ActionTreeItem(actionMapProperty, actionsArrayProperty, i);
                actionItem.SetObjectToSerialize(action);
                var actionName = action.name;
                var bindingsCount = InputActionSerializationHelpers.GetBindingCount(bindingsArrayProperty, actionName);

                bool actionSearchMatched = IsSearching() && actionName.ToLower().Contains(m_NameFilter.ToLower());

                CompositeGroupTreeItem compositeGroupTreeItem = null;
                for (var j = 0; j < bindingsCount; j++)
                {
                    var bindingProperty = InputActionSerializationHelpers.GetBinding(bindingsArrayProperty, actionName, j);
                    var binding = action.bindings[j];
                    if (!string.IsNullOrEmpty(m_GroupFilter) && !binding.groups.Split(';').Contains(m_GroupFilter))
                    {
                        continue;
                    }
                    if (binding.isComposite)
                    {
                        compositeGroupTreeItem = new CompositeGroupTreeItem(actionMapName, bindingProperty, j);
                        compositeGroupTreeItem.SetObjectToSerialize(binding);
                        actionItem.AddChild(compositeGroupTreeItem);
                        continue;
                    }
                    if (binding.isPartOfComposite)
                    {
                        var compositeItem = new CompositeTreeItem(actionMapName, bindingProperty, j);
                        compositeItem.SetObjectToSerialize(binding);
                        if (compositeGroupTreeItem != null)
                            compositeGroupTreeItem.AddChild(compositeItem);
                        continue;
                    }
                    compositeGroupTreeItem = null;
                    var bindingsItem = new BindingTreeItem(actionMapName, bindingProperty, j);
                    bindingsItem.SetObjectToSerialize(binding);
                    if (!actionSearchMatched && IsSearching() && !binding.path.ToLower().Contains(m_NameFilter.ToLower()))
                    {
                        continue;
                    }
                    actionItem.AddChild(bindingsItem);
                }

                if (actionSearchMatched || IsSearching() && actionItem.children != null && actionItem.children.Any())
                {
                    parentTreeItem.AddChild(actionItem);
                }
                else if (!IsSearching())
                {
                    parentTreeItem.AddChild(actionItem);
                }
            }
        }

        protected override void ContextClicked()
        {
            OnContextClick();
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (!HasSelection())
                return;

            OnSelectionChanged();
        }

        public InputTreeViewLine GetSelectedRow()
        {
            if (!HasSelection())
                return null;

            return (InputTreeViewLine)FindItem(GetSelection().First(), rootItem);
        }

        public IEnumerable<InputTreeViewLine> GetSelectedRows()
        {
            return FindRows(GetSelection()).Cast<InputTreeViewLine>();
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

            return (item as InputTreeViewLine).elementProperty;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return 18;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item is CompositeGroupTreeItem || item is InputTreeViewLine && !(item is BindingTreeItem);
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item == null)
                return;
            if (item is BindingTreeItem && !(item is CompositeGroupTreeItem))
                return;
            BeginRename(item);
            (item as InputTreeViewLine).renaming = true;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            var item = FindItem(args.itemID, rootItem);
            if (item == null)
                return;

            (item as InputTreeViewLine).renaming = false;

            if (!args.acceptedRename)
                return;

            var actionItem = item as InputTreeViewLine;
            if (actionItem == null)
                return;

            if (actionItem is ActionTreeItem)
            {
                (actionItem as ActionTreeItem).Rename(args.newName);
            }
            else if (actionItem is ActionMapTreeItem)
            {
                (actionItem as ActionMapTreeItem).Rename(args.newName);
            }
            else if (actionItem is CompositeGroupTreeItem)
            {
                (actionItem as CompositeGroupTreeItem).Rename(args.newName);
            }
            else
            {
                throw new NotImplementedException("Can't rename this row");
            }

            m_ApplyAction();

            item.displayName = args.newName;
            Reload();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            // We try to predict the indentation
            var indent = (args.item.depth + 2) * 6 + 10;
            if (args.item is InputTreeViewLine)
            {
                (args.item as InputTreeViewLine).OnGUI(args.rowRect, args.selected, args.focused, indent);
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            if (args.draggedItemIDs.Count > 1)
                return false;
            var item = FindItem(args.draggedItemIDs[0], rootItem) as InputTreeViewLine;
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
            var row = (InputTreeViewLine)item;

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
