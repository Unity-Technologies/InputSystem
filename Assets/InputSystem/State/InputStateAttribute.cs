using System;

namespace ISX
{
    // Associates a state representation with an input device.
    [AttributeUsage(AttributeTargets.Class)]
    public class InputStateAttribute : Attribute
    {
        public Type inputStateType;
	    public Type outputStateType;

        public InputStateAttribute(Type inputStateType, Type outputStateType = null)
        {
            this.inputStateType = inputStateType;
	        this.outputStateType = outputStateType;
        }
    }
}