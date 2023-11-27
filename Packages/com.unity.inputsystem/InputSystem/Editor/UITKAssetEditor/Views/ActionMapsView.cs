#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
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

            m_ListView.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);

            CreateSelector(s => new ViewStateCollection<string>(Selectors.GetActionMapNames(s)),
                (actionMapNames, state) => new ViewState(Selectors.GetSelectedActionMap(state), actionMapNames));

            addActionMapButton.clicked += AddActionMap;
            ContextMenu.GetContextMenuForActionMapListView(this, m_ListView.parent);
        }

        private Button addActionMapButton => m_Root?.Q<Button>("add-new-action-map-button");

        public override void RedrawUI(ViewState viewState)
        {
            m_ListView.itemsSource = viewState.actionMapNames?.ToList() ?? new List<string>();
            if (viewState.selectedActionMap.HasValue)
            {
                var indexOf = viewState.actionMapNames.IndexOf(viewState.selectedActionMap.Value.name);
                m_ListView.SetSelection(indexOf);
            }
            m_ListView.Rebuild();
            RenameNewActionMaps();
        }

        public override void DestroyView()
        {
            addActionMapButton.clicked -= AddActionMap;
        }

        private void RenameNewActionMaps()
        {
            if (!m_EnterRenamingMode)
                return;
            m_ListView.ScrollToItem(m_ListView.selectedIndex);
            var element = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            ((InputActionMapsTreeViewItem)element).FocusOnRenameTextField();
            m_EnterRenamingMode = false;
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

        private void ChangeActionMapName(int index, string newName)
        {
            Dispatch(Commands.ChangeActionMapName(index, newName));
        }

        private void AddActionMap()
        {
            Dispatch(Commands.AddActionMap());
            m_EnterRenamingMode = true;
        }

        private void OnKeyDownEvent(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.F2)
                OnKeyDownEventForRename();
            else if (e.keyCode == KeyCode.Delete)
                OnKeyDownEventForDelete();
            else if (IsDuplicateShortcutPressed(e))
                OnKeyDownEventForDuplicate();
        }

        private void OnKeyDownEventForRename()
        {
            var item = (InputActionMapsTreeViewItem)m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            item.FocusOnRenameTextField();
        }

        private void OnKeyDownEventForDelete()
        {
            var item = (InputActionMapsTreeViewItem)m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            item.DeleteItem();
        }

        private void OnKeyDownEventForDuplicate()
        {
            ((InputActionMapsTreeViewItem)m_ListView.GetRootElementForIndex(m_ListView.selectedIndex))?.DuplicateItem();
        }

        private readonly CollectionViewSelectionChangeFilter m_ListViewSelectionChangeFilter;
        private bool m_EnterRenamingMode;
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
