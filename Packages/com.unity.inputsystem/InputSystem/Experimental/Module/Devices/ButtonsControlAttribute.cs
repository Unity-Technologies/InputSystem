using System;

namespace UnityEngine.InputSystem.Experimental
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ButtonsControlAttribute : Attribute
    {
        public ButtonsControlAttribute(Type buttonEnumType = null)
        {
            if (buttonEnumType != null && !buttonEnumType.IsEnum)
                throw new ArgumentException($"{nameof(buttonEnumType)} must be an enum type.");
            
            this.buttonEnumType = buttonEnumType;
        }

        public Type buttonEnumType { get; }
    }
}