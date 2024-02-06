#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class BindingPropertiesView : ViewBase<BindingPropertiesView.ViewState>
    {
        private readonly Foldout m_ParentFoldout;
        private CompositeBindingPropertiesView m_CompositeBindingPropertiesView;
        private CompositePartBindingPropertiesView m_CompositePartBindingPropertiesView;

        public BindingPropertiesView(VisualElement root, Foldout foldout, StateContainer stateContainer)
            : base(root, stateContainer)
        {
            m_ParentFoldout = foldout;

            CreateSelector(state => state.selectedBindingIndex,
                s => new ViewStateCollection<InputControlScheme>(Selectors.GetControlSchemes(s)),
                (_, controlSchemes, s) => new ViewState
                {
                    controlSchemes = controlSchemes,
                    currentControlScheme = s.selectedControlScheme,
                    selectedBinding = Selectors.GetSelectedBinding(s),
                    selectedBindingIndex = s.selectedBindingIndex,
                    selectedBindingPath = Selectors.GetSelectedBindingPath(s),
                    selectedInputAction = Selectors.GetSelectedAction(s)
                });
        }

        protected override void RedrawUI(ViewState viewState)
        {
            var selectedBindingIndex = viewState.selectedBindingIndex;
            if (selectedBindingIndex == -1)
                return;

            rootElement.Clear();

            var binding = viewState.selectedBinding;
            if (!binding.HasValue)
                return;

            m_ParentFoldout.text = "Binding";
            if (binding.Value.isComposite)
            {
                m_ParentFoldout.text = "Composite";
                m_CompositeBindingPropertiesView = CreateChildView(new CompositeBindingPropertiesView(rootElement, stateContainer));
            }
            else if (binding.Value.isPartOfComposite)
            {
                m_CompositePartBindingPropertiesView = CreateChildView(new CompositePartBindingPropertiesView(rootElement, stateContainer));
                DrawControlSchemeToggles(viewState, binding.Value);
            }
            else
            {
                var controlPathEditor = new InputControlPathEditor(viewState.selectedBindingPath, new InputControlPickerState(),
                    () => { Dispatch(Commands.ApplyModifiedProperties()); });
                controlPathEditor.SetControlPathsToMatch(viewState.currentControlScheme.deviceRequirements.Select(x => x.controlPath));

                var inputAction = viewState.selectedInputAction;
                controlPathEditor.SetExpectedControlLayout(inputAction?.expectedControlType ?? "");

                var controlPathContainer = new IMGUIContainer(controlPathEditor.OnGUI);
                rootElement.Add(controlPathContainer);

                DrawControlSchemeToggles(viewState, binding.Value);
            }
        }

        public override void DestroyView()
        {
            m_CompositeBindingPropertiesView?.DestroyView();
            m_CompositePartBindingPropertiesView?.DestroyView();
        }

        private void DrawControlSchemeToggles(ViewState viewState, SerializedInputBinding binding)
        {
            if (!viewState.controlSchemes.Any()) return;

            var useInControlSchemeLabel = new Label("Use in control scheme");
            rootElement.Add(useInControlSchemeLabel);

            foreach (var controlScheme in viewState.controlSchemes)
            {
                var checkbox = new Toggle(controlScheme.name)
                {
                    value = binding.controlSchemes.Any(scheme => controlScheme.name == scheme)
                };
                rootElement.Add(checkbox);
                checkbox.RegisterValueChangedCallback(changeEvent =>
                {
                    Dispatch(ControlSchemeCommands.ChangeSelectedBindingsControlSchemes(controlScheme.name, changeEvent.newValue));
                });
            }
        }

        internal class ViewState
        {
            public int selectedBindingIndex;
            public SerializedInputBinding? selectedBinding;
            public ViewStateCollection<InputControlScheme> controlSchemes;
            public InputControlScheme currentControlScheme;
            public SerializedProperty selectedBindingPath;
            public SerializedInputAction? selectedInputAction;
        }
    }
}

#endif
