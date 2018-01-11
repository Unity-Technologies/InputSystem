using System;
using ISX.Utilities;

namespace ISX
{
    /// <summary>
    /// Attribute to control template settings of a type used to generate an <see cref="InputTemplate"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class InputTemplateAttribute : Attribute
    {
        /// <summary>
        /// Associates a state representation with an input device and drives
        /// the control template generated for the device from its state rather
        /// than from the device class.
        /// </summary>
        /// <remarks>This is *only* useful if you have a state struct dictating a specific
        /// state layout and you want the device template to automatically take offsets from
        /// the fields annoated with <see cref="InputControlAttribute"/>.
        /// </remarks>
        public Type stateType;

        public FourCC stateFormat;

        public string[] commonUsages;
    }
}
