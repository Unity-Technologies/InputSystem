using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A single character text input event.
    /// </summary>
    /// <remarks>
    /// Text input does not fit the control-based input model well and thus is
    /// represented as its own form of input. A device that is capable of receiving
    /// text input (such as <see cref="Keyboard"/>) receives text input events
    /// and should implement <see cref="ITextInputReceiver"/> in order for the
    /// input system to be able to relay these events to the device.
    /// </remarks>
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

        public FourCC typeStatic => Type;

        public static unsafe TextEvent* From(InputEventPtr eventPtr)
        {
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));
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
