using System;

namespace UnityEngine.InputSystem.Experimental
{
    public class InputNodeFactoryAttribute : Attribute
    {
        public Type type { get; set; }
    }
}