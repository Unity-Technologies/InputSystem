using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
	internal delegate void Command(ref GlobalInputActionsEditorState state);

	internal static class Commands
	{
		public static Command SelectAction(string actionName)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				state.SelectAction(actionName);
			};
		}

		public static Command SelectActionMap(string actionMapName)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				state.SelectActionMap(actionMapName);
			};
		}

		public static Command ExpandCompositeBinding(SerializedInputBinding binding)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				state.ExpandCompositeBinding(binding);
			};
		}

		public static Command CollapseCompositeBinding(SerializedInputBinding binding)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				state.CollapseCompositeBinding(binding);
			};
		}

		public static Command SelectBinding(SerializedInputBinding binding)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				state.selectedBindingIndex.value = binding.indexOfBinding;
				state.selectionType.value = SelectionType.Binding;
			};
		}

		public static Command UpdatePathNameAndValues(NamedValue[] parameters, SerializedProperty pathProperty)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				var path = pathProperty.stringValue;
				var nameAndParameters = NameAndParameters.Parse(path);
				nameAndParameters.parameters = parameters;

				pathProperty.stringValue = nameAndParameters.ToString();
				state.serializedObject.ApplyModifiedProperties();
			};
		}

		public static Command SetCompositeBindingType(SerializedInputBinding bindingProperty, IEnumerable<string> compositeTypes,
			ParameterListView parameterListView, int selectedCompositeTypeIndex)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				var nameAndParameters = new NameAndParameters
				{
					name = compositeTypes.ElementAt(selectedCompositeTypeIndex),
					parameters = parameterListView.GetParameters()
				};
				InputActionSerializationHelpers.ChangeCompositeBindingType(bindingProperty.wrappedProperty, nameAndParameters);
				state.serializedObject.ApplyModifiedProperties();
			};
		}
		
		public static Command SetCompositeBindingPartName(SerializedInputBinding bindingProperty, string partName)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				InputActionSerializationHelpers.ChangeBinding(bindingProperty.wrappedProperty, partName);
				state.serializedObject.ApplyModifiedProperties();
			};
		}

		public static Command ChangeActionType(SerializedInputAction inputAction, InputActionType newValue)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Type)).intValue = (int)newValue;
				state.serializedObject.ApplyModifiedProperties();
			};
		}

		public static Command ChangeInitialStateCheck(SerializedInputAction inputAction, bool value)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				var property = inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Flags));
				if (value)
					property.intValue |= (int)InputAction.ActionFlags.WantsInitialStateCheck;
				else
					property.intValue &= ~(int)InputAction.ActionFlags.WantsInitialStateCheck;
				state.serializedObject.ApplyModifiedProperties();
			};
		}

		public static Command ChangeActionControlType(SerializedInputAction inputAction, int controlTypeIndex)
		{
			return (ref GlobalInputActionsEditorState state) =>
			{
				var controlTypes = Selectors.BuildSortedControlList(inputAction.type).ToList();
				inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_ExpectedControlType)).stringValue = controlTypes[controlTypeIndex];
				state.serializedObject.ApplyModifiedProperties();
			};
		}
	}
}