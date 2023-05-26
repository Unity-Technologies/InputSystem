#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
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

        private Foldout interactionsFoldout => m_Root.Q<Foldout>("interactions-foldout");
        private Foldout processorsFoldout => m_Root.Q<Foldout>("processors-foldout");

        private TextElement addInteractionButton;
        private TextElement addProcessorButton;

        public PropertiesView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;

            CreateSelector(
                Selectors.GetSelectedBinding,
                Selectors.GetSelectedAction,
                state => state.selectionType,
                (_, _, selectionType, _) => selectionType);

            var interactionsToggle = interactionsFoldout.Q<Toggle>();
            interactionsToggle.AddToClassList("properties-foldout-toggle");
            if (addInteractionButton == null)
            {
                addInteractionButton = CreateAddButton(interactionsToggle, "add-new-interaction-button");
                CreatContextMenuInteraction(addInteractionButton, AddInteraction);
            }
            var processorToggle = processorsFoldout.Q<Toggle>();
            processorToggle.AddToClassList("properties-foldout-toggle");
            if (addProcessorButton == null)
            {
                addProcessorButton = CreateAddButton(processorToggle, "add-new-processor-button");
                CreatContextMenuProcessor(addProcessorButton, AddProcessor);
            }
        }

        private TextElement CreateAddButton(Toggle toggle, string name)
        {
            var addProcessorButton = new TextElement();
            addProcessorButton.text = "+";
            addProcessorButton.name = name;
            addProcessorButton.AddToClassList("add-interaction-processor-button");
            toggle.Add(addProcessorButton);
            return addProcessorButton;
        }

        private void CreatContextMenuProcessor(VisualElement targetElement, Action onClick)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction("do", action =>
                {
                    onClick.Invoke();
                });
            }) { target = targetElement, activators = {new ManipulatorActivationFilter(){button = MouseButton.LeftMouse}}};
        }

        private void CreatContextMenuInteraction(VisualElement targetElement, Action onClick)
        {
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction("do", action =>
                {
                    onClick.Invoke();
                });
            }) { target = targetElement, activators = {new ManipulatorActivationFilter(){button = MouseButton.LeftMouse}}};
        }

        private void AddInteraction()
        {
        }

        private void AddProcessor()
        {
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

            m_InteractionsListView = CreateChildView(new NameAndParametersListView(
                interactionsFoldout,
                stateContainer,
                Selectors.GetInteractionsAsParameterListViews));


            m_ProcessorsListView = CreateChildView(new NameAndParametersListView(
                processorsFoldout,
                stateContainer,
                Selectors.GetProcessorsAsParameterListViews));
        }
    }
}

#endif
