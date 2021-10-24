using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Event that causes the state of an <see cref="InputDevice"/> to be reset (see <see cref="InputSystem.ResetDevice"/>).
    /// </summary>
    /// <seealso cref="InputSystem.ResetDevice"/>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize)]
    public struct DeviceResetEvent : IInputEventTypeInfo
    {
        public const int Type = 0x44525354; // DRST

        /// <summary>
        /// Common event data.
        /// </summary>
        [FieldOffset(0)]
        public InputEvent baseEvent;

        /// <summary>
        /// Whether to also reset <see cref="Layouts.InputControlAttribute.dontReset"/> controls.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool hardReset;

        public FourCC typeStatic => Type;

        public static DeviceResetEvent Create(int deviceId, bool hardReset = false, double time = -1)
        {
            var inputEvent =
                new DeviceResetEvent {baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize, deviceId, time)};
            inputEvent.hardReset = hardReset;
            return inputEvent;
        }
    }
}
