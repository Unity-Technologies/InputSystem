#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class PropertiesView : ViewBase<PropertiesView.ViewState>
    {
        private ActionPropertiesView m_ActionPropertyView;
        private BindingPropertiesView m_BindingPropertyView;
        private NameAndParametersListView m_InteractionsListView;
        private NameAndParametersListView m_ProcessorsListView;

        private Foldout interactionsFoldout => rootElement.Q<Foldout>("interactions-foldout");
        private Foldout processorsFoldout => rootElement.Q<Foldout>("processors-foldout");

        private TextElement addInteractionButton;
        private TextElement addProcessorButton;

        public PropertiesView(VisualElement root, StateContainer stateContainer)
            : base(root, stateContainer)
        {
            CreateSelector(
                Selectors.GetSelectedAction,
                Selectors.GetSelectedBinding,
                state => state.selectionType,
                (inputAction, inputBinding, selectionType, s) => new ViewState()
                {
                    selectionType = selectionType,
                    serializedInputAction = inputAction,
                    inputBinding = inputBinding,
                    relatedInputAction = Selectors.GetRelatedInputAction(s)
                });

            var interactionsToggle = interactionsFoldout.Q<Toggle>();
            interactionsToggle.AddToClassList("properties-foldout-toggle");
            if (addInteractionButton == null)
            {
                addInteractionButton = CreateAddButton(interactionsToggle, "add-new-interaction-button");
                addInteractionButton.AddToClassList(EditorGUIUtility.isProSkin ? "add-binging-button-dark-theme" : "add-binging-button");

                new ContextualMenuManipulator(_ => {}){target = addInteractionButton, activators = {new ManipulatorActivationFilter(){button = MouseButton.LeftMouse}}};
            }
            var processorToggle = processorsFoldout.Q<Toggle>();
            processorToggle.AddToClassList("properties-foldout-toggle");
            if (addProcessorButton == null)
            {
                addProcessorButton = CreateAddButton(processorToggle, "add-new-processor-button");
                addProcessorButton.AddToClassList(EditorGUIUtility.isProSkin ? "add-binging-button-dark-theme" : "add-binging-button");

                new ContextualMenuManipulator(_ => {}){target = addProcessorButton, activators = {new ManipulatorActivationFilter(){button = MouseButton.LeftMouse}}};
            }
        }

        private TextElement CreateAddButton(Toggle toggle, string name)
        {
            var addButton = new Button();
            addButton.name = name;
            addButton.focusable = false;
            #if UNITY_EDITOR_OSX
            addButton.clickable.activators.Clear();
            #endif
            addButton.AddToClassList("add-interaction-processor-button");
            toggle.Add(addButton);
            return addButton;
        }

        private void CreateContextMenuProcessor(string expectedControlType)
        {
            var processors = InputProcessor.s_Processors;
            Type expectedValueType = string.IsNullOrEmpty(expectedControlType) ? null : EditorInputControlLayoutCache.GetValueType(expectedControlType);
            addProcessorButton.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.ClearItems();
                foreach (var name in processors.internedNames.Where(x => !processors.ShouldHideInUI(x)).OrderBy(x => x.ToString()))
                {
                    // Skip if not compatible with value type.
                    if (!IsValidProcessorForControl(expectedValueType, name))
                        continue;
                    var niceName = ObjectNames.NicifyVariableName(name);
                    evt.menu.AppendAction(niceName, _ => m_ProcessorsListView.OnAddElement(name.ToString()));
                }
            });
        }

        private bool IsValidProcessorForControl(Type expectedValueType, string name)
        {
            if (expectedValueType == null) return true;
            var type = InputProcessor.s_Processors.LookupTypeRegistration(name);
            var valueType = InputProcessor.GetValueTypeFromType(type);
            if (valueType != null && !expectedValueType.IsAssignableFrom(valueType))
                return false;
            return true;
        }

        private void CreateContextMenuInteraction(string expectedControlType)
        {
            var interactions = InputInteraction.s_Interactions;
            Type expectedValueType = string.IsNullOrEmpty(expectedControlType) ? null : EditorInputControlLayoutCache.GetValueType(expectedControlType);
            addInteractionButton.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.ClearItems();
                foreach (var name in interactions.internedNames.Where(x => !interactions.ShouldHideInUI(x)).OrderBy(x => x.ToString()))
                {
                    // Skip if not compatible with value type.
                    if (!IsValidInteractionForControl(expectedValueType, name))
                        continue;
                    var niceName = ObjectNames.NicifyVariableName(name);
                    evt.menu.AppendAction(niceName, _ => m_InteractionsListView.OnAddElement(name.ToString()));
                }
            });
        }

        private bool IsValidInteractionForControl(Type expectedValueType, string name)
        {
            if (expectedValueType == null) return true;
            var type = InputInteraction.s_Interactions.LookupTypeRegistration(name);
            var valueType = InputInteraction.GetValueType(type);
            if (valueType != null && !expectedValueType.IsAssignableFrom(valueType))
                return false;
            return true;
        }

        public override void RedrawUI(ViewState viewState)
        {
            DestroyChildView(m_ActionPropertyView);
            DestroyChildView(m_BindingPropertyView);
            DestroyChildView(m_InteractionsListView);
            DestroyChildView(m_ProcessorsListView);

            var propertiesContainer = rootElement.Q<VisualElement>("properties-container");

            var foldout = propertiesContainer.Q<Foldout>("properties-foldout");
            foldout.Clear();

            var visualElement = new VisualElement();
            foldout.Add(visualElement);
            foldout.Q<Toggle>().AddToClassList("properties-foldout-toggle");

            var inputAction = viewState.serializedInputAction;
            var inputActionOrBinding = inputAction?.wrappedProperty;

            switch (viewState.selectionType)
            {
                case SelectionType.Action:
                    rootElement.Q<Label>("properties-header-label").text = "Action Properties";
                    m_ActionPropertyView = CreateChildView(new ActionPropertiesView(visualElement, foldout, stateContainer));
                    break;

                case SelectionType.Binding:
                    rootElement.Q<Label>("properties-header-label").text = "Binding Properties";
                    m_BindingPropertyView = CreateChildView(new BindingPropertiesView(visualElement, foldout, stateContainer));
                    inputAction = viewState.relatedInputAction;
                    inputActionOrBinding = viewState.inputBinding?.wrappedProperty;
                    break;
            }

            CreateContextMenuProcessor(inputAction?.expectedControlType);
            CreateContextMenuInteraction(inputAction?.expectedControlType);

            var isPartOfComposite = viewState.selectionType == SelectionType.Binding &&
                viewState.inputBinding?.isPartOfComposite == true;
            //don't show for Bindings in Composites
            if (!isPartOfComposite)
            {
                interactionsFoldout.style.display = DisplayStyle.Flex;
                m_InteractionsListView = CreateChildView(new NameAndParametersListView(
                    interactionsFoldout,
                    stateContainer,
                    inputActionOrBinding?.FindPropertyRelative(nameof(InputAction.m_Interactions)),
                    state => Selectors.GetInteractionsAsParameterListViews(state, inputAction)));
            }
            else
                interactionsFoldout.style.display = DisplayStyle.None;


            m_ProcessorsListView = CreateChildView(new NameAndParametersListView(
                processorsFoldout,
                stateContainer,
                inputActionOrBinding?.FindPropertyRelative(nameof(InputAction.m_Processors)),
                state => Selectors.GetProcessorsAsParameterListViews(state, inputAction)));
        }

        internal class ViewState
        {
            public SerializedInputAction? relatedInputAction;
            public SerializedInputBinding? inputBinding;
            public SerializedInputAction? serializedInputAction;
            public SelectionType selectionType;
        }
    }
}

#endif
