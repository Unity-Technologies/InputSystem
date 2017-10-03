using System;
using UnityEngine;

namespace ISX
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
            return UnsafeUtility.SizeOf<StateEvent>();
        }

        // Pack the given state structure into a StateEvent.
        public static StateEvent Create<TState>(int deviceId, double time, TState state)
            where TState : struct, IInputStateTypeInfo
        {
            var inputEvent = new StateEvent
            {
                baseEvent = new InputEvent(Type, UnsafeUtility.SizeOf<StateEvent>(), deviceId, time),
                stateType = state.GetTypeStatic()
            };
            var src = UnsafeUtility.AddressOf(ref state);
            fixed(byte* dst = inputEvent.stateData)
            {
                UnsafeUtility.MemCpy(new IntPtr(dst), src, UnsafeUtility.SizeOf<TState>());
            }
            return inputEvent;
        }
    }
}
