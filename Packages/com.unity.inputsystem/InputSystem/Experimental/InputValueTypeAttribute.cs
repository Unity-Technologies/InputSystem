using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Attributes a value-type as being supported as an input value-type in input bindings.
    /// </summary>
    /// <remarks>
    /// Only blittable value-types may be marked as input value types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct)]
    public class InputValueTypeAttribute : Attribute { }
}