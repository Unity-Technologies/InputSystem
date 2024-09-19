#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ContextMenu
    {
        private static readonly string copy_String = "Copy";
        private static readonly string cut_String = "Cut";
        private static readonly string paste_String = "Paste";

        private static readonly string rename_String = "Rename";
        private static readonly string duplicate_String = "Duplicate";
        private static readonly string delete_String = "Delete";

        private static readonly string add_Action_Map_String = "Add Action Map";
        private static readonly string add_Action_String = "Add Action";
        private static readonly string add_Binding_String = "Add Binding";

        #region ActionMaps
        public static void GetContextMenuForActionMapItem(ActionMapsView mapView, InputActionMapsTreeViewItem treeViewItem, int index)
        {
            treeViewItem.OnContextualMenuPopulateEvent = (menuEvent =>
            {
                // TODO: AddAction should enable m_RenameOnActionAdded
                menuEvent.menu.AppendAction(add_Action_String, _ => mapView.Dispatch(Commands.AddAction()));
                menuEvent.menu.AppendSeparator();
                menuEvent.menu.AppendAction(rename_String, _ => mapView.RenameActionMap(index));
                menuEvent.menu.AppendAction(duplicate_String, _ => mapView.DuplicateActionMap(index));
                menuEvent.menu.AppendAction(delete_String, _ => mapView.DeleteActionMap(index));
                menuEvent.menu.AppendSeparator();
                menuEvent.menu.AppendAction(copy_String, _ => mapView.CopyItems());
                menuEvent.menu.AppendAction(cut_String, _ => mapView.CutItems());

                var copiedAction = CopyPasteHelper.GetCopiedClipboardType() == typeof(InputAction);
                if (CopyPasteHelper.HasPastableClipboardData(typeof(InputActionMap)))
                    menuEvent.menu.AppendAction(paste_String, _ => mapView.PasteItems(copiedAction));
            });
        }

        // Add "Add Action Map" option to empty space under the ListView. Matches with old IMGUI style (ISX-1519).
        // Include Paste here as well, since it makes sense for adding ActionMaps.
        public static void GetContextMenuForActionMapsEmptySpace(ActionMapsView mapView, VisualElement element, bool onlyShowIfListIsEmpty = false)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                if (!onlyShowIfListIsEmpty || mapView.GetMapCount() == 0)
                {
                    var copiedAction = CopyPasteHelper.GetCopiedClipboardType() == typeof(InputAction);
                    if (CopyPasteHelper.HasPastableClipboardData(typeof(InputActionMap)))
                        menuEvent.menu.AppendAction(paste_String, _ => mapView.PasteItems(copiedAction));

                    menuEvent.menu.AppendSeparator();
                    menuEvent.menu.AppendAction(add_Action_Map_String, _ => mapView.AddActionMap());
                }
            }) { target = element };
        }

        #endregion

        #region Actions
        // Add the "Paste" option to all elements in the Action area.
        public static void GetContextMenuForActionListView(ActionsTreeView actionsTreeView, TreeView treeView, VisualElement target)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                var item = treeView.GetItemDataForIndex<ActionOrBindingData>(treeView.selectedIndex);
                var hasPastableData = CopyPasteHelper.HasPastableClipboardData(item.isAction ? typeof(InputAction) : typeof(InputBinding));
                if (hasPastableData)
                    menuEvent.menu.AppendAction(paste_String, _ => actionsTreeView.PasteItems());
            }) { target = target };
        }

        // Add "Add Action" option to empty space under the TreeView. Matches with old IMGUI style (ISX-1519).
        // Include Paste here as well, since it makes sense for Actions; thus users would expect it for Bindings too.
        public static void GetContextMenuForActionsEmptySpace(ActionsTreeView actionsTreeView, TreeView treeView, VisualElement target, bool onlyShowIfTreeIsEmpty = false)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                if (actionsTreeView.GetMapCount() > 0 && (!onlyShowIfTreeIsEmpty || treeView.GetTreeCount() == 0))
                {
                    var item = treeView.GetItemDataForIndex<ActionOrBindingData>(treeView.selectedIndex);
                    if (CopyPasteHelper.HasPastableClipboardData(item.isAction ? typeof(InputAction) : typeof(InputBinding)))
                        menuEvent.menu.AppendAction(paste_String, _ => actionsTreeView.PasteItems());

                    menuEvent.menu.AppendSeparator();
                    menuEvent.menu.AppendAction(add_Action_String, _ => actionsTreeView.AddAction());
                }
            }) { target = target };
        }

        public static void GetContextMenuForActionItem(ActionsTreeView treeView, InputActionsTreeViewItem treeViewItem, string controlLayout, int index)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction(add_Binding_String, _ => treeView.AddBinding(index));
                AppendCompositeMenuItems(treeView, controlLayout, index, (name, action) => menuEvent.menu.AppendAction(name, _ => action.Invoke()));
                menuEvent.menu.AppendSeparator();
                AppendRenameAction(menuEvent, treeView, index);
                AppendDuplicateDeleteCutAndCopyActionsSection(menuEvent, treeView, index);
            }) { target = treeViewItem };
        }

        public static Action GetContextMenuForActionAddItem(ActionsTreeView treeView, string controlLayout, int index)
        {
            return () =>
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent(add_Binding_String), false, () => treeView.AddBinding(index));
                AppendCompositeMenuItems(treeView, controlLayout, index, (name, action) => menu.AddItem(new GUIContent(name), false, action.Invoke));
                menu.ShowAsContext();
            };
        }

        private static void AppendCompositeMenuItems(ActionsTreeView treeView, string expectedControlLayout, int index, Action<string, Action> addToMenuAction)
        {
            foreach (var compositeName in InputBindingComposite.s_Composites.internedNames.Where(x =>
                !InputBindingComposite.s_Composites.aliases.Contains(x)).OrderBy(x => x))
            {
                // Skip composites we should hide
                var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
                var designTimeVisible = compositeType.GetCustomAttribute<DesignTimeVisibleAttribute>();
                if (designTimeVisible != null && !designTimeVisible.Visible)
                    continue;

                // Skip composites that don't match the expected control layout
                // NOTE: "Any" is a special case and expected to be null
                if (!string.IsNullOrEmpty(expectedControlLayout))
                {
                    var valueType = InputBindingComposite.GetValueType(compositeName);
                    if (valueType != null &&
                        !InputControlLayout.s_Layouts.ValueTypeIsAssignableFrom(
                            new InternedString(expectedControlLayout), valueType))
                        continue;
                }

                var displayName = compositeType.GetCustomAttribute<DisplayNameAttribute>();
                var niceName = displayName != null ? displayName.DisplayName.Replace('/', '\\') : ObjectNames.NicifyVariableName(compositeName) + " Composite";
                addToMenuAction.Invoke($"Add {niceName}",  () => treeView.AddComposite(index, compositeName));
            }
        }

        public static void GetContextMenuForCompositeItem(ActionsTreeView treeView, InputActionsTreeViewItem treeViewItem, int index)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                AppendRenameAction(menuEvent, treeView, index);
                AppendDuplicateDeleteCutAndCopyActionsSection(menuEvent, treeView, index);
            }) { target = treeViewItem };
        }

        public static void GetContextMenuForBindingItem(ActionsTreeView treeView, InputActionsTreeViewItem treeViewItem, int index)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                AppendDuplicateDeleteCutAndCopyActionsSection(menuEvent, treeView, index);
            }) { target = treeViewItem };
        }

        private static void AppendRenameAction(ContextualMenuPopulateEvent menuEvent, ActionsTreeView treeView, int index)
        {
            menuEvent.menu.AppendAction(rename_String, _ => treeView.RenameActionItem(index));
        }

        // These actions are always either all present, or all missing, so we can group their Append calls here.
        private static void AppendDuplicateDeleteCutAndCopyActionsSection(ContextualMenuPopulateEvent menuEvent, ActionsTreeView actionsTreeView, int index)
        {
            menuEvent.menu.AppendAction(duplicate_String, _ => actionsTreeView.DuplicateItem(index));
            menuEvent.menu.AppendAction(delete_String, _ => actionsTreeView.DeleteItem(index));
            menuEvent.menu.AppendSeparator();
            menuEvent.menu.AppendAction(copy_String, _ => actionsTreeView.CopyItems());
            menuEvent.menu.AppendAction(cut_String, _ => actionsTreeView.CutItems());
        }

        #endregion
    }
}
#endif
