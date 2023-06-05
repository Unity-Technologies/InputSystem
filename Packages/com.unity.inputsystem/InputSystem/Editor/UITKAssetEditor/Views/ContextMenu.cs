#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal static partial class ContextMenu
    {
        private static readonly string rename_String = "Rename";
        private static readonly string delete_String = "Delete";
        public static void GetContextMenuForActionMapItem(InputActionsTreeViewItem targetElement, ListView listView)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction(rename_String, action =>
                {
                    listView.SetSelection(listView.itemsSource.IndexOf(targetElement.label.text));
                    targetElement.FocusOnRenameTextField();
                });
                AppendDeleteAction(menuEvent, targetElement);
            }) { target = targetElement };
        }

        public static void GetContextMenuForActionOrCompositeItem(InputActionsTreeViewItem targetElement, TreeView treeView, int index)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction(rename_String, action =>
                {
                    treeView.SetSelection(index);
                    targetElement.FocusOnRenameTextField();
                });
                AppendDeleteAction(menuEvent, targetElement);
            }) { target = targetElement };
        }

        public static void GetContextMenuForBindingItem(InputActionsTreeViewItem targetElement)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                AppendDeleteAction(menuEvent, targetElement);
            }) { target = targetElement };
        }

        private static void AppendDeleteAction(ContextualMenuPopulateEvent menuEvent, InputActionsTreeViewItem targetElement)
        {
            menuEvent.menu.AppendAction(delete_String, action => {targetElement.DeleteItem();});
        }
    }
}
#endif
