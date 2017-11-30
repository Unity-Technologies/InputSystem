using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace ISX
{
    // Full state update for an input device.
    // Variable-size event.
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = InputEvent.kBaseEventSize + 5)]
    public unsafe struct StateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x53544154; // 'STAT'

        [FieldOffset(0)] public InputEvent baseEvent;
        [FieldOffset(InputEvent.kBaseEventSize)] public FourCC stateFormat;
        [FieldOffset(InputEvent.kBaseEventSize + 4)] public fixed byte stateData[1]; // Variable-sized.

        public uint stateSizeInBytes
        {
            get { return (uint)(baseEvent.sizeInBytes - (InputEvent.kBaseEventSize + 4)); }
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

        public static int GetEventSizeWithPayload<TState>()
            where TState : struct
        {
            return UnsafeUtility.SizeOf<TState>() + 24;
        }

        public static StateEvent* From(InputEventPtr ptr)
        {
            if (!ptr.valid)
                throw new ArgumentNullException("ptr");
            if (!ptr.IsA<StateEvent>())
                throw new InvalidCastException(string.Format("Cannot cast event with type '{0}' into StateEvent",
                        ptr.type));

            return (StateEvent*)ptr.data;
        }
    }
}
