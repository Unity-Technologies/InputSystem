#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
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
        internal static Action<InputActionsTreeViewItem> RenameActionMap => RenameActionMapItem;
        internal static Action<InputActionsTreeViewItem> DeleteAction => Delete;
        internal static Action<InputActionsTreeViewItem> AddBinding => AddNewBinding;
        internal static Action<InputActionsTreeViewItem> AddCompositePositivNegativModifier => AddNewPositiveNegativeComposite;
        internal static Action<InputActionsTreeViewItem> AddCompositeOneModifier => AddNewOneModifierComposite;
        internal static Action<InputActionsTreeViewItem> AddCompositeTwoModifier => AddNewTwoModifierComposite;
        internal static Action<InputActionsTreeViewItem> CreateAction => CreateNewAction;
        internal static Action<InputActionsTreeViewItem> DuplicateAction => Duplicate;

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

        private static void RenameActionMapItem(InputActionsTreeViewItem treeViewItem)
        {
            var index = m_ListView.itemsSource.IndexOf(treeViewItem.label.text);
            if (index < 0 || index >= m_ListView.itemsSource.Count)
                return;
            m_ListView.SetSelection(index);
            treeViewItem.FocusOnRenameTextField();
        }

        private static void Delete(InputActionsTreeViewItem treeViewItem)
        {
            treeViewItem.DeleteItem();
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

        private static void AddNewPositiveNegativeComposite(InputActionsTreeViewItem inputActionsTreeViewItem)
        {
            var action = inputActionsTreeViewItem.label.text;
            m_ActionsTreeView.AddComposite(action, "1DAxis");
        }

        private static void AddNewOneModifierComposite(InputActionsTreeViewItem inputActionsTreeViewItem)
        {
            var action = inputActionsTreeViewItem.label.text;
            m_ActionsTreeView.AddComposite(action, "OneModifier");
        }

        private static void AddNewTwoModifierComposite(InputActionsTreeViewItem inputActionsTreeViewItem)
        {
            var action = inputActionsTreeViewItem.label.text;
            m_ActionsTreeView.AddComposite(action, "TwoModifiers");
        }

        private static void Duplicate(InputActionsTreeViewItem inputActionsTreeViewItem)
        {
            inputActionsTreeViewItem.DuplicateItem();
        }
    }
}
#endif
