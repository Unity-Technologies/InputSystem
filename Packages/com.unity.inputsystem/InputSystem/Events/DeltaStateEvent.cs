using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Partial state update for an input device.
    /// </summary>
    /// <remarks>
    /// Avoids having to send a full state memory snapshot when only a small
    /// part of the state has changed.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = InputEvent.kBaseEventSize + 9)]
    public unsafe struct DeltaStateEvent : IInputEventTypeInfo
    {
        /// <summary>The FourCC type identifier for delta state events.</summary>
        public const int Type = 0x444C5441; // 'DLTA'

        /// <summary>The base <see cref="InputEvent"/> header.</summary>
        [FieldOffset(0)]
        public InputEvent baseEvent;

        /// <summary>The FourCC format code of the state data in this event.</summary>
        [FieldOffset(InputEvent.kBaseEventSize)]
        public FourCC stateFormat;

        /// <summary>The byte offset within the device state that this delta applies to.</summary>
        [FieldOffset(InputEvent.kBaseEventSize + 4)]
        public uint stateOffset;

        [FieldOffset(InputEvent.kBaseEventSize + 8)]
        internal fixed byte stateData[1]; // Variable-sized.

        /// <summary>The size in bytes of the delta state payload.</summary>
        public uint deltaStateSizeInBytes => baseEvent.sizeInBytes - (InputEvent.kBaseEventSize + 8);

        /// <summary>Pointer to the first byte of the delta state data.</summary>
        public void* deltaState
        {
            get
            {
                fixed(byte* data = stateData)
                {
                    return data;
                }
            }
        }

        /// <summary>Static FourCC type code used to identify this event type.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Returns an <see cref="InputEventPtr"/> pointing to this event.</summary>
        public InputEventPtr ToEventPtr()
        {
            fixed(DeltaStateEvent * ptr = &this)
            {
                return new InputEventPtr((InputEvent*)ptr);
            }
        }

        /// <summary>Casts the given event pointer to a <see cref="DeltaStateEvent"/> pointer.</summary>
        public static DeltaStateEvent* From(InputEventPtr ptr)
        {
            if (!ptr.valid)
                throw new ArgumentNullException(nameof(ptr));
            if (!ptr.IsA<DeltaStateEvent>())
                throw new InvalidCastException($"Cannot cast event with type '{ptr.type}' into DeltaStateEvent");

            return FromUnchecked(ptr);
        }

        internal static DeltaStateEvent* FromUnchecked(InputEventPtr ptr)
        {
            return (DeltaStateEvent*)ptr.data;
        }

        /// <summary>Allocates and initializes a delta state event for the given control.</summary>
        public static NativeArray<byte> From(InputControl control, out InputEventPtr eventPtr,  Allocator allocator = Allocator.Temp)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            var device = control.device;
            if (!device.added)
                throw new ArgumentException($"Device for control '{control}' has not been added to system",
                    nameof(control));
            if (control.currentStatePtr == null) // Protects statePtr assignment below
                throw new ArgumentNullException($"Control '{control}' does not have an associated state");

            ref var deviceStateBlock = ref device.m_StateBlock;
            ref var controlStateBlock = ref control.m_StateBlock;

            var stateFormat = deviceStateBlock.format; // The event is sent against the *device* so that's the state format we use.
            var stateSize = 0u;
            if (controlStateBlock.bitOffset != 0)
                stateSize = (controlStateBlock.bitOffset + controlStateBlock.sizeInBits + 7) / 8;
            else
                stateSize = controlStateBlock.alignedSizeInBytes;
            var stateOffset = controlStateBlock.byteOffset;
            var statePtr = (byte*)control.currentStatePtr + (int)stateOffset;
            var eventSize = InputEvent.kBaseEventSize + sizeof(int) * 2 + stateSize;

            var buffer = new NativeArray<byte>((int)eventSize.AlignToMultipleOf(4), allocator);
            var stateEventPtr = (DeltaStateEvent*)buffer.GetUnsafePtr();

            stateEventPtr->baseEvent = new InputEvent(Type, (int)eventSize, device.deviceId, InputRuntime.s_Instance.currentTime);
            stateEventPtr->stateFormat = stateFormat;
            stateEventPtr->stateOffset = controlStateBlock.byteOffset - deviceStateBlock.byteOffset; // Make offset relative to device.
            UnsafeUtility.MemCpy(stateEventPtr->deltaState, statePtr, stateSize);

            eventPtr = stateEventPtr->ToEventPtr();
            return buffer;
        }
    }
}
