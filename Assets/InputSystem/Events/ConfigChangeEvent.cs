using System.Runtime.InteropServices;

namespace ISX.LowLevel
{
    /// <summary>
    /// Indicates that the configuration of a device has changed.
    /// </summary>
    /// <seealso cref="InputSystem.QueueConfigChangeEvent"/>
    /// <seealso cref="InputDevice.OnConfigChange"/>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize)]
    public struct ConfigChangeEvent : IInputEventTypeInfo
    {
        public const int Type = 0x44434647;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        ////REVIEW: have some kind of payload that allows indicating what changed in the config?

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static ConfigChangeEvent Create(int deviceId, double time)
        {
            var inputEvent = new ConfigChangeEvent();
            inputEvent.baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize, deviceId, time);
            return inputEvent;
        }
    }
}
