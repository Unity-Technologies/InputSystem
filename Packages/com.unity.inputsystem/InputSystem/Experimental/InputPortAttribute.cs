using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Used to mark method or constructor arguments as being input ports which makes them detectable by reflection
    /// and indicates that they are backed by serialized fields of the node struct.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Constructor | AttributeTargets.Field)]
    public class InputPortAttribute : Attribute
    {
        /// <summary>
        /// Optionally override the input port type name with a custom display name. Defaults to null in which
        /// case the parameter name will be used instead.
        /// </summary>
        public string name { get; set; }
    }
}