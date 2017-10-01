using System;

namespace InputSystem
{
    // Mark a data member in a state struct as being an input control.
    // The system will scan the state for those and automatically construct InputControl instances
    // from them.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class InputControlAttribute : Attribute
    {
        public string type;
        public string name;
	    public string usage;
        public string options;
	    public string[] processors;
	    public string[] aliases;
        public int bit;
    }
}