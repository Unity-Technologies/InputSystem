using System;

namespace UnityEngine.InputSystem.Experimental
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class InputSourceAttribute : System.Attribute
    {
        /// <summary>
        /// Optionally override the display name of the type.
        /// </summary>
        public string displayName { get; set; }
    }
}