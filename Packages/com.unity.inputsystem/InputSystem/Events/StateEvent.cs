using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A complete state snapshot for an entire input device.
    /// </summary>
    /// <remarks>
    /// This is a variable-sized event.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 5)]
    public unsafe struct StateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x53544154; // 'STAT'

        [FieldOffset(0)] public InputEvent baseEvent;

        /// <summary>
        /// Type code for the state stored in the event.
        /// </summary>
        [FieldOffset(InputEvent.kBaseEventSize)] public FourCC stateFormat;

        [FieldOffset(InputEvent.kBaseEventSize + sizeof(int))] public fixed byte stateData[1]; // Variable-sized.

        public uint stateSizeInBytes
        {
            get { return baseEvent.sizeInBytes - (InputEvent.kBaseEventSize + sizeof(int)); }
        }

        public IntPtr state
        {
            get
            {
                fixed(byte* data = stateData)
                {
                    return new IntPtr(data);
                }
            }
        }

        public InputEventPtr ToEventPtr()
        {
            fixed(StateEvent * ptr = &this)
            {
                return new InputEventPtr((InputEvent*)ptr);
            }
        }

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static int GetEventSizeWithPayload<TState>()
            where TState : struct
        {
            return UnsafeUtility.SizeOf<TState>() + InputEvent.kBaseEventSize + sizeof(int);
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

        /// <summary>
        /// Read the current state of <paramref name="device"/> and create a state event from it.
        /// </summary>
        /// <param name="device">Device to grab the state from. Must be a device that has been added to the system.</param>
        /// <param name="eventPtr">Receives a pointer to the newly created state event.</param>
        /// <param name="allocator">Which native allocator to allocate memory for the event from. By default, the buffer is
        /// allocated as temporary memory (<see cref="Allocator.Temp"/>. Note that this means the buffer will not be valid
        /// past the current frame. Use <see cref="Allocator.Persistent"/> if the buffer for the state event is meant to
        /// persist for longer.</param>
        /// <returns>Buffer of unmanaged memory allocated for the event.</returns>
        /// <exception cref="ArgumentException"><paramref name="device"/> has not been added to the system.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        public static NativeArray<byte> From(InputDevice device, out InputEventPtr eventPtr,  Allocator allocator = Allocator.Temp)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (!device.added)
                throw new ArgumentException(string.Format("Device '{0}' has not been added to system", device),
                    "device");

            var stateFormat = device.m_StateBlock.format;
            var stateSize = device.m_StateBlock.alignedSizeInBytes;
            var stateOffset = device.m_StateBlock.byteOffset;
            var statePtr = (byte*)device.currentStatePtr.ToPointer() + (int)stateOffset;
            var eventSize = InputEvent.kBaseEventSize + sizeof(int) + stateSize;

            var buffer = new NativeArray<byte>((int)eventSize, allocator);
            var stateEventPtr = (StateEvent*)buffer.GetUnsafePtr();

            stateEventPtr->baseEvent = new InputEvent(Type, (int)eventSize, device.id, InputRuntime.s_Instance.currentTime);
            stateEventPtr->stateFormat = stateFormat;
            UnsafeUtility.MemCpy(stateEventPtr->state.ToPointer(), statePtr, stateSize);

            eventPtr = stateEventPtr->ToEventPtr();
            return buffer;
        }
    }
}
