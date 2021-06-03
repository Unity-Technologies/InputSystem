using System;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Provide a format string to use when creating display strings for instances of the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class DisplayStringFormatAttribute : Attribute
    {
        /// <summary>
        /// Format template string in the form of "{namedPart} verbatimText". All named parts enclosed in
        /// curly braces are replaced from context whereas other text is included as is.
        /// </summary>
        public string formatString { get; set; }

        public DisplayStringFormatAttribute(string formatString)
        {
            this.formatString = formatString;
        }
    }
}
