using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class ActionsPanelView : UIToolkitView
	{
		private readonly VisualElement m_Root;
		private readonly VisualTreeAsset m_ActionItemRowTemplate;
		private readonly ListView m_ActionsListView;

		public ActionsPanelView(VisualElement root, StateContainer stateContainer)
			: base(stateContainer)
		{
			m_Root = root;
			m_ActionItemRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
				GlobalInputActionsConstants.PackagePath + 
				GlobalInputActionsConstants.ResourcesPath + 
				GlobalInputActionsConstants.ActionsPanelViewName);

			m_ActionsListView = m_Root.Q<ListView>("actions-list-view");
			m_ActionsListView.selectionType = UIElements.SelectionType.Single;
			m_ActionsListView.makeItem = () => m_ActionItemRowTemplate.CloneTree();
			m_ActionsListView.bindItem = (e, i) =>
			{
				e.Q<Label>("name").text = (string)m_ActionsListView.itemsSource[i];
			};

			m_ActionsListView.onSelectionChange += _ =>
			{
				var selectedItem = (string)m_ActionsListView.selectedItem;
				Dispatch(Commands.SelectAction(selectedItem));
			};
		}

		public override void CreateUI(GlobalInputActionsEditorState state)
		{
			m_ActionsListView.itemsSource?.Clear();
			m_ActionsListView.itemsSource = Selectors.GetActionsForSelectedActionMap(state)
				.Select(p => p.name)
				.ToList();
			m_ActionsListView.Rebuild();
		}
	}
}