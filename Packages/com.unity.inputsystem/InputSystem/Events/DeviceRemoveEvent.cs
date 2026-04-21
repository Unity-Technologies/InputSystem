using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Notifies about the removal of an input device.
    /// </summary>
    /// <remarks>
    /// Device that got removed is the one identified by <see cref="InputEvent.deviceId"/>
    /// of <see cref="baseEvent"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize)]
    public struct DeviceRemoveEvent : IInputEventTypeInfo
    {
        /// <summary>The FourCC type identifier for device remove events.</summary>
        public const int Type = 0x4452454D;

        /// <summary>
        /// Common event data.
        /// </summary>
        [FieldOffset(0)]
        public InputEvent baseEvent;

        /// <summary>Static FourCC type code used to identify this event type.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Returns an <see cref="InputEventPtr"/> pointing to this event.</summary>
        public unsafe InputEventPtr ToEventPtr()
        {
            fixed(DeviceRemoveEvent * ptr = &this)
            {
                return new InputEventPtr((InputEvent*)ptr);
            }
        }

        /// <summary>Creates a device remove event for the given device ID and timestamp.</summary>
        public static DeviceRemoveEvent Create(int deviceId, double time = -1)
        {
            var inputEvent =
                new DeviceRemoveEvent {baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize, deviceId, time)};
            return inputEvent;
        }
    }
}
