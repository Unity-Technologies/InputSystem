using System;

namespace ISX
{
    // Mark a data member in a state struct as being an input control.
    // The system will scan the state for those and automatically construct InputControl instances
    // from them.
    // If applied to a field, set ups an actual instance of InputControl. If applied to a property,
    // modifies the InputControl instances *inside* the control the property references.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class InputControlAttribute : Attribute
    {
        public string template;
        public string name;
        public string usage;
        public string[] usages;
        public string parameters;
        public string[] processors;
        public string alias;
        public string[] aliases;
        public int bit;
        public uint offset = InputStateBlock.kInvalidOffset;
    }
}
