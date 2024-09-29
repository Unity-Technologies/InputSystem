using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Attributes a value-type as being supported as an input value-type in input bindings.
    /// </summary>
    /// <remarks>
    /// Only blittable value-types may be marked as input value types.
    /// </remarks>
    [AttributeUsage(validOn: AttributeTargets.Struct)]
    public class InputValueTypeAttribute : Attribute
    {
        /// <summary>
        /// Creates an attribute that marks a type as being usable with the Input System as a value type.
        /// </summary>
        public InputValueTypeAttribute() { }
    }
    
    /// <summary>
    /// Attributes a value-type as being supported as an input value-type in input bindings.
    /// </summary>
    /// <remarks>
    /// Only blittable value-types may be marked as input value types.
    /// 
    /// The following types are supported by default and hence do not need to be attributed with
    /// <see cref="InputValueTypeAttribute"/>.
    /// </remarks>
    [AttributeUsage(validOn: AttributeTargets.Struct, AllowMultiple = true)]
    public class InputValueTypeReferenceAttribute : Attribute
    {
        /// <summary>
        /// Creates an attribute that marks a type as being usable with the Input System as a value type.
        /// </summary>
        /// <param name="type">Option type reference to a type that cannot be directly annotated.</param>
        public InputValueTypeReferenceAttribute(Type type)
        {
            this.type = type;
        }

        /// <summary>
        /// Returns a referenced type, not that this may be <c>null</c> in which case the associated type marked
        /// as a input value-type should be considered to be the type that has been attributed.
        /// </summary>
        public Type type { get; }
    }
}