using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class PropertiesView : UIToolkitView
    {
	    private readonly VisualElement m_Root;
	    private SerializedInputBinding m_PreviousBindingState;
	    private SerializedInputAction m_PreviousActionState;
	    private SelectionType m_PreviousSelectionType;
	    private ActionPropertiesView m_ActionPropertyView;
	    private BindingPropertiesView m_BindingPropertyView;
	    private NameAndParametersListView m_InteractionsListView;
	    private NameAndParametersListView m_ProcessorsListView;

	    public PropertiesView(VisualElement root, StateContainer stateContainer)
			: base(stateContainer)
        {
	        m_Root = root;
        }
		
	    public override void CreateUI(GlobalInputActionsEditorState state)
	    {
			var binding = Selectors.GetSelectedBinding(state);
			var action = Selectors.GetSelectedAction(state);
			var selectionType = state.selectionType.value;
			if (binding.Equals(m_PreviousBindingState)
			    && action.Equals(m_PreviousActionState)
			    && selectionType == m_PreviousSelectionType)
				return;

			m_PreviousBindingState = binding;
			m_PreviousActionState = action;
			m_PreviousSelectionType = selectionType;

			m_ActionPropertyView?.Dispose();
			m_BindingPropertyView?.Dispose();
			m_InteractionsListView?.Dispose();
			m_ProcessorsListView?.Dispose();

			var propertiesContainer = m_Root.Q<VisualElement>("properties-container");

		    var foldout = propertiesContainer.Q<Foldout>("properties-foldout");
		    foldout.Clear();

		    var visualElement = new VisualElement();
			foldout.Add(visualElement);

		    switch (state.selectionType.value)
		    {
				case SelectionType.Action:
					m_Root.Q<Label>("properties-header-label").text = "Action Properties";
					m_ActionPropertyView = new ActionPropertiesView(visualElement, m_StateContainer);
					m_ActionPropertyView.CreateUI(state);
					break;

				case SelectionType.Binding:
					m_Root.Q<Label>("properties-header-label").text = "Binding Properties";
					m_BindingPropertyView = new BindingPropertiesView(visualElement, foldout, m_StateContainer);
					m_BindingPropertyView.CreateUI(state);
					break;
		    }

		    var interactionsFoldout = m_Root.Q<Foldout>("interactions-foldout");
		    m_InteractionsListView = new NameAndParametersListView(interactionsFoldout, m_StateContainer, Selectors.GetInteractionsAsParameterListViews);

		    var processorsFoldout = m_Root.Q<Foldout>("processors-foldout");
		    m_ProcessorsListView = new NameAndParametersListView(processorsFoldout, m_StateContainer, Selectors.GetProcessorsAsParameterListViews);
	    }
    }
}