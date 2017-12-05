using System.Runtime.InteropServices;
using ISX.Utilities;

namespace ISX.LowLevel
{
    /// <summary>
    /// Signals that a device got re-connected after a disconnect.
    /// </summary>
    /// <seealso cref="InputDeviceChange.Connected"/>
    /// <seealso cref="InputSystem.QueueConnectEvent"/>
    /// <seealso cref="InputDevice.connected"/>
    /// <seealso cref="InputSystem.onDeviceChange"/>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize)]
    public struct ConnectEvent : IInputEventTypeInfo
    {
        public const int Type = 0x44434F4E;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        /// <summary>
        /// Create a connection event for the given device at the given time.
        /// </summary>
        /// <param name="deviceId">ID of input device (<see cref="InputDevice.id"/>).</param>
        /// <param name="time">Time (in seconds) for event.</param>
        /// <returns>A device connect event.</returns>
        public static ConnectEvent Create(int deviceId, double time)
        {
            var inputEvent = new ConnectEvent();
            inputEvent.baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize, deviceId, time);
            return inputEvent;
        }
    }
}
