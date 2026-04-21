using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

////REVIEW: should this have optional data that identifies *what* has changed?

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Indicates that the configuration of a device has changed.
    /// </summary>
    /// <seealso cref="InputSystem.QueueConfigChangeEvent"/>
    /// <seealso cref="InputDevice.OnConfigurationChanged"/>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize)]
    public struct DeviceConfigurationEvent : IInputEventTypeInfo
    {
        /// <summary>The FourCC type identifier for device configuration events.</summary>
        public const int Type = 0x44434647;

        /// <summary>The base <see cref="InputEvent"/> header.</summary>
        [FieldOffset(0)]
        public InputEvent baseEvent;

        ////REVIEW: have some kind of payload that allows indicating what changed in the config?

        /// <summary>Static FourCC type code used to identify this event type.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Returns an <see cref="InputEventPtr"/> pointing to this event.</summary>
        public unsafe InputEventPtr ToEventPtr()
        {
            fixed(DeviceConfigurationEvent * ptr = &this)
            {
                return new InputEventPtr((InputEvent*)ptr);
            }
        }

        /// <summary>Creates a device configuration event for the given device ID and timestamp.</summary>
        public static DeviceConfigurationEvent Create(int deviceId, double time)
        {
            var inputEvent = new DeviceConfigurationEvent();
            inputEvent.baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize, deviceId, time);
            return inputEvent;
        }
    }
}
