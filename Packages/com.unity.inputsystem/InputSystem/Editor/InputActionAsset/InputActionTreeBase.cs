#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    abstract class InputActionTreeBase : TreeView
    {
        public Action OnSelectionChanged;
        public Action<SerializedProperty> OnContextClick;
        protected Action m_ApplyAction;

        [SerializeField]
        protected string m_NameFilter;

        ////TODO: move to a better place
        public static string SharedResourcesPath = "Packages/com.unity.inputsystem/InputSystem/Editor/InputActionAsset/Resources/";
        public static string ResourcesPath
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return SharedResourcesPath + "pro/";
                return SharedResourcesPath + "personal/";
            }
        }

        protected InputActionTreeBase(Action applyAction, TreeViewState state)
            : base(state)
        {
            m_ApplyAction = applyAction;
        }

        // Return true is the child node should be removed from the parent
        protected bool FilterResults(TreeViewItem root)
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

                    if (FilterResults(child))
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

        public void SetNameFilter(string filter)
        {
            m_NameFilter = filter.ToLower();
            Reload();
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

        public bool CanRenameCurrentSelection()
        {
            var selection = GetSelectedRows();
            if (selection.Count() != 1)
                return false;
            return CanRename(selection.Single());
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

        public void SelectFirstRow()
        {
            if (rootItem.children.Any())
                SetSelection(new[] {rootItem.children[0].id});
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

        protected override void ContextClicked()
        {
            OnContextClick(null);
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

        public bool SetSelection(string actionMapName)
        {
            foreach (var child in rootItem.children)
            {
                if (string.Compare(child.displayName, actionMapName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    SetSelection(new int[] { child.id });
                    return true;
                }
            }
            return false;
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
    }
}
#endif // UNITY_EDITOR
