using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Attribute used to decorate a class to be included as an input source in editors and other parts where
    /// a meaningful display name is required.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class InputSourceAttribute : System.Attribute
    {
        /// <summary>
        /// Optionally override the display name of the type.
        /// </summary>
        public string displayName { get; set; }
    }
}