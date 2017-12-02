using System.Runtime.InteropServices;

namespace ISX.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 4)]
    public struct TextEvent : IInputEventTypeInfo
    {
        public const int Type = 0x54455854;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize)]
        public int character;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static TextEvent Create(int deviceId, char character, double time)
        {
            var inputEvent = new TextEvent();
            inputEvent.baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize + 4, deviceId, time);
            inputEvent.character = character;
            return inputEvent;
        }
    }
}
