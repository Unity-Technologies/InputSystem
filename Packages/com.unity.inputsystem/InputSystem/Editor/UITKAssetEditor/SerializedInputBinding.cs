#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A read-only view of the data in a SerializedProperty representing an InputBinding.
    /// <remarks>
    /// After construction this class loses all connection to the original SerializedProperty. You cannot
    /// use it for edit operations.
    /// </remarks>
    /// </summary>
    internal readonly struct SerializedInputBinding
    {
        public SerializedInputBinding(SerializedProperty serializedProperty)
        {
            wrappedProperty = serializedProperty ?? throw new ArgumentNullException(nameof(serializedProperty));

            id = serializedProperty.FindPropertyRelative("m_Id").stringValue;
            name = serializedProperty.FindPropertyRelative("m_Name").stringValue;
            path = serializedProperty.FindPropertyRelative("m_Path").stringValue;
            interactions = serializedProperty.FindPropertyRelative("m_Interactions").stringValue;
            processors = serializedProperty.FindPropertyRelative("m_Processors").stringValue;
            action = serializedProperty.FindPropertyRelative("m_Action").stringValue;
            var bindingGroups = serializedProperty.FindPropertyRelative(nameof(InputBinding.m_Groups)).stringValue;
            controlSchemes = bindingGroups != null
                ? bindingGroups.Split(InputBinding.kSeparatorString, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();
            flags = (InputBinding.Flags)serializedProperty.FindPropertyRelative(nameof(InputBinding.m_Flags)).intValue;
            indexOfBinding = serializedProperty.GetIndexOfArrayElement();
            isComposite = (flags & InputBinding.Flags.Composite) == InputBinding.Flags.Composite;
            isPartOfComposite = (flags & InputBinding.Flags.PartOfComposite) == InputBinding.Flags.PartOfComposite;
            compositePath = string.Empty;

            if (isPartOfComposite)
                compositePath = GetCompositePath(serializedProperty);
        }

        public string name { get; }
        public string id { get; }
        public string path { get; }
        public string interactions { get; }
        public string processors { get; }
        public string action { get; }
        public string[] controlSchemes { get; }
        public InputBinding.Flags flags { get; }

        /// <summary>
        /// The index of this binding in the array that it is stored in.
        /// </summary>
        public int indexOfBinding { get; }
        public bool isComposite { get; }
        public bool isPartOfComposite { get; }

        /// <summary>
        /// Get the composite path of this input binding, which must itself be a composite part.
        /// </summary>
        /// <remarks>
        /// The composite path of a composite part is simply the path of the composite binding that the
        /// part belongs to.
        /// </remarks>
        public string compositePath { get; }

        public SerializedProperty wrappedProperty { get; }

        private static string GetCompositePath(SerializedProperty serializedProperty)
        {
            var bindingArrayProperty = serializedProperty.GetArrayPropertyFromElement();
            var partBindingIndex = InputActionSerializationHelpers.GetIndex(bindingArrayProperty, serializedProperty);
            var compositeStartIndex =
                InputActionSerializationHelpers.GetCompositeStartIndex(bindingArrayProperty, partBindingIndex);
            var compositeBindingProperty = bindingArrayProperty.GetArrayElementAtIndex(compositeStartIndex);
            return compositeBindingProperty.FindPropertyRelative("m_Path").stringValue;
        }

        public bool Equals(SerializedInputBinding other)
        {
            return name == other.name
                && path == other.path
                && interactions == other.interactions
                && processors == other.processors
                && action == other.action
                && flags == other.flags
                && indexOfBinding == other.indexOfBinding
                && isComposite == other.isComposite
                && isPartOfComposite == other.isPartOfComposite
                && compositePath == other.compositePath
                && controlSchemes.SequenceEqual(other.controlSchemes)
                && wrappedProperty.propertyPath == other.wrappedProperty.propertyPath;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializedInputBinding other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(name);
            hashCode.Add(path);
            hashCode.Add(interactions);
            hashCode.Add(processors);
            hashCode.Add(action);
            hashCode.Add((int)flags);
            hashCode.Add(indexOfBinding);
            hashCode.Add(isComposite);
            hashCode.Add(isPartOfComposite);
            hashCode.Add(compositePath);
            hashCode.Add(controlSchemes);
            hashCode.Add(wrappedProperty.propertyPath);
            return hashCode.ToHashCode();
        }
    }
}

#endif
