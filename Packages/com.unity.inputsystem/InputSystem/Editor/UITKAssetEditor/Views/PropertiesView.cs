#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class PropertiesView : ViewBase<SelectionType>
    {
        private readonly VisualElement m_Root;
        private ActionPropertiesView m_ActionPropertyView;
        private BindingPropertiesView m_BindingPropertyView;
        private NameAndParametersListView m_InteractionsListView;
        private NameAndParametersListView m_ProcessorsListView;

        public PropertiesView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;

            CreateSelector(
                Selectors.GetSelectedBinding,
                Selectors.GetSelectedAction,
                state => state.selectionType,
                (_, _, selectionType, _) => selectionType);
        }

        public override void RedrawUI(SelectionType selectionType)
        {
            DestroyChildView(m_ActionPropertyView);
            DestroyChildView(m_BindingPropertyView);
            DestroyChildView(m_InteractionsListView);
            DestroyChildView(m_ProcessorsListView);

            var propertiesContainer = m_Root.Q<VisualElement>("properties-container");

            var foldout = propertiesContainer.Q<Foldout>("properties-foldout");
            foldout.Clear();

            var visualElement = new VisualElement();
            foldout.Add(visualElement);
            foldout.Q<Toggle>().AddToClassList("properties-foldout-toggle");

            switch (selectionType)
            {
                case SelectionType.Action:
                    m_Root.Q<Label>("properties-header-label").text = "Action Properties";
                    m_ActionPropertyView = CreateChildView(new ActionPropertiesView(visualElement, stateContainer));
                    break;

                case SelectionType.Binding:
                    m_Root.Q<Label>("properties-header-label").text = "Binding Properties";
                    m_BindingPropertyView = CreateChildView(new BindingPropertiesView(visualElement, foldout, stateContainer));
                    break;
            }

            var interactionsFoldout = m_Root.Q<Foldout>("interactions-foldout");
            interactionsFoldout.Q<Toggle>().AddToClassList("properties-foldout-toggle");
            m_InteractionsListView = CreateChildView(new NameAndParametersListView(
                interactionsFoldout,
                stateContainer,
                Selectors.GetInteractionsAsParameterListViews));

            var processorsFoldout = m_Root.Q<Foldout>("processors-foldout");
            processorsFoldout.Q<Toggle>().AddToClassList("properties-foldout-toggle");
            m_ProcessorsListView = CreateChildView(new NameAndParametersListView(
                processorsFoldout,
                stateContainer,
                Selectors.GetProcessorsAsParameterListViews));
        }
    }
}

#endif
