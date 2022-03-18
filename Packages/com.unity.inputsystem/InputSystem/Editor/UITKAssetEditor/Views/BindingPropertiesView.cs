using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class BindingPropertiesView : UIToolkitView<InputActionsEditorState>
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
				(_, state) => state);
		}

		public override void RedrawUI(InputActionsEditorState state)
		{
			var selectedBinding = state.selectedBindingIndex;
			if (selectedBinding == -1)
				return;

			m_Root.Clear();

			var binding = Selectors.GetSelectedBinding(state);
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

				var controlPathEditor = new InputControlPathEditor(Selectors.GetSelectedBindingPath(state), new InputControlPickerState(),
					() => { Dispatch(Commands.ApplyModifiedProperties()); });

				var inputAction = Selectors.GetSelectedAction(state);
				controlPathEditor.SetExpectedControlLayout(inputAction.expectedControlType ?? "");

				var controlPathContainer = new IMGUIContainer(controlPathEditor.OnGUI);
				m_Root.Add(controlPathContainer);
			}
		}

		public override void DestroyView()
		{
			m_CompositeBindingPropertiesView?.DestroyView();
			m_CompositePartBindingPropertiesView?.DestroyView();
		}
	}
}