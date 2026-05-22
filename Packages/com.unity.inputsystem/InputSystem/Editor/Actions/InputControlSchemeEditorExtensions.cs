#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Editor extensions for InputControlScheme to handle SerializedProperty construction.
    /// </summary>
    internal static class InputControlSchemeEditorExtensions
    {
        /// <summary>
        /// Creates an InputControlScheme from a SerializedProperty.
        /// </summary>
        public static InputControlScheme FromSerializedProperty(SerializedProperty sp)
        {
            var requirements = new List<InputControlScheme.DeviceRequirement>();
            var deviceRequirementsArray = sp.FindPropertyRelative("m_DeviceRequirements");
            if (deviceRequirementsArray == null)
                throw new System.ArgumentException("The serialized property does not contain an InputControlScheme object.");

            foreach (SerializedProperty deviceRequirement in deviceRequirementsArray)
            {
                requirements.Add(new InputControlScheme.DeviceRequirement
                {
                    controlPath = deviceRequirement.FindPropertyRelative("m_ControlPath").stringValue,
                    m_Flags = (InputControlScheme.DeviceRequirement.Flags)deviceRequirement.FindPropertyRelative("m_Flags").enumValueFlag
                });
            }

            var scheme = new InputControlScheme();
            scheme.m_Name = sp.FindPropertyRelative("m_Name").stringValue;
            scheme.m_DeviceRequirements = requirements.ToArray();
            scheme.m_BindingGroup = sp.FindPropertyRelative("m_BindingGroup").stringValue;
            return scheme;
        }
    }
}
#endif
