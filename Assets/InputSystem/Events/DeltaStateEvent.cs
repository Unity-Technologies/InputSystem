using System;
using System.Runtime.InteropServices;
using ISX.Utilities;

////TODO: use deltastateevents as basis for ActionEvent

namespace ISX.LowLevel
{
    // Partial state update for an input device.
    // Avoids having to send a full state memory snapshot when only a small
    // part of the state has changed.
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = InputEvent.kBaseEventSize + 9)]
    public unsafe struct DeltaStateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x444C5441; // 'DLTA'

        [FieldOffset(0)] public InputEvent baseEvent;
        [FieldOffset(InputEvent.kBaseEventSize)] public FourCC stateFormat;
        [FieldOffset(InputEvent.kBaseEventSize + 4)] public uint stateOffset;
        [FieldOffset(InputEvent.kBaseEventSize + 8)] public fixed byte stateData[1]; // Variable-sized.

        public uint stateSizeInBytes
        {
            get { return (uint)(baseEvent.sizeInBytes - (InputEvent.kBaseEventSize + 8)); }
        }

        public IntPtr state
        {
            get
            {
                fixed(byte* data = stateData)
                {
                    return new IntPtr((void*)data);
                }
            }
        }

        public FourCC GetTypeStatic()
        {
            return Type;
        }
    }
}
