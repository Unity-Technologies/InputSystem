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
        // Determine whether current clipboard contents can can pasted into the ActionMaps view
        //
        //  can always paste an ActionMap
        //  need an existing map to be able to paste an Action
        //
        private static bool CanPasteIntoActionMaps(ActionMapsView mapView)
        {
            bool haveMap = mapView.GetMapCount() > 0;
            var copiedType = CopyPasteHelper.GetCopiedClipboardType();
            bool copyIsMap = copiedType == typeof(InputActionMap);
            bool copyIsAction = copiedType == typeof(InputAction);
            bool hasPastableData = (copyIsMap || (copyIsAction && haveMap));
            return hasPastableData;
        }

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

                if (CanPasteIntoActionMaps(mapView))
                {
                    bool copyIsAction = CopyPasteHelper.GetCopiedClipboardType() == typeof(InputAction);
                    if (CopyPasteHelper.HasPastableClipboardData(typeof(InputActionMap)))
                        menuEvent.menu.AppendAction(paste_String, _ => mapView.PasteItems(copyIsAction));
                }

                menuEvent.menu.AppendSeparator();
                menuEvent.menu.AppendAction(add_Action_Map_String, _ => mapView.AddActionMap());
            });
        }

        // Add "Add Action Map" option to empty space under the ListView. Matches with old IMGUI style (ISX-1519).
        // Include Paste here as well, since it makes sense for adding ActionMaps.
        public static void GetContextMenuForActionMapsEmptySpace(ActionMapsView mapView, VisualElement element)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                if (CanPasteIntoActionMaps(mapView))
                {
                    bool copyIsAction = CopyPasteHelper.GetCopiedClipboardType() == typeof(InputAction);
                    menuEvent.menu.AppendAction(paste_String, _ => mapView.PasteItems(copyIsAction));
                    menuEvent.menu.AppendSeparator();
                }
                menuEvent.menu.AppendAction(add_Action_Map_String, _ => mapView.AddActionMap());
            }) { target = element };
        }

        #endregion

        #region Actions
        // Determine whether current clipboard contents can pasted into the Actions TreeView
        //
        //  item selected   => can paste either Action or Binding (depends on selected item context)
        //  empty view      => can only paste Action
        //  no selection    => can only paste Action
        //
        private static bool CanPasteIntoActions(TreeView treeView)
        {
            bool hasPastableData = false;
            bool selected = treeView.selectedIndex != -1;
            if (selected)
            {
                var item = treeView.GetItemDataForIndex<ActionOrBindingData>(treeView.selectedIndex);
                var itemType = item.isAction ? typeof(InputAction) : typeof(InputBinding);
                hasPastableData = CopyPasteHelper.HasPastableClipboardData(itemType);
            }
            else
            {
                // Cannot paste Binding when no Action is selected or into an empty view
                bool copyIsBinding = CopyPasteHelper.GetCopiedClipboardType() == typeof(InputBinding);
                hasPastableData = !copyIsBinding && CopyPasteHelper.HasPastableClipboardData(typeof(InputAction));
            }
            return hasPastableData;
        }

        // Add the "Paste" option to all elements in the Action area.
        public static void GetContextMenuForActionListView(ActionsTreeView actionsTreeView, TreeView treeView, VisualElement target)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                bool haveMap = actionsTreeView.GetMapCount() > 0;
                if (haveMap)
                {
                    bool hasPastableData = CanPasteIntoActions(treeView);
                    if (hasPastableData)
                    {
                        menuEvent.menu.AppendAction(paste_String, _ => actionsTreeView.PasteItems());
                    }
                    menuEvent.menu.AppendSeparator();
                    menuEvent.menu.AppendAction(add_Action_String, _ => actionsTreeView.AddAction());
                }
            }) { target = target };
        }

        // Add "Add Action" option to empty space under the TreeView. Matches with old IMGUI style (ISX-1519).
        // Include Paste here as well, since it makes sense for Actions; thus users would expect it for Bindings too.
        public static void GetContextMenuForActionsEmptySpace(ActionsTreeView actionsTreeView, TreeView treeView, VisualElement target, bool onlyShowIfTreeIsEmpty = false)
        {
            _ = new ContextualMenuManipulator(menuEvent =>
            {
                bool haveMap = actionsTreeView.GetMapCount() > 0;
                if (haveMap && (!onlyShowIfTreeIsEmpty || treeView.GetTreeCount() == 0))
                {
                    bool hasPastableData = CanPasteIntoActions(treeView);
                    if (hasPastableData)
                    {
                        menuEvent.menu.AppendAction(paste_String, _ => actionsTreeView.PasteItems());
                        menuEvent.menu.AppendSeparator();
                    }
                    menuEvent.menu.AppendAction(add_Action_String, _ => actionsTreeView.AddAction());
                }
            }) { target = target };
        }

        public static void GetContextMenuForActionItem(ActionsTreeView treeView, InputActionsTreeViewItem treeViewItem, string controlLayout, int index)
        {
            treeViewItem.OnContextualMenuPopulateEvent = (menuEvent =>
            {
                menuEvent.menu.AppendAction(add_Binding_String, _ => treeView.AddBinding(index));
                AppendCompositeMenuItems(treeView, controlLayout, index, (name, action) => menuEvent.menu.AppendAction(name, _ => action.Invoke()));
                menuEvent.menu.AppendSeparator();
                AppendRenameAction(menuEvent, treeView, index);
                AppendDuplicateDeleteCutAndCopyActionsSection(menuEvent, treeView, index);
            });
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
            treeViewItem.OnContextualMenuPopulateEvent = (menuEvent =>
            {
                AppendRenameAction(menuEvent, treeView, index);
                AppendDuplicateDeleteCutAndCopyActionsSection(menuEvent, treeView, index);
            });
        }

        public static void GetContextMenuForBindingItem(ActionsTreeView treeView, InputActionsTreeViewItem treeViewItem, int index)
        {
            treeViewItem.OnContextualMenuPopulateEvent = (menuEvent =>
            {
                AppendDuplicateDeleteCutAndCopyActionsSection(menuEvent, treeView, index);
            });
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
