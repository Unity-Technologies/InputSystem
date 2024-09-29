using System;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ControlAttribute : Attribute
    {
        public ControlAttribute(bool relative = false)
        {
            
        }
    }
}