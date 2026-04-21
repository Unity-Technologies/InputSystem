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
        /// <summary>The FourCC type identifier for text input events.</summary>
        public const int Type = 0x54455854;

        /// <summary>The base <see cref="InputEvent"/> header.</summary>
        [FieldOffset(0)]
        public InputEvent baseEvent;

        /// <summary>
        /// Character in UTF-32 encoding.
        /// </summary>
        [FieldOffset(InputEvent.kBaseEventSize)]
        public int character;

        /// <summary>Static FourCC type code used to identify this event type.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Casts the given event pointer to a <see cref="TextEvent"/> pointer.</summary>
        public static unsafe TextEvent* From(InputEventPtr eventPtr)
        {
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));
            if (!eventPtr.IsA<TextEvent>())
                throw new InvalidCastException(string.Format("Cannot cast event with type '{0}' into TextEvent",
                    eventPtr.type));

            return (TextEvent*)eventPtr.data;
        }

        /// <summary>Creates a text event for the given device ID, character, and timestamp.</summary>
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

        /// <summary>Creates a text event for the given device ID, Unicode code point, and timestamp.</summary>
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
