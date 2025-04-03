#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

// The action tree view illustrates one of the weaknesses of Unity's editing model. While operating directly
// on serialized data does have a number of advantages (the built-in undo system being one of them), making the
// persistence model equivalent to the edit model doesn't work well. Serialized data will be laid out for persistence,
// not for the convenience of editing operations. This means that editing operations have to constantly jump through
// hoops to map themselves onto the persistence model of the data.

////TODO: With many actions and bindings the list becomes really hard to grok; make things more visually distinctive

////TODO: add context menu items for reordering action and binging entries (like "Move Up" and "Move Down")

////FIXME: context menu cannot be brought up when there's no items in the tree

////FIXME: RMB context menu for actions displays composites that aren't applicable to the action

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A tree view showing action maps, actions, and bindings. This is the core piece around which the various
    /// pieces of action editing functionality revolve.
    /// </summary>
    /// <remarks>
    /// The tree view can be flexibly used to contain only parts of a specific action setup. For example,
    /// by only adding items for action maps (<see cref="ActionMapTreeItem"/> to the tree, the tree view
    /// will become a flat list of action maps. Or by adding only items for actions (<see cref="ActionTreeItem"/>
    /// and items for their bindings (<see cref="BindingTreeItem"/>) to the tree, it will become an action-only
    /// tree view.
    ///
    /// This is used by the action asset editor to separate action maps and their actions into two separate
    /// tree views (the leftmost and the middle column of the editor).
    ///
    /// Each action tree comes with copy-paste and context menu support.
    /// </remarks>
    internal class InputActionTreeView : TreeView
    {
        #region Creation

        public InputActionTreeView(SerializedObject serializedObject, TreeViewState state = null)
            : base(state ?? new TreeViewState())
        {
            Debug.Assert(serializedObject != null, "Must have serialized object");
            this.serializedObject = serializedObject;
            UpdateSerializedObjectDirtyCount();
            foldoutOverride = DrawFoldout;
            drawHeader = true;
            drawPlusButton = true;
            drawMinusButton = true;
            m_ForceAcceptRename = false;
            m_Title = new GUIContent("");
        }

        /// <summary>
        /// Build an action tree that shows only the bindings for the given action.
        /// </summary>
        public static TreeViewItem BuildWithJustBindingsFromAction(SerializedProperty actionProperty, SerializedProperty actionMapProperty = null)
        {
            Debug.Assert(actionProperty != null, "Action property cannot be null");
            var root = new ActionTreeItem(actionMapProperty, actionProperty);
            root.depth = -1;
            root.AddBindingsTo(root);
            return root;
        }

        /// <summary>
        /// Build an action tree that shows only the actions and bindings for the given action map.
        /// </summary>
        public static TreeViewItem BuildWithJustActionsAndBindingsFromMap(SerializedProperty actionMapProperty)
        {
            Debug.Assert(actionMapProperty != null, "Action map property cannot be null");
            var root = new ActionMapTreeItem(actionMapProperty);
            root.depth = -1;
            root.AddActionsAndBindingsTo(root);
            return root;
        }

        /// <summary>
        /// Build an action tree that contains only the action maps from the given .inputactions asset.
        /// </summary>
        public static TreeViewItem BuildWithJustActionMapsFromAsset(SerializedObject assetObject)
        {
            Debug.Assert(assetObject != null, "Asset object cannot be null");
            var root = new ActionMapListItem {id = 0, depth = -1};
            ActionMapTreeItem.AddActionMapsFromAssetTo(root, assetObject);
            return root;
        }

        public static TreeViewItem BuildFullTree(SerializedObject assetObject)
        {
            Debug.Assert(assetObject != null, "Asset object cannot be null");
            var root = new TreeViewItem {id = 0, depth = -1};
            ActionMapTreeItem.AddActionMapsFromAssetTo(root, assetObject);
            if (root.hasChildren)
                foreach (var child in root.children)
                    ((ActionMapTreeItem)child).AddActionsAndBindingsTo(child);
            return root;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = onBuildTree?.Invoke() ?? new TreeViewItem(0, -1);

            // If we have a filter, remove unwanted items from the tree.
            // NOTE: We use this method rather than TreeView's built-in search functionality as we want
            //       to keep the tree structure fully intact whereas searchString switches the tree into
            //       a different view mode.
            if (m_ItemFilterCriteria.LengthSafe() > 0 && root.hasChildren)
            {
                foreach (var child in root.children.OfType<ActionTreeItemBase>().ToArray())
                    PruneTreeByItemSearchFilter(child);
            }

            // Root node is required to have `children` not be null. Add empty list,
            // if necessary. Can happen, for example, if we have a singleton action at the
            // root but no bindings on it.
            if (root.children == null)
                root.children = new List<TreeViewItem>();

            return root;
        }

        #endregion

        #region Filtering

        internal bool hasFilter => m_ItemFilterCriteria != null;

        public void ClearItemSearchFilterAndReload()
        {
            if (m_ItemFilterCriteria == null)
                return;
            m_ItemFilterCriteria = null;
            Reload();
        }

        public void SetItemSearchFilterAndReload(string criteria)
        {
            SetItemSearchFilterAndReload(FilterCriterion.FromString(criteria));
        }

        public void SetItemSearchFilterAndReload(IEnumerable<FilterCriterion> criteria)
        {
            m_ItemFilterCriteria = criteria.ToArray();
            Reload();
        }

        private void PruneTreeByItemSearchFilter(ActionTreeItemBase item)
        {
            // Prune subtree if item is forced out by any of our criteria.
            if (m_ItemFilterCriteria.Any(x => x.Matches(item) == FilterCriterion.Match.Failure))
            {
                item.parent.children.Remove(item);

                // Add to list of hidden children.
                if (item.parent is ActionTreeItemBase parent)
                {
                    if (parent.m_HiddenChildren == null)
                        parent.m_HiddenChildren = new List<ActionTreeItemBase>();
                    parent.m_HiddenChildren.Add(item);
                }

                return;
            }

            ////REVIEW: should we *always* do this? (regardless of whether a control scheme is selected)
            // When filtering by binding group, we tag bindings that are not in any binding group as "{GLOBAL}".
            // This helps when having a specific control scheme selected, to also see the bindings that are active
            // in that control scheme by virtue of not being associated with *any* specific control scheme.
            if (item is BindingTreeItem bindingItem &&
                !(item is CompositeBindingTreeItem) &&
                string.IsNullOrEmpty(bindingItem.groups) &&
                m_ItemFilterCriteria.Any(x => x.type == FilterCriterion.Type.ByBindingGroup))
            {
                item.displayName += " {GLOBAL}";
            }

            // Prune children.
            if (item.hasChildren)
            {
                foreach (var child in item.children.OfType<ActionTreeItemBase>().ToArray()) // We're modifying the child list so copy.
                    PruneTreeByItemSearchFilter(child);
            }
        }

        #endregion

        #region Finding Items

        public ActionTreeItemBase FindItemByPath(string path)
        {
            var components = path.Split('/');
            var current = rootItem;
            foreach (var component in components)
            {
                if (current.hasChildren)
                {
                    var found = false;
                    foreach (var child in current.children)
                    {
                        if (child.displayName.Equals(component, StringComparison.InvariantCultureIgnoreCase))
                        {
                            current = child;
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue;
                }

                return null;
            }
            return (ActionTreeItemBase)current;
        }

        public ActionTreeItemBase FindItemByPropertyPath(string propertyPath)
        {
            return FindFirstItem<ActionTreeItemBase>(x => x.property.propertyPath == propertyPath);
        }

        public ActionTreeItemBase FindItemFor(SerializedProperty element)
        {
            // We may be looking at a SerializedProperty that refers to the same element but is
            // its own instance different from the one we're using in the tree. Compare properties
            // by path, not by object instance.
            return FindFirstItem<ActionTreeItemBase>(x => x.property.propertyPath == element.propertyPath);
        }

        public TItem FindFirstItem<TItem>(Func<TItem, bool> predicate)
            where TItem : ActionTreeItemBase
        {
            return FindFirstItemRecursive(rootItem, predicate);
        }

        private static TItem FindFirstItemRecursive<TItem>(TreeViewItem current, Func<TItem, bool> predicate)
            where TItem : ActionTreeItemBase
        {
            if (current is TItem itemOfType && predicate(itemOfType))
                return itemOfType;

            if (current.hasChildren)
                foreach (var child in current.children)
                {
                    var item = FindFirstItemRecursive(child, predicate);
                    if (item != null)
                        return item;
                }

            return null;
        }

        #endregion

        #region Selection

        public void ClearSelection()
        {
            SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
        }

        public void SelectItems(IEnumerable<ActionTreeItemBase> items)
        {
            SetSelection(items.Select(x => x.id).ToList(), TreeViewSelectionOptions.FireSelectionChanged);
        }

        public void SelectItem(SerializedProperty element, bool additive = false)
        {
            var item = FindItemFor(element);
            if (item == null)
                throw new ArgumentException($"Cannot find item for property path '{element.propertyPath}'", nameof(element));

            SelectItem(item, additive);
        }

        public void SelectItem(string path, bool additive = false)
        {
            if (!TrySelectItem(path, additive))
                throw new ArgumentException($"Cannot find item with path 'path'", nameof(path));
        }

        public bool TrySelectItem(string path, bool additive = false)
        {
            var item = FindItemByPath(path);
            if (item == null)
                return false;

            SelectItem(item, additive);
            return true;
        }

        public void SelectItem(ActionTreeItemBase item, bool additive = false)
        {
            if (additive)
            {
                var selection = new List<int>();
                selection.AddRange(GetSelection());
                selection.Add(item.id);
                SetSelection(selection, TreeViewSelectionOptions.FireSelectionChanged);
            }
            else
            {
                SetSelection(new[] { item.id }, TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        public IEnumerable<ActionTreeItemBase> GetSelectedItems()
        {
            foreach (var id in GetSelection())
            {
                if (FindItem(id, rootItem) is ActionTreeItemBase item)
                    yield return item;
            }
        }

        /// <summary>
        /// Same as <see cref="GetSelectedItems"/> but with items that are selected but are children of other items
        /// that also selected being filtered out.
        /// </summary>
        /// <remarks>
        /// This is useful for operations such as copy-paste where copy a parent will implicitly copy the child.
        /// </remarks>
        public IEnumerable<ActionTreeItemBase> GetSelectedItemsWithChildrenFilteredOut()
        {
            var selectedItems = GetSelectedItems().ToArray();
            foreach (var item in selectedItems)
            {
                if (selectedItems.Any(x => x.IsParentOf(item)))
                    continue;
                yield return item;
            }
        }

        public IEnumerable<TItem> GetSelectedItemsOrParentsOfType<TItem>()
            where TItem : ActionTreeItemBase
        {
            // If there is no selection and the root item has the type we're looking for,
            // consider it selected. This allows adding items at the toplevel.
            if (!HasSelection() && rootItem is TItem root)
            {
                yield return root;
            }
            else
            {
                foreach (var id in GetSelection())
                {
                    var item = FindItem(id, rootItem);
                    while (item != null)
                    {
                        if (item is TItem itemOfType)
                            yield return itemOfType;
                        item = item.parent;
                    }
                }
            }
        }

        public void SelectFirstToplevelItem()
        {
            if (rootItem.children.Any())
                SetSelection(new[] {rootItem.children[0].id}, TreeViewSelectionOptions.FireSelectionChanged);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            onSelectionChanged?.Invoke();
        }

        #endregion

        #region Renaming

        public new void BeginRename(TreeViewItem item)
        {
            // If a rename is already in progress, force it to end first.
            EndRename();
            onBeginRename?.Invoke((ActionTreeItemBase)item);
            base.BeginRename(item);
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item is ActionTreeItemBase actionTreeItem && actionTreeItem.canRename;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!(FindItem(args.itemID, rootItem) is ActionTreeItemBase actionItem))
                return;

            if (!(args.acceptedRename || m_ForceAcceptRename) || args.originalName == args.newName)
                return;

            Debug.Assert(actionItem.canRename, "Cannot rename " + actionItem);

            actionItem.Rename(args.newName);
            OnSerializedObjectModified();
        }

        public void EndRename(bool forceAccept)
        {
            m_ForceAcceptRename = forceAccept;
            EndRename();
            m_ForceAcceptRename = false;
        }

        protected override void DoubleClickedItem(int id)
        {
            if (!(FindItem(id, rootItem) is ActionTreeItemBase item))
                return;

            // If we have a double-click handler, give it control over what happens.
            if (onDoubleClick != null)
            {
                onDoubleClick(item);
            }
            else if (item.canRename)
            {
                // Otherwise, perform a rename by default.
                BeginRename(item);
            }
        }

        #endregion

        #region Drag&Drop

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("itemIDs", args.draggedItemIDs.ToArray());
            DragAndDrop.SetGenericData("tree", this);
            DragAndDrop.StartDrag(string.Join(",", args.draggedItemIDs.Select(id => FindItem(id, rootItem).displayName)));
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var sourceTree = DragAndDrop.GetGenericData("tree") as InputActionTreeView;
            if (sourceTree == null)
                return DragAndDropVisualMode.Rejected;

            var itemIds = (int[])DragAndDrop.GetGenericData("itemIDs");
            var altKeyIsDown = Event.current.alt;

            // Reject the drag if the parent item does not accept the drop.
            if (args.parentItem is ActionTreeItemBase parentItem)
            {
                if (itemIds.Any(id =>
                    !parentItem.AcceptsDrop((ActionTreeItemBase)sourceTree.FindItem(id, sourceTree.rootItem))))
                    return DragAndDropVisualMode.Rejected;
            }
            else
            {
                // If the root item isn't an ActionTreeItemBase, we're looking at a tree that starts
                // all the way up at the InputActionAsset. Require the drop to be all action maps.
                if (itemIds.Any(id => !(sourceTree.FindItem(id, sourceTree.rootItem) is ActionMapTreeItem)))
                    return DragAndDropVisualMode.Rejected;
            }

            // Handle drop using copy-paste. This allows handling all the various operations
            // using a single code path.
            var isMove = !altKeyIsDown;
            if (args.performDrop)
            {
                // Copy item data.
                var copyBuffer = new StringBuilder();
                var items = itemIds.Select(id => (ActionTreeItemBase)sourceTree.FindItem(id, sourceTree.rootItem));
                CopyItems(items, copyBuffer);

                // If we're moving items within the same tree, no need to generate new IDs.
                var assignNewIDs = !(isMove && sourceTree == this);

                // Determine where we are moving/copying the data.
                var target = args.parentItem ?? rootItem;
                int? childIndex = null;
                if (args.dragAndDropPosition == DragAndDropPosition.BetweenItems)
                    childIndex = args.insertAtIndex;

                // If alt isn't down (i.e. we're not duplicating), delete old items.
                // Do this *before* pasting so that assigning new names will not cause names to
                // change when just moving items around.
                if (isMove)
                {
                    // Don't use DeleteDataOfSelectedItems() as that will record as a separate operation.
                    foreach (var item in items)
                    {
                        // If we're dropping *between* items on the same parent as the current item and the
                        // index we're dropping at (in the parent, NOT in the array) is coming *after* this item,
                        // then deleting the item will shift the target index down by one.
                        if (item.parent == target && childIndex != null && childIndex > target.children.IndexOf(item))
                            --childIndex;

                        item.DeleteData();
                    }
                }

                // Paste items onto target.
                var oldBindingGroupForNewBindings = bindingGroupForNewBindings;
                try
                {
                    // With drag&drop, preserve binding groups.
                    bindingGroupForNewBindings = null;

                    PasteItems(copyBuffer.ToString(),
                        new[] { new InsertLocation { item = target, childIndex = childIndex } },
                        assignNewIDs: assignNewIDs);
                }
                finally
                {
                    bindingGroupForNewBindings = oldBindingGroupForNewBindings;
                }

                DragAndDrop.AcceptDrag();
            }

            return isMove ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Copy;
        }

        #endregion

        #region Copy&Paste

        // These need to correspond to what the editor is sending from the "Edit" menu.
        public const string k_CopyCommand = "Copy";
        public const string k_PasteCommand = "Paste";
        public const string k_DuplicateCommand = "Duplicate";
        public const string k_CutCommand = "Cut";
        public const string k_DeleteCommand = "Delete";
        public const string k_SoftDeleteCommand = "SoftDelete";

        public void HandleCopyPasteCommandEvent(Event uiEvent)
        {
            if (uiEvent.type == EventType.ValidateCommand)
            {
                switch (uiEvent.commandName)
                {
                    case k_CopyCommand:
                    case k_CutCommand:
                    case k_DuplicateCommand:
                    case k_DeleteCommand:
                    case k_SoftDeleteCommand:
                        if (HasSelection())
                            uiEvent.Use();
                        break;

                    case k_PasteCommand:
                        var systemCopyBuffer = EditorHelpers.GetSystemCopyBufferContents();
                        if (systemCopyBuffer != null && systemCopyBuffer.StartsWith(k_CopyPasteMarker))
                            uiEvent.Use();
                        break;
                }
            }
            else if (uiEvent.type == EventType.ExecuteCommand)
            {
                switch (uiEvent.commandName)
                {
                    case k_CopyCommand:
                        CopySelectedItemsToClipboard();
                        break;
                    case k_PasteCommand:
                        PasteDataFromClipboard();
                        break;
                    case k_CutCommand:
                        CopySelectedItemsToClipboard();
                        DeleteDataOfSelectedItems();
                        break;
                    case k_DuplicateCommand:
                        DuplicateSelection();
                        break;
                    case k_DeleteCommand:
                    case k_SoftDeleteCommand:
                        DeleteDataOfSelectedItems();
                        break;
                    default:
                        return;
                }
                uiEvent.Use();
            }
        }

        private void DuplicateSelection()
        {
            var buffer = new StringBuilder();

            // If we have a multi-selection, we want to perform the duplication as if each item
            // was duplicated individually. Meaning we paste each duplicate right after the item
            // it was duplicated from. So if, say, an action is selected at the beginning of the
            // tree and one is selected from the end of it, we still paste the copies into the
            // two separate locations correctly.
            //
            // Technically, if both parents and children are selected, we're order dependent here
            // but not sure we really need to care.

            var selection = GetSelection();
            ClearSelection();

            // Copy-paste each selected item in turn.
            var newItemIds = new List<int>();
            foreach (var id in selection)
            {
                SetSelection(new[] { id });

                buffer.Length = 0;
                CopySelectedItemsTo(buffer);
                PasteDataFrom(buffer.ToString());

                newItemIds.AddRange(GetSelection());
            }

            SetSelection(newItemIds);
        }

        internal const string k_CopyPasteMarker = "INPUTASSET ";
        private const string k_StartOfText = "\u0002";
        private const string k_StartOfHeading = "\u0001";
        private const string k_EndOfTransmission = "\u0004";
        private const string k_EndOfTransmissionBlock = "\u0017";

        /// <summary>
        /// Copy the currently selected items to the clipboard.
        /// </summary>
        /// <seealso cref="EditorGUIUtility.systemCopyBuffer"/>
        public void CopySelectedItemsToClipboard()
        {
            var copyBuffer = new StringBuilder();
            CopySelectedItemsTo(copyBuffer);
            EditorHelpers.SetSystemCopyBufferContents(copyBuffer.ToString());
        }

        public void CopySelectedItemsTo(StringBuilder buffer)
        {
            CopyItems(GetSelectedItemsWithChildrenFilteredOut(), buffer);
        }

        public static void CopyItems(IEnumerable<ActionTreeItemBase> items, StringBuilder buffer)
        {
            buffer.Append(k_CopyPasteMarker);
            foreach (var item in items)
            {
                CopyItemData(item, buffer);
                buffer.Append(k_EndOfTransmission);
            }
        }

        private static void CopyItemData(ActionTreeItemBase item, StringBuilder buffer)
        {
            buffer.Append(k_StartOfHeading);
            buffer.Append(item.GetType().Name);
            buffer.Append(k_StartOfText);
            // InputActionMaps have back-references to InputActionAssets. Make sure we ignore those.
            buffer.Append(item.property.CopyToJson(ignoreObjectReferences: true));
            buffer.Append(k_EndOfTransmissionBlock);

            if (!item.serializedDataIncludesChildren && item.hasChildrenIncludingHidden)
                foreach (var child in item.childrenIncludingHidden)
                    CopyItemData(child, buffer);
        }

        /// <summary>
        /// Remove the data from the currently selected items from the <see cref="SerializedObject"/>
        /// referenced by the tree's data.
        /// </summary>
        public void DeleteDataOfSelectedItems()
        {
            // When deleting data, indices will shift around. However, we do not delete elements by array indices
            // directly but rather by GUIDs which are used to look up array indices dynamically. This means that
            // we can safely delete the items without worrying about one deletion affecting the next.
            //
            // NOTE: It is important that we first fetch *all* of the selection filtered for parent/child duplicates.
            //       If we don't do so up front, the deletions happening later may start interacting with our
            //       parent/child test.
            var selection = GetSelectedItemsWithChildrenFilteredOut().ToArray();

            // Clear our current selection. If we don't do this first, TreeView will implicitly
            // clear the selection as items disappear but we will not see a selection change notification
            // being triggered.
            ClearSelection();

            DeleteItems(selection);
        }

        public void DeleteItems(IEnumerable<ActionTreeItemBase> items)
        {
            foreach (var item in items)
                item.DeleteData();

            OnSerializedObjectModified();
        }

        public bool HavePastableClipboardData()
        {
            var clipboard = EditorHelpers.GetSystemCopyBufferContents();
            return clipboard.StartsWith(k_CopyPasteMarker);
        }

        public void PasteDataFromClipboard()
        {
            PasteDataFrom(EditorHelpers.GetSystemCopyBufferContents());
        }

        public void PasteDataFrom(string copyBufferString)
        {
            if (!copyBufferString.StartsWith(k_CopyPasteMarker))
                return;

            var locations = GetSelectedItemsWithChildrenFilteredOut().Select(x => new InsertLocation { item = x }).ToList();
            if (locations.Count == 0)
                locations.Add(new InsertLocation { item = rootItem });

            ////REVIEW: filtering out children may remove the very item we need to get the right match for a copy block?
            PasteItems(copyBufferString, locations);
        }

        public struct InsertLocation
        {
            public TreeViewItem item;
            public int? childIndex;
        }

        public void PasteItems(string copyBufferString, IEnumerable<InsertLocation> locations, bool assignNewIDs = true, bool selectNewItems = true)
        {
            var newItemPropertyPaths = new List<string>();

            // Split buffer into transmissions and then into transmission blocks. Each transmission is an item subtree
            // meant to be pasted as a whole and each transmission block is a single chunk of serialized data.
            foreach (var transmission in copyBufferString.Substring(k_CopyPasteMarker.Length)
                     .Split(new[] {k_EndOfTransmission}, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var location in locations)
                    PasteBlocks(transmission, location, assignNewIDs, newItemPropertyPaths);
            }

            OnSerializedObjectModified();

            // If instructed to do so, go and select all newly added items.
            if (selectNewItems && newItemPropertyPaths.Count > 0)
            {
                // We may have pasted into a different tree view. Only select the items if we can find them in
                // our current tree view.
                var newItems = newItemPropertyPaths.Select(FindItemByPropertyPath).Where(x => x != null);
                if (newItems.Any())
                    SelectItems(newItems);
            }
        }

        private const string k_ActionMapTag = k_StartOfHeading + "ActionMapTreeItem" + k_StartOfText;
        private const string k_ActionTag = k_StartOfHeading + "ActionTreeItem" + k_StartOfText;
        private const string k_BindingTag = k_StartOfHeading + "BindingTreeItem" + k_StartOfText;
        private const string k_CompositeBindingTag = k_StartOfHeading + "CompositeBindingTreeItem" + k_StartOfText;
        private const string k_PartOfCompositeBindingTag = k_StartOfHeading + "PartOfCompositeBindingTreeItem" + k_StartOfText;

        private void PasteBlocks(string transmission, InsertLocation location, bool assignNewIDs, List<string> newItemPropertyPaths)
        {
            Debug.Assert(location.item != null, "Should have drop target");

            var blocks = transmission.Split(new[] {k_EndOfTransmissionBlock},
                StringSplitOptions.RemoveEmptyEntries);
            if (blocks.Length < 1)
                return;

            Type CopyTagToType(string tagName)
            {
                switch (tagName)
                {
                    case k_ActionMapTag: return typeof(ActionMapTreeItem);
                    case k_ActionTag: return typeof(ActionTreeItem);
                    case k_BindingTag: return typeof(BindingTreeItem);
                    case k_CompositeBindingTag: return typeof(CompositeBindingTreeItem);
                    case k_PartOfCompositeBindingTag: return typeof(PartOfCompositeBindingTreeItem);
                    default:
                        throw new Exception($"Unrecognized copy block tag '{tagName}'");
                }
            }

            SplitTagAndData(blocks[0], out var tag, out var data);

            // Determine where to drop the item.
            SerializedProperty array = null;
            var arrayIndex = -1;
            var itemType = CopyTagToType(tag);
            if (location.item is ActionTreeItemBase dropTarget)
            {
                // Specific case - Composite parts cannot be dropped into Bindings
                if (tag == k_PartOfCompositeBindingTag && location.item is not(CompositeBindingTreeItem or PartOfCompositeBindingTreeItem))
                    return;

                if (!dropTarget.GetDropLocation(itemType, location.childIndex, ref array, ref arrayIndex))
                    return;
            }
            else if (tag == k_ActionMapTag)
            {
                // Paste into InputActionAsset.
                array = serializedObject.FindProperty("m_ActionMaps");
                arrayIndex = location.childIndex ?? array.arraySize;
            }
            else
            {
                throw new InvalidOperationException($"Cannot paste {tag} into {location.item.displayName}");
            }

            // If not given a specific index, we paste onto the end of the array.
            if (arrayIndex == -1 || arrayIndex > array.arraySize)
                arrayIndex = array.arraySize;

            // Determine action to assign to pasted bindings.
            string actionForNewBindings = null;
            if (location.item is ActionTreeItem actionItem)
                actionForNewBindings = actionItem.name;
            else if (location.item is BindingTreeItem bindingItem)
                actionForNewBindings = bindingItem.action;

            // Paste new element.
            var newElement = PasteBlock(tag, data, array, arrayIndex, assignNewIDs, actionForNewBindings);
            newItemPropertyPaths.Add(newElement.propertyPath);

            // If the element can have children, read whatever blocks are following the current one (if any).
            if ((tag == k_ActionTag || tag == k_CompositeBindingTag) && blocks.Length > 1)
            {
                var bindingArray = array;

                if (tag == k_ActionTag)
                {
                    // We don't support pasting actions separately into action maps in the same paste operations so
                    // there must be an ActionMapTreeItem in the hierarchy we pasted into.
                    var actionMapItem = location.item.TryFindItemInHierarchy<ActionMapTreeItem>();
                    Debug.Assert(actionMapItem != null, "Cannot find ActionMapTreeItem in hierarchy of pasted action");
                    bindingArray = actionMapItem.bindingsArrayProperty;
                    actionForNewBindings = InputActionSerializationHelpers.GetName(newElement);
                }

                for (var i = 1; i < blocks.Length; ++i)
                {
                    SplitTagAndData(blocks[i], out var blockTag, out var blockData);

                    PasteBlock(blockTag, blockData, bindingArray,
                        tag == k_CompositeBindingTag ? arrayIndex + i : -1,
                        assignNewIDs,
                        actionForNewBindings);
                }
            }
        }

        private static void SplitTagAndData(string block, out string tag, out string data)
        {
            var indexOfStartOfTextChar = block.IndexOf(k_StartOfText);
            if (indexOfStartOfTextChar == -1)
                throw new ArgumentException($"Incorrect copy data format: Expecting '{k_StartOfText}' in '{block}'",
                    nameof(block));

            tag = block.Substring(0, indexOfStartOfTextChar + 1);
            data = block.Substring(indexOfStartOfTextChar + 1);
        }

        public static SerializedProperty AddElement(SerializedProperty arrayProperty, string name, int index = -1)
        {
            var uniqueName = InputActionSerializationHelpers.FindUniqueName(arrayProperty, name);
            if (index < 0)
                index = arrayProperty.arraySize;

            arrayProperty.InsertArrayElementAtIndex(index);
            var elementProperty = arrayProperty.GetArrayElementAtIndex(index);
            elementProperty.ResetValuesToDefault();

            elementProperty.FindPropertyRelative("m_Name").stringValue = uniqueName;
            elementProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();

            return elementProperty;
        }

        private SerializedProperty PasteBlock(string tag, string data, SerializedProperty array, int arrayIndex,
            bool assignNewIDs, string actionForNewBindings = null)
        {
            // Add an element to the array. Then read the serialized properties stored in the copy data
            // back into the element.
            var property = AddElement(array, "tempName", arrayIndex);
            property.RestoreFromJson(data);
            if (tag == k_ActionTag || tag == k_ActionMapTag)
                InputActionSerializationHelpers.EnsureUniqueName(property);
            if (assignNewIDs)
            {
                // Assign new IDs to the element as well as to any elements it contains. This means
                // that for action maps, we will also assign new IDs to every action and binding in the map.
                InputActionSerializationHelpers.AssignUniqueIDs(property);
            }

            // If the element is a binding, update its action target and binding group, if necessary.
            if (tag == k_BindingTag || tag == k_CompositeBindingTag || tag == k_PartOfCompositeBindingTag)
            {
                ////TODO: use {id} rather than plain name
                // Update action to refer to given action.
                InputActionSerializationHelpers.ChangeBinding(property, action: actionForNewBindings);

                // If we have a binding group to set for new bindings, overwrite the binding's
                // group with it.
                if (!string.IsNullOrEmpty(bindingGroupForNewBindings) && tag != k_CompositeBindingTag)
                {
                    InputActionSerializationHelpers.ChangeBinding(property,
                        groups: bindingGroupForNewBindings);
                }

                onBindingAdded?.Invoke(property);
            }

            return property;
        }

        #endregion

        #region Context Menus

        public void BuildContextMenuFor(Type itemType, GenericMenu menu, bool multiSelect, ActionTreeItem actionItem = null, bool noSelection = false)
        {
            var canRename = false;
            if (itemType == typeof(ActionMapTreeItem))
            {
                menu.AddItem(s_AddActionLabel, false, AddNewAction);
            }
            else if (itemType == typeof(ActionTreeItem))
            {
                canRename = true;
                BuildMenuToAddBindings(menu, actionItem);
            }
            else if (itemType == typeof(CompositeBindingTreeItem))
            {
                canRename = true;
            }
            else if (itemType == typeof(ActionMapListItem))
            {
                menu.AddItem(s_AddActionMapLabel, false, AddNewActionMap);
            }

            // Common menu entries shared by all types of items.
            menu.AddSeparator("");
            if (noSelection)
            {
                menu.AddDisabledItem(s_CutLabel);
                menu.AddDisabledItem(s_CopyLabel);
            }
            else
            {
                menu.AddItem(s_CutLabel, false, () =>
                {
                    CopySelectedItemsToClipboard();
                    DeleteDataOfSelectedItems();
                });
                menu.AddItem(s_CopyLabel, false, CopySelectedItemsToClipboard);
            }
            if (HavePastableClipboardData())
                menu.AddItem(s_PasteLabel, false, PasteDataFromClipboard);
            else
                menu.AddDisabledItem(s_PasteLabel);
            menu.AddSeparator("");
            if (!noSelection && canRename && !multiSelect)
                menu.AddItem(s_RenameLabel, false, () => BeginRename(GetSelectedItems().First()));
            else if (canRename)
                menu.AddDisabledItem(s_RenameLabel);
            if (noSelection)
            {
                menu.AddDisabledItem(s_DuplicateLabel);
                menu.AddDisabledItem(s_DeleteLabel);
            }
            else
            {
                menu.AddItem(s_DuplicateLabel, false, DuplicateSelection);
                menu.AddItem(s_DeleteLabel, false, DeleteDataOfSelectedItems);
            }

            if (itemType != typeof(ActionMapTreeItem))
            {
                menu.AddSeparator("");
                menu.AddItem(s_ExpandAllLabel, false, ExpandAll);
                menu.AddItem(s_CollapseAllLabel, false, CollapseAll);
            }
        }

        public void BuildMenuToAddBindings(GenericMenu menu, ActionTreeItem actionItem = null)
        {
            // Add entry to add "normal" bindings.
            menu.AddItem(s_AddBindingLabel, false,
                () =>
                {
                    if (actionItem != null)
                        AddNewBinding(actionItem.property, actionItem.actionMapProperty);
                    else
                        AddNewBinding();
                });

            // Add one entry for each registered type of composite binding.
            var expectedControlLayout = new InternedString(actionItem?.expectedControlLayout);
            foreach (var compositeName in InputBindingComposite.s_Composites.internedNames.Where(x =>
                !InputBindingComposite.s_Composites.aliases.Contains(x)).OrderBy(x => x))
            {
                // Skip composites we should hide from the UI.
                var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
                var designTimeVisible = compositeType.GetCustomAttribute<DesignTimeVisibleAttribute>();
                if (designTimeVisible != null && !designTimeVisible.Visible)
                    continue;

                // If the action is expected a specific control layout, check
                // whether the value type use by the composite matches that of
                // the layout.
                if (!expectedControlLayout.IsEmpty())
                {
                    var valueType = InputBindingComposite.GetValueType(compositeName);
                    if (valueType != null &&
                        !InputControlLayout.s_Layouts.ValueTypeIsAssignableFrom(expectedControlLayout, valueType))
                        continue;
                }

                var displayName = compositeType.GetCustomAttribute<DisplayNameAttribute>();
                var niceName = displayName != null ? displayName.DisplayName.Replace('/', '\\') : ObjectNames.NicifyVariableName(compositeName) + " Composite";
                menu.AddItem(new GUIContent($"Add {niceName}"), false,
                    () =>
                    {
                        if (actionItem != null)
                            AddNewComposite(actionItem.property, actionItem.actionMapProperty, compositeName);
                        else
                            AddNewComposite(compositeName);
                    });
            }
        }

        private void PopUpContextMenu()
        {
            // See if we have a selection of mixed types.
            var selected = GetSelectedItems().ToList();
            var mixedSelection = selected.Select(x => x.GetType()).Distinct().Count() > 1;
            var noSelection = selected.Count == 0;

            // Create and pop up context menu.
            var menu = new GenericMenu();
            if (noSelection)
            {
                BuildContextMenuFor(rootItem.GetType(), menu, true, noSelection: noSelection);
            }
            else if (mixedSelection)
            {
                BuildContextMenuFor(typeof(ActionTreeItemBase), menu, true, noSelection: noSelection);
            }
            else
            {
                var item = selected.First();
                BuildContextMenuFor(item.GetType(), menu, GetSelection().Count > 1, actionItem: item as ActionTreeItem);
            }
            menu.ShowAsContext();
        }

        protected override void ContextClickedItem(int id)
        {
            // When right-clicking an unselected item, TreeView does change the selection to the
            // clicked item but the visual feedback only comes in the *next* repaint. This means that
            // if we pop up a context menu right away here, the user does not correctly see which item
            // is affected.
            //
            // So, instead we force a repaint and open the context menu on the next OnGUI() call. Note
            // that we can't use something like EditorApplication.delayCall here as ShowAsContext()
            // can only be called from UI callbacks (otherwise it will simply be ignored).

            m_InitiateContextMenuOnNextRepaint = true;
            Repaint();

            Event.current.Use();
        }

        protected override void ContextClicked()
        {
            ClearSelection();
            m_InitiateContextMenuOnNextRepaint = true;
            Repaint();

            Event.current.Use();
        }

        #endregion

        #region Add New Items

        /// <summary>
        /// Add a new action map to the toplevel <see cref="InputActionAsset"/>.
        /// </summary>
        public void AddNewActionMap()
        {
            var actionMapProperty = InputActionSerializationHelpers.AddActionMap(serializedObject);
            var actionProperty = InputActionSerializationHelpers.AddAction(actionMapProperty);
            InputActionSerializationHelpers.AddBinding(actionProperty, actionMapProperty, groups: bindingGroupForNewBindings);
            OnNewItemAdded(actionMapProperty);
        }

        /// <summary>
        /// Add new action to the currently active action map(s).
        /// </summary>
        public void AddNewAction()
        {
            foreach (var actionMapItem in GetSelectedItemsOrParentsOfType<ActionMapTreeItem>())
                AddNewAction(actionMapItem.property);
        }

        public void AddNewAction(SerializedProperty actionMapProperty)
        {
            if (onHandleAddNewAction != null)
                onHandleAddNewAction(actionMapProperty);
            else
            {
                var actionProperty = InputActionSerializationHelpers.AddAction(actionMapProperty);
                InputActionSerializationHelpers.AddBinding(actionProperty, actionMapProperty, groups: bindingGroupForNewBindings);
                OnNewItemAdded(actionProperty);
            }
        }

        public void AddNewBinding()
        {
            foreach (var actionItem in GetSelectedItemsOrParentsOfType<ActionTreeItem>())
                AddNewBinding(actionItem.property, actionItem.actionMapProperty);
        }

        public void AddNewBinding(SerializedProperty actionProperty, SerializedProperty actionMapProperty)
        {
            var bindingProperty = InputActionSerializationHelpers.AddBinding(actionProperty, actionMapProperty,
                groups: bindingGroupForNewBindings);
            onBindingAdded?.Invoke(bindingProperty);
            OnNewItemAdded(bindingProperty);
        }

        public void AddNewComposite(string compositeType)
        {
            foreach (var actionItem in GetSelectedItemsOrParentsOfType<ActionTreeItem>())
                AddNewComposite(actionItem.property, actionItem.actionMapProperty, compositeType);
        }

        public void AddNewComposite(SerializedProperty actionProperty, SerializedProperty actionMapProperty, string compositeName)
        {
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
            if (compositeType == null)
                throw new ArgumentException($"Cannot find composite registration for {compositeName}",
                    nameof(compositeName));
            var compositeProperty = InputActionSerializationHelpers.AddCompositeBinding(actionProperty,
                actionMapProperty, compositeName, compositeType, groups: bindingGroupForNewBindings);
            onBindingAdded?.Invoke(compositeProperty);
            OnNewItemAdded(compositeProperty);
        }

        private void OnNewItemAdded(SerializedProperty property)
        {
            OnSerializedObjectModified();
            SelectItemAndBeginRename(property);
        }

        private void SelectItemAndBeginRename(SerializedProperty property)
        {
            var item = FindItemFor(property);
            if (item == null)
            {
                // if we could not find the item, try clearing search filters.
                ClearItemSearchFilterAndReload();
                item = FindItemFor(property);
            }
            Debug.Assert(item != null, $"Cannot find newly created item for {property.propertyPath}");
            SetExpandedRecursive(item.id, true);
            SelectItem(item);
            SetFocus();
            FrameItem(item.id);
            if (item.canRename)
                BeginRename(item);
        }

        #endregion

        #region Drawing

        public override void OnGUI(Rect rect)
        {
            if (m_InitiateContextMenuOnNextRepaint)
            {
                m_InitiateContextMenuOnNextRepaint = false;
                PopUpContextMenu();
            }

            if (ReloadIfSerializedObjectHasBeenChanged())
                return;

            // Draw border rect.
            EditorGUI.LabelField(rect, GUIContent.none, Styles.backgroundWithBorder);
            rect.x += 1;
            rect.y += 1;
            rect.height -= 1;
            rect.width -= 2;

            if (drawHeader)
                DrawHeader(ref rect);

            base.OnGUI(rect);

            HandleCopyPasteCommandEvent(Event.current);
        }

        private void DrawHeader(ref Rect rect)
        {
            var headerRect = rect;
            headerRect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            rect.y += headerRect.height;
            rect.height -= headerRect.height;

            // Draw label.
            EditorGUI.LabelField(headerRect, m_Title, Styles.columnHeaderLabel);

            // Draw minus button.
            var buttonRect = headerRect;
            buttonRect.width = EditorGUIUtility.singleLineHeight;
            buttonRect.x += rect.width - buttonRect.width - EditorGUIUtility.standardVerticalSpacing;
            if (drawMinusButton)
            {
                var minusButtonDisabled = !HasSelection();
                using (new EditorGUI.DisabledScope(minusButtonDisabled))
                {
                    if (GUI.Button(buttonRect, minusIcon, GUIStyle.none))
                        DeleteDataOfSelectedItems();
                }

                buttonRect.x -= buttonRect.width + EditorGUIUtility.standardVerticalSpacing;
            }

            // Draw plus button.
            if (drawPlusButton)
            {
                var plusIconDisabled = onBuildTree == null;
                using (new EditorGUI.DisabledScope(plusIconDisabled))
                {
                    if (GUI.Button(buttonRect, plusIcon, GUIStyle.none))
                    {
                        if (rootItem is ActionMapTreeItem mapItem)
                        {
                            AddNewAction(mapItem.property);
                        }
                        else if (rootItem is ActionTreeItem actionItem)
                        {
                            // Adding a composite has multiple options. Pop up a menu.
                            var menu = new GenericMenu();
                            BuildMenuToAddBindings(menu, actionItem);
                            menu.ShowAsContext();
                        }
                        else
                        {
                            AddNewActionMap();
                        }
                    }

                    buttonRect.x -= buttonRect.width + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            // Draw action properties button.
            if (drawActionPropertiesButton && rootItem is ActionTreeItem item)
            {
                if (GUI.Button(buttonRect, s_ActionPropertiesIcon, GUIStyle.none))
                    onDoubleClick?.Invoke(item);
            }
        }

        // For each item, we draw
        //  1) color tag
        //  2) foldout
        //  3) display name
        //  4) Line underneath item

        private const int kColorTagWidth = 6;
        private const int kFoldoutWidth = 15;

        ////FIXME: foldout hover region is way too large; partly overlaps the text of items
        private bool DrawFoldout(Rect position, bool expandedState, GUIStyle style)
        {
            // We don't get the depth of the item we're drawing the foldout for but we can
            // infer it by the amount that the given rectangle was indented.
            var indent = (int)(position.x / kFoldoutWidth);
            var indentLevel = EditorGUI.indentLevel;

            // When drawing input actions in the input actions editor, we don't want to offset the foldout
            // icon any further than the position that's passed in to this function, so take advantage of
            // the fact that indentLevel is always zero in that editor.
            position.x = EditorGUI.IndentedRect(position).x * Mathf.Clamp01(indentLevel) + kColorTagWidth + 2 + indent * kColorTagWidth;

            position.width = kFoldoutWidth;

            var hierarchyMode = EditorGUIUtility.hierarchyMode;

            // We remove the editor indent level and set hierarchy mode to false when drawing the foldout
            // arrow so that in the inspector we don't get additional padding on the arrow for the inspector
            // gutter, and so that the indent level doesn't apply because we've done that ourselves.
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.hierarchyMode = false;

            var foldoutExpanded = EditorGUI.Foldout(position, expandedState, GUIContent.none, true, style);

            EditorGUI.indentLevel = indentLevel;
            EditorGUIUtility.hierarchyMode = hierarchyMode;

            return foldoutExpanded;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (ActionTreeItemBase)args.item;
            var isRepaint = Event.current.type == EventType.Repaint;

            // Color tag at beginning of line.
            var colorTagRect = EditorGUI.IndentedRect(args.rowRect);
            colorTagRect.x += item.depth * kColorTagWidth;
            colorTagRect.width = kColorTagWidth;
            if (isRepaint)
                item.colorTagStyle.Draw(colorTagRect, GUIContent.none, false, false, false, false);

            // Text.
            // NOTE: When renaming, the renaming overlay gets drawn outside of our control so don't draw the label in that case
            //       as otherwise it will peak out from underneath the overlay.
            if (!args.isRenaming && isRepaint)
            {
                var text = item.displayName;
                var textRect = GetTextRect(args.rowRect, item);

                var style = args.selected ? Styles.selectedText : Styles.text;

                if (item.showWarningIcon)
                {
                    var content = new GUIContent(text, EditorGUIUtility.FindTexture("console.warnicon.sml"));
                    style.Draw(textRect, content, false, false, args.selected, args.focused);
                }
                else
                    style.Draw(textRect, text, false, false, args.selected, args.focused);
            }

            // Bottom line.
            var lineRect = EditorGUI.IndentedRect(args.rowRect);
            lineRect.y += lineRect.height - 1;
            lineRect.height = 1;
            if (isRepaint)
                Styles.border.Draw(lineRect, GUIContent.none, false, false, false, false);

            // For action items, add a dropdown menu to add bindings.
            if (item is ActionTreeItem actionItem)
            {
                var buttonRect = args.rowRect;
                buttonRect.x = buttonRect.width - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                buttonRect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                if (GUI.Button(buttonRect, s_PlusBindingIcon, GUIStyle.none))
                {
                    var menu = new GenericMenu();
                    BuildMenuToAddBindings(menu, actionItem);
                    menu.ShowAsContext();
                }
            }
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return 18;
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            var textRect = GetTextRect(rowRect, item, false);
            textRect.x += 2;
            textRect.height -= 2;
            return textRect;
        }

        private Rect GetTextRect(Rect rowRect, TreeViewItem item, bool applyIndent = true)
        {
            var indent = (item.depth + 1) * kColorTagWidth + kFoldoutWidth;
            var textRect = applyIndent ? EditorGUI.IndentedRect(rowRect) : rowRect;
            textRect.x += indent;
            return textRect;
        }

        #endregion

        // Undo is a problem. When an undo or redo is performed, the SerializedObject may change behind
        // our backs which means that the information shown in the tree may be outdated now.
        //
        // We do have the Undo.undoRedoPerformed global callback but because PropertyDrawers
        // have no observable life cycle, we cannot always easily hook into the callback and force a reload
        // of the tree. Also, while returning false from PropertyDrawer.CanCacheInspectorGUI() might one make suspect that
        // a PropertyDrawer would automatically be thrown away and recreated if the SerializedObject
        // is modified by undo, that does not happen in practice.
        //
        // We could just Reload() the tree all the time but TreeView.Reload() itself forces a repaint and
        // this will thus easily lead to infinite repaints.
        //
        // So, what we do is make use of the built-in dirty count we can get for Unity objects. If the count
        // changes and it wasn't caused by us, we reload the tree. Means we still reload unnecessarily if
        // some other property on a component changes but at least we don't reload all the time.
        //
        // A positive side-effect is that we will catch *any* change to the SerializedObject, not just
        // undo/redo and we can do so without having to hook into Undo.undoRedoPerformed anywhere.

        private void OnSerializedObjectModified()
        {
            serializedObject.ApplyModifiedProperties();
            UpdateSerializedObjectDirtyCount();
            Reload();
            onSerializedObjectModified?.Invoke();
        }

        public void UpdateSerializedObjectDirtyCount()
        {
            m_SerializedObjectDirtyCount = serializedObject != null ? EditorUtility.GetDirtyCount(serializedObject.targetObject) : 0;
        }

        private bool ReloadIfSerializedObjectHasBeenChanged()
        {
            var oldCount = m_SerializedObjectDirtyCount;
            UpdateSerializedObjectDirtyCount();
            if (oldCount != m_SerializedObjectDirtyCount)
            {
                Reload();
                onSerializedObjectModified?.Invoke();
                return true;
            }
            return false;
        }

        public SerializedObject serializedObject { get; }
        public string bindingGroupForNewBindings { get; set; }
        public new TreeViewItem rootItem => base.rootItem;

        public Action onSerializedObjectModified { get; set; }
        public Action onSelectionChanged { get; set; }
        public Action<ActionTreeItemBase> onDoubleClick { get; set; }
        public Action<ActionTreeItemBase> onBeginRename { get; set; }
        public Func<TreeViewItem> onBuildTree { get; set; }
        public Action<SerializedProperty> onBindingAdded { get; set; }

        public bool drawHeader { get; set; }
        public bool drawPlusButton { get; set; }
        public bool drawMinusButton { get; set; }
        public bool drawActionPropertiesButton { get; set; }

        public Action<SerializedProperty> onHandleAddNewAction { get; set; }

        public (string, string) title
        {
            get => (m_Title?.text, m_Title?.tooltip);
            set => m_Title = new GUIContent(value.Item1, value.Item2);
        }

        public new float totalHeight
        {
            get
            {
                var height = base.totalHeight;
                if (drawHeader)
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                height += 1; // Border.
                return height;
            }
        }

        public ActionTreeItemBase this[string path]
        {
            get
            {
                var item = FindItemByPath(path);
                if (item == null)
                    throw new KeyNotFoundException(path);
                return item;
            }
        }

        private GUIContent plusIcon
        {
            get
            {
                if (rootItem is ActionMapTreeItem)
                    return s_PlusActionIcon;
                if (rootItem is ActionTreeItem)
                    return s_PlusBindingIcon;
                return s_PlusActionMapIcon;
            }
        }

        private GUIContent minusIcon => s_DeleteSectionIcon;

        private FilterCriterion[] m_ItemFilterCriteria;
        private GUIContent m_Title;
        private bool m_InitiateContextMenuOnNextRepaint;
        private bool m_ForceAcceptRename;
        private int m_SerializedObjectDirtyCount;

        private static readonly GUIContent s_AddBindingLabel = EditorGUIUtility.TrTextContent("Add Binding");
        private static readonly GUIContent s_AddActionLabel = EditorGUIUtility.TrTextContent("Add Action");
        private static readonly GUIContent s_AddActionMapLabel = EditorGUIUtility.TrTextContent("Add Action Map");
        private static readonly GUIContent s_PlusBindingIcon = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Add Binding");
        private static readonly GUIContent s_PlusActionIcon = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add Action");
        private static readonly GUIContent s_PlusActionMapIcon = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add Action Map");
        private static readonly GUIContent s_DeleteSectionIcon = EditorGUIUtility.TrIconContent("Toolbar Minus", "Delete Selection");
        private static readonly GUIContent s_ActionPropertiesIcon = EditorGUIUtility.TrIconContent("Settings", "Action Properties");

        private static readonly GUIContent s_CutLabel = EditorGUIUtility.TrTextContent("Cut");
        private static readonly GUIContent s_CopyLabel = EditorGUIUtility.TrTextContent("Copy");
        private static readonly GUIContent s_PasteLabel = EditorGUIUtility.TrTextContent("Paste");
        private static readonly GUIContent s_DeleteLabel = EditorGUIUtility.TrTextContent("Delete");
        private static readonly GUIContent s_DuplicateLabel = EditorGUIUtility.TrTextContent("Duplicate");
        private static readonly GUIContent s_RenameLabel = EditorGUIUtility.TrTextContent("Rename");
        private static readonly GUIContent s_ExpandAllLabel = EditorGUIUtility.TrTextContent("Expand All");
        private static readonly GUIContent s_CollapseAllLabel = EditorGUIUtility.TrTextContent("Collapse All");

        public static string SharedResourcesPath = "Packages/com.unity.inputsystem/InputSystem/Editor/AssetEditor/PackageResources/";
        public static string ResourcesPath
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return SharedResourcesPath + "pro/";
                return SharedResourcesPath + "personal/";
            }
        }

        public struct FilterCriterion
        {
            public enum Type
            {
                ByName,
                ByBindingGroup,
                ByDeviceLayout,
            }

            public enum Match
            {
                Success,
                Failure,
                None,
            }

            public string text;
            public Type type;

            public static string k_BindingGroupTag = "g:";
            public static string k_DeviceLayoutTag = "d:";

            public Match Matches(ActionTreeItemBase item)
            {
                Debug.Assert(item != null, "Item cannot be null");

                switch (type)
                {
                    case Type.ByName:
                    {
                        // NOTE: Composite items have names (and part bindings in a way, too) but we don't filter on them.
                        if (item is ActionMapTreeItem || item is ActionTreeItem)
                        {
                            var matchesSelf = item.displayName.Contains(text, StringComparison.InvariantCultureIgnoreCase);

                            // Name filters behave recursively. I.e. if any item in the subtree is matched by the name filter,
                            // the item is included.
                            if (!matchesSelf && CheckChildrenFor(Match.Success, item))
                                return Match.Success;

                            return matchesSelf ? Match.Success : Match.Failure;
                        }
                        break;
                    }

                    case Type.ByBindingGroup:
                    {
                        if (item is BindingTreeItem bindingItem)
                        {
                            // For composites, succeed the match if any children match.
                            if (item is CompositeBindingTreeItem)
                                return CheckChildrenFor(Match.Success, item) ? Match.Success : Match.Failure;

                            // Items that are in no binding group match any binding group.
                            if (string.IsNullOrEmpty(bindingItem.groups))
                                return Match.Success;

                            var groups = bindingItem.groups.Split(InputBinding.Separator);
                            var bindingGroup = text;
                            return groups.Any(x => x.Equals(bindingGroup, StringComparison.InvariantCultureIgnoreCase))
                                ? Match.Success
                                : Match.Failure;
                        }
                        break;
                    }

                    case Type.ByDeviceLayout:
                    {
                        if (item is BindingTreeItem bindingItem)
                        {
                            // For composites, succeed the match if any children match.
                            if (item is CompositeBindingTreeItem)
                                return CheckChildrenFor(Match.Success, item) ? Match.Success : Match.Failure;

                            var deviceLayout = InputControlPath.TryGetDeviceLayout(bindingItem.path);
                            return string.Equals(deviceLayout, text, StringComparison.InvariantCultureIgnoreCase)
                                || InputControlLayout.s_Layouts.IsBasedOn(new InternedString(deviceLayout), new InternedString(text))
                                ? Match.Success
                                : Match.Failure;
                        }
                        break;
                    }
                }

                return Match.None;
            }

            private bool CheckChildrenFor(Match match, ActionTreeItemBase item)
            {
                if (!item.hasChildren)
                    return false;

                foreach (var child in item.children.OfType<ActionTreeItemBase>())
                    if (Matches(child) == match)
                        return true;

                return false;
            }

            public static FilterCriterion ByName(string name)
            {
                return new FilterCriterion {text = name, type = Type.ByName};
            }

            public static FilterCriterion ByBindingGroup(string group)
            {
                return new FilterCriterion {text = group, type = Type.ByBindingGroup};
            }

            public static FilterCriterion ByDeviceLayout(string layout)
            {
                return new FilterCriterion {text = layout, type = Type.ByDeviceLayout};
            }

            public static List<FilterCriterion> FromString(string criteria)
            {
                if (string.IsNullOrEmpty(criteria))
                    return null;

                var list = new List<FilterCriterion>();
                foreach (var substring in criteria.Tokenize())
                {
                    if (substring.StartsWith(k_DeviceLayoutTag))
                        list.Add(ByDeviceLayout(substring.Substr(2).Unescape()));
                    else if (substring.StartsWith(k_BindingGroupTag))
                        list.Add(ByBindingGroup(substring.Substr(2).Unescape()));
                    else
                        list.Add(ByName(substring.ToString().Unescape()));
                }

                return list;
            }

            public static string ToString(IEnumerable<FilterCriterion> criteria)
            {
                var builder = new StringBuilder();
                foreach (var criterion in criteria)
                {
                    if (builder.Length > 0)
                        builder.Append(' ');

                    if (criterion.type == Type.ByBindingGroup)
                        builder.Append(k_BindingGroupTag);
                    else if (criterion.type == Type.ByDeviceLayout)
                        builder.Append(k_DeviceLayoutTag);

                    builder.Append(criterion.text);
                }
                return builder.ToString();
            }
        }

        public static class Styles
        {
            public static readonly GUIStyle text = new GUIStyle("Label").WithAlignment(TextAnchor.MiddleLeft);
            public static readonly GUIStyle selectedText = new GUIStyle("Label").WithAlignment(TextAnchor.MiddleLeft).WithNormalTextColor(Color.white);
            public static readonly GUIStyle backgroundWithoutBorder = new GUIStyle("Label")
                .WithNormalBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "actionTreeBackgroundWithoutBorder.png"));
            public static readonly GUIStyle border = new GUIStyle("Label")
                .WithNormalBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "actionTreeBackground.png"))
                .WithBorder(new RectOffset(0, 0, 0, 1));
            public static readonly GUIStyle backgroundWithBorder = new GUIStyle("Label")
                .WithNormalBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "actionTreeBackground.png"))
                .WithBorder(new RectOffset(3, 3, 3, 3))
                .WithMargin(new RectOffset(4, 4, 4, 4));
            public static readonly GUIStyle columnHeaderLabel = new GUIStyle(EditorStyles.toolbar)
                .WithAlignment(TextAnchor.MiddleLeft)
                .WithFontStyle(FontStyle.Bold)
                .WithPadding(new RectOffset(10, 6, 0, 0));
        }

        // Just so that we can tell apart TreeViews containing only maps.
        internal class ActionMapListItem : TreeViewItem
        {
        }
    }
}
#endif // UNITY_EDITOR
