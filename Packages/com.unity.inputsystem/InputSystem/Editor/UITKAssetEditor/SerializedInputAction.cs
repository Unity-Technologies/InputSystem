#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal readonly struct SerializedInputAction
    {
        public SerializedInputAction(SerializedProperty serializedProperty)
        {
            // TODO: check that the passed serialized property actually is an InputAction. Reflect over all
            // serialized fields and make sure they're present?
            wrappedProperty = serializedProperty ?? throw new ArgumentNullException(nameof(serializedProperty));

            id = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Id)).stringValue;
            name = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
            expectedControlType = ReadExpectedControlType(serializedProperty);
            type = (InputActionType)serializedProperty.FindPropertyRelative(nameof(InputAction.m_Type)).intValue;
            interactions = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Interactions)).stringValue;
            processors = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Processors)).stringValue;
            initialStateCheck = ReadInitialStateCheck(serializedProperty);
            actionTypeTooltip = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Type)).GetTooltip();
            expectedControlTypeTooltip = serializedProperty.FindPropertyRelative(nameof(InputAction.m_ExpectedControlType)).GetTooltip();
        }

        public string id { get; }
        public string name { get; }
        public string expectedControlType { get; }
        public InputActionType type { get; }
        public string interactions { get; }
        public string processors { get; }
        public bool initialStateCheck { get; }
        public string actionTypeTooltip { get; }
        public string expectedControlTypeTooltip { get; }
        public SerializedProperty wrappedProperty { get; }

        private static string ReadExpectedControlType(SerializedProperty serializedProperty)
        {
            var controlType = serializedProperty.FindPropertyRelative(nameof(InputAction.m_ExpectedControlType)).stringValue;
            if (!string.IsNullOrEmpty(controlType))
                return controlType;

            var actionType = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Type)).intValue;
            return actionType == (int)InputActionType.Button ? "Button" : null;
        }

        private static bool ReadInitialStateCheck(SerializedProperty serializedProperty)
        {
            var actionFlags = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Flags));
            return (actionFlags.intValue & (int)InputAction.ActionFlags.WantsInitialStateCheck) != 0;
        }

        public bool Equals(SerializedInputAction other)
        {
            return name == other.name
                && expectedControlType == other.expectedControlType
                && type == other.type
                && interactions == other.interactions
                && processors == other.processors
                && initialStateCheck == other.initialStateCheck
                && actionTypeTooltip == other.actionTypeTooltip
                && expectedControlTypeTooltip == other.expectedControlTypeTooltip
                && wrappedProperty.propertyPath == other.wrappedProperty.propertyPath;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializedInputAction other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(name);
            hashCode.Add(expectedControlType);
            hashCode.Add((int)type);
            hashCode.Add(interactions);
            hashCode.Add(processors);
            hashCode.Add(initialStateCheck);
            hashCode.Add(actionTypeTooltip);
            hashCode.Add(expectedControlTypeTooltip);
            hashCode.Add(wrappedProperty.propertyPath);
            return hashCode.ToHashCode();
        }
    }
}

#endif
