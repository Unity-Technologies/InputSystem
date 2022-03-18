using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
	internal delegate GlobalInputActionsEditorState Command(in GlobalInputActionsEditorState state);

	internal static class Commands
	{
		public static Command SelectAction(string actionName)
		{
			return (in GlobalInputActionsEditorState state) => state.SelectAction(actionName);
		}

		public static Command SelectActionMap(string actionMapName)
		{
			return (in GlobalInputActionsEditorState state) => state.SelectActionMap(actionMapName);
		}

		public static Command ExpandCompositeBinding(SerializedInputBinding binding)
		{
			return (in GlobalInputActionsEditorState state) => state.ExpandCompositeBinding(binding);
		}

		public static Command CollapseCompositeBinding(SerializedInputBinding binding)
		{
			return (in GlobalInputActionsEditorState state) => state.CollapseCompositeBinding(binding);
		}

		public static Command SelectBinding(SerializedInputBinding binding)
		{
			return (in GlobalInputActionsEditorState state) => 
				state.With(selectedBindingIndex: binding.indexOfBinding, selectionType: SelectionType.Binding);
		}

		public static Command UpdatePathNameAndValues(NamedValue[] parameters, SerializedProperty pathProperty)
		{
			return (in GlobalInputActionsEditorState state) =>
			{
				var path = pathProperty.stringValue;
				var nameAndParameters = NameAndParameters.Parse(path);
				nameAndParameters.parameters = parameters;

				pathProperty.stringValue = nameAndParameters.ToString();
				state.serializedObject.ApplyModifiedProperties();
				return state;
			};
		}

		public static Command SetCompositeBindingType(SerializedInputBinding bindingProperty, IEnumerable<string> compositeTypes,
			ParameterListView parameterListView, int selectedCompositeTypeIndex)
		{
			return (in GlobalInputActionsEditorState state) =>
			{
				var nameAndParameters = new NameAndParameters
				{
					name = compositeTypes.ElementAt(selectedCompositeTypeIndex),
					parameters = parameterListView.GetParameters()
				};
				InputActionSerializationHelpers.ChangeCompositeBindingType(bindingProperty.wrappedProperty, nameAndParameters);
				state.serializedObject.ApplyModifiedProperties();
				return state;
			};
		}
		
		public static Command SetCompositeBindingPartName(SerializedInputBinding bindingProperty, string partName)
		{
			return (in GlobalInputActionsEditorState state) =>
			{
				InputActionSerializationHelpers.ChangeBinding(bindingProperty.wrappedProperty, partName);
				state.serializedObject.ApplyModifiedProperties();
				return state;
			};
		}

		public static Command ChangeActionType(SerializedInputAction inputAction, InputActionType newValue)
		{
			return (in GlobalInputActionsEditorState state) =>
			{
				inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Type)).intValue = (int)newValue;
				state.serializedObject.ApplyModifiedProperties();
				return state;
			};
		}

		public static Command ChangeInitialStateCheck(SerializedInputAction inputAction, bool value)
		{
			return (in GlobalInputActionsEditorState state) =>
			{
				var property = inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Flags));
				if (value)
					property.intValue |= (int)InputAction.ActionFlags.WantsInitialStateCheck;
				else
					property.intValue &= ~(int)InputAction.ActionFlags.WantsInitialStateCheck;
				state.serializedObject.ApplyModifiedProperties();
				return state;
			};
		}

		public static Command ChangeActionControlType(SerializedInputAction inputAction, int controlTypeIndex)
		{
			return (in GlobalInputActionsEditorState state) =>
			{
				var controlTypes = Selectors.BuildSortedControlList(inputAction.type).ToList();
				inputAction.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_ExpectedControlType)).stringValue = controlTypes[controlTypeIndex];
				state.serializedObject.ApplyModifiedProperties();
				return state;
			};
		}

		/// <summary>
		/// Exists to integrate with some existing UI stuff, like InputControlPathEditor
		/// </summary>
		/// <returns></returns>
		public static Command ApplyModifiedProperties()
		{
			return (in GlobalInputActionsEditorState state) =>
			{
				state.serializedObject.ApplyModifiedProperties();
				return state;
			};
		}
	}
}