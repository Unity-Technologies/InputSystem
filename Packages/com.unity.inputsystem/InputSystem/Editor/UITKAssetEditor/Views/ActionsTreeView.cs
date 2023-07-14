// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
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
                treeViewItem.OnDeleteItem += treeViewItem.DeleteCallback;

                if (item.isAction || item.isComposite)
                    ContextMenu.GetContextMenuForActionOrCompositeItem(treeViewItem, m_ActionsTreeView, i);
                else
                    ContextMenu.GetContextMenuForBindingItem(treeViewItem);

                if (item.isAction)
                {
                    addBindingButton.style.display = DisplayStyle.Flex;
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
                treeViewItem.EditTextFinished -= treeViewItem.EditTextFinishedCallback;
            };

            m_ActionsTreeView.selectedIndicesChanged += indicies =>
            {
                var index = indicies.First();
                if (index == -1)
                    return;
                var item = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(index);
                Dispatch(item.isAction ? Commands.SelectAction(item.name) : Commands.SelectBinding(item.bindingIndex));
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

        private int GetComponentOrBindingID(List<TreeViewItemData<ActionOrBindingData>> treeList, int selectedBindingIndex)
        {
            var currentBindingIndex = -1;
            foreach (var action in treeList)
            {
                foreach (var bindingOrComponent in action.children)
                {
                    currentBindingIndex++;
                    if (currentBindingIndex == selectedBindingIndex) return bindingOrComponent.id;
                    if (bindingOrComponent.hasChildren)
                    {
                        foreach (var binding in bindingOrComponent.children)
                        {
                            currentBindingIndex++;
                            if (currentBindingIndex == selectedBindingIndex) return binding.id;
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
                m_ActionsTreeView.SetSelectionById(viewState.newElementID);
            RenameNewAction(viewState.newElementID);
            addActionButton.SetEnabled(viewState.actionMapCount > 0);
        }

        private void RenameNewAction(int id)
        {
            if (!m_RenameOnActionAdded || id == -1)
                return;
            m_ActionsTreeView.ScrollToItemById(id);
            var treeViewItem = m_ActionsTreeView.GetRootElementForId(id).Q<InputActionsTreeViewItem>();
            treeViewItem.FocusOnRenameTextField();
        }

        private void AddAction()
        {
            Dispatch(Commands.AddAction());
            m_RenameOnActionAdded = true;
        }

        private void AddBinding(string actionName)
        {
            Dispatch(Commands.SelectAction(actionName));
            Dispatch(Commands.AddBinding());
        }

        private void DeleteItem(ActionOrBindingData data)
        {
            if (data.isAction)
                Dispatch(Commands.DeleteAction(data.actionMapIndex, data.name));
            else
                Dispatch(Commands.DeleteBinding(data.actionMapIndex, data.bindingIndex));
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
            else if (e.keyCode == KeyCode.Space)
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
                            var name = GetHumanReadableCompositeName(nextBinding);

                            compositeItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                                new ActionOrBindingData(false, name, actionMapIndex, false, GetControlLayout(nextBinding.path), nextBinding.indexOfBinding)));

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
                        bindingItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                            new ActionOrBindingData(false, GetHumanReadableBindingName(serializedInputBinding), actionMapIndex,
                                false, GetControlLayout(serializedInputBinding.path), serializedInputBinding.indexOfBinding)));
                    }
                }
                actionItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                    new ActionOrBindingData(true, action.name, actionMapIndex, false, action.expectedControlType), bindingItems.Count > 0 ? bindingItems : null));
            }
            return actionItems;
        }

        private static string GetHumanReadableBindingName(SerializedInputBinding serializedInputBinding)
        {
            var name = InputControlPath.ToHumanReadableString(serializedInputBinding.path);
            if (String.IsNullOrEmpty(name))
                name = "<No Binding>";
            return name;
        }

        internal static string GetHumanReadableCompositeName(SerializedInputBinding binding)
        {
            return $"{ObjectNames.NicifyVariableName(binding.name)}: " +
                $"{InputControlPath.ToHumanReadableString(binding.path)}";
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
