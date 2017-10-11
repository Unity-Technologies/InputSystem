using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // Full state update for an input device.
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 25)]
    public unsafe struct StateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x53544154;

        [FieldOffset(0)]
        public InputEvent baseEvent;
        [FieldOffset(20)]
        public FourCC stateFormat;
        [FieldOffset(24)]
        public fixed byte stateData[1]; // Variable-sized.

        public int stateSizeInBytes => baseEvent.sizeInBytes - 24;

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
                throw new ArgumentNullException(nameof(ptr));
            if (!ptr.IsA<StateEvent>())
                throw new InvalidCastException($"Cannot cast event with type '{ptr.type}' into StateEvent");

            return (StateEvent*)ptr.data;
        }
    }
}
