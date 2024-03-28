// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using CmdEvents = UnityEngine.InputSystem.Editor.InputActionsEditorConstants.CommandEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A view for displaying the actions of the selected action map in a tree with bindings
    /// as children.
    /// </summary>
    internal class ActionsTreeView : ViewBase<ActionsTreeView.ViewState>
    {
        private readonly TreeView m_ActionsTreeView;
        private readonly Button m_AddActionButton;
        private readonly ScrollView m_PropertiesScrollview;

        private bool m_RenameOnActionAdded;
        private readonly CollectionViewSelectionChangeFilter m_ActionsTreeViewSelectionChangeFilter;

        //save TreeView element id's of individual input actions and bindings to ensure saving of expanded state
        private Dictionary<Guid, int> m_GuidToTreeViewId;

        public ActionsTreeView(VisualElement root, StateContainer stateContainer)
            : base(root, stateContainer)
        {
            m_AddActionButton = root.Q<Button>("add-new-action-button");
            m_PropertiesScrollview = root.Q<ScrollView>("properties-scrollview");
            m_ActionsTreeView = root.Q<TreeView>("actions-tree-view");
            //assign unique viewDataKey to store treeView states like expanded/collapsed items - make it unique to avoid conflicts with other TreeViews
            m_ActionsTreeView.viewDataKey = "InputActionTreeView " + stateContainer.GetState().serializedObject.targetObject.GetInstanceID();
            m_GuidToTreeViewId = new Dictionary<Guid, int>();
            m_ActionsTreeView.selectionType = UIElements.SelectionType.Single;
            m_ActionsTreeView.makeItem = () => new InputActionsTreeViewItem();
            m_ActionsTreeView.reorderable = true;
            m_ActionsTreeView.bindItem = (e, i) =>
            {
                var item = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(i);
                e.Q<Label>("name").text = item.name;
                var addBindingButton = e.Q<Button>("add-new-binding-button");
                addBindingButton.AddToClassList(EditorGUIUtility.isProSkin ? "add-binging-button-dark-theme" : "add-binging-button");
                var treeViewItem = (InputActionsTreeViewItem)e;
                if (item.isComposite)
                    ContextMenu.GetContextMenuForCompositeItem(this, treeViewItem, i);
                else if (item.isAction)
                    ContextMenu.GetContextMenuForActionItem(this, treeViewItem, item.controlLayout, i);
                else
                    ContextMenu.GetContextMenuForBindingItem(this, treeViewItem, i);

                if (item.isAction)
                {
                    Action action = ContextMenu.GetContextMenuForActionAddItem(this, item.controlLayout, i);
                    addBindingButton.clicked += action;
                    addBindingButton.userData = action; // Store to use in unbindItem
                    addBindingButton.clickable.activators.Add(new ManipulatorActivationFilter(){button = MouseButton.RightMouse});
                    addBindingButton.style.display = DisplayStyle.Flex;
                    treeViewItem.EditTextFinishedCallback = newName =>
                    {
                        ChangeActionOrCompositName(item, newName);
                    };
                    treeViewItem.EditTextFinished += treeViewItem.EditTextFinishedCallback;
                }
                else
                {
                    addBindingButton.style.display = DisplayStyle.None;
                    if (!item.isComposite)
                        treeViewItem.UnregisterInputField();
                    else
                    {
                        treeViewItem.EditTextFinishedCallback = newName =>
                        {
                            ChangeActionOrCompositName(item, newName);
                        };
                        treeViewItem.EditTextFinished += treeViewItem.EditTextFinishedCallback;
                    }
                }

                if (!string.IsNullOrEmpty(item.controlLayout))
                    e.Q<VisualElement>("icon").style.backgroundImage =
                        new StyleBackground(
                            EditorInputControlLayoutCache.GetIconForLayout(item.controlLayout));
                else
                    e.Q<VisualElement>("icon").style.backgroundImage =
                        new StyleBackground(
                            EditorInputControlLayoutCache.GetIconForLayout("Control"));

                e.SetEnabled(!item.isCut);
            };

            m_ActionsTreeView.itemsChosen += objects =>
            {
                var data = (ActionOrBindingData)objects.First();
                if (!data.isAction && !data.isComposite)
                    return;
                var item = m_ActionsTreeView.GetRootElementForIndex(m_ActionsTreeView.selectedIndex).Q<InputActionsTreeViewItem>();
                item.FocusOnRenameTextField();
            };

            m_ActionsTreeView.unbindItem = (element, i) =>
            {
                var item = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(i);
                var treeViewItem = (InputActionsTreeViewItem)element;
                //reset the editing variable before reassigning visual elements
                if (item.isAction || item.isComposite)
                    treeViewItem.Reset();

                if (item.isAction)
                {
                    var button = element.Q<Button>("add-new-binding-button");
                    button.clicked -= button.userData as Action;
                }

                treeViewItem.EditTextFinished -= treeViewItem.EditTextFinishedCallback;
            };

            ContextMenu.GetContextMenuForActionListView(this, m_ActionsTreeView, m_ActionsTreeView.parent);
            ContextMenu.GetContextMenuForActionsEmptySpace(this, m_ActionsTreeView, root.Q<VisualElement>("rclick-area-to-add-new-action"));

            m_ActionsTreeViewSelectionChangeFilter = new CollectionViewSelectionChangeFilter(m_ActionsTreeView);
            m_ActionsTreeViewSelectionChangeFilter.selectedIndicesChanged += (_) =>
            {
                if (m_ActionsTreeView.selectedIndex >= 0)
                {
                    var item = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(m_ActionsTreeView.selectedIndex);
                    Dispatch(item.isAction ? Commands.SelectAction(item.name) : Commands.SelectBinding(item.bindingIndex));
                }
                else
                {
                    Dispatch(Commands.SelectAction(null));
                    Dispatch(Commands.SelectBinding(-1));
                }
            };

            m_ActionsTreeView.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            m_ActionsTreeView.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            m_ActionsTreeView.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            m_ActionsTreeView.RegisterCallback<DragPerformEvent>(OnDraggedItem);

            // ISXB-748 - Scrolling the view causes a visual glitch with the rename TextField. As a work-around we
            // need to cancel the rename operation in this scenario.
            m_ActionsTreeView.RegisterCallback<WheelEvent>(e => InputActionsTreeViewItem.CancelRename(), TrickleDown.TrickleDown);

            CreateSelector(Selectors.GetActionsForSelectedActionMap, Selectors.GetActionMapCount,
                (_, count, state) =>
                {
                    var treeData = Selectors.GetActionsAsTreeViewData(state, m_GuidToTreeViewId);
                    return new ViewState
                    {
                        treeViewData = treeData,
                        actionMapCount = count ?? 0,
                        newElementID = GetSelectedElementId(state, treeData)
                    };
                });

            m_AddActionButton.clicked += AddAction;
        }

        private int GetSelectedElementId(InputActionsEditorState state, List<TreeViewItemData<ActionOrBindingData>> treeData)
        {
            var id = -1;
            if (state.selectionType == SelectionType.Action)
            {
                if (treeData.Count > state.selectedActionIndex && state.selectedActionIndex >= 0)
                    id = treeData[state.selectedActionIndex].id;
            }
            else if (state.selectionType == SelectionType.Binding)
                id = GetComponentOrBindingID(treeData, state.selectedBindingIndex);
            return id;
        }

        private int GetComponentOrBindingID(List<TreeViewItemData<ActionOrBindingData>> treeItemList, int selectedBindingIndex)
        {
            foreach (var actionItem in treeItemList)
            {
                // Look for the element ID by checking if the selected binding index matches the binding index of
                // the ActionOrBindingData of the item. Deals with composite bindings as well.
                foreach (var bindingOrComponentItem in actionItem.children)
                {
                    if (bindingOrComponentItem.data.bindingIndex == selectedBindingIndex)
                        return bindingOrComponentItem.id;
                    if (bindingOrComponentItem.hasChildren)
                    {
                        foreach (var bindingItem in bindingOrComponentItem.children)
                        {
                            if (bindingOrComponentItem.data.bindingIndex == selectedBindingIndex)
                                return bindingItem.id;
                        }
                    }
                }
            }
            return -1;
        }

        public override void DestroyView()
        {
            m_AddActionButton.clicked -= AddAction;
        }

        public override void RedrawUI(ViewState viewState)
        {
            m_ActionsTreeView.Clear();
            m_ActionsTreeView.SetRootItems(viewState.treeViewData);
            m_ActionsTreeView.Rebuild();
            if (viewState.newElementID != -1)
            {
                m_ActionsTreeView.SetSelectionById(viewState.newElementID);
                m_ActionsTreeView.ScrollToItemById(viewState.newElementID);
            }
            RenameNewAction(viewState.newElementID);;
            m_AddActionButton.SetEnabled(viewState.actionMapCount > 0);

            // Don't want to show action properties if there's no actions.
            m_PropertiesScrollview.visible = m_ActionsTreeView.GetTreeCount() > 0;
        }

        private void OnDraggedItem(DragPerformEvent evt)
        {
            bool discardDrag = false;
            foreach (var index in m_ActionsTreeView.selectedIndices)
            {
                // currentTarget & target are always in TreeView as the event is registered on the TreeView - we need to discard drags into other parts of the editor (e.g. the maps list view)
                var treeView = m_ActionsTreeView.panel.Pick(evt.mousePosition)?.GetFirstAncestorOfType<TreeView>();
                if (treeView is null || treeView != m_ActionsTreeView)
                {
                    discardDrag = true;
                    break;
                }
                var draggedItemData = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(index);
                var itemID = m_ActionsTreeView.GetIdForIndex(index);
                var childIndex = m_ActionsTreeView.viewController.GetChildIndexForId(itemID);
                var parentId = m_ActionsTreeView.viewController.GetParentId(itemID);
                ActionOrBindingData? directParent = parentId == -1 ? null : m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(m_ActionsTreeView.viewController.GetIndexForId(parentId));
                if (draggedItemData.isAction)
                {
                    if (!MoveAction(directParent, draggedItemData, childIndex))
                    {
                        discardDrag = true;
                        break;
                    }
                }
                else if (!draggedItemData.isPartOfComposite)
                {
                    if (!MoveBindingOrComposite(directParent, draggedItemData, childIndex))
                    {
                        discardDrag = true;
                        break;
                    }
                }
                else if (!MoveCompositeParts(directParent, childIndex, draggedItemData))
                {
                    discardDrag = true;
                    break;
                }
            }

            if (!discardDrag) return;
            var selectedItem = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(m_ActionsTreeView.selectedIndices.First());
            Dispatch(selectedItem.isAction
                ? Commands.SelectAction(selectedItem.name)
                : Commands.SelectBinding(selectedItem.bindingIndex));
            //TODO find a better way to reject the drag (for better visual feedback & to not run an extra command)
        }

        private bool MoveAction(ActionOrBindingData? directParent, ActionOrBindingData draggedItemData, int childIndex)
        {
            if (directParent != null)
                return false;
            Dispatch(Commands.MoveAction(draggedItemData.actionIndex, childIndex));
            return true;
        }

        private bool MoveBindingOrComposite(ActionOrBindingData? directParent, ActionOrBindingData draggedItemData, int childIndex)
        {
            if (directParent == null || !directParent.Value.isAction)
                return false;
            if (draggedItemData.isComposite)
                Dispatch(Commands.MoveComposite(draggedItemData.bindingIndex, directParent.Value.actionIndex, childIndex));
            else
                Dispatch(Commands.MoveBinding(draggedItemData.bindingIndex, directParent.Value.actionIndex, childIndex));
            return true;
        }

        private bool MoveCompositeParts(ActionOrBindingData? directParent, int childIndex, ActionOrBindingData draggedItemData)
        {
            if (directParent == null || !directParent.Value.isComposite)
                return false;
            var newBindingIndex = directParent.Value.bindingIndex + childIndex + (directParent.Value.bindingIndex > draggedItemData.bindingIndex ? 0 : 1);
            Dispatch(Commands.MovePartOfComposite(draggedItemData.bindingIndex, newBindingIndex, directParent.Value.bindingIndex));
            return true;
        }

        private void RenameNewAction(int id)
        {
            if (!m_RenameOnActionAdded || id == -1)
                return;
            m_ActionsTreeView.ScrollToItemById(id);
            var treeViewItem = m_ActionsTreeView.GetRootElementForId(id)?.Q<InputActionsTreeViewItem>();
            treeViewItem?.FocusOnRenameTextField();
        }

        internal void RenameActionItem(int index)
        {
            m_ActionsTreeView.ScrollToItem(index);
            m_ActionsTreeView.GetRootElementForIndex(index)?.Q<InputActionsTreeViewItem>()?.FocusOnRenameTextField();
        }

        internal void AddAction()
        {
            Dispatch(Commands.AddAction());
            m_RenameOnActionAdded = true;
        }

        internal void AddBinding(int index)
        {
            Dispatch(Commands.SelectAction(m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(index).actionIndex));
            Dispatch(Commands.AddBinding());
        }

        internal void AddComposite(int index, string compositeType)
        {
            Dispatch(Commands.SelectAction(m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(index).actionIndex));
            Dispatch(Commands.AddComposite(compositeType));
        }

        internal void DeleteItem(int selectedIndex)
        {
            var data = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(selectedIndex);

            if (data.isAction)
                Dispatch(Commands.DeleteAction(data.actionMapIndex, data.name));
            else
                Dispatch(Commands.DeleteBinding(data.actionMapIndex, data.bindingIndex));

            // Deleting an item sometimes causes the UI Panel to lose focus; make sure we keep it
            m_ActionsTreeView.Focus();
        }

        internal void DuplicateItem(int selectedIndex)
        {
            var data = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(selectedIndex);

            Dispatch(data.isAction ? Commands.DuplicateAction() : Commands.DuplicateBinding());
        }

        internal void CopyItems()
        {
            Dispatch(Commands.CopyActionBindingSelection());
        }

        internal void CutItems()
        {
            Dispatch(Commands.CutActionsOrBindings());
        }

        internal void PasteItems()
        {
            Dispatch(Commands.PasteActionsOrBindings(InputActionsEditorView.s_OnPasteCutElements));
        }

        private void ChangeActionOrCompositName(ActionOrBindingData data, string newName)
        {
            m_RenameOnActionAdded = false;

            if (data.isAction)
                Dispatch(Commands.ChangeActionName(data.actionMapIndex, data.name, newName));
            else if (data.isComposite)
                Dispatch(Commands.ChangeCompositeName(data.actionMapIndex, data.bindingIndex, newName));
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (m_ActionsTreeView.selectedItem == null)
                return;

            if (allowUICommandExecution)
            {
                var data = (ActionOrBindingData)m_ActionsTreeView.selectedItem;
                switch (evt.commandName)
                {
                    case CmdEvents.Rename:
                        if (data.isAction || data.isComposite)
                            RenameActionItem(m_ActionsTreeView.selectedIndex);
                        else
                            return;
                        break;
                    case CmdEvents.Delete:
                    case CmdEvents.SoftDelete:
                        DeleteItem(m_ActionsTreeView.selectedIndex);
                        break;
                    case CmdEvents.Duplicate:
                        DuplicateItem(m_ActionsTreeView.selectedIndex);
                        break;
                    case CmdEvents.Copy:
                        CopyItems();
                        break;
                    case CmdEvents.Cut:
                        CutItems();
                        break;
                    case CmdEvents.Paste:
                        var hasPastableData = CopyPasteHelper.HasPastableClipboardData(data.isAction ? typeof(InputAction) : typeof(InputBinding));
                        if (hasPastableData)
                            PasteItems();
                        break;
                    default:
                        return; // Skip StopPropagation if we didn't execute anything
                }

                // Prevent any UI commands from executing until after UI has been updated
                allowUICommandExecution = false;
            }
            evt.StopPropagation();
        }

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            // Mark commands as supported for Execute by stopping propagation of the event
            switch (evt.commandName)
            {
                case CmdEvents.Rename:
                case CmdEvents.Delete:
                case CmdEvents.SoftDelete:
                case CmdEvents.Duplicate:
                case CmdEvents.Copy:
                case CmdEvents.Cut:
                case CmdEvents.Paste:
                    evt.StopPropagation();
                    break;
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            // Allow right clicks to select an item before we bring up the matching context menu.
            if (evt.button == (int)MouseButton.RightMouse && evt.clickCount == 1)
            {
                // Look upwards to the immediate child of the scroll view, so we know what Index to use
                var element = evt.target as VisualElement;
                while (element != null && element.name != "unity-tree-view__item")
                    element = element.parent;

                if (element == null)
                    return;

                m_ActionsTreeView.SetSelection(element.parent.IndexOf(element));
            }
        }

        private string GetPreviousActionNameFromViewTree(in ActionOrBindingData data)
        {
            Debug.Assert(data.isAction);

            // If TreeView currently (before delete) has more than one Action, select the one immediately
            // above or immediately below depending if data is first in the list
            var treeView = ViewStateSelector.GetViewState(stateContainer.GetState()).treeViewData;
            if (treeView.Count > 1)
            {
                string actionName = data.name;
                int index = treeView.FindIndex(item => item.data.name == actionName);
                if (index > 0)
                    index--;
                else
                    index++; // Also handles case if actionName wasn't found; FindIndex() returns -1 that's incremented to 0

                return treeView[index].data.name;
            }

            return string.Empty;
        }

        private int GetPreviousBindingIndexFromViewTree(in ActionOrBindingData data, out string parentActionName)
        {
            Debug.Assert(!data.isAction);

            int retVal = -1;
            parentActionName = string.Empty;

            // The bindindIndex is global and doesn't correspond to the binding's "child index" within the TreeView.
            // To find the "previous" Binding to select, after deleting the current one, we must:
            // 1. Traverse the ViewTree to find the parent of the binding and its index under that parent
            // 2. Identify the Binding to select after deletion and retrieve its bindingIndex
            // 3. Return the bindingIndex and the parent Action name (select the Action if bindingIndex is invalid)

            var treeView = ViewStateSelector.GetViewState(stateContainer.GetState()).treeViewData;
            foreach (var action in treeView)
            {
                if (!action.hasChildren)
                    continue;

                if (FindBindingOrComponentTreeViewParent(action, data.bindingIndex, out var parentNode, out int childIndex))
                {
                    parentActionName = action.data.name;
                    if (parentNode.children.Count() > 1)
                    {
                        int prevIndex = Math.Max(childIndex - 1, 0);
                        var node = parentNode.children.ElementAt(prevIndex);
                        retVal = node.data.bindingIndex;
                        break;
                    }
                }
            }

            return retVal;
        }

        private static bool FindBindingOrComponentTreeViewParent(TreeViewItemData<ActionOrBindingData> root, int bindingIndex, out TreeViewItemData<ActionOrBindingData> parent, out int childIndex)
        {
            Debug.Assert(root.hasChildren);

            int index = 0;
            foreach (var item in root.children)
            {
                if (item.data.bindingIndex == bindingIndex)
                {
                    parent = root;
                    childIndex = index;
                    return true;
                }

                if (item.hasChildren && FindBindingOrComponentTreeViewParent(item, bindingIndex, out parent, out childIndex))
                    return true;

                index++;
            }

            parent = default;
            childIndex = -1;
            return false;
        }

        internal class ViewState
        {
            public List<TreeViewItemData<ActionOrBindingData>> treeViewData;
            public int actionMapCount;
            public int newElementID;
        }
    }

    internal struct ActionOrBindingData
    {
        public ActionOrBindingData(bool isAction, string name, int actionMapIndex, bool isComposite = false, bool isPartOfComposite = false, string controlLayout = "", int bindingIndex = -1, int actionIndex = -1, bool isCut = false)
        {
            this.name = name;
            this.isComposite = isComposite;
            this.isPartOfComposite = isPartOfComposite;
            this.actionMapIndex = actionMapIndex;
            this.controlLayout = controlLayout;
            this.bindingIndex = bindingIndex;
            this.isAction = isAction;
            this.actionIndex = actionIndex;
            this.isCut = isCut;
        }

        public string name { get; }
        public bool isAction { get; }
        public int actionMapIndex { get; }
        public bool isComposite { get; }
        public bool isPartOfComposite { get; }
        public string controlLayout { get; }
        public int bindingIndex { get; }
        public int actionIndex { get; }
        public bool isCut { get; }
    }

    internal static partial class Selectors
    {
        public static List<TreeViewItemData<ActionOrBindingData>> GetActionsAsTreeViewData(InputActionsEditorState state, Dictionary<Guid, int> idDictionary)
        {
            var actionMapIndex = state.selectedActionMapIndex;
            var controlSchemes = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
            var actionMap = GetSelectedActionMap(state);

            if (actionMap == null)
                return new List<TreeViewItemData<ActionOrBindingData>>();

            var actions = actionMap.Value.wrappedProperty
                .FindPropertyRelative(nameof(InputActionMap.m_Actions))
                .Select(sp => new SerializedInputAction(sp));

            var bindings = actionMap.Value.wrappedProperty
                .FindPropertyRelative(nameof(InputActionMap.m_Bindings))
                .Select(sp => new SerializedInputBinding(sp))
                .ToList();

            var actionItems = new List<TreeViewItemData<ActionOrBindingData>>();
            foreach (var action in actions)
            {
                var actionBindings = bindings.Where(spb => spb.action == action.name).ToList();
                var bindingItems = new List<TreeViewItemData<ActionOrBindingData>>();
                var actionId = new Guid(action.id);

                for (var i = 0; i < actionBindings.Count; i++)
                {
                    var serializedInputBinding = actionBindings[i];
                    var inputBindingId = new Guid(serializedInputBinding.id);

                    if (serializedInputBinding.isComposite)
                    {
                        var compositeItems = new List<TreeViewItemData<ActionOrBindingData>>();
                        var nextBinding = actionBindings[++i];
                        var hiddenCompositeParts = false;
                        while (nextBinding.isPartOfComposite)
                        {
                            var isVisible = ShouldBindingBeVisible(nextBinding, state.selectedControlScheme, state.selectedDeviceRequirementIndex);
                            if (isVisible)
                            {
                                var name = GetHumanReadableCompositeName(nextBinding, state.selectedControlScheme, controlSchemes);
                                compositeItems.Add(new TreeViewItemData<ActionOrBindingData>(GetIdForGuid(new Guid(nextBinding.id), idDictionary),
                                    new ActionOrBindingData(isAction: false, name, actionMapIndex, isComposite: false,
                                        isPartOfComposite: true, GetControlLayout(nextBinding.path), bindingIndex: nextBinding.indexOfBinding, isCut: state.IsBindingCut(actionMapIndex, nextBinding.indexOfBinding))));
                            }
                            else
                                hiddenCompositeParts = true;

                            if (++i >= actionBindings.Count)
                                break;

                            nextBinding = actionBindings[i];
                        }
                        i--;

                        var shouldCompositeBeVisible = !(compositeItems.Count == 0 && hiddenCompositeParts); //hide composite if all parts are hidden
                        if (shouldCompositeBeVisible)
                            bindingItems.Add(new TreeViewItemData<ActionOrBindingData>(GetIdForGuid(inputBindingId, idDictionary),
                                new ActionOrBindingData(isAction: false, serializedInputBinding.name, actionMapIndex, isComposite: true, isPartOfComposite: false, action.expectedControlType, bindingIndex: serializedInputBinding.indexOfBinding, isCut: state.IsBindingCut(actionMapIndex, serializedInputBinding.indexOfBinding)),
                                compositeItems.Count > 0 ? compositeItems : null));
                    }
                    else
                    {
                        var isVisible = ShouldBindingBeVisible(serializedInputBinding, state.selectedControlScheme, state.selectedDeviceRequirementIndex);
                        if (isVisible)
                            bindingItems.Add(new TreeViewItemData<ActionOrBindingData>(GetIdForGuid(inputBindingId, idDictionary),
                                new ActionOrBindingData(isAction: false, GetHumanReadableBindingName(serializedInputBinding, state.selectedControlScheme, controlSchemes), actionMapIndex,
                                    isComposite: false, isPartOfComposite: false, GetControlLayout(serializedInputBinding.path), bindingIndex: serializedInputBinding.indexOfBinding, isCut: state.IsBindingCut(actionMapIndex, serializedInputBinding.indexOfBinding))));
                    }
                }
                var actionIndex = action.wrappedProperty.GetIndexOfArrayElement();
                actionItems.Add(new TreeViewItemData<ActionOrBindingData>(GetIdForGuid(actionId, idDictionary),
                    new ActionOrBindingData(isAction: true, action.name, actionMapIndex, isComposite: false, isPartOfComposite: false, action.expectedControlType, actionIndex: actionIndex, isCut: state.IsActionCut(actionMapIndex, actionIndex)), bindingItems.Count > 0 ? bindingItems : null));
            }
            return actionItems;
        }

        private static int GetIdForGuid(Guid guid, Dictionary<Guid, int> idDictionary)
        {
            if (!idDictionary.TryGetValue(guid, out var id))
            {
                id = idDictionary.Values.Count > 0 ? idDictionary.Values.Max() + 1 : 0;
                idDictionary.Add(guid, id);
            }
            return id;
        }

        private static string GetHumanReadableBindingName(SerializedInputBinding serializedInputBinding, InputControlScheme? currentControlScheme, SerializedProperty allControlSchemes)
        {
            var name = InputControlPath.ToHumanReadableString(serializedInputBinding.path);
            if (String.IsNullOrEmpty(name))
                name = "<No Binding>";
            if (IsBindingAssignedToNoControlSchemes(serializedInputBinding, allControlSchemes, currentControlScheme))
                name += " {GLOBAL}";
            return name;
        }

        private static bool IsBindingAssignedToNoControlSchemes(SerializedInputBinding serializedInputBinding, SerializedProperty allControlSchemes, InputControlScheme? currentControlScheme)
        {
            if (allControlSchemes.arraySize <= 0 || !currentControlScheme.HasValue || string.IsNullOrEmpty(currentControlScheme.Value.name))
                return false;
            if (serializedInputBinding.controlSchemes.Length <= 0)
                return true;
            return false;
        }

        private static bool ShouldBindingBeVisible(SerializedInputBinding serializedInputBinding, InputControlScheme? currentControlScheme, int deviceIndex)
        {
            if (currentControlScheme.HasValue && !string.IsNullOrEmpty(currentControlScheme.Value.name))
            {
                var isMatchingDevice = true;
                if (deviceIndex >= 0)
                {
                    var devicePathToMatch = InputControlPath.TryGetDeviceLayout(currentControlScheme.Value.deviceRequirements.ElementAt(deviceIndex).controlPath);
                    var devicePath = InputControlPath.TryGetDeviceLayout(serializedInputBinding.path);
                    isMatchingDevice = string.Equals(devicePathToMatch, devicePath, StringComparison.InvariantCultureIgnoreCase) || InputControlLayout.s_Layouts.IsBasedOn(new InternedString(devicePath), new InternedString(devicePathToMatch));
                }
                var hasNoControlScheme = serializedInputBinding.controlSchemes.Length <= 0; //also show GLOBAL bindings
                var isAssignedToCurrentControlScheme = serializedInputBinding.controlSchemes.Contains(currentControlScheme.Value.name);
                return (isAssignedToCurrentControlScheme || hasNoControlScheme) && isMatchingDevice;
            }
            //if no control scheme selected then show all bindings
            return true;
        }

        internal static string GetHumanReadableCompositeName(SerializedInputBinding binding, InputControlScheme? currentControlScheme, SerializedProperty allControlSchemes)
        {
            return $"{ObjectNames.NicifyVariableName(binding.name)}: " +
                $"{GetHumanReadableBindingName(binding, currentControlScheme, allControlSchemes)}";
        }

        private static string GetControlLayout(string path)
        {
            var controlLayout = string.Empty;
            try
            {
                controlLayout = InputControlPath.TryGetControlLayout(path);
            }
            catch (Exception)
            {
            }

            return controlLayout;
        }
    }
}

#endif
