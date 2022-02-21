using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class BindingPropertiesView : UIToolkitView
	{
		private readonly VisualElement m_Root;
		private readonly Foldout m_ParentFoldout;

		public BindingPropertiesView(VisualElement root, Foldout foldout, StateContainer stateContainer)
			: base(stateContainer)
		{
			m_Root = root;
			m_ParentFoldout = foldout;
		}

		public override void CreateUI(GlobalInputActionsEditorState state)
		{
			var selectedBinding = state.selectedBindingIndex.value;
			if (selectedBinding == -1)
				return;

			m_Root.Clear();

			var binding = Selectors.GetSelectedBinding(state);
			if (binding.isComposite)
			{
				var inputAction = Selectors.GetSelectedAction(state);
				var compositeTypes = Selectors.GetCompositeTypes(binding.path, inputAction.expectedControlType).ToList();

				var compositeNameAndParameters = NameAndParameters.Parse(binding.path);
				var compositeName = compositeNameAndParameters.name;
				var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);

				var compositeParameters = new ParameterListView();
				compositeParameters.onChange = () => Dispatch(
					Commands.UpdatePathNameAndValues(compositeParameters.GetParameters(), Selectors.GetSelectedBindingPath(state)));

				if (compositeType != null)
					compositeParameters.Initialize(compositeType, compositeNameAndParameters.parameters);

				var compositeTypeField = new DropdownField("Composite Type")
				{
					tooltip = GlobalInputActionsConstants.CompositeTypeTooltip
				};
				compositeTypeField.choices.AddRange(compositeTypes.Select(ObjectNames.NicifyVariableName));
				compositeTypeField.index = compositeTypes.FindIndex(str =>
					InputBindingComposite.s_Composites.LookupTypeRegistration(str) == compositeType);
			    
				compositeTypeField.RegisterValueChangedCallback(evt =>
					Dispatch(
						Commands.SetCompositeBindingType(
							binding,
							new NameAndParameters
							{
								name = compositeTypes[compositeTypeField.index],
								parameters = compositeParameters.GetParameters()
							})));

				m_ParentFoldout.text = "Composite";
				m_Root.Add(compositeTypeField);

				compositeParameters.OnDrawVisualElements(m_Root);
			}
			else if (binding.isPartOfComposite)
			{
				// TODO: Persist control picker state
				var controlPathEditor = new InputControlPathEditor(Selectors.GetSelectedBindingPath(state), new InputControlPickerState(),
					() => { /* the InputControlPathEditor already updates the serialized object internally, so we have nothing to do here */ });

				var partName = binding.name;
				var compositeName = binding.compositePath;
				var layoutName = InputBindingComposite.GetExpectedControlLayoutName(compositeName, partName);

				controlPathEditor.SetExpectedControlLayout(layoutName ?? "");

				var controlPathContainer = new IMGUIContainer(controlPathEditor.OnGUI);
				m_Root.Add(controlPathContainer);


				var compositeParts = Selectors.GetCompositePartOptions(binding.name, binding.compositePath).ToList();
				var compositePartField = new DropdownField("Composite Part", 
					compositeParts.Select(ObjectNames.NicifyVariableName).ToList(),
					compositeParts.FindIndex(str => str == binding.name))
				{
					tooltip = GlobalInputActionsConstants.CompositePartAssignmentTooltip
				};
				compositePartField.RegisterValueChangedCallback(evt =>
				{
					Dispatch(Commands.SetCompositeBindingPartName(Selectors.GetSelectedBinding(state), evt.newValue));
				});
				m_Root.Add(compositePartField);
			}
			else
			{
				m_ParentFoldout.text = "Binding";

				var controlPathEditor = new InputControlPathEditor(Selectors.GetSelectedBindingPath(state), new InputControlPickerState(),
					() => { /* the InputControlPathEditor already updates the serialized object internally, so we have nothing to do here */ });

				var inputAction = Selectors.GetSelectedAction(state);
				controlPathEditor.SetExpectedControlLayout(inputAction.expectedControlType ?? "");

				var controlPathContainer = new IMGUIContainer(controlPathEditor.OnGUI);
				m_Root.Add(controlPathContainer);
			}
		}
	}
}