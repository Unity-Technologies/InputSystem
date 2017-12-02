using System.Runtime.InteropServices;

namespace ISX.LowLevel
{
    // Input device got re-connected after a disconnect.
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

        public static ConnectEvent Create(int deviceId, double time)
        {
            var inputEvent = new ConnectEvent();
            inputEvent.baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize, deviceId, time);
            return inputEvent;
        }
    }
}
