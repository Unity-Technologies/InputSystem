#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using CmdEvents = UnityEngine.InputSystem.Editor.InputActionsEditorConstants.CommandEvents;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A list view to display the action maps of the currently opened input actions asset.
    /// </summary>
    internal class ActionMapsView : ViewBase<ActionMapsView.ViewState>
    {
        public ActionMapsView(VisualElement root, StateContainer stateContainer)
            : base(root, stateContainer)
        {
            m_ListView = root.Q<ListView>("action-maps-list-view");
            m_ListView.selectionType = UIElements.SelectionType.Single;
            m_ListView.reorderable = true;
            m_ListViewSelectionChangeFilter = new CollectionViewSelectionChangeFilter(m_ListView);
            m_ListViewSelectionChangeFilter.selectedIndicesChanged += (selectedIndices) =>
            {
                Dispatch(Commands.SelectActionMap(((ActionMapData)m_ListView.selectedItem).mapName));
            };

            m_ListView.bindItem = (element, i) =>
            {
                var treeViewItem = (InputActionMapsTreeViewItem)element;
                var mapData = (ActionMapData)m_ListView.itemsSource[i];
                treeViewItem.label.text = mapData.mapName;
                treeViewItem.EditTextFinishedCallback = newName => ChangeActionMapName(i, newName);
                treeViewItem.EditTextFinished += treeViewItem.EditTextFinishedCallback;
                treeViewItem.userData = i;
                element.SetEnabled(!mapData.isDisabled);

                ContextMenu.GetContextMenuForActionMapItem(this, treeViewItem, i);
            };
            m_ListView.makeItem = () => new InputActionMapsTreeViewItem();
            m_ListView.unbindItem = (element, i) =>
            {
                var treeViewElement = (InputActionMapsTreeViewItem)element;
                treeViewElement.Reset();
                treeViewElement.EditTextFinished -= treeViewElement.EditTextFinishedCallback;
            };

            m_ListView.itemsChosen += objects =>
            {
                var item = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex).Q<InputActionMapsTreeViewItem>();
                item.FocusOnRenameTextField();
            };

            m_ListView.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            m_ListView.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            m_ListView.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

            // ISXB-748 - Scrolling the view causes a visual glitch with the rename TextField. As a work-around we
            // need to cancel the rename operation in this scenario.
            m_ListView.RegisterCallback<WheelEvent>(e => InputActionMapsTreeViewItem.CancelRename(), TrickleDown.TrickleDown);

            var treeView = root.Q<TreeView>("actions-tree-view");
            m_ListView.AddManipulator(new DropManipulator(OnDroppedHandler, treeView));
            m_ListView.itemIndexChanged += OnReorder;

            CreateSelector(Selectors.GetActionMapNames, Selectors.GetSelectedActionMap, (actionMapNames, actionMap, state) => new ViewState(actionMap, actionMapNames, state.GetDisabledActionMaps(actionMapNames.ToList())));

            m_AddActionMapButton = root.Q<Button>("add-new-action-map-button");
            m_AddActionMapButton.clicked += AddActionMap;

            ContextMenu.GetContextMenuForActionMapsEmptySpace(this, root.Q<VisualElement>("rclick-area-to-add-new-action-map"));
            // Only bring up this context menu for the List when it's empty, so we can treat it like right-clicking the empty space:
            ContextMenu.GetContextMenuForActionMapsEmptySpace(this, m_ListView, onlyShowIfListIsEmpty: true);
        }

        void OnDroppedHandler(int mapIndex)
        {
            Dispatch(Commands.PasteActionIntoActionMap(mapIndex));
        }

        void OnReorder(int oldIndex, int newIndex)
        {
            Dispatch(Commands.ReorderActionMap(oldIndex, newIndex));
        }

        public override void RedrawUI(ViewState viewState)
        {
            m_ListView.itemsSource = viewState.actionMapData?.ToList() ?? new List<ActionMapData>();
            if (viewState.selectedActionMap.HasValue)
            {
                var actionMapData = viewState.actionMapData?.Find(map => map.mapName.Equals(viewState.selectedActionMap.Value.name));
                if (actionMapData.HasValue)
                    m_ListView.SetSelection(viewState.actionMapData.IndexOf(actionMapData.Value));
            }
            m_ListView.Rebuild();
            RenameNewActionMaps();
        }

        public override void DestroyView()
        {
            m_AddActionMapButton.clicked -= AddActionMap;
        }

        private void RenameNewActionMaps()
        {
            if (!m_EnterRenamingMode)
                return;
            m_ListView.ScrollToItem(m_ListView.selectedIndex);
            var element = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            if (element == null)
                return;
            ((InputActionMapsTreeViewItem)element).FocusOnRenameTextField();
        }

        internal void RenameActionMap(int index)
        {
            m_ListView.ScrollToItem(index);
            var element = m_ListView.GetRootElementForIndex(index);
            if (element == null)
                return;
            ((InputActionMapsTreeViewItem)element).FocusOnRenameTextField();
        }

        internal void DeleteActionMap(int index)
        {
            Dispatch(Commands.DeleteActionMap(index));
        }

        internal void DuplicateActionMap(int index)
        {
            Dispatch(Commands.DuplicateActionMap(index));
        }

        internal void CopyItems()
        {
            Dispatch(Commands.CopyActionMapSelection());
        }

        internal void CutItems()
        {
            Dispatch(Commands.CutActionMapSelection());
        }

        internal void PasteItems(bool copiedAction)
        {
            Dispatch(copiedAction ? Commands.PasteActionFromActionMap(InputActionsEditorView.s_OnPasteCutElements) : Commands.PasteActionMaps(InputActionsEditorView.s_OnPasteCutElements));
        }

        private void ChangeActionMapName(int index, string newName)
        {
            m_EnterRenamingMode = false;
            Dispatch(Commands.ChangeActionMapName(index, newName));
        }

        internal void AddActionMap()
        {
            Dispatch(Commands.AddActionMap());
            m_EnterRenamingMode = true;
        }

        internal int GetMapCount()
        {
            return m_ListView.itemsSource.Count;
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            var selectedItem = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            if (selectedItem == null)
                return;

            if (allowUICommandExecution)
            {
                switch (evt.commandName)
                {
                    case CmdEvents.Rename:
                        ((InputActionMapsTreeViewItem)selectedItem).FocusOnRenameTextField();
                        break;
                    case CmdEvents.Delete:
                    case CmdEvents.SoftDelete:
                        DeleteActionMap(m_ListView.selectedIndex);
                        break;
                    case CmdEvents.Duplicate:
                        DuplicateActionMap(m_ListView.selectedIndex);
                        break;
                    case CmdEvents.Copy:
                        CopyItems();
                        break;
                    case CmdEvents.Cut:
                        CutItems();
                        break;
                    case CmdEvents.Paste:
                        var isActionCopied = CopyPasteHelper.GetCopiedClipboardType() == typeof(InputAction);
                        if (CopyPasteHelper.HasPastableClipboardData(typeof(InputActionMap)))
                            PasteItems(isActionCopied);
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
                var actionMap = (evt.target as VisualElement).GetFirstAncestorOfType<InputActionMapsTreeViewItem>();
                if (actionMap != null)
                    m_ListView.SetSelection(actionMap.parent.IndexOf(actionMap));
            }
        }

        private readonly CollectionViewSelectionChangeFilter m_ListViewSelectionChangeFilter;
        private bool m_EnterRenamingMode;
        private readonly ListView m_ListView;
        private readonly Button m_AddActionMapButton;

        internal struct ActionMapData
        {
            internal string mapName;
            internal bool isDisabled;

            public ActionMapData(string mapName, bool isDisabled)
            {
                this.mapName = mapName;
                this.isDisabled = isDisabled;
            }
        }

        internal class ViewState
        {
            public SerializedInputActionMap? selectedActionMap;
            public List<ActionMapData> actionMapData;

            public ViewState(SerializedInputActionMap? selectedActionMap, IEnumerable<string> actionMapNames, IEnumerable<string> disabledActionMapNames)
            {
                this.selectedActionMap = selectedActionMap;
                actionMapData = new List<ActionMapData>();
                foreach (var name in actionMapNames)
                {
                    actionMapData.Add(new ActionMapData(name, disabledActionMapNames.Contains(name)));
                }
            }
        }
    }
}

#endif
