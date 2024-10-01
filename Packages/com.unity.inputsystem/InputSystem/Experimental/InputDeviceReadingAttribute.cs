using System;

namespace UnityEngine.InputSystem.Experimental
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class InputDeviceReadingAttribute : Attribute
    {
        public InputDeviceReadingAttribute(string deviceClassName)
        {
            if (deviceClassName == null)
                throw new ArgumentNullException(nameof(deviceClassName));
            if (deviceClassName.Length == 0)
                throw new ArgumentException(nameof(deviceClassName));
            
            this.deviceClassName = deviceClassName;
        }
        
        public string deviceClassName { get; }
    }
}