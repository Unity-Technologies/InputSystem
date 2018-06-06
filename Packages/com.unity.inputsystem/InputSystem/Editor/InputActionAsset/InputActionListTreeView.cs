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
        
        public Action<InputTreeViewLine> OnSelectionChanged;
        
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
                var actionItem = new ActionSetItem(actionMap, actionMapsProperty, i);
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
                
                var actionItem = new ActionItem(actionMap.name, action, actionsArrayProperty, i);
                treeViewItem.AddChild(actionItem);

                var actionName = actionProperty.FindPropertyRelative("m_Name").stringValue;
                var bindingsCount = InputActionSerializationHelpers.GetBindingCount(bindingsArrayProperty, actionName);

                CompositeGroupItem compositeGroupItem = null;
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
                        compositeGroupItem = new CompositeGroupItem(actionMap.name, binding, bindingProperty, j);
                        actionItem.AddChild(compositeGroupItem);
                        continue;
                    }
                    if (binding.isPartOfComposite)
                    {
                        var compositeItem = new CompositeItem(actionMap.name, binding, bindingProperty, j);
                        if(compositeGroupItem != null)
                            compositeGroupItem.AddChild(compositeItem);
                        continue;
                    }
                    compositeGroupItem = null;
                    var bindingsItem = new BindingItem(actionMap.name, binding, bindingProperty, j);
                    actionItem.AddChild(bindingsItem);
                }
            }
        }
        
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (!HasSelection())
                return;
            
            var item = FindItem(selectedIds.First(), rootItem);
            OnSelectionChanged((InputTreeViewLine)item);
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

        public ActionItem GetSelectedAction()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);

            while (!(item is ActionItem) && item.parent != null)
            {
                item = item.parent;
            }

            return item as ActionItem;
        }

        public ActionSetItem GetSelectedActionMap()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);

            while (!(item is ActionSetItem) && item.parent != null)
            {
                item = item.parent;
            }

            return item as ActionSetItem;
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
            return item is InputTreeViewLine && !(item is BindingItem);
        }
        
        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item == null)
                return;
            if(item is BindingItem)
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

            if (actionItem is ActionItem)
            {
                InputActionSerializationHelpers.RenameAction(actionItem.elementProperty, args.newName);
            }
            else if(actionItem is ActionSetItem)
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
    }
}
