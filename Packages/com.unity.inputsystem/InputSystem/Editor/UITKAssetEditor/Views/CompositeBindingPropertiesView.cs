#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class CompositeBindingPropertiesView : ViewBase<CompositeBindingPropertiesView.ViewState>
    {
        private readonly VisualElement m_Root;
        private readonly DropdownField m_CompositeTypeField;
        private EventCallback<ChangeEvent<string>> m_CompositeTypeFieldChangedHandler;

        private const string UxmlName = InputActionsEditorConstants.PackagePath +
            InputActionsEditorConstants.ResourcesPath +
            InputActionsEditorConstants.CompositeBindingPropertiesViewUxml;

        public CompositeBindingPropertiesView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlName);
            var container = visualTreeAsset.CloneTree();
            m_Root.Add(container);

            m_CompositeTypeField = container.Q<DropdownField>("composite-type-dropdown");

            CreateSelector(Selectors.GetSelectedBinding,
                (binding, state) => Selectors.GetCompositeBindingViewState(state, binding));
        }

        public override void RedrawUI(ViewState viewState)
        {
            m_CompositeTypeField.choices.Clear();
            m_CompositeTypeField.choices.AddRange(viewState.compositeNames);
            m_CompositeTypeField.SetValueWithoutNotify(viewState.selectedCompositeName);

            m_CompositeTypeFieldChangedHandler = _ => OnCompositeTypeFieldChanged(viewState);
            m_CompositeTypeField.RegisterValueChangedCallback(m_CompositeTypeFieldChangedHandler);

            viewState.parameterListView.onChange = () =>
            {
                Dispatch(Commands.UpdatePathNameAndValues(viewState.parameterListView.GetParameters(), viewState.selectedBindingPath));
            };
            viewState.parameterListView.OnDrawVisualElements(m_Root);
        }

        public override void DestroyView()
        {
            m_CompositeTypeField.UnregisterValueChangedCallback(m_CompositeTypeFieldChangedHandler);
        }

        private void OnCompositeTypeFieldChanged(ViewState viewState)
        {
            Dispatch(
                Commands.SetCompositeBindingType(
                    viewState.selectedBinding,
                    viewState.compositeTypes,
                    viewState.parameterListView,
                    m_CompositeTypeField.index));
        }

        internal class ViewState
        {
            public SerializedInputBinding selectedBinding;
            public IEnumerable<string> compositeTypes;
            public SerializedProperty selectedBindingPath;
            public ParameterListView parameterListView;
            public string selectedCompositeName;
            public IEnumerable<string> compositeNames;
        }
    }

    internal static partial class Selectors
    {
        public static CompositeBindingPropertiesView.ViewState GetCompositeBindingViewState(in InputActionsEditorState state,
            SerializedInputBinding binding)
        {
            var inputAction = GetSelectedAction(state);
            var compositeNameAndParameters = NameAndParameters.Parse(binding.path);
            var compositeName = compositeNameAndParameters.name;
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);

            var parameterListView = new ParameterListView();
            if (compositeType != null)
                parameterListView.Initialize(compositeType, compositeNameAndParameters.parameters);

            var compositeTypes = GetCompositeTypes(binding.path, inputAction.expectedControlType).ToList();
            var compositeNames = compositeTypes.Select(ObjectNames.NicifyVariableName).ToList();
            var selectedCompositeName = compositeNames[compositeTypes.FindIndex(str =>
                InputBindingComposite.s_Composites.LookupTypeRegistration(str) == compositeType)];

            return new CompositeBindingPropertiesView.ViewState
            {
                selectedBinding = binding,
                selectedBindingPath = GetSelectedBindingPath(state),
                compositeTypes = compositeTypes,
                compositeNames = compositeNames,
                parameterListView = parameterListView,
                selectedCompositeName = selectedCompositeName
            };
        }
    }
}

#endif
