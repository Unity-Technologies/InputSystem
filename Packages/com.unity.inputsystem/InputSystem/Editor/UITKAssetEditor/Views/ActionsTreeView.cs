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
                if (item.isAction)
                {
                    addBindingButton.style.display = DisplayStyle.Flex;
                    addBindingButton.clicked += () => AddBinding(item.name);
                }
                else
                    addBindingButton.style.display = DisplayStyle.None;

                if (!string.IsNullOrEmpty(item.controlLayout))
                    e.Q<VisualElement>("icon").style.backgroundImage =
                        new StyleBackground(
                            EditorInputControlLayoutCache.GetIconForLayout(item.controlLayout));
                else
                    e.Q<VisualElement>("icon").style.backgroundImage =
                        new StyleBackground(
                            EditorInputControlLayoutCache.GetIconForLayout("Control"));
            };

            m_ActionsTreeView.selectedIndicesChanged += indicies =>
            {
                var item = m_ActionsTreeView.GetItemDataForIndex<ActionOrBindingData>(indicies.First());
                Dispatch(item.isAction ? Commands.SelectAction(item.name) : Commands.SelectBinding(item.bindingIndex));
            };

            CreateSelector(Selectors.GetActionsForSelectedActionMap,
                (_, state) =>
                {
                    var treeData = Selectors.GetActionsAsTreeViewData(state);
                    return new ViewState
                    {
                        treeViewData = treeData,
                        newElementID = state.selectionType == SelectionType.Action ? treeData[state.selectedActionIndex].id : GetComponentOrBindingID(treeData, state.selectedBindingIndex)
                    };
                });

            addActionButton.clicked += AddAction;
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
            m_ActionsTreeView.SetSelectionById(viewState.newElementID);
            m_ActionsTreeView.Rebuild();
        }

        private void AddAction()
        {
            Dispatch(Commands.AddAction());
        }

        private void AddBinding(string actionName)
        {
            Dispatch(Commands.SelectAction(actionName));
            Dispatch(Commands.AddBinding());
        }

        internal class ViewState
        {
            public List<TreeViewItemData<ActionOrBindingData>> treeViewData;
            public int newElementID;
        }
    }

    internal struct ActionOrBindingData
    {
        public ActionOrBindingData(bool isAction, string name, string controlLayout = "", int bindingIndex = -1)
        {
            this.name = name;
            this.controlLayout = controlLayout;
            this.bindingIndex = bindingIndex;
            this.isAction = isAction;
        }

        public string name { get; }
        public bool isAction { get; }
        public string controlLayout { get; }
        public int bindingIndex { get; }
    }

    internal static partial class Selectors
    {
        public static List<TreeViewItemData<ActionOrBindingData>> GetActionsAsTreeViewData(InputActionsEditorState state)
        {
            var actionMapIndex = state.selectedActionMapIndex;
            var actionMaps = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ActionMaps));

            var actionMap = actionMapIndex == -1 ?
                actionMaps.GetArrayElementAtIndex(0) :
                actionMaps.GetArrayElementAtIndex(actionMapIndex);

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
                                new ActionOrBindingData(false, name, GetControlLayout(nextBinding.path), nextBinding.indexOfBinding)));

                            if (++i >= actionBindings.Count)
                                break;

                            nextBinding = actionBindings[i];
                        }
                        i--;
                        bindingItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                            new ActionOrBindingData(false, serializedInputBinding.name, action.expectedControlType, serializedInputBinding.indexOfBinding),
                            compositeItems.Count > 0 ? compositeItems : null));
                    }
                    else
                    {
                        bindingItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                            new ActionOrBindingData(false, InputControlPath.ToHumanReadableString(serializedInputBinding.path),
                                GetControlLayout(serializedInputBinding.path), serializedInputBinding.indexOfBinding)));
                    }
                }
                actionItems.Add(new TreeViewItemData<ActionOrBindingData>(id++,
                    new ActionOrBindingData(true, action.name, action.expectedControlType), bindingItems.Count > 0 ? bindingItems : null));
            }
            return actionItems;
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
