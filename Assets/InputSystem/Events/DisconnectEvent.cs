using System.Runtime.InteropServices;

namespace ISX
{
    // Input device got disconnected.
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct DisconnectEvent : IInputEventTypeInfo
    {
        public const int Type = 0x44444953;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static DisconnectEvent Create(int deviceId, double time)
        {
            var inputEvent = new DisconnectEvent();
            inputEvent.baseEvent = new InputEvent(Type, 20, deviceId, time);
            return inputEvent;
        }
    }
}
