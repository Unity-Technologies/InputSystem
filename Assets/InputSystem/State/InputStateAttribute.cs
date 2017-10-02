using System;

namespace ISX
{
    // Associates a state representation with an input device.
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