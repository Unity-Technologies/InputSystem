#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class PropertiesView : ViewBase<PropertiesView.ViewState>
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

        private ContextualMenuManipulator interactionMenu;
        private ContextualMenuManipulator processorMenu;

        public PropertiesView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;

            CreateSelector(
                Selectors.GetRelatedInputAction,
                Selectors.GetSelectedAction,
                state => state.selectionType,
                (relatedAction, inputAction, selectionType, _) => new ViewState()
                {
                    selectionType = selectionType,
                    serializedInputAction = inputAction,
                    relatedInputAction = relatedAction
                });

            var interactionsToggle = interactionsFoldout.Q<Toggle>();
            interactionsToggle.AddToClassList("properties-foldout-toggle");
            if (addInteractionButton == null)
            {
                addInteractionButton = CreateAddButton(interactionsToggle, "add-new-interaction-button");
                interactionMenu = new ContextualMenuManipulator(menuEvent => {}){target = addInteractionButton, activators = {new ManipulatorActivationFilter(){button = MouseButton.LeftMouse}}};
            }
            var processorToggle = processorsFoldout.Q<Toggle>();
            processorToggle.AddToClassList("properties-foldout-toggle");
            if (addProcessorButton == null)
            {
                addProcessorButton = CreateAddButton(processorToggle, "add-new-processor-button");
                processorMenu = new ContextualMenuManipulator(menuEvent => {}){target = addProcessorButton, activators = {new ManipulatorActivationFilter(){button = MouseButton.LeftMouse}}};
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

        private void CreateContextMenuProcessor(SerializedInputAction? inputAction)
        {
            var processors = InputProcessor.s_Processors;
            Type expectedValueType = null;
            if (!string.IsNullOrEmpty(inputAction?.expectedControlType))
                expectedValueType = EditorInputControlLayoutCache.GetValueType(inputAction.Value.expectedControlType);

            addProcessorButton.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.ClearItems();
                foreach (var name in processors.internedNames.Where(x => !processors.ShouldHideInUI(x)).OrderBy(x => x.ToString()))
                {
                    // Skip if not compatible with value type.
                    if (expectedValueType != null)
                    {
                        var type = processors.LookupTypeRegistration(name);
                        var valueType = InputProcessor.GetValueTypeFromType(type);
                        if (valueType != null && !expectedValueType.IsAssignableFrom(valueType))
                            continue;
                    }
                    var niceName = ObjectNames.NicifyVariableName(name);
                    var oldProcessors = inputAction?.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Processors));
                    evt.menu.AppendAction(niceName, _ => m_ProcessorsListView.OnAddElement(name.ToString(), oldProcessors, inputAction?.processors));
                }
            });
        }

        private void CreateContextMenuInteraction(SerializedInputAction? inputAction)
        {
            var interactions = InputInteraction.s_Interactions;
            Type expectedValueType = null;
            if (!string.IsNullOrEmpty(inputAction?.expectedControlType))
                expectedValueType = EditorInputControlLayoutCache.GetValueType(inputAction.Value.expectedControlType);
            addInteractionButton.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.ClearItems();
                foreach (var name in interactions.internedNames.Where(x => !interactions.ShouldHideInUI(x)).OrderBy(x => x.ToString()))
                {
                    // Skip if not compatible with value type.
                    if (expectedValueType != null)
                    {
                        var type = interactions.LookupTypeRegistration(name);
                        var valueType = InputInteraction.GetValueType(type);
                        if (valueType != null && !expectedValueType.IsAssignableFrom(valueType))
                            continue;
                    }

                    var niceName = ObjectNames.NicifyVariableName(name);
                    var oldInteractions = inputAction?.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Interactions));
                    evt.menu.AppendAction(niceName, _ => m_InteractionsListView.OnAddElement(name.ToString(), oldInteractions, inputAction?.interactions));
                }
            });
        }

        public override void RedrawUI(ViewState viewState)
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

            var inputAction = viewState.serializedInputAction;
            switch (viewState.selectionType)
            {
                case SelectionType.Action:
                    m_Root.Q<Label>("properties-header-label").text = "Action Properties";
                    m_ActionPropertyView = CreateChildView(new ActionPropertiesView(visualElement, stateContainer));
                    break;

                case SelectionType.Binding:
                    m_Root.Q<Label>("properties-header-label").text = "Binding Properties";
                    m_BindingPropertyView = CreateChildView(new BindingPropertiesView(visualElement, foldout, stateContainer));
                    inputAction = viewState.relatedInputAction;
                    break;
            }

            CreateContextMenuProcessor(inputAction);
            CreateContextMenuInteraction(inputAction);

            m_InteractionsListView = CreateChildView(new NameAndParametersListView(
                interactionsFoldout,
                stateContainer,
                state => Selectors.GetInteractionsAsParameterListViews(state, inputAction)));


            m_ProcessorsListView = CreateChildView(new NameAndParametersListView(
                processorsFoldout,
                stateContainer,
                state => Selectors.GetProcessorsAsParameterListViews(state, inputAction)));
        }

        internal class ViewState
        {
            public SelectionType selectionType;
            public SerializedInputAction? relatedInputAction;
            public SerializedInputAction? serializedInputAction;
        }
    }
}

#endif
