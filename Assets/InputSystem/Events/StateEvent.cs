using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // Full state update for an input device.
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct StateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x53544154;

        [FieldOffset(0)]
        public InputEvent baseEvent;
        [FieldOffset(20)]
        public FourCC stateType;
        [FieldOffset(24)]
        public fixed byte stateData[1]; // Variable-sized.

        public int stateSizeInBytes
        {
            get { return baseEvent.sizeInBytes - UnsafeUtility.SizeOf<InputEvent>() - UnsafeUtility.SizeOf<FourCC>(); }
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

        public int GetSizeStatic()
        {
            return UnsafeUtility.SizeOf<StateEvent>();
        }

        // Convenience method that infers the TState type argument from the given value.
        public static StateEvent<TState> Create<TState>(int deviceId, double time, TState state)
            where TState : struct, IInputStateTypeInfo
        {
            return StateEvent<TState>.Create(deviceId, time, state);
        }
    }

    // Convenience wrapper to create state events from state structures.
    // Representationally, StateEvent and StateEvent<TState> are identical.
    [StructLayout(LayoutKind.Sequential)]
    public struct StateEvent<TState> : IInputEventTypeInfo
        where TState : struct, IInputStateTypeInfo
    {
        public const int Type = 0x42554C4B;

        public InputEvent baseEvent;
        public FourCC stateType;
        public TState state;


        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public int GetSizeStatic()
        {
            return UnsafeUtility.SizeOf<StateEvent<TState>>();
        }

        // Pack the given state structure into a StateEvent.
        public static StateEvent<TState> Create(int deviceId, double time, TState state)
        {
            return new StateEvent<TState>
            {
                baseEvent = new InputEvent(Type, UnsafeUtility.SizeOf<StateEvent<TState>>(), deviceId, time),
                stateType = state.GetTypeStatic(),
                state = state
            };
        }
    }
}
