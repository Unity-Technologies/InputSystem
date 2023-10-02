// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A view for displaying the actions of the selected action map in a tree with bindings
    /// as children.
    /// </summary>
    internal class ActionsTreeView : ViewBase<ActionsTreeView.ViewState>
    {
        private readonly VisualElement m_Root;
        private readonly TreeView m_ActionsTreeView;
        private Button addActionButton => m_Root?.Q<Button>("add-new-action-button");

        private bool m_RenameOnActionAdded;
        private readonly DeselectionHelper m_DeselectionHelper = new();
        
        public ActionsTreeView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;

            m_ActionsTreeView = m_Root.Q<TreeView>("actions-tree-view");
            m_ActionsTreeView.selectionType = UIElements.SelectionType.Single;
            m_ActionsTreeView.makeItem = () => new InputActionsTreeViewItem();
            m_ActionsTreeView.bindItem = (e, i) =>
            {
                var item = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(i);
                e.Q<Label>("name").text = item.name;
                var addBindingButton = e.Q<Button>("add-new-binding-button");
                var treeViewItem = (InputActionsTreeViewItem)e;
                treeViewItem.DeleteCallback = _ => DeleteItem(item);
                treeViewItem.DuplicateCallback = _ => DuplicateItem(item);
                treeViewItem.OnDeleteItem += treeViewItem.DeleteCallback;
                treeViewItem.OnDuplicateItem += treeViewItem.DuplicateCallback;
                if (item.isComposite)
                    ContextMenu.GetContextMenuForCompositeItem(treeViewItem, i);
                else if (item.isAction)
                    ContextMenu.GetContextMenuForActionItem(treeViewItem, item.controlLayout, i);
                else
                    ContextMenu.GetContextMenuForBindingItem(treeViewItem);

                if (item.isAction)
                {
                    addBindingButton.style.display = DisplayStyle.Flex;
                    addBindingButton.clickable = null; //reset the clickable to avoid multiple subscriptions
                    addBindingButton.clicked += () => AddBinding(item.name);
                    treeViewItem.EditTextFinishedCallback = newName =>
                    {
                        m_RenameOnActionAdded = false;
                        ChangeActionName(item, newName);
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
                            m_RenameOnActionAdded = false;
                            ChangeCompositeName(item, newName);
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

                treeViewItem.OnDeleteItem -= treeViewItem.DeleteCallback;
                treeViewItem.OnDuplicateItem -= treeViewItem.DuplicateCallback;
                treeViewItem.EditTextFinished -= treeViewItem.EditTextFinishedCallback;
            };

            m_ActionsTreeView.selectedIndicesChanged += indices =>
            {
                if (!m_DeselectionHelper.Select(m_ActionsTreeView, indices))
                    return; // abort since triggered again from within Select(...)

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

            m_ActionsTreeView.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);

            CreateSelector(Selectors.GetActionsForSelectedActionMap, Selectors.GetActionMapCount,
                (_, count, state) =>
                {
                    var treeData = Selectors.GetActionsAsTreeViewData(state);
                    return new ViewState
                    {
                        treeViewData = treeData,
                        actionMapCount = count ?? 0,
                        newElementID = GetSelectedElementId(state, treeData)
                    };
                });

            addActionButton.clicked += AddAction;
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
            addActionButton.clicked -= AddAction;
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
            addActionButton.SetEnabled(viewState.actionMapCount > 0);
        }

        private void RenameNewAction(int id)
        {
            if (!m_RenameOnActionAdded || id == -1)
                return;
            m_ActionsTreeView.ScrollToItemById(id);
            var treeViewItem = m_ActionsTreeView.GetRootElementForId(id)?.Q<InputActionsTreeViewItem>();
            treeViewItem?.FocusOnRenameTextField();
        }

        internal void AddAction()
        {
            Dispatch(Commands.AddAction());
            m_RenameOnActionAdded = true;
        }

        internal void AddBinding(string actionName)
        {
            Dispatch(Commands.SelectAction(actionName));
            Dispatch(Commands.AddBinding());
        }

        internal void AddComposite(string actionName, string compositeType)
        {
            Dispatch(Commands.SelectAction(actionName));
            Dispatch(Commands.AddComposite(compositeType));
        }

        private void DeleteItem(ActionOrBindingData data)
        {
            if (data.isAction)
                Dispatch(Commands.DeleteAction(data.actionMapIndex, data.name));
            else
                Dispatch(Commands.DeleteBinding(data.actionMapIndex, data.bindingIndex));
        }

        private void DuplicateItem(ActionOrBindingData data)
        {
            Dispatch(data.isAction ? Commands.DuplicateAction() : Commands.DuplicateBinding());
        }

        private void ChangeActionName(ActionOrBindingData data, string newName)
        {
            m_RenameOnActionAdded = false;
            Dispatch(Commands.ChangeActionName(data.actionMapIndex, data.name, newName));
        }

        private void ChangeCompositeName(ActionOrBindingData data, string newName)
        {
            m_RenameOnActionAdded = false;
            Dispatch(Commands.ChangeCompositeName(data.actionMapIndex, data.bindingIndex, newName));
        }

        private void OnKeyDownEvent(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.F2)
                OnKeyDownEventForRename();
            else if (e.keyCode == KeyCode.Delete)
                OnKeyDownEventForDelete();
        }

        private void OnKeyDownEventForRename()
        {
            var item = m_ActionsTreeView.GetRootElementForIndex(m_ActionsTreeView.selectedIndex)?.Q<InputActionsTreeViewItem>();
            var data = (ActionOrBindingData)m_ActionsTreeView.selectedItem;
            if (item != null && (data.isAction || data.isComposite))
                item.FocusOnRenameTextField();
        }

        private void OnKeyDownEventForDelete()
        {
            var item = m_ActionsTreeView.GetRootElementForIndex(m_ActionsTreeView.selectedIndex)?.Q<InputActionsTreeViewItem>();
            item?.DeleteItem();
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
        public ActionOrBindingData(bool isAction, string name, int actionMapIndex, bool isComposite = false, string controlLayout = "", int bindingIndex = -1)
        {
            this.name = name;
            this.isComposite = isComposite;
            this.actionMapIndex = actionMapIndex;
            this.controlLayout = controlLayout;
            this.bindingIndex = bindingIndex;
            this.isAction = isAction;
        }

        public string name { get; }
        public bool isAction { get; }
        public int actionMapIndex { get; }
        public bool isComposite { get; }
        public string controlLayout { get; }
        public int bindingIndex { get; }
    }

    internal static partial class Selectors
    {
        public static List<TreeViewItemData<ActionOrBindingData>> GetActionsAsTreeViewData(InputActionsEditorState state)
        {
            var actionMapIndex = state.selectedActionMapIndex;
            var actionMaps = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ActionMaps));

            var controlSchemes = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
            var actionMap = actionMapIndex == -1 || actionMaps.arraySize <= 0 ?
                null : actionMaps.GetArrayElementAtIndex(actionMapIndex);

            if (actionMap == null)
                return new List<TreeViewItemData<ActionOrBindingData>>();

            var actions = actionMap
                .FindPropertyRelative(nameof(InputActionMap.m_Actions))
                .Select(sp => new SerializedInputAction(sp));

            var bindings = actionMap
                .FindPropertyRelative(nameof(InputActionMap.m_Bindings))
                .Select(sp => new SerializedInputBinding(sp))
                .ToList();

            var id = 0;
            var actionItems = new List<TreeViewItemData<ActionOrBindingData>>();
            foreach (var action in actions)
            {
                var actionBindings = bindings.Where(spb => spb.action == action.name).ToList();
                var bindingItems = new List<TreeViewItemData<ActionOrBindingData>>();

                for (var i = 0; i < actionBindings.Count; i++)
                {
                    var serializedInputBinding = actionBindings[i];

                    if (serializedInputBinding.isComposite)
                    {
                        var compositeItems = new List<TreeViewItemData<ActionOrBindingData>>();
                        var nextBinding = actionBindings[++i];
                        while (nextBinding.isPartOfComposite)
                        {
                            var isVisible = ShouldBindingBeVisible(nextBinding, state.selectedControlScheme);
                            if (isVisible)
                            {
                                var name = GetHumanReadableCompositeName(nextBinding, state.selectedControlScheme, controlSchemes);
                                compositeItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                                    new ActionOrBindingData(false, name, actionMapIndex, false,
                                        GetControlLayout(nextBinding.path), nextBinding.indexOfBinding)));
                            }

                            if (++i >= actionBindings.Count)
                                break;

                            nextBinding = actionBindings[i];
                        }
                        i--;
                        bindingItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                            new ActionOrBindingData(false, serializedInputBinding.name, actionMapIndex, true, action.expectedControlType, serializedInputBinding.indexOfBinding),
                            compositeItems.Count > 0 ? compositeItems : null));
                    }
                    else
                    {
                        var isVisible = ShouldBindingBeVisible(serializedInputBinding, state.selectedControlScheme);
                        if (isVisible)
                            bindingItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                                new ActionOrBindingData(false, GetHumanReadableBindingName(serializedInputBinding, state.selectedControlScheme, controlSchemes), actionMapIndex,
                                    false, GetControlLayout(serializedInputBinding.path), serializedInputBinding.indexOfBinding)));
                    }
                }
                actionItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                    new ActionOrBindingData(true, action.name, actionMapIndex, false, action.expectedControlType), bindingItems.Count > 0 ? bindingItems : null));
            }
            return actionItems;
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

        private static bool ShouldBindingBeVisible(SerializedInputBinding serializedInputBinding, InputControlScheme? currentControlScheme)
        {
            if (currentControlScheme.HasValue && !string.IsNullOrEmpty(currentControlScheme.Value.name))
            {
                //if binding is global (not assigned to any control scheme) show always
                if (serializedInputBinding.controlSchemes.Length <= 0)
                    return true;
                return serializedInputBinding.controlSchemes.Contains(currentControlScheme.Value.name);
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
