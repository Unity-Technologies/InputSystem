using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class BindingPropertiesView : ViewBase<BindingPropertiesView.ViewState>
	{
		private readonly VisualElement m_Root;
		private readonly Foldout m_ParentFoldout;
		private CompositeBindingPropertiesView m_CompositeBindingPropertiesView;
		private CompositePartBindingPropertiesView m_CompositePartBindingPropertiesView;

		public BindingPropertiesView(VisualElement root, Foldout foldout, StateContainer stateContainer)
			: base(stateContainer)
		{
			m_Root = root;
			m_ParentFoldout = foldout;

			CreateSelector(state => state.selectedBindingIndex,
				s => new ViewStateCollection<InputControlScheme>(Selectors.GetControlSchemes(s)),
				(_, controlSchemes, s) => new ViewState
				{
					controlSchemes = controlSchemes,
					selectedBinding = Selectors.GetSelectedBinding(s),
					selectedBindingIndex = s.selectedBindingIndex,
					selectedBindingPath = Selectors.GetSelectedBindingPath(s),
					selectedInputAction = Selectors.GetSelectedAction(s)
				});
		}

		public override void RedrawUI(ViewState viewState)
		{
			var selectedBindingIndex = viewState.selectedBindingIndex;
			if (selectedBindingIndex == -1)
				return;

			m_Root.Clear();

			var binding = viewState.selectedBinding;
			if (binding.isComposite)
			{
				m_ParentFoldout.text = "Composite";
				m_CompositeBindingPropertiesView = CreateChildView(new CompositeBindingPropertiesView(m_Root, stateContainer));
			}
			else if (binding.isPartOfComposite)
			{
				m_CompositePartBindingPropertiesView = CreateChildView(new CompositePartBindingPropertiesView(m_Root, stateContainer));
			}
			else
			{
				m_ParentFoldout.text = "Binding";

				var controlPathEditor = new InputControlPathEditor(viewState.selectedBindingPath, new InputControlPickerState(),
					() => { Dispatch(Commands.ApplyModifiedProperties()); });

				var inputAction = viewState.selectedInputAction;
				controlPathEditor.SetExpectedControlLayout(inputAction.expectedControlType ?? "");

				var controlPathContainer = new IMGUIContainer(controlPathEditor.OnGUI);
				m_Root.Add(controlPathContainer);
			}

			var controlSchemesContainer = m_Root.Q<VisualElement>("control-schemes-container");
			if (viewState.controlSchemes.Any() == false)
			{
				controlSchemesContainer.style.visibility = new StyleEnum<Visibility>(Visibility.Hidden);
			}
			else
			{
				foreach (var controlScheme in viewState.controlSchemes)
				{
					var checkbox = new Toggle(controlScheme.name);
					Selectors.IsControlSchemeSelectedForBinding()
					viewState.selectedBinding.controlSchemes.FirstOrDefault(controlScheme.name);
				}
			}
		}

		public override void DestroyView()
		{
			m_CompositeBindingPropertiesView?.DestroyView();
			m_CompositePartBindingPropertiesView?.DestroyView();
		}

		internal class ViewState
		{
			public int selectedBindingIndex;
			public SerializedInputBinding selectedBinding;
			public ViewStateCollection<InputControlScheme> controlSchemes;
			public SerializedProperty selectedBindingPath;
			public SerializedInputAction selectedInputAction;
		}

		internal static partial class Selectors
		{
		}
	}
}