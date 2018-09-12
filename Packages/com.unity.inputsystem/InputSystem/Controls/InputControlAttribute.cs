using System;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Layouts
{
    /// <summary>
    /// Mark a data member in a state struct as being an input control.
    /// </summary>
    /// <remarks>
    /// The system will scan the state for those and automatically construct InputControl instances
    /// from them.
    ///
    /// If applied to a field, set ups an actual instance of InputControl. If applied to a property,
    /// modifies the InputControl instances *inside* the control the property references.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class InputControlAttribute : Attribute
    {
        public string layout;
        public string variants;
        public string name;
        public string format;
        public string usage;
        public string[] usages;
        public string parameters;
        public string processors;
        public string alias;
        public string[] aliases;
        public string useStateFrom;
        public uint bit = InputStateBlock.kInvalidOffset;
        public uint offset = InputStateBlock.kInvalidOffset;
        public uint sizeInBits;
        public int arraySize;
        public string displayName;
        public string imageName;
        public bool noisy;
        public object defaultState;
    }
}
