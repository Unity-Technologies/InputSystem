using System;

////REVIEW: should this *not* be inherited? inheritance can lead to surprises

namespace UnityEngine.Experimental.Input.Layouts
{
    /// <summary>
    /// Attribute to control layout settings of a type used to generate an <see cref="InputControlLayout"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class InputControlLayoutAttribute : Attribute
    {
        /// <summary>
        /// Associates a state representation with an input device and drives
        /// the control layout generated for the device from its state rather
        /// than from the device class.
        /// </summary>
        /// <remarks>This is *only* useful if you have a state struct dictating a specific
        /// state layout and you want the device layout to automatically take offsets from
        /// the fields annoated with <see cref="InputControlAttribute"/>.
        /// </remarks>
        public Type stateType;

        public string stateFormat;

        public string[] commonUsages;

        public string variants;
    }
}
