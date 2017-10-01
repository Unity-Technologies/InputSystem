using System.Runtime.InteropServices;

namespace InputSystem.Events
{
    // Full state update for an input device.
    public unsafe struct StateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x42554C4B;

        public InputEvent baseEvent;
        public FourCC stateType;
        public fixed byte stateData[1]; // Variable-sized.

        public FourCC GetTypeStatic()
        {
            return Type;
        }
        public int GetSizeStatic()
        {
            return Marshal.SizeOf<StateEvent>();
        }
    }
}