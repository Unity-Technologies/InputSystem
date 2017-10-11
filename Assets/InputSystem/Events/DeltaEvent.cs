using System.Runtime.InteropServices;

namespace ISX
{
    // Partial state update for an input device.
    // Avoids having to send a full state memory snapshot when only a small
    // part of the state has changed.
    public unsafe struct DeltaEvent : IInputEventTypeInfo
    {
        public const int Type = 0x444C5441;

        public InputEvent baseEvent;
        public FourCC stateType;
        public int stateOffset;
        public fixed byte stateData[1]; // Variable-sized.

        public FourCC GetTypeStatic()
        {
            return Type;
        }
    }
}
