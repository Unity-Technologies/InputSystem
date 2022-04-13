#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class ActionsListView : ViewBase<IEnumerable<SerializedInputAction>>
    {
        private readonly VisualElement m_Root;
        private readonly VisualTreeAsset m_ActionItemRowTemplate;
        private readonly ListView m_ActionsListView;

        public ActionsListView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;
            m_ActionItemRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.ActionsPanelViewNameUxml);

            m_ActionsListView = m_Root.Q<ListView>("actions-list-view");
            m_ActionsListView.selectionType = UIElements.SelectionType.Single;
            m_ActionsListView.makeItem = () => m_ActionItemRowTemplate.CloneTree();
            m_ActionsListView.bindItem = (e, i) =>
            {
                var selectedInputAction = (SerializedInputAction)m_ActionsListView.itemsSource[i];
                e.Q<Label>("name").text = selectedInputAction.name;
                e.Q<VisualElement>("icon").style.backgroundImage =
                    new StyleBackground(
                        EditorInputControlLayoutCache.GetIconForLayout(selectedInputAction.expectedControlType));
            };

            m_ActionsListView.onSelectionChange += _ =>
            {
                var selectedItem = (SerializedInputAction)m_ActionsListView.selectedItem;
                Dispatch(Commands.SelectAction(selectedItem.name));
            };

            CreateSelector(Selectors.GetActionsForSelectedActionMap,
                (actions, _) => actions);
        }

        public override void RedrawUI(IEnumerable<SerializedInputAction> viewState)
        {
            m_ActionsListView.itemsSource?.Clear();
            m_ActionsListView.itemsSource = viewState.ToList();
            m_ActionsListView.Rebuild();
        }
    }
}

#endif
