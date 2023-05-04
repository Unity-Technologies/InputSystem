#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
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
            m_ListView.selectionChanged += _ => SelectActionMap();

            m_ListView.bindItem = (element, i) =>
            {
                var treeViewItem = (InputActionsTreeViewItem)element;
                treeViewItem.label.text = (string)m_ListView.itemsSource[i];
                treeViewItem.EditTextFinished += newName => ChangeActionMapName(i, newName);
            };
            m_ListView.makeItem = () => new InputActionsTreeViewItem();
            m_ListView.unbindItem = (element, i) =>
            {
                ((InputActionsTreeViewItem)element).EditTextFinished -= newName => ChangeActionMapName(i, newName);
            };

            CreateSelector(s => new ViewStateCollection<string>(Selectors.GetActionMapNames(s)),
                (actionMapNames, state) => new ViewState(Selectors.GetSelectedActionMap(state), actionMapNames));

            addActionMapButton.clicked += AddActionMap;
        }

        private Button addActionMapButton => m_Root?.Q<Button>("add-new-action-map-button");

        public override void RedrawUI(ViewState viewState)
        {
            m_ListView.itemsSource = viewState.actionMapNames?.ToList() ?? new List<string>();
            var indexOf = viewState.actionMapNames.IndexOf(viewState.selectedActionMap.name);
            m_ListView.SetSelection(indexOf);
            m_ListView.Rebuild();
            RenameNewActionMaps();
        }

        public override void DestroyView()
        {
            addActionMapButton.clicked -= AddActionMap;
        }

        private void RenameNewActionMaps()
        {
            if (!mapAdded)
                return;
            var element = m_ListView.GetRootElementForIndex(m_ListView.selectedIndex);
            ((InputActionsTreeViewItem)element).FocusOnRenameTextField();
            mapAdded = false;
        }

        private void ChangeActionMapName(int index, string newName)
        {
            Dispatch(Commands.ChangeActionMapName(index, newName));
        }

        private void SelectActionMap()
        {
            Dispatch(Commands.SelectActionMap((string)m_ListView.selectedItem));
        }

        private void AddActionMap()
        {
            Dispatch(Commands.AddActionMap());
            mapAdded = true;
        }

        private bool mapAdded;
        private readonly VisualElement m_Root;
        private ListView m_ListView;

        internal class ViewState
        {
            public SerializedInputActionMap selectedActionMap;
            public IEnumerable<string> actionMapNames;

            public ViewState(SerializedInputActionMap selectedActionMap, IEnumerable<string> actionMapNames)
            {
                this.selectedActionMap = selectedActionMap;
                this.actionMapNames = actionMapNames;
            }
        }
    }
}

#endif
