using System.Runtime.InteropServices;

namespace ISX
{
    // Input device got re-connected after a disconnect.
    [StructLayout(LayoutKind.Sequential)]
    public struct ConnectEvent : IInputEventTypeInfo
    {
        public const int Type = 0x44434F4E;

        public InputEvent baseEvent;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public int GetSizeStatic()
        {
            return Marshal.SizeOf<ConnectEvent>();
        }
    }
}
