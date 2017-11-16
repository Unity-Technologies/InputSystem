#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ISX.Editor
{
    // A TreeView of one or more action sets.
    internal class InputActionTreeView : TreeView
    {
        public static InputActionTreeView Create(SerializedProperty actionSetProperty, Action applyAction, ref TreeViewState treeViewState, ref MultiColumnHeaderState headerViewState)
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            var newHeaderState = CreateHeaderState();
            if (headerViewState != null)
                MultiColumnHeaderState.OverwriteSerializedFields(headerViewState, newHeaderState);
            headerViewState = newHeaderState;

            var header = new MultiColumnHeader(headerViewState);
            var treeView = new InputActionTreeView(actionSetProperty, applyAction, treeViewState, header);

            // Expand all action set items.
            foreach (var item in treeView.rootItem.children)
                treeView.SetExpanded(item.id, true);

            return treeView;
        }

        private enum ColumnId
        {
            Name,
            Bindings,
            Groups,
            COUNT
        }

        private static MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new MultiColumnHeaderState.Column[(int)ColumnId.COUNT];

            columns[(int)ColumnId.Name] =
                new MultiColumnHeaderState.Column
            {
                width = 120,
                minWidth = 60,
                autoResize = false,
                headerContent = new GUIContent("Name")
            };
            columns[(int)ColumnId.Bindings] =
                new MultiColumnHeaderState.Column
            {
                width = 280,
                minWidth = 60,
                headerContent = new GUIContent("Bindings")
            };
            columns[(int)ColumnId.Groups] =
                new MultiColumnHeaderState.Column
            {
                width = 110,
                minWidth = 60,
                headerContent = new GUIContent("Groups")
            };

            return new MultiColumnHeaderState(columns);
        }

        private SerializedProperty m_ActionSetsProperty;
        private Action m_ApplyAction;

        protected InputActionTreeView(SerializedProperty actionSetsProperty, Action applyAction, TreeViewState state, MultiColumnHeader multiColumnHeader)
            : base(state, multiColumnHeader)
        {
            m_ActionSetsProperty = actionSetsProperty;
            m_ApplyAction = applyAction;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var id = 0;

            var root = new TreeViewItem
            {
                id = id++,
                depth = -1
            };

            // Add tree for each action set.
            var actionSetCount = m_ActionSetsProperty.arraySize;
            if (actionSetCount > 0)
            {
                for (var i = 0; i < actionSetCount; ++i)
                {
                    var actionSet = m_ActionSetsProperty.GetArrayElementAtIndex(i);
                    var item = BuildActionSetItem(actionSet, ref id);
                    item.actionSetIndex = i;
                    root.AddChild(item);
                }

                // Sort action sets by name.
                root.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
            }

            return root;
        }

        private ActionSetItem BuildActionSetItem(SerializedProperty actionSet, ref int id)
        {
            var nameProperty = actionSet.FindPropertyRelative("m_Name");

            // Item for set itself.
            var item = new ActionSetItem
            {
                id = id++,
                displayName = nameProperty.stringValue,
                depth = 0,
                property = actionSet
            };

            // A child for each action in the set.
            var actions = actionSet.FindPropertyRelative("m_Actions");
            var actionCount = actions.arraySize;

            if (actionCount > 0)
            {
                for (var i = 0; i < actionCount; ++i)
                {
                    var action = actions.GetArrayElementAtIndex(i);
                    var childItem = BuildActionItem(action, ref id);
                    childItem.actionIndex = i;
                    item.AddChild(childItem);
                }

                // Sort actions by name.
                item.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
            }

            ////TODO: ideally this should have a tooltip that instructs to double-click
            // Add final entry which acts as a placeholder for adding new actions
            // to the set. Just select it and start editing to create a new action.
            var addActionItem = new AddNewActionItem
            {
                id = id++,
                displayName = "<Add Action...>",
                depth = 1,
                actionSetProperty = actionSet
            };
            item.AddChild(addActionItem);

            return item;
        }

        private ActionItem BuildActionItem(SerializedProperty action, ref int id)
        {
            var nameProperty = action.FindPropertyRelative("m_Name");

            var item = new ActionItem
            {
                id = id++,
                displayName = nameProperty.stringValue,
                depth = 1,
                property = action
            };

            return item;
        }

        // Action items have varying row heights based on how many bindings they have.
        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            var actionItem = item as ActionItem;
            if (actionItem == null)
            {
                // Not an action item. Uses default row height.
                return rowHeight;
            }

            InitializeBindingListView(actionItem);

            return actionItem.bindingListView.GetHeight();
        }

        private void InitializeBindingListView(ActionItem item)
        {
            if (item.bindingListView != null)
                return;

            var actionSetItem = (ActionSetItem)item.parent;
            item.bindingListView =
                new InputBindingListView(item.property, actionSetItem.property, displayHeader: false);

            item.bindingListView.onChangedCallback =
                (list) =>
                {
                    RefreshCustomRowHeights();
                    m_ApplyAction();
                };
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var columnCount = args.GetNumVisibleColumns();
            for (var i = 0; i < columnCount; ++i)
                ColumnGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
        }

        private void ColumnGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
        {
            switch (column)
            {
                case (int)ColumnId.Name:
                    CenterRectUsingSingleLineHeight(ref cellRect);
                    args.rowRect = cellRect;
                    base.RowGUI(args);
                    break;
                case (int)ColumnId.Bindings:
                    var actionItem = item as ActionItem;
                    if (actionItem != null)
                    {
                        InitializeBindingListView(actionItem);
                        actionItem.bindingListView.DoList(cellRect);
                    }
                    break;
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item == null)
                return;

            var addNewAction = item as AddNewActionItem;
            if (addNewAction != null)
            {
                ////FIXME: for some reason, the item initially appears *before* other actions in the list and then later moves
                var actionSetItem = (ActionSetItem)addNewAction.parent;
                InputActionSerializationHelpers.AddAction(actionSetItem.property);
                m_ApplyAction();
                ////TODO: initiate rename right away
                Reload();
                return;
            }

            BeginRename(item);
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item == null)
                return;

            var actionItem = item as ActionItem;
            if (actionItem != null)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete Action"), false, OnDeleteAction, actionItem);
                menu.ShowAsContext();
                return;
            }

            var actionSetItem = item as ActionSetItem;
            if (actionSetItem != null)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete Set"), false, OnDeleteActionSet, actionSetItem);
                menu.ShowAsContext();
                return;
            }
        }

        // Context menu handler for "Delete Action".
        private void OnDeleteAction(object actionItemObject)
        {
            var actionItem = (ActionItem)actionItemObject;
            var actionSetItem = (ActionSetItem)actionItem.parent;
            InputActionSerializationHelpers.DeleteAction(actionSetItem.property, actionItem.actionIndex);
            m_ApplyAction();
            Reload();
        }

        // Context menu handler for "Delete Set".
        private void OnDeleteActionSet(object actionSetItemObject)
        {
            var actionSetItem = (ActionSetItem)actionSetItemObject;
            InputActionSerializationHelpers.DeleteActionSet(actionSetItem.property.serializedObject, actionSetItem.actionSetIndex);
            m_ApplyAction();
            Reload();
        }

//        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
//        {
//            CenterRectUsingSingleLineHeight(ref cellRect);
//        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename)
                return;

            var item = FindItem(args.itemID, rootItem);
            if (item == null)
                return;

            ////TODO: verify that name is unique; if it isn't, make it unique automatically by appending some suffix

            var actionItem = item as ActionItem;
            var actionSetItem = item as ActionSetItem;

            SerializedProperty nameProperty;
            if (actionItem != null)
                nameProperty = actionItem.property.FindPropertyRelative("m_Name");
            else if (actionSetItem != null)
                nameProperty = actionSetItem.property.FindPropertyRelative("m_Name");
            else
                return;

            nameProperty.stringValue = args.newName;
            m_ApplyAction();

            item.displayName = args.newName;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item is ActionItem || item is ActionSetItem;
        }

        private class ActionItem : TreeViewItem
        {
            public SerializedProperty property;
            public int actionIndex;
            public InputBindingListView bindingListView;
        }

        private class ActionSetItem : TreeViewItem
        {
            public SerializedProperty property;
            public int actionSetIndex;
        }

        private class AddNewActionItem : TreeViewItem
        {
            public SerializedProperty actionSetProperty;
        }
    }
}
#endif
