using System;

namespace UnityEngine.InputSystem.Experimental
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class InputPresetAttribute : System.Attribute
    {
        public InputPresetAttribute(string category)
        {
            this.category = category;
        }

        public string category { get; }
    }
}