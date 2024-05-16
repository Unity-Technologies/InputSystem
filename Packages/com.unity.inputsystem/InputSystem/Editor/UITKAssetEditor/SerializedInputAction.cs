#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal readonly struct SerializedInputAction
    {
        public SerializedInputAction(SerializedProperty serializedProperty)
        {
            wrappedProperty = serializedProperty ?? throw new ArgumentNullException(nameof(serializedProperty));

            Debug.Assert(serializedProperty.boxedValue is InputAction);

            id = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Id)).stringValue;
            name = serializedProperty.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
        }

        private SerializedProperty typeProperty => wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Type));
        private SerializedProperty expectedControlTypeProperty => wrappedProperty.FindPropertyRelative(nameof(InputAction.m_ExpectedControlType));
        private SerializedProperty flagsProperty => wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Flags));
        private SerializedProperty interactionsProperty =>
            wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Interactions));
        private SerializedProperty processorsProperty =>
            wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Processors));

        public string id { get; }
        public string name { get; }

        public string path
        {
            get
            {
                var map = actionMap;
                return map.HasValue ? map.Value.name + '/' + name : name;
            }
        }

        public string expectedControlType
        {
            get
            {
                var controlType = expectedControlTypeProperty.stringValue;
                if (!string.IsNullOrEmpty(controlType))
                    return controlType;

                var actionType = typeProperty.intValue;
                return actionType == (int)InputActionType.Button ? "Button" : string.Empty;
            }
            set => expectedControlTypeProperty.stringValue = value?.ToString();
        }
        public InputActionType type
        {
            get => (InputActionType)typeProperty.intValue;
            set => typeProperty.intValue = (int)value;
        }

        public string interactions
        {
            get => interactionsProperty.stringValue;
            set => interactionsProperty.stringValue = value;
        }

        public string processors
        {
            get => processorsProperty.stringValue;
            set => processorsProperty.stringValue = value;
        }

        public string propertyPath => wrappedProperty.propertyPath;
        public bool initialStateCheck
        {
            get => (flagsProperty.intValue & (int)InputAction.ActionFlags.WantsInitialStateCheck) != 0;
            set
            {
                var flags = flagsProperty.intValue;
                if (value)
                    flags |= (int)InputAction.ActionFlags.WantsInitialStateCheck;
                else
                    flags &= ~(int)InputAction.ActionFlags.WantsInitialStateCheck;
                flagsProperty.intValue = flags;
            }
        }

        public string actionTypeTooltip => typeProperty.GetTooltip();
        public string expectedControlTypeTooltip => expectedControlTypeProperty.GetTooltip();
        public SerializedProperty wrappedProperty { get; }

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
                && propertyPath == other.propertyPath;
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
            hashCode.Add(propertyPath);
            return hashCode.ToHashCode();
        }

        private SerializedProperty parentActionMapProperty =>
            wrappedProperty?.GetParentProperty()?.GetParentProperty()?.GetParentProperty();

        public SerializedInputActionMap? actionMap
        {
            get
            {
                var property = parentActionMapProperty;
                if (property == null)
                    return null;
                return new SerializedInputActionMap(property);
            }
        }

        public StringBuilder CopyToBuffer(StringBuilder buffer)
        {
            buffer ??= new StringBuilder();
            CopyPasteHelper.CopyItems(new List<SerializedProperty> { wrappedProperty }, buffer, typeof(InputAction),
                actionMap: parentActionMapProperty);
            return buffer;
        }

        public void Duplicate()
        {
            CopyPasteHelper.DuplicateAction(wrappedProperty.GetParentProperty(), wrappedProperty, parentActionMapProperty);
        }
    }
}

#endif
