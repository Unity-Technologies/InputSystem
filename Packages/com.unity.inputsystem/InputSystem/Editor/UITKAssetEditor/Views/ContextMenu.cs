#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ContextMenu
    {
        private static readonly string rename_String = "Rename";
        private static readonly string duplicate_String = "Duplicate";
        private static readonly string delete_String = "Delete";

        private static readonly string add_Action_String = "Add Action";
        private static readonly string add_Binding_String = "Add Binding";
        private static readonly string add_positiveNegative_Binding_String = "Add Positive\\Negative Binding";
        private static readonly string add_oneModifier_Binding_String = "Add Binding With One Modifier";
        private static readonly string add_twoModifier_Binding_String = "Add Binding With Two Modifiers";
        public static void GetContextMenuForActionMapItem(InputActionsTreeViewItem treeViewItem)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction(add_Action_String, _ => InputActionViewsControlsHolder.CreateAction.Invoke(treeViewItem));
                menuEvent.menu.AppendSeparator();
                menuEvent.menu.AppendAction(rename_String, _ => InputActionViewsControlsHolder.RenameActionMap.Invoke(treeViewItem));
                AppendDuplicateAction(menuEvent, treeViewItem);
                AppendDeleteAction(menuEvent, treeViewItem);
            }) { target = treeViewItem };
        }

        public static void GetContextMenuForActionItem(InputActionsTreeViewItem treeViewItem, int index)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction(add_Binding_String, _ => InputActionViewsControlsHolder.AddBinding.Invoke(treeViewItem));
                menuEvent.menu.AppendAction(add_positiveNegative_Binding_String, _ => InputActionViewsControlsHolder.AddCompositePositivNegativModifier.Invoke(treeViewItem));
                menuEvent.menu.AppendAction(add_oneModifier_Binding_String, _ => InputActionViewsControlsHolder.AddCompositeOneModifier.Invoke(treeViewItem));
                menuEvent.menu.AppendAction(add_twoModifier_Binding_String, _ => InputActionViewsControlsHolder.AddCompositeTwoModifier.Invoke(treeViewItem));
                menuEvent.menu.AppendSeparator();
                AppendRenameAction(menuEvent, index, treeViewItem);
                AppendDuplicateAction(menuEvent, treeViewItem);
                AppendDeleteAction(menuEvent, treeViewItem);
            }) { target = treeViewItem };
        }

        public static void GetContextMenuForCompositeItem(InputActionsTreeViewItem treeViewItem, int index)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                AppendRenameAction(menuEvent, index, treeViewItem);
                AppendDuplicateAction(menuEvent, treeViewItem);
                AppendDeleteAction(menuEvent, treeViewItem);
            }) { target = treeViewItem };
        }

        public static void GetContextMenuForBindingItem(InputActionsTreeViewItem treeViewItem)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                AppendDuplicateAction(menuEvent, treeViewItem);
                AppendDeleteAction(menuEvent, treeViewItem);
            }) { target = treeViewItem };
        }

        private static void AppendDeleteAction(ContextualMenuPopulateEvent menuEvent, InputActionsTreeViewItem treeViewItem)
        {
            menuEvent.menu.AppendAction(delete_String, _ => {InputActionViewsControlsHolder.DeleteAction.Invoke(treeViewItem);});
        }

        private static void AppendDuplicateAction(ContextualMenuPopulateEvent menuEvent, InputActionsTreeViewItem treeViewItem)
        {
            menuEvent.menu.AppendAction(duplicate_String, _ => {InputActionViewsControlsHolder.DuplicateAction.Invoke(treeViewItem);});
        }

        private static void AppendRenameAction(ContextualMenuPopulateEvent menuEvent, int index, InputActionsTreeViewItem treeViewItem)
        {
            menuEvent.menu.AppendAction(rename_String, _ => {InputActionViewsControlsHolder.RenameAction.Invoke(index, treeViewItem);});
        }
    }
}
#endif
