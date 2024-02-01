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
            : base(stateContainer)
        {
            m_Root = root;

            m_ListView = m_Root?.Q<ListView>("action-maps-list-view");
            m_ListView.selectionType = UIElements.SelectionType.Single;

            m_ListViewSelectionChangeFilter = new CollectionViewSelectionChangeFilter(m_ListView);
            m_ListViewSelectionChangeFilter.selectedIndicesChanged += (selectedIndices) =>
            {
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
                (actionMapNames, state) => new ViewState(Selectors.GetSelectedActionMap(state), actionMapNames));

            addActionMapButton.clicked += AddActionMap;
            ContextMenu.GetContextMenuForActionMapListView(this, m_ListView.parent);
        }

        private Button addActionMapButton => m_Root?.Q<Button>("add-new-action-map-button");

        public override void RedrawUI(ViewState viewState)
        {
            m_ListView.itemsSource = viewState.actionMapNames?.ToList() ?? new List<string>();
            /*if (viewState.selectedActionMap.HasValue)
            {
                var indexOf = viewState.actionMapNames.IndexOf(viewState.selectedActionMap.Value.name);
                m_ListView.SetSelection(indexOf);
            }*/
            
            // Update view to reflect model
            var desiredSelectedIndex = viewState.selectedActionMap.HasValue
                ? viewState.actionMapNames.IndexOf(viewState.selectedActionMap.Value.name)
                : -1;
            if (desiredSelectedIndex != m_ListView.selectedIndex)
            {
                m_ListView.ScrollToItem(desiredSelectedIndex); // Note: sizing or UITK bug, also just clicking many times shrinks the panel (!!!!)
                m_ListView.SetSelection(desiredSelectedIndex);
            }
                
            //var selected = viewState.selectedActionMap.HasValue ? viewState.selectedActionMap.Value.name : "none";
            //Debug.Log($"Redraw, selection: {desiredSelectedIndex}, listSelection: {m_ListView.selectedItem}");
            m_ListView.Rebuild();
            //RenameNewActionMaps();
        }

        public override void DestroyView()
        {
            addActionMapButton.clicked -= AddActionMap;
        }

        private void RenameNewActionMaps()
        {
            Debug.Log("RenameNewActionMaps");
            
            //Debug.Log($"RenameNewActionMaps | Count: {m_ListView.itemsSource.Count}, selectedIndex: {m_ListView.selectedIndex}");

            /*if (m_ListView.itemsSource == null)
            {
                Debug.Log("No itemsSource");
                return;
            }*/
            
            //Debug.Log($"selectedIndex: {m_ListView.selectedIndex}");
            //Debug.Log($"viewStateSelected: {viewState.selectedActionMap}");
            
            //Debug.Assert(m_ListView.itemsSource.Count > 0);
            //Debug.Assert(m_ListView.selectedIndex >= 0);
            
            //m_ListView.ScrollToItem(m_ListView.selectedIndex); // TODO This is wrong this should be when changing selection
            var element = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            //if (element == null)
            //{
            //    Debug.Log("No element selected");
            //    return;
            //}
            ((InputActionMapsTreeViewItem)element).FocusOnRenameTextField();
        }

        private void DeleteActionMap(int index)
        {
            Dispatch(Commands.DeleteActionMap(index));
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
            Dispatch(Commands.AddActionMap());
            //Execute(() => RenameNewActionMaps());
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
        private readonly VisualElement m_Root;
        private ListView m_ListView;

        internal class ViewState
        {
            public SerializedInputActionMap? selectedActionMap;
            public IEnumerable<string> actionMapNames;

            public ViewState(SerializedInputActionMap? selectedActionMap, IEnumerable<string> actionMapNames)
            {
                this.selectedActionMap = selectedActionMap;
                this.actionMapNames = actionMapNames;
            }
        }
    }
}

#endif
