using System;

namespace ISX
{
    // Associates a state representation with an input device and drives
    // the control template generated for the device from its state rather
    // than from the device class. This is *only* useful if you have a state
    // struct dictating a specific state layout and you want the device template
    // to automatically take offsets from the fields annoated with
    // InputControlAttribute.
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class InputStateAttribute : Attribute
    {
        public Type type;

        public InputStateAttribute(Type type)
        {
            this.type = type;
        }
    }
}