using System;

namespace UnityEngine.InputSystem.Experimental
{
    public enum ValueFlags
    {
        Default = 0,
        Relative = 1,
        Normalized = 2,
        // TODO Check HID spec for actuation state, e.g. whether a button is a spring button or switch
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ControlAttribute : Attribute
    {
        private readonly ValueFlags m_Flags;
        
        public ControlAttribute(ValueFlags flags = ValueFlags.Default)
        {
            m_Flags = flags;
        }

        /// <summary>
        /// Returns <c>true</c> if the control is relative, else false.
        /// </summary>
        public bool isRelative => 0 != (m_Flags & ValueFlags.Relative);
        
        /// <summary>
        /// Returns <c>true</c> if the control is normalized, else false.
        /// </summary>
        public bool isNormalized => 0 != (m_Flags & ValueFlags.Normalized);
    }
}