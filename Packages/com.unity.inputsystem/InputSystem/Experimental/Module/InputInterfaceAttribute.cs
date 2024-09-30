using System;
using UnityEngine.InputSystem.Experimental;

namespace UnityEngine.InputSystem.Experimental
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class InputInterfaceAttribute : Attribute
    {
        public InputInterfaceAttribute(Usage usage)
        {
            this.usage = usage;
        }
        
        public Usage usage { get; }
    }
}