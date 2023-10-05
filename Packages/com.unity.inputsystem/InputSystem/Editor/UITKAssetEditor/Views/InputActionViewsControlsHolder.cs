#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal static class InputActionViewsControlsHolder
    {
        private static TreeView m_TreeView;
        private static ActionsTreeView m_ActionsTreeView;
        private static ListView m_ListView;
        internal static Action<int, InputActionsTreeViewItem> RenameAction => RenameActionItem;
        internal static Action<InputActionMapsTreeViewItem> RenameActionMap => RenameActionMapItem;
        internal static Action<InputActionsTreeViewItem> DeleteAction => DeleteActionItem;
        internal static Action<InputActionMapsTreeViewItem> DeleteActionMap => DeleteActionMapItem;
        internal static Action<InputActionsTreeViewItem> AddBinding => AddNewBinding;
        internal static Action<InputActionsTreeViewItem, string> AddComposite => AddNewComposite;
        internal static Action<InputActionMapsTreeViewItem> CreateActionMap => CreateNewActionMap;
        internal static Action<InputActionsTreeViewItem> CreateAction => CreateNewAction;
        internal static Action<InputActionsTreeViewItem> DuplicateAction => DuplicateActionItem;
        internal static Action<InputActionMapsTreeViewItem> DuplicateActionMap => DuplicateActionMapItem;

        internal static void Initialize(VisualElement root, ActionsTreeView actionsTreeView)
        {
            m_TreeView = root?.Q<TreeView>("actions-tree-view");
            m_ActionsTreeView = actionsTreeView;
            m_ListView = root?.Q<ListView>("action-maps-list-view");
        }

        private static void RenameActionItem(int index, InputActionsTreeViewItem treeViewItem)
        {
            m_TreeView.SetSelection(index);
            treeViewItem.FocusOnRenameTextField();
        }

        private static void RenameActionMapItem(InputActionMapsTreeViewItem treeViewItem)
        {
            var index = m_ListView.itemsSource.IndexOf(treeViewItem.label.text);
            if (index < 0 || index >= m_ListView.itemsSource.Count)
                return;
            m_ListView.SetSelection(index);
            treeViewItem.FocusOnRenameTextField();
        }

        private static void DeleteActionItem(InputActionsTreeViewItem treeViewItem)
        {
            treeViewItem.DeleteItem();
        }

        private static void DeleteActionMapItem(InputActionMapsTreeViewItem treeViewItem)
        {
            treeViewItem.DeleteItem();
        }

        private static void CreateNewActionMap(InputActionMapsTreeViewItem item)
        {
            var index = m_ListView.itemsSource.IndexOf(item.label.text);
            if (index < 0 || index >= m_ListView.itemsSource.Count)
                return;
            m_ListView.SetSelection(index);
            m_ActionsTreeView.AddAction();
        }

        private static void CreateNewAction(InputActionsTreeViewItem item)
        {
            var index = m_ListView.itemsSource.IndexOf(item.label.text);
            if (index < 0 || index >= m_ListView.itemsSource.Count)
                return;
            m_ListView.SetSelection(index);
            m_ActionsTreeView.AddAction();
        }

        private static void AddNewBinding(InputActionsTreeViewItem inputActionsTreeViewItem)
        {
            var action = inputActionsTreeViewItem.label.text;
            m_ActionsTreeView.AddBinding(action);
        }

        private static void AddNewComposite(InputActionsTreeViewItem inputActionsTreeViewItem, string compositeType)
        {
            var action = inputActionsTreeViewItem.label.text;
            m_ActionsTreeView.AddComposite(action, compositeType);
        }

        private static void DuplicateActionMapItem(InputActionMapsTreeViewItem inputActionsTreeViewItem)
        {
            inputActionsTreeViewItem.DuplicateItem();
        }

        private static void DuplicateActionItem(InputActionsTreeViewItem inputActionsTreeViewItem)
        {
            inputActionsTreeViewItem.DuplicateItem();
        }
    }
}
#endif
