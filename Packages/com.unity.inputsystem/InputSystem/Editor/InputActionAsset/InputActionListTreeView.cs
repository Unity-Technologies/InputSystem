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
        Action m_ApplyAction;
        
        public Action OnSelectionChanged;
        public Action OnContextClick;
        
        public static InputActionListTreeView Create(Action applyAction, InputActionAsset asset, SerializedObject serializedObject, ref TreeViewState treeViewState)
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();
            
            var treeView = new InputActionListTreeView(applyAction, asset, serializedObject, treeViewState);
            treeView.ExpandAll();
            return treeView;
        }

        protected InputActionListTreeView(Action applyAction,  InputActionAsset asset, SerializedObject serializedObject, TreeViewState state)
            : base(state)
        {
            m_ApplyAction = applyAction;
            m_Asset = asset;
            m_SerializedObject = serializedObject;
            Reload();
        }

        public void FilterResults(string filter)
        {
            m_GroupFilter = filter;
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
            m_SerializedObject.Update();
            var actionMapsProperty = m_SerializedObject.FindProperty("m_ActionMaps");
            for (var i = 0; i < m_Asset.actionMaps.Count; i++)
            {
                var actionMap = m_Asset.actionMaps[i];
                var actionItem = new ActionMapTreeItem(actionMap, actionMapsProperty, i);
                ParseActionMap(actionItem, actionMap, actionItem.elementProperty);
                root.AddChild(actionItem);
            }
            return root;
        }

        void ParseActionMap(TreeViewItem treeViewItem, InputActionMap actionMap, SerializedProperty actionMapProperty)
        {
            var bindingsArrayProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
            var actionsArrayProperty = actionMapProperty.FindPropertyRelative("m_Actions");
            
            for (var i = 0; i < actionsArrayProperty.arraySize; i++)
            {
                var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(i);

                var action = actionMap.actions[i];
                
                var actionItem = new ActionTreeItem(actionMap.name, action, actionsArrayProperty, i);
                treeViewItem.AddChild(actionItem);

                var actionName = actionProperty.FindPropertyRelative("m_Name").stringValue;
                var bindingsCount = InputActionSerializationHelpers.GetBindingCount(bindingsArrayProperty, actionName);

                CompositeGroupTreeItem compositeGroupTreeItem = null;
                for (var j = 0; j < bindingsCount; j++)
                {
                    var bindingProperty = InputActionSerializationHelpers.GetBinding(bindingsArrayProperty, actionName, j);
                    var binding = action.bindings[j];
                    if(!string.IsNullOrEmpty(m_GroupFilter) && !binding.groups.Split(';').Contains(m_GroupFilter))
                    {
                        continue;
                    }
                    if (binding.isComposite)
                    {
                        compositeGroupTreeItem = new CompositeGroupTreeItem(actionMap.name, binding, bindingProperty, j);
                        actionItem.AddChild(compositeGroupTreeItem);
                        continue;
                    }
                    if (binding.isPartOfComposite)
                    {
                        var compositeItem = new CompositeTreeItem(actionMap.name, binding, bindingProperty, j);
                        if(compositeGroupTreeItem != null)
                            compositeGroupTreeItem.AddChild(compositeItem);
                        continue;
                    }
                    compositeGroupTreeItem = null;
                    var bindingsItem = new BindingTreeItem(actionMap.name, binding, bindingProperty, j);
                    actionItem.AddChild(bindingsItem);
                }
            }
        }

        protected override void ContextClickedItem(int id)
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
            
            return (InputTreeViewLine) FindItem(GetSelection().First(), rootItem);
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

            while (!(item is ActionTreeItem) && item.parent != null)
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

            while (!(item is ActionMapTreeItem) && item.parent != null)
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
            return item is InputTreeViewLine && !(item is BindingTreeItem);
        }
        
        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item == null)
                return;
            if(item is BindingTreeItem)
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

            ////TODO: verify that name is unique; if it isn't, make it unique automatically by appending some suffix

            var actionItem = item as InputTreeViewLine;

            if (actionItem == null)
                return;

            if (actionItem is ActionTreeItem)
            {
                var a = actionItem as ActionTreeItem;
                var map = GetSelectedActionMap();
                InputActionSerializationHelpers.RenameAction(actionItem.elementProperty, map.elementProperty, args.newName);
            }
            else if(actionItem is ActionMapTreeItem)
            {
                InputActionSerializationHelpers.RenameActionMap(actionItem.elementProperty, args.newName);
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
            var indent = GetContentIndent(args.item);
            if (args.item is InputTreeViewLine)
            {
                (args.item as InputTreeViewLine).OnGUI(args.rowRect, args.selected, args.focused, indent);
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            if (args.draggedItemIDs.Count > 1)
                return false;
            if (FindItem(args.draggedItemIDs[0], rootItem).GetType() == typeof(BindingTreeItem))
                return true;
            return false;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var row = FindItem(args.draggedItemIDs.First(), rootItem);
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = args.draggedItemIDs.Select(i=>""+i).ToArray();
            DragAndDrop.StartDrag(row.displayName);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var id = Int32.Parse(DragAndDrop.paths.First());
            var row = (BindingTreeItem)FindItem(id, rootItem);

            if (args.parentItem.GetType() != typeof(ActionTreeItem)
                || args.parentItem != row.parent)
            {
                return DragAndDropVisualMode.None;
            }

            if (args.performDrop)
            {
                var action = (ActionTreeItem) args.parentItem;
                var map = (ActionMapTreeItem) args.parentItem.parent;
                var srcIndex = action.bindingsStartIndex + row.index;
                var dstIndex = action.bindingsStartIndex + args.insertAtIndex;
                if (dstIndex > srcIndex)
                    dstIndex--;
                InputActionSerializationHelpers.MoveBinding(map.elementProperty, srcIndex, dstIndex);
                m_ApplyAction();
                DragAndDrop.AcceptDrag();
            }
            return DragAndDropVisualMode.Move;
        }
    }
}
