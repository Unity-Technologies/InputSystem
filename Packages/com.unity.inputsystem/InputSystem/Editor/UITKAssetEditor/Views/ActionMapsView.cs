#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using CmdEvents = UnityEngine.InputSystem.Editor.InputActionsEditorConstants.CommandEvents;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Utilities;
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

            // Setup a selection change filter that prevents the user from deselecting items.
            // This is desirable since deselecting an Action Map (ESC) would make the Action panel empty with
            // the current UI layout. Note that we also avoid dispatching select commands if callback
            // is indirectly triggered by a redraw (instead of constantly hooking and unhooking listener).
            m_ListViewSelectionChangeFilter = new CollectionViewSelectionChangeFilter(m_ListView);
            m_ListViewSelectionChangeFilter.selectedIndicesChanged += (selectedIndices) =>
            {
                if (!isRedrawInProgress)
                    Dispatch(Commands.SelectActionMap((string)m_ListView.selectedItem));
            };

            m_ListView.bindItem = (element, i) =>
            {
                var treeViewItem = (InputActionMapsTreeViewItem)element;
                treeViewItem.label.text = (string)m_ListView.itemsSource[i];
                treeViewItem.EditTextFinishedCallback = newName => ChangeActionMapName(i, newName);
                treeViewItem.EditTextFinished += treeViewItem.EditTextFinishedCallback;
                treeViewItem.DeleteCallback = _ => DeleteActionMap(i);
                treeViewItem.DuplicateCallback = _ => DuplicateActionMap(i);
                treeViewItem.OnDeleteItem += treeViewItem.DeleteCallback;
                treeViewItem.OnDuplicateItem += treeViewItem.DuplicateCallback;

                ContextMenu.GetContextMenuForActionMapItem(treeViewItem);
            };
            m_ListView.makeItem = () => new InputActionMapsTreeViewItem();
            m_ListView.unbindItem = (element, i) =>
            {
                var treeViewElement = (InputActionMapsTreeViewItem)element;
                treeViewElement.Reset();
                treeViewElement.OnDeleteItem -= treeViewElement.DeleteCallback;
                treeViewElement.OnDuplicateItem -= treeViewElement.DuplicateCallback;
                treeViewElement.EditTextFinished -= treeViewElement.EditTextFinishedCallback;
            };

            m_ListView.itemsChosen += objects =>
            {
                var item = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex).Q<InputActionMapsTreeViewItem>();
                item.FocusOnRenameTextField();
            };

            m_ListView.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            m_ListView.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);

            CreateSelector(s => new ViewStateCollection<string>(Selectors.GetActionMapNames(s)),
                (actionMapNames, state) => new ViewState(
                    Selectors.GetSelectedActionMap(state), actionMapNames?.ToList() ?? new List<string>()));

            m_AddActionMapButton = root.Q<Button>("add-new-action-map-button");
            m_AddActionMapButton.clicked += AddActionMap;
            ContextMenu.GetContextMenuForActionMapListView(this, m_ListView.parent);
        }

        protected override void RedrawUI(ViewState viewState)
        {
            m_ListView.itemsSource = viewState.actionMapNames;
            m_ListView.Rebuild();

            // Update view to reflect model selection
            var desiredSelectedIndex = viewState.selectedActionMapIndex;
            if (desiredSelectedIndex >= 0 && desiredSelectedIndex != m_ListView.selectedIndex)
            {
                m_ListView.SetSelection(desiredSelectedIndex);
                m_ListView.ScrollToItem(desiredSelectedIndex);
            }
        }

        public override void DestroyView()
        {
            m_AddActionMapButton.clicked -= AddActionMap;
        }

        private void RenameNewActionMap()
        {
            var element = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            m_ListView.ScrollToItem(m_ListView.selectedIndex);
            ((InputActionMapsTreeViewItem)element).FocusOnRenameTextField();
        }

        private void DeleteActionMap(int index)
        {
            Dispatch(Commands.DeleteActionMap(index), () =>
            {
                // WORKAROUND: For some reason after delete (haven' identified a pattern), m_ListView would
                // lose focus preventing further navigation or commands associated with the view.
                // Hence we explicitly reclaim focus here post the delete command as a workaround.
                m_ListView.Focus();
            });
        }

        private void DuplicateActionMap(int index)
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
            Dispatch(copiedAction ? Commands.PasteActionFromActionMap() : Commands.PasteActionMaps());
        }

        private void ChangeActionMapName(int index, string newName)
        {
            Dispatch(Commands.ChangeActionMapName(index, newName));
        }

        private void AddActionMap()
        {
            Dispatch(Commands.AddActionMap(), continueWith: RenameNewActionMap);
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            var selectedItem = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            if (selectedItem == null)
                return;
            switch (evt.commandName)
            {
                case CmdEvents.Rename:
                    ((InputActionMapsTreeViewItem)selectedItem).FocusOnRenameTextField();
                    break;
                case CmdEvents.Delete:
                case CmdEvents.SoftDelete:
                    ((InputActionMapsTreeViewItem)selectedItem).DeleteItem();
                    break;
                case CmdEvents.Duplicate:
                    ((InputActionMapsTreeViewItem)selectedItem).DuplicateItem();
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

        private readonly CollectionViewSelectionChangeFilter m_ListViewSelectionChangeFilter;
        private readonly ListView m_ListView;
        private readonly Button m_AddActionMapButton;

        internal class ViewState
        {
            public readonly SerializedInputActionMap? selectedActionMap;
            public readonly System.Collections.IList actionMapNames;
            public readonly int selectedActionMapIndex;

            public ViewState(SerializedInputActionMap? selectedActionMap, System.Collections.IList actionMapNames)
            {
                this.selectedActionMap = selectedActionMap;
                this.actionMapNames = actionMapNames;
                this.selectedActionMapIndex = selectedActionMap.HasValue
                    ? actionMapNames.IndexOf(selectedActionMap.Value.name)
                    : -1;
            }
        }
    }
}

#endif
