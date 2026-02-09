using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A Focus input event.
    /// </summary>
    /// <remarks>
    /// <see cref="InputFocusEvent"/> is sent when an application gains or loses focus.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 4)]
    internal unsafe struct InputFocusEvent : IInputEventTypeInfo
    {
        // Keep in sync with Input.cs in the input module
        public const int Type = 0x464f4355; //FOCU

        [FieldOffset(0)]
        public InputEvent baseEvent;

        /// <summary>
        /// Whether the application has gained (true) or lost (false) focus.
        /// </summary>
        [FieldOffset(InputEvent.kBaseEventSize)]
        public bool focus;
        
        public FourCC typeStatic => Type;

        public static InputFocusEvent Create(bool focus, double time = -1)
        {
            var inputEvent = new InputFocusEvent
            {
                baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize + 4, 0xfffff, time),
                focus = focus
            };
            return inputEvent;
        }

        public InputEventPtr ToEventPtr()
        {
            fixed (InputFocusEvent* ptr = &this)
            {
                return new InputEventPtr((InputEvent*)ptr);
            }
        }
    }
}
