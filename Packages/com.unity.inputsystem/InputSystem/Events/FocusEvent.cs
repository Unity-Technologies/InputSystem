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
    internal struct InputFocusEvent : IInputEventTypeInfo
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

        public static unsafe InputFocusEvent* From(InputEventPtr eventPtr)
        {
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));
            if (!eventPtr.IsA<InputFocusEvent>())
                throw new InvalidCastException(string.Format("Cannot cast event with type '{0}' into FocusEvent",
                    eventPtr.type));

            return (InputFocusEvent*)eventPtr.data;
        }
    }
}
