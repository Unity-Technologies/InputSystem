using System;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Should we include source awareness into attribute or only detect via analyzer?
    
    [AttributeUsage(AttributeTargets.Method)]
    public class InputOperationAttribute : Attribute
    {
        public InputOperationAttribute(string name = null)
        {
            this.name = name;
        }
        
        public string name { get; }
    }
}