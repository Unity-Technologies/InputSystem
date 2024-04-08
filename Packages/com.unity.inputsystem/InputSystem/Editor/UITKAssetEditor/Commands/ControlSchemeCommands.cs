#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ControlSchemeCommands
    {
        private const string kAllControlSchemesName = "All Control Schemes";
        private const string kNewControlSchemeName = "New Control Scheme";

        public static Command AddNewControlScheme()
        {
            return (in InputActionsEditorState state) => state.With(selectedControlScheme: new InputControlScheme(
                MakeUniqueControlSchemeName(state, kNewControlSchemeName)));
        }

        public static Command AddDeviceRequirement(InputControlScheme.DeviceRequirement requirement)
        {
            return (in InputActionsEditorState state) => state.With(selectedControlScheme: new InputControlScheme(state.selectedControlScheme.name,
                state.selectedControlScheme.deviceRequirements.Append(requirement)));
        }

        public static Command RemoveDeviceRequirement(int selectedDeviceIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                var newDeviceIndex =
                    Mathf.Clamp(
                        selectedDeviceIndex <= state.selectedDeviceRequirementIndex
                        ? state.selectedDeviceRequirementIndex - 1
                        : state.selectedDeviceRequirementIndex, -1, state.selectedDeviceRequirementIndex);
                return state.With(selectedControlScheme: new InputControlScheme(state.selectedControlScheme.name,
                    state.selectedControlScheme.deviceRequirements.Where((r, i) => i != selectedDeviceIndex)), selectedDeviceRequirementIndex: newDeviceIndex);
            };
        }

        public static Command SaveControlScheme(string newControlSchemeName = "", bool updateExisting = false)
        {
            return (in InputActionsEditorState state) =>
            {
                var controlSchemeName = state.selectedControlScheme.name;

                var controlSchemesArray = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
                var controlScheme = controlSchemesArray
                    .FirstOrDefault(sp => sp.FindPropertyRelative(nameof(InputControlScheme.m_Name)).stringValue == controlSchemeName);

                var actionMaps = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ActionMaps));

                // If the control scheme is null, we're saving a new control scheme, otherwise editing an existing one
                if (controlScheme == null && updateExisting)
                    throw new InvalidOperationException("Tried to update a non-existent control scheme.");

                if (updateExisting == false)
                {
                    controlSchemeName = MakeUniqueControlSchemeName(state, controlSchemeName);
                    controlSchemesArray.InsertArrayElementAtIndex(controlSchemesArray.arraySize);
                    controlScheme = controlSchemesArray.GetArrayElementAtIndex(controlSchemesArray.arraySize - 1);
                }
                // If we're renaming a control scheme, we need to update the bindings that use it and make a unique name
                if (!string.IsNullOrEmpty(newControlSchemeName))
                {
                    newControlSchemeName = MakeUniqueControlSchemeName(state, newControlSchemeName);
                    RenameBindingsControlSchemeHelper(controlScheme, actionMaps, controlSchemeName, newControlSchemeName);
                }

                controlScheme.FindPropertyRelative(nameof(InputControlScheme.m_Name)).stringValue = string.IsNullOrEmpty(newControlSchemeName) ? controlSchemeName  : newControlSchemeName;
                controlScheme.FindPropertyRelative(nameof(InputControlScheme.m_BindingGroup)).stringValue = string.IsNullOrEmpty(newControlSchemeName) ? controlSchemeName  : newControlSchemeName;

                var serializedDeviceRequirements = controlScheme.FindPropertyRelative(nameof(InputControlScheme.m_DeviceRequirements));
                serializedDeviceRequirements.ClearArray();
                for (var i = 0; i < state.selectedControlScheme.deviceRequirements.Count; i++)
                {
                    var deviceRequirement = state.selectedControlScheme.deviceRequirements[i];
                    serializedDeviceRequirements.InsertArrayElementAtIndex(i);

                    var serializedRequirement = serializedDeviceRequirements.GetArrayElementAtIndex(i);
                    serializedRequirement
                        .FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_ControlPath))
                        .stringValue = deviceRequirement.controlPath;
                    serializedRequirement.FindPropertyRelative(nameof(InputControlScheme.DeviceRequirement.m_Flags))
                        .enumValueFlag = (int)deviceRequirement.m_Flags;
                }

                state.serializedObject.ApplyModifiedProperties();
                return state.With(
                    selectedControlScheme: new InputControlScheme(controlScheme),
                    // Select the control scheme updated, otherwise select the new one it was added
                    selectedControlSchemeIndex: updateExisting? state.selectedControlSchemeIndex: controlSchemesArray.arraySize - 1);
            };
        }

        static void RenameBindingsControlSchemeHelper(SerializedProperty controlScheme, SerializedProperty actionMaps, string controlSchemeName, string newName)
        {
            foreach (SerializedProperty actionMap in actionMaps)
            {
                var bindings = actionMap
                    .FindPropertyRelative(nameof(InputActionMap.m_Bindings))
                    .Select(sp => new SerializedInputBinding(sp))
                    .ToList();

                var bindingsToRename = bindings.Where(b => b.controlSchemes.Contains(controlSchemeName)).ToList();

                foreach (var binding in bindingsToRename)
                {
                    var bindingGroups = binding.controlSchemes.ToList();
                    bindingGroups.Remove(controlSchemeName);
                    bindingGroups.Add(newName);
                    binding.wrappedProperty.FindPropertyRelative(nameof(InputBinding.m_Groups)).stringValue = bindingGroups.Join(InputBinding.kSeparatorString);
                }
            }
        }

        public static Command SelectControlScheme(int controlSchemeIndex)
        {
            return (in InputActionsEditorState state) =>
            {
                if (controlSchemeIndex == -1)
                    return state.With(selectedControlSchemeIndex: controlSchemeIndex, selectedControlScheme: new InputControlScheme());

                var controlSchemeSerializedProperty = state.serializedObject
                    .FindProperty(nameof(InputActionAsset.m_ControlSchemes))
                    .GetArrayElementAtIndex(controlSchemeIndex);

                return state.With(
                    selectedControlSchemeIndex: controlSchemeIndex,
                    selectedControlScheme: new InputControlScheme(controlSchemeSerializedProperty));
            };
        }

        public static Command ResetSelectedControlScheme()
        {
            return (in InputActionsEditorState state) =>
            {
                var controlSchemeSerializedProperty = state.serializedObject
                    .FindProperty(nameof(InputActionAsset.m_ControlSchemes))
                    .GetArrayElementAtIndex(state.selectedControlSchemeIndex);

                if (controlSchemeSerializedProperty == null)
                {
                    return state.With(
                        selectedControlSchemeIndex: -1,
                        selectedControlScheme: new InputControlScheme());
                }

                return state.With(
                    selectedControlScheme: new InputControlScheme(controlSchemeSerializedProperty));
            };
        }

        public static Command SelectDeviceRequirement(int deviceRequirementIndex)
        {
            return (in InputActionsEditorState state) => state.With(selectedDeviceRequirementIndex: deviceRequirementIndex);
        }

        /// <summary>
        /// Duplicate creates a new instance of the selected control scheme and places it in the selected
        /// control scheme property of the state but doesn't persist anything.
        /// </summary>
        public static Command DuplicateSelectedControlScheme()
        {
            return (in InputActionsEditorState state) => state.With(selectedControlScheme: new InputControlScheme(
                MakeUniqueControlSchemeName(state, state.selectedControlScheme.name),
                state.selectedControlScheme.deviceRequirements));
        }

        public static Command DeleteSelectedControlScheme()
        {
            return (in InputActionsEditorState state) =>
            {
                var selectedControlSchemeName = state.selectedControlScheme.name;

                var serializedArray = InputActionSerializationHelpers.GetControlSchemesArray(state.serializedObject);
                var indexOfArrayElement = InputActionSerializationHelpers.IndexOfControlScheme(serializedArray, selectedControlSchemeName);
                if (indexOfArrayElement < 0)
                    throw new InvalidOperationException("Control scheme doesn't exist in collection.");

                // Ask for confirmation.
                if (Dialog.Result.Cancel == Dialog.ControlScheme.ShowDeleteControlScheme(selectedControlSchemeName))
                    return state;

                serializedArray.DeleteArrayElementAtIndex(indexOfArrayElement);
                state.serializedObject.ApplyModifiedProperties();

                if (serializedArray.arraySize == 0)
                    return state.With(
                        selectedControlSchemeIndex: -1,
                        selectedControlScheme: new InputControlScheme(),
                        selectedDeviceRequirementIndex: -1);

                if (indexOfArrayElement > serializedArray.arraySize - 1)
                    return state.With(
                        selectedControlSchemeIndex: serializedArray.arraySize - 1,
                        selectedControlScheme: new InputControlScheme(serializedArray.GetArrayElementAtIndex(serializedArray.arraySize - 1)), selectedDeviceRequirementIndex: -1);

                return state.With(
                    selectedControlSchemeIndex: indexOfArrayElement,
                    selectedControlScheme: new InputControlScheme(serializedArray.GetArrayElementAtIndex(indexOfArrayElement)), selectedDeviceRequirementIndex: -1);
            };
        }

        internal static string MakeUniqueControlSchemeName(InputActionsEditorState state, string name)
        {
            var controlSchemes = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));

            IEnumerable<string> controlSchemeNames = Array.Empty<string>();
            if (controlSchemes != null)
                controlSchemeNames =
                    controlSchemes.Select(sp => sp.FindPropertyRelative(nameof(InputControlScheme.m_Name)).stringValue);

            return StringHelpers.MakeUniqueName(name, controlSchemeNames.Append(kAllControlSchemesName), x => x);
        }

        public static Command ChangeDeviceRequirement(int deviceRequirementIndex, bool isRequired)
        {
            return (in InputActionsEditorState state) =>
            {
                var deviceRequirements = state.selectedControlScheme.deviceRequirements.ToList();
                var requirement = deviceRequirements[deviceRequirementIndex];
                requirement.isOptional = !isRequired;
                deviceRequirements[deviceRequirementIndex] = requirement;

                return state.With(selectedControlScheme: new InputControlScheme(
                    state.selectedControlScheme.name,
                    deviceRequirements,
                    state.selectedControlScheme.bindingGroup));
            };
        }

        public static Command ReorderDeviceRequirements(int oldPosition, int newPosition)
        {
            return (in InputActionsEditorState state) =>
            {
                var deviceRequirements = state.selectedControlScheme.deviceRequirements.ToList();
                var requirement = deviceRequirements[oldPosition];
                deviceRequirements.RemoveAt(oldPosition);
                deviceRequirements.Insert(newPosition, requirement);

                return state.With(selectedControlScheme: new InputControlScheme(
                    state.selectedControlScheme.name,
                    deviceRequirements,
                    state.selectedControlScheme.bindingGroup));
            };
        }

        public static Command ChangeSelectedBindingsControlSchemes(string controlScheme, bool add)
        {
            return (in InputActionsEditorState state) =>
            {
                var actionMapSO = state.serializedObject
                    ?.FindProperty(nameof(InputActionAsset.m_ActionMaps))
                    ?.GetArrayElementAtIndex(state.selectedActionMapIndex);
                var serializedProperty = actionMapSO?.FindPropertyRelative(nameof(InputActionMap.m_Bindings))
                    ?.GetArrayElementAtIndex(state.selectedBindingIndex);

                var groupsProperty = serializedProperty.FindPropertyRelative(nameof(InputBinding.m_Groups));
                var groups = groupsProperty.stringValue;

                if (add)
                    groupsProperty.stringValue = groups
                        .Split(InputBinding.kSeparatorString)
                        .Append(controlScheme)
                        .Distinct()
                        .Join(InputBinding.kSeparatorString);
                else
                    groupsProperty.stringValue = groups
                        .Split(InputBinding.kSeparatorString)
                        .Where(s => s != controlScheme)
                        .Join(InputBinding.kSeparatorString);

                state.serializedObject.ApplyModifiedProperties();
                return state;
            };
        }
    }
}

#endif
