using System;
using System.Reflection;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Used to mark constructor arguments as being input ports which makes them detectable by reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Constructor | AttributeTargets.Field)]
    public class InputPortAttribute : System.Attribute
    {
        /// <summary>
        /// Optionally override the input port type name with a custom display name. Defaults to null in which
        /// case the parameter name will be used instead.
        /// </summary>
        public string name { get; set; }
    }

    public class InputNodeFactoryAttribute : System.Attribute
    {
        public Type type { get; set; }
    }
}