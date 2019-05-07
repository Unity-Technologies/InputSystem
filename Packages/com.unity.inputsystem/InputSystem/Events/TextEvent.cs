using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A single character text input event.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 4)]
    public struct TextEvent : IInputEventTypeInfo
    {
        public const int Type = 0x54455854;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        /// <summary>
        /// Character in UTF-32 encoding.
        /// </summary>
        [FieldOffset(InputEvent.kBaseEventSize)]
        public int character;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static unsafe TextEvent* From(InputEventPtr eventPtr)
        {
            if (!eventPtr.valid)
                throw new ArgumentNullException("ptr");
            if (!eventPtr.IsA<TextEvent>())
                throw new InvalidCastException(string.Format("Cannot cast event with type '{0}' into TextEvent",
                    eventPtr.type));

            return (TextEvent*)eventPtr.data;
        }

        public static TextEvent Create(int deviceId, char character, double time = -1)
        {
            ////TODO: detect and throw when if character is surrogate
            var inputEvent = new TextEvent
            {
                baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize + 4, deviceId, time),
                character = character
            };
            return inputEvent;
        }

        public static TextEvent Create(int deviceId, int character, double time = -1)
        {
            var inputEvent = new TextEvent
            {
                baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize + 4, deviceId, time),
                character = character
            };
            return inputEvent;
        }
    }
}
